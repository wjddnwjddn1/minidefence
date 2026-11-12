using System;
using System.Collections.Generic;
using UnityEngine;
namespace EmberBastion {
 public sealed partial class BattleView {
  int menuSlot=-1,menuPreview=-1,menuDown=-1,menuTouchPreview=-1;
  bool directPlacement,menuTouch;
  Vector2 menuDownPoint;
  Battle menuBattle;
  Unit menuUnit;
  string menuLogged="";
  readonly Dictionary<string,Texture2D> blackSheets=new Dictionary<string,Texture2D>();
  float menuStunAt=-1,menuReloadAt=-1;string fixtureViewport="";
  int FixtureInt(string key,int fallback){foreach(string item in Application.absoluteURL.Split('?','&')){string[] pair=item.Split('=');int value;if(pair.Length==2&&pair[0]==key&&int.TryParse(pair[1],out value))return value;}return fallback;}
  void InitSlotMenuFixture(){if(!Debug.isDebugBuild||Application.platform!=RuntimePlatform.WebGLPlayer||!Application.absoluteURL.Contains("fx-smoke=slotmenu"))return;battle=Battle.CreateForRules(29,2,4);battle.Gold=1000;int kind=FixtureInt("kind",-1),slot=FixtureInt("slot",9),level=FixtureInt("level",1);if(kind>=0&&kind<5&&slot>=0&&slot<10){battle.Place(kind,slot);if(level>=2)battle.Upgrade(slot);if(level>=3)battle.UpgradeBranch(slot,2);}battle.Gold=Math.Max(0,FixtureInt("gold",battle.Gold));BindBattle();tutorialStage=0;if(FixtureInt("fighting",0)==1){battle.Start();paused=FixtureInt("pause",0)==1;}Debug.Log("SLOT_FIXTURE_READY");}
  struct SlotAction {public int Id;public Rect Bounds;public SlotAction(int id,Rect bounds){Id=id;Bounds=bounds;}}
  void CloseSlotMenu(){if(menuSlot>=0&&Debug.isDebugBuild&&Application.platform==RuntimePlatform.WebGLPlayer)Debug.Log("SLOT_CLOSED "+menuSlot);menuSlot=-1;menuPreview=-1;menuDown=-1;menuTouchPreview=-1;menuUnit=null;menuBattle=null;menuLogged="";pressed=false;}
  void OpenSlotMenu(int slot){CloseSlotMenu();selected=slot;menuSlot=slot;menuBattle=battle;menuUnit=battle.At(slot);pendingTouchSlot=-1;hoverSlot=-1;menuStunAt=Debug.isDebugBuild&&Application.platform==RuntimePlatform.WebGLPlayer&&Application.absoluteURL.Contains("fx-smoke=slotmenu")&&FixtureInt("stun",0)==1?Time.unscaledTime+1:-1;menuReloadAt=Debug.isDebugBuild&&Application.platform==RuntimePlatform.WebGLPlayer&&Application.absoluteURL.Contains("fx-smoke=slotmenu")&&FixtureInt("reload",0)==1?Time.unscaledTime+1:-1;}
  void ValidateSlotMenu(){if(Debug.isDebugBuild&&Application.platform==RuntimePlatform.WebGLPlayer&&Application.absoluteURL.Contains("review-flow=1")){string size=Screen.width+"x"+Screen.height;if(fixtureViewport!=size){fixtureViewport=size;Debug.Log("SLOT_VIEW|"+scale.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+"|"+ox.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+"|"+oy.ToString("R",System.Globalization.CultureInfo.InvariantCulture));}}if(menuSlot<0)return;if(menuReloadAt>0&&Time.unscaledTime>=menuReloadAt){menuReloadAt=-1;SaveCurrent();LoadPrep();return;}if(menuStunAt>0&&Time.unscaledTime>=menuStunAt&&menuUnit!=null){menuUnit.Stun=1;menuStunAt=-1;}if(WindowTooSmall()||menuBattle!=battle||selected!=menuSlot||menuSlot>=battle.Field.Slots.Length||battle.At(menuSlot)!=menuUnit||menuUnit!=null&&menuUnit.Stun>0||titleMenu||toolsMenu||fieldMenu||guideMenu||branchMenu||aiming||battle.ReinforcementPending||battle.Phase!=Phase.Preparation&&battle.Phase!=Phase.Fighting)CloseSlotMenu();}
  // Claude r29: ring menu around the slot instead of a 360x264 plate. Build: five round troop buttons on a
  // circle. Unit: upgrade/branch on the left and right of the unit, sale always below, target above. Sizes follow the
  // screen (MenuUnit). Press-drag-release on a button confirms directly; a plain tap keeps the touch preview.
  bool menuSlid;Texture2D disc;
  float MenuUnit(){return Mathf.Clamp(Mathf.Min(Screen.width/1280f,Screen.height/720f),.8f,1.6f);}
  float MenuRadius(){return (menuUnit==null?86:82)*MenuUnit();}
  float MenuButton(){return (menuUnit==null?64:68)*MenuUnit();}
  Vector2 MenuCenter(){Point p=battle.Field.Slots[menuSlot];float pad=MenuRadius()+MenuButton()/2+20*MenuUnit();float x=Mathf.Clamp(ox+(float)p.X*scale,pad,Mathf.Max(pad,Screen.width-pad)),y=oy+(float)p.Y*scale;bool stacked=MenuStacked(x);return new Vector2(x,stacked?8+MenuRadius()+MenuButton()/2:Mathf.Clamp(y,Mathf.Max(pad,HeaderHeight+MenuRadius()+MenuButton()/2+8),Mathf.Max(HeaderHeight+MenuRadius()+MenuButton()/2+8,Screen.height-FooterHeight-MenuRadius()-MenuButton()/2-8)));}
  // Claude r35: pin the ring to the top only when the window is low AND the info card fits on neither side (320x300);
  // landscape phones (e.g. 844x390) keep the ring on its slot with the card beside it.
  bool MenuStacked(float cx){if(Screen.height>=400)return false;float u=MenuUnit(),w=Mathf.Min(270*u,Screen.width-16),reach=MenuRadius()+MenuButton()/2+10*u;return cx+reach+w>Screen.width-8&&cx-reach-w<8;}
  static Rect RingRect(Vector2 c,float radius,float size,float degrees){double a=degrees*Math.PI/180;return new Rect(c.x+(float)Math.Cos(a)*radius-size/2,c.y+(float)Math.Sin(a)*radius-size/2,size,size);}
  List<SlotAction> SlotActions(){var actions=new List<SlotAction>();Vector2 c=MenuCenter();float r=MenuRadius(),d=MenuButton(),u=MenuUnit();
   if(menuUnit==null){for(int k=0;k<5;k++)actions.Add(new SlotAction(k,RingRect(c,r,d,-90+72*k)));}
   else if(menuUnit.Level==1){actions.Add(new SlotAction(10,RingRect(c,r,d,180)));actions.Add(new SlotAction(13,RingRect(c,r,d,90)));}
   else if(menuUnit.Level==2){actions.Add(new SlotAction(11,RingRect(c,r,d,180)));actions.Add(new SlotAction(12,RingRect(c,r,d,0)));actions.Add(new SlotAction(13,RingRect(c,r,d,90)));}
   else{if(battle.SkillsActive&&menuUnit.Mastery==0)actions.Add(new SlotAction(16,RingRect(c,r,d,180)));actions.Add(new SlotAction(13,RingRect(c,r,d,90)));}
   if(menuUnit!=null&&(menuUnit.Kind==1||menuUnit.Kind==3))actions.Add(new SlotAction(14,RingRect(c,r,d,-90)));
   if(menuUnit==null)actions.Add(new SlotAction(15,new Rect(c.x-Mathf.Max(44,34*u)/2,c.y-Mathf.Max(44,34*u)/2,Mathf.Max(44,34*u),Mathf.Max(44,34*u))));else actions.Add(new SlotAction(15,RingRect(c,r,Mathf.Max(44,30*u),-45)));return actions;}
  Rect MenuInfoRect(){Vector2 c=MenuCenter();float u=MenuUnit(),w=Mathf.Min(270*u,Screen.width-16),h=104*u,reach=MenuRadius()+MenuButton()/2+10*u;if(MenuStacked(c.x))return new Rect((Screen.width-w)/2,Screen.height-h-8,w,h);float x=c.x+reach;if(x+w>Screen.width-8)x=c.x-reach-w;float y=c.y-h/2;if(x<8){x=Mathf.Clamp(c.x-w/2,8,Screen.width-w-8);y=c.y+reach+h+8>Screen.height?c.y-reach-h:c.y+reach;}return new Rect(x,Mathf.Clamp(y,HeaderHeight+4,Mathf.Max(HeaderHeight+4,Screen.height-FooterHeight-h-4)),w,h);}
  bool MenuContains(Vector2 p){return Vector2.Distance(p,MenuCenter())<=MenuRadius()+MenuButton()/2+6*MenuUnit()||MenuInfoRect().Contains(p);}
  static int MenuHit(List<SlotAction> actions,Vector2 p){int hit=-1;foreach(var a in actions)if(Vector2.Distance(a.Bounds.center,p)<=a.Bounds.width/2)hit=a.Id;return hit;}
  // Open on press so the same pointer can continue into an upgrade without a second click.
  bool BeginUnitMenuDrag(Event e){
   if(e.type!=EventType.MouseDown||e.button!=0||aiming||battle.Phase!=Phase.Preparation&&battle.Phase!=Phase.Fighting)return false;
   Point p=Map(e.mousePosition);if(!OnMap(p))return false;int slot=-1;double best=Screen.width<700?Math.Max(36,20/scale):36;
   foreach(var unit in battle.Units){double distance=Point.Distance(p,battle.Field.Slots[unit.Slot]);if(distance<best){best=distance;slot=unit.Slot;}}
   if(slot<0)return false;
   if(battle.Phase==Phase.Fighting&&best>24&&battle.Enemies.Exists(enemy=>Point.Distance(p,enemy.Position)<best))return false;
   OpenSlotMenu(slot);menuDownPoint=e.mousePosition;menuDown=-1;menuSlid=false;menuTouch=Input.touchCount>0;GUIUtility.hotControl=0;notice=UnitNotice(menuUnit);e.Use();return true;
  }
  static Rect FitMenuPortrait(Rect area){float width=Mathf.Min(area.width,area.height*100f/120f),height=width*120f/100f;return new Rect(area.center.x-width/2,area.center.y-height/2,width,height);}
  bool HandleSlotMenuInput(Event e){ValidateSlotMenu();if(menuSlot<0)return false;
   if(e.type==EventType.KeyDown){if(e.keyCode==KeyCode.Escape){CloseSlotMenu();held.Clear();e.Use();return true;}CloseSlotMenu();return false;}
   bool pointer=e.type==EventType.MouseDown||e.type==EventType.MouseUp||e.type==EventType.MouseDrag||e.type==EventType.ScrollWheel;if(!pointer)return false;
   int hit=MenuHit(SlotActions(),e.mousePosition);
   if(e.type==EventType.MouseDown&&e.button==0){if(!MenuContains(e.mousePosition)){CloseSlotMenu();return false;}menuDown=hit;menuSlid=false;menuTouch=Input.touchCount>0;menuDownPoint=e.mousePosition;if(hit>=0)menuPreview=hit;e.Use();return true;}
   if(e.type==EventType.MouseDrag){if(Vector2.Distance(menuDownPoint,e.mousePosition)>12*MenuUnit())menuSlid=true;if(hit>=0)menuPreview=hit;e.Use();return true;}
   if(e.type==EventType.MouseUp&&e.button==0){int down=menuDown;bool slid=menuSlid;menuDown=-1;menuSlid=false;if(hit>=0&&slid)SelectSlotAction(hit,false);else if(hit>=0&&hit==down)SelectSlotAction(hit,menuTouch);e.Use();return true;}
   if(e.type==EventType.ScrollWheel){e.Use();return true;}return false;
  }
  bool SlotActionEnabled(int id){return id<5?battle.Gold>=Balance.Troops[id].Cost:id==10?battle.Gold>=50:id==11||id==12?battle.Gold>=90:id==16?battle.Gold>=Boon.MasteryCost:true;}
  string SlotActionLabel(int id){if(id==15)return "닫기";if(id<5)return Balance.Troops[id].Name+" "+Balance.Troops[id].Cost+"G";if(id==10)return "강화 50G";if(id==11||id==12)return BranchTitle(menuUnit.Kind,id-10)+" · 90G";if(id==13)return SaleLabel(menuUnit);if(id==16)return "숙련 "+Boon.MasteryCost+"G";return "목표: "+PriorityName(menuUnit.Priority);}
  string SlotActionInfo(int id){if(id==15)return "메뉴 닫기 · 전투는 계속됩니다.";if(id<0){
   if(menuUnit==null)return "병과 위에 올려 효과 확인 · 터치는 두 번 눌러 확정";
   battle.SupportBonuses(menuUnit,out double haste,out double damage);
   if(battle.SkillsActive)return "기술 · "+ExpeditionText.SkillName(menuUnit.Kind)+": "+ExpeditionText.SkillEffect(menuUnit.Kind)+(haste>0?" · 대장장이 지원 +"+Math.Round(haste*100)+"%":"");
   if(haste>0)return "초록색 + : 대장장이 지원 · 공격 속도 +"+Math.Round(haste*100)+"%"+(damage>0?" · 피해 +"+Math.Round(damage*100)+"%":"");
   return "누른 채 강화 쪽으로 끌어 놓기 · 바깥/Esc 닫기";
  }if(id==16)return ExpeditionText.MasteryEffect(menuUnit.Kind);if(id<5)return TroopDescription(id)+" · 사거리 "+Math.Round(battle.PlacementRange(id));if(id==10){double before,interval,after,next; battle.AttackStats(menuUnit,out before,out interval);var u=new Unit{Kind=menuUnit.Kind,Slot=menuSlot,Level=2};battle.AttackStats(u,out after,out next);return menuUnit.Kind==4?"주변 공격 속도 +25% → +35%":"피해 "+Math.Round(before,1)+" → "+Math.Round(after,1)+" · 간격 "+Math.Round(interval,2)+" → "+Math.Round(next,2)+"초 · 사거리 "+Math.Round(battle.AttackRange(u));}if(id==11||id==12)return BranchTitle(menuUnit.Kind,id-10)+" · "+BranchStats(menuUnit,id-10).Replace("\n"," · ");if(id==13)return "환급 "+Battle.SaleRefund(menuUnit)+"G · 손실 "+(menuUnit.Invested-Battle.SaleRefund(menuUnit))+"G"+(battle.Phase==Phase.Fighting?" · 손실이 있으면 두 번 확인":"");return "앞선 적 → 체력 높은 적 → 빠른 적 → 대장 우선";}
  void SelectSlotAction(int id,bool touch){if(id==15){CloseSlotMenu();return;}if(!SlotActionEnabled(id)){menuPreview=id;notice="골드가 부족합니다.";return;}
   bool combatSale=id==13&&battle.Phase==Phase.Fighting&&menuUnit.Invested>Battle.SaleRefund(menuUnit);
   if(touch&&!combatSale&&menuTouchPreview!=id){menuTouchPreview=id;menuPreview=id;if(Debug.isDebugBuild)Debug.Log("SLOT_TOUCH_PREVIEW "+id);return;}menuPreview=id;
   if(id<5){if(battle.Place(id,menuSlot)){sound.Play("place");notice=Balance.Troops[id].Name+" 배치 완료";if(tutorialStage==1||tutorialStage==2){tutorialStage=3;if(Debug.isDebugBuild)Debug.Log("TUTORIAL_STAGE 3");}LogSlotAction(id);CloseSlotMenu();}}
   else if(id==10){if(battle.Upgrade(menuSlot)){UpgradeCue(menuUnit);notice="2레벨 강화 완료";LogSlotAction(id);CloseSlotMenu();}}
   else if(id==16){if(battle.UpgradeMastery(menuSlot)){UpgradeCue(menuUnit);notice=Balance.Troops[menuUnit.Kind].Name+" 숙련 완료";LogSlotAction(id);CloseSlotMenu();}}
   else if(id==11||id==12){if(battle.UpgradeBranch(menuSlot,id-10)){UpgradeCue(menuUnit);notice="3레벨 "+BranchTitle(menuUnit.Kind,id-10);LogSlotAction(id);CloseSlotMenu();}}
   else if(id==13){SellSelected();LogSlotAction(id);if(battle.At(menuSlot)==null)CloseSlotMenu();}
   else {CyclePriority(menuUnit);LogSlotAction(id);}
  }
  void LogSlotAction(int id){if(Debug.isDebugBuild&&Application.platform==RuntimePlatform.WebGLPlayer)Debug.Log("SLOT_ACTION "+menuSlot+" "+id+" "+battle.Gold);}
  Texture2D BranchSheet(string name,int branch,Texture2D fallback){if(branch!=2)return fallback;Texture2D sheet;if(!blackSheets.TryGetValue(name,out sheet)){sheet=Resources.Load<Texture2D>(name+"Black");blackSheets[name]=sheet;}return sheet==null?fallback:sheet;}
  Texture2D MenuPortrait(int kind,int branch){string[] names={"WarriorIdle","ArcherIdle","Pawn","FrostIdle","SmithIdle"};Texture2D[] sheets={warriorIdleLevel[2],archerIdleLevel[2],pawnLevel[2],frostIdleLevel[2],smithIdleLevel[2]};return BranchSheet(names[kind],branch,sheets[kind]);}
  void DrawSlotPreview(){
   if(menuSlot<0)return;int id=menuPreview;if(id>=0&&id<5){Point p=battle.Field.Slots[menuSlot];RangeRing(p,battle.PlacementRange(id));if(Balance.Troops[id].MinRange>0)Ring(p,Balance.Troops[id].MinRange,new Color(1,.4f,.28f,.8f));}}
  Texture2D Disc(){if(disc!=null)return disc;const int n=64;disc=new Texture2D(n,n,TextureFormat.RGBA32,false);disc.filterMode=FilterMode.Bilinear;disc.wrapMode=TextureWrapMode.Clamp;var px=new Color32[n*n];for(int y=0;y<n;y++)for(int x=0;x<n;x++){float d=Mathf.Sqrt((x+.5f-n/2f)*(x+.5f-n/2f)+(y+.5f-n/2f)*(y+.5f-n/2f));px[y*n+x]=new Color32(255,255,255,(byte)(255*Mathf.Clamp01(n/2f-d)));}disc.SetPixels32(px);disc.Apply();return disc;}
  void Circle(Rect r,Color c){Color old=GUI.color;GUI.color=c;GUI.DrawTexture(r,Disc());GUI.color=old;}
  static Rect Inset(Rect r,float by){return new Rect(r.x+by,r.y+by,r.width-2*by,r.height-2*by);}
  Color ActionEdge(int id){return id<0?uiGold:id==11?new Color(.78f,.5f,1):id==12?new Color(.62f,.66f,.74f):id==10?new Color(.98f,.83f,.3f):id==13?new Color(.9f,.55f,.35f):id==16?new Color(1f,.86f,.35f):uiGold;}
  string ActionShort(int id){if(id<0)return "";if(id<5)return Balance.Troops[id].Cost+"G";if(id==10)return "Lv2 · 50G";if(id==11||id==12)return (id==11?"보라":"검정")+" · 90G";if(id==13)return battle.Phase==Phase.Fighting&&saleSlot==menuSlot&&Time.unscaledTime<saleUntil?"재확인":"+"+Battle.SaleRefund(menuUnit)+"G";if(id==14)return PriorityName(menuUnit.Priority);if(id==16)return "숙련 · "+Boon.MasteryCost+"G";return "";}
  string ActionTitle(int id){if(id>=0&&id<5)return Balance.Troops[id].Name+" · "+Balance.Troops[id].Cost+"G";if(id==10)return Balance.Troops[menuUnit.Kind].Name+" 2레벨 · 50G";if(id==11||id==12)return (id==11?"◀ ":"▶ ")+BranchTitle(menuUnit.Kind,id-10)+" · 90G";if(id==13)return "판매 · "+SaleLabel(menuUnit);if(id==14)return "목표: "+PriorityName(menuUnit.Priority);if(id==15)return "닫기";if(id==16)return Balance.Troops[menuUnit.Kind].Name+" 숙련 · "+Boon.MasteryCost+"G";return menuUnit==null?(menuSlot+1)+"번 자리 · 병과 선택":(menuSlot+1)+"번 · "+Balance.Troops[menuUnit.Kind].Name+" Lv"+menuUnit.Level+(menuUnit.Branch>0?" · "+BranchTitle(menuUnit.Kind,menuUnit.Branch):"")+(menuUnit.Mastery>0?" · 숙련":"");}
  void SlotMenuPanel(){ValidateSlotMenu();if(menuSlot<0)return;Matrix4x4 matrix=GUI.matrix;GUI.matrix=Matrix4x4.identity;
   var actions=SlotActions();Vector2 c=MenuCenter();float u=MenuUnit(),r=MenuRadius(),d=MenuButton();
   int old=hud.fontSize;bool wrap=hud.wordWrap;TextAnchor align=hud.alignment;Color textColor=hud.normal.textColor;hud.wordWrap=false;hud.alignment=TextAnchor.MiddleCenter;
   // Soft halo so the ring reads on any terrain, then a thin guide circle.
   float halo=(r+d/2+8*u)*2;Circle(new Rect(c.x-halo/2,c.y-halo/2,halo,halo),new Color(.05f,.07f,.08f,.42f));
   int preview=menuPreview;Vector2 mouse=Event.current.mousePosition;
   foreach(var action in actions){int id=action.Id;Rect b=action.Bounds;bool hover=b.Contains(mouse)&&Input.touchCount==0;if(hover)preview=id;bool enabled=SlotActionEnabled(id);bool lit=hover||menuPreview==id;
    if(id==15){Circle(b,new Color(.12f,.14f,.16f,.92f));Circle(Inset(b,2*u),lit?new Color(.35f,.38f,.42f):new Color(.22f,.25f,.28f));hud.fontSize=Mathf.RoundToInt(16*u);hud.normal.textColor=uiText;GUI.Label(b,"×",hud);continue;}
    Rect face=lit?Inset(b,-3*u):b;Circle(face,new Color(0,0,0,.55f));Circle(Inset(face,1.5f*u),enabled?ActionEdge(id):uiDisabled);Circle(Inset(face,4.5f*u),lit?uiInner:uiPlate);
    Color tint=GUI.color;if(!enabled)GUI.color=new Color(.55f,.55f,.55f,.9f);
    Rect icon=FitMenuPortrait(new Rect(face.x+face.width*.13f,face.y+face.height*.04f,face.width*.74f,face.height*.64f));
    if(id<5){Texture2D[] icons={warriorIdle,archerIdle,pawn,frostIdle,smithIdle};TerrainView.Image(icons[id],icon,50,30,100,120);}
    else if(id==10){int k=menuUnit.Kind;Texture2D[] next={warriorIdleLevel[1],archerIdleLevel[1],pawnLevel[1],frostIdleLevel[1],smithIdleLevel[1]};TerrainView.Image(next[k],icon,50,30,100,120);}
    else if(id==11||id==12)TerrainView.Image(MenuPortrait(menuUnit.Kind,id-10),icon,50,30,100,120);
    else if(id==16){TerrainView.Image(MenuPortrait(menuUnit.Kind,menuUnit.Branch),icon,50,30,100,120);hud.fontSize=Mathf.RoundToInt(16*u);hud.normal.textColor=new Color(1f,.86f,.35f);GUI.Label(new Rect(face.x,face.y,face.width*.45f,face.height*.4f),"★",hud);}
    else if(id==13){if(coin!=null)GUI.DrawTexture(new Rect(face.center.x-10*u,face.y+4*u,20*u,20*u),coin);hud.fontSize=Mathf.RoundToInt(12*u);hud.normal.textColor=new Color(1,.78f,.55f);GUI.Label(new Rect(face.x,face.y+face.height*.43f,face.width,face.height*.28f),"판매",hud);}
    else if(id==14){hud.fontSize=Mathf.RoundToInt(20*u);hud.normal.textColor=uiAccent;GUI.Label(new Rect(face.x,face.y+face.height*.1f,face.width,face.height*.5f),"◎",hud);}
    GUI.color=tint;
    // Price / short label pill under the icon.
    hud.fontSize=Mathf.RoundToInt(12*u);string label=ActionShort(id);float pw=Mathf.Max(face.width*.78f,hud.CalcSize(new GUIContent(label)).x+10*u),ph=17*u;Rect pill=new Rect(face.center.x-pw/2,face.yMax-ph*.9f,pw,ph);Box(pill,new Color(.06f,.07f,.08f,.92f));hud.normal.textColor=enabled?(id==13?new Color(1,.78f,.55f):uiAccent):uiDisabled;GUI.Label(pill,label,hud);
   }
   // Current unit level badge in the middle of the unit menu (the unit itself stays visible under the ring centre).
   if(menuUnit!=null){int k=menuUnit.Kind,l=menuUnit.Level-1;Texture2D[] now={warriorIdleLevel[l],archerIdleLevel[l],pawnLevel[l],frostIdleLevel[l],smithIdleLevel[l]};Texture2D sheet=menuUnit.Level==3?MenuPortrait(k,menuUnit.Branch):now[k];float ps=58*u;Circle(new Rect(c.x-ps/2-3*u,c.y-ps/2-6*u,ps+6*u,ps+6*u),new Color(.06f,.07f,.08f,.75f));TerrainView.Image(sheet,FitMenuPortrait(new Rect(c.x-ps*.42f,c.y-ps*.55f,ps*.84f,ps*1.0f)),50,30,100,120);
    hud.fontSize=Mathf.RoundToInt(11*u);hud.normal.textColor=uiTitle;Rect lv=new Rect(c.x-22*u,c.y+24*u,44*u,15*u);Box(lv,new Color(.06f,.07f,.08f,.85f));GUI.Label(lv,"Lv"+menuUnit.Level+(menuUnit.Mastery>0?"★":""),hud);}
   menuPreview=preview;
   // Info card: title + effect text for the hovered/previewed action (before pressing).
   Rect info=MenuInfoRect();Box(new Rect(info.x-1,info.y-1,info.width+2,info.height+2),new Color(0,0,0,.5f));UiPlate(info);
   Color accent=preview>=0?ActionEdge(preview):uiGold;Box(new Rect(info.x,info.y,4*u,info.height),accent);
   hud.alignment=TextAnchor.UpperLeft;hud.wordWrap=true;hud.fontSize=Mathf.RoundToInt(14*u);hud.normal.textColor=uiTitle;string actionTitle=ActionTitle(preview);while(hud.fontSize>9&&MeasureText(hud,actionTitle).x>info.width-20*u)hud.fontSize--;GUI.Label(new Rect(info.x+12*u,info.y+8*u,info.width-20*u,20*u),actionTitle,hud);
   hud.fontSize=Mathf.RoundToInt(12*u);hud.normal.textColor=uiText;Rect detailRect=new Rect(info.x+12*u,info.y+32*u,info.width-20*u,info.height-38*u);
   string body=SlotActionInfo(preview);if(preview<0)body+=" · 끌어서 놓아도 선택";var detail=new GUIContent(body);
   while(hud.fontSize>9&&hud.CalcHeight(detail,detailRect.width)>detailRect.height)hud.fontSize--;if(Debug.isDebugBuild&&hud.CalcHeight(detail,detailRect.width)>detailRect.height+.1f)Debug.LogError("SLOT_DETAIL_OVERFLOW");GUI.Label(detailRect,detail,hud);
   hud.fontSize=old;hud.wordWrap=wrap;hud.alignment=align;hud.normal.textColor=textColor;
   if(Debug.isDebugBuild&&Application.platform==RuntimePlatform.WebGLPlayer){string state=menuSlot+"|"+(menuUnit==null?0:menuUnit.Level)+"|";foreach(var action in actions){Rect b=action.Bounds;state+=action.Id+","+b.x.ToString("F1",System.Globalization.CultureInfo.InvariantCulture)+","+b.y.ToString("F1",System.Globalization.CultureInfo.InvariantCulture)+","+b.width.ToString("F1",System.Globalization.CultureInfo.InvariantCulture)+","+b.height.ToString("F1",System.Globalization.CultureInfo.InvariantCulture)+";";}if(menuLogged!=state){menuLogged=state;Debug.Log("SLOT_MENU|"+state);var inv=System.Globalization.CultureInfo.InvariantCulture;Debug.Log("SLOT_INFO|"+info.x.ToString("R",inv)+"|"+info.y.ToString("R",inv)+"|"+info.width.ToString("R",inv)+"|"+info.height.ToString("R",inv));}}
   GUI.matrix=matrix;
  }
 }
}
