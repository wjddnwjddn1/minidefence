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
  void ValidateSlotMenu(){if(Debug.isDebugBuild&&Application.platform==RuntimePlatform.WebGLPlayer&&Application.absoluteURL.Contains("fx-smoke=slotmenu")){string size=Screen.width+"x"+Screen.height;if(fixtureViewport!=size){fixtureViewport=size;Debug.Log("SLOT_VIEW|"+scale.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+"|"+ox.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+"|"+oy.ToString("R",System.Globalization.CultureInfo.InvariantCulture));}}if(menuSlot<0)return;if(menuReloadAt>0&&Time.unscaledTime>=menuReloadAt){menuReloadAt=-1;SaveCurrent();LoadPrep();return;}if(menuStunAt>0&&Time.unscaledTime>=menuStunAt&&menuUnit!=null){menuUnit.Stun=1;menuStunAt=-1;}if(WindowTooSmall()||menuBattle!=battle||selected!=menuSlot||menuSlot>=battle.Field.Slots.Length||battle.At(menuSlot)!=menuUnit||menuUnit!=null&&menuUnit.Stun>0||titleMenu||toolsMenu||fieldMenu||guideMenu||branchMenu||aiming||battle.ReinforcementPending||battle.Phase!=Phase.Preparation&&battle.Phase!=Phase.Fighting)CloseSlotMenu();}
  Rect SlotMenuBounds(){Point p=battle.Field.Slots[menuSlot];float width=Mathf.Min(360,Screen.width-16),height=264;return new Rect(Mathf.Clamp(ox+(float)p.X*scale-width/2,8,Screen.width-width-8),Mathf.Clamp(oy+(float)p.Y*scale-(menuUnit==null?height/2:66),8,Screen.height-height-8),width,height);}
  List<SlotAction> SlotActions(Rect r){var actions=new List<SlotAction>();float w=(r.width-88)/2;if(menuUnit==null){float cw=Mathf.Min(104,(r.width-24)/3);float[] xs={(r.width-cw)/2,8,r.width-cw-8,20,r.width-cw-20};float[] ys={30,88,88,146,146};for(int k=0;k<5;k++)actions.Add(new SlotAction(k,new Rect(r.x+xs[k],r.y+ys[k],cw,52)));}
   else if(menuUnit.Level==1){actions.Add(new SlotAction(10,new Rect(r.x+8,r.y+34,w,64)));actions.Add(new SlotAction(13,new Rect(r.x+r.width-8-w,r.y+34,w,64)));}
   else if(menuUnit.Level==2){actions.Add(new SlotAction(11,new Rect(r.x+8,r.y+34,w,64)));actions.Add(new SlotAction(12,new Rect(r.x+r.width-8-w,r.y+34,w,64)));actions.Add(new SlotAction(13,new Rect(r.x+8,r.y+104,r.width-16,44)));}
   else actions.Add(new SlotAction(13,new Rect(r.x+8,r.y+34,r.width-16,44)));
   if(menuUnit!=null&&(menuUnit.Kind==1||menuUnit.Kind==3))actions.Add(new SlotAction(14,new Rect(r.x+8,r.y+152,r.width-16,44)));actions.Add(new SlotAction(15,new Rect(r.xMax-56,r.y+208,48,44)));return actions;}
  bool HandleSlotMenuInput(Event e){ValidateSlotMenu();if(menuSlot<0)return false;
   if(e.type==EventType.KeyDown){if(e.keyCode==KeyCode.Escape){CloseSlotMenu();held.Clear();e.Use();return true;}CloseSlotMenu();return false;}
   bool pointer=e.type==EventType.MouseDown||e.type==EventType.MouseUp||e.type==EventType.MouseDrag||e.type==EventType.ScrollWheel;if(!pointer)return false;
   Rect panel=SlotMenuBounds();var actions=SlotActions(panel);int hit=-1;foreach(var a in actions)if(a.Bounds.Contains(e.mousePosition))hit=a.Id;
   if(e.type==EventType.MouseDown&&e.button==0){if(!panel.Contains(e.mousePosition)){CloseSlotMenu();return false;}menuDown=hit;menuTouch=Input.touchCount>0;menuDownPoint=e.mousePosition;e.Use();return true;}
   if(e.type==EventType.MouseUp&&e.button==0){int action=menuDown;menuDown=-1;if(action>=0&&hit==action&&Vector2.Distance(menuDownPoint,e.mousePosition)<12)SelectSlotAction(action,menuTouch);e.Use();return true;}
   if(e.type==EventType.MouseDrag||e.type==EventType.ScrollWheel){e.Use();return true;}return false;
  }
  bool SlotActionEnabled(int id){return id<5?battle.Gold>=Balance.Troops[id].Cost:id==10?battle.Gold>=50:id==11||id==12?battle.Gold>=90:true;}
  string SlotActionLabel(int id){if(id==15)return "닫기";if(id<5)return Balance.Troops[id].Name+" "+Balance.Troops[id].Cost+"G";if(id==10)return "강화 50G";if(id==11||id==12)return BranchTitle(menuUnit.Kind,id-10)+" · 90G";if(id==13)return SaleLabel(menuUnit);return "목표: "+PriorityName(menuUnit.Priority);}
  string SlotActionInfo(int id){if(id==15)return "메뉴 닫기 · 전투는 계속됩니다.";if(id<0)return menuUnit==null?"병과 위에 올려 효과 확인 · 터치는 두 번 눌러 확정":"강화 효과 확인 · 바깥 또는 Esc로 닫기";if(id<5)return TroopDescription(id)+" · 사거리 "+Math.Round(battle.PlacementRange(id));if(id==10){double before,interval,after,next; battle.AttackStats(menuUnit,out before,out interval);var u=new Unit{Kind=menuUnit.Kind,Slot=menuSlot,Level=2};battle.AttackStats(u,out after,out next);return menuUnit.Kind==4?"주변 공격 속도 +25% → +35%":"피해 "+Math.Round(before,1)+" → "+Math.Round(after,1)+" · 간격 "+Math.Round(interval,2)+" → "+Math.Round(next,2)+"초 · 사거리 "+Math.Round(battle.AttackRange(u));}if(id==11||id==12)return BranchTitle(menuUnit.Kind,id-10)+" · "+BranchStats(menuUnit,id-10).Replace("\n"," · ");if(id==13)return "환급 "+Battle.SaleRefund(menuUnit)+"G · 손실 "+(menuUnit.Invested-Battle.SaleRefund(menuUnit))+"G"+(battle.Phase==Phase.Fighting?" · 손실이 있으면 두 번 확인":"");return "앞선 적 → 체력 높은 적 → 빠른 적 → 대장 우선";}
  void SelectSlotAction(int id,bool touch){if(id==15){CloseSlotMenu();return;}if(!SlotActionEnabled(id)){menuPreview=id;notice="골드가 부족합니다.";return;}
   bool combatSale=id==13&&battle.Phase==Phase.Fighting&&menuUnit.Invested>Battle.SaleRefund(menuUnit);
   if(touch&&!combatSale&&menuTouchPreview!=id){menuTouchPreview=id;menuPreview=id;if(Debug.isDebugBuild)Debug.Log("SLOT_TOUCH_PREVIEW "+id);return;}menuPreview=id;
   if(id<5){if(battle.Place(id,menuSlot)){sound.Play("place");notice=Balance.Troops[id].Name+" 배치 완료";if(tutorialStage==1||tutorialStage==2){tutorialStage=3;if(Debug.isDebugBuild)Debug.Log("TUTORIAL_STAGE 3");}LogSlotAction(id);CloseSlotMenu();}}
   else if(id==10){if(battle.Upgrade(menuSlot)){UpgradeCue(menuUnit);notice="2레벨 강화 완료";LogSlotAction(id);CloseSlotMenu();}}
   else if(id==11||id==12){if(battle.UpgradeBranch(menuSlot,id-10)){UpgradeCue(menuUnit);notice="3레벨 "+BranchTitle(menuUnit.Kind,id-10);LogSlotAction(id);CloseSlotMenu();}}
   else if(id==13){SellSelected();LogSlotAction(id);if(battle.At(menuSlot)==null)CloseSlotMenu();}
   else {CyclePriority(menuUnit);LogSlotAction(id);}
  }
  void LogSlotAction(int id){if(Debug.isDebugBuild&&Application.platform==RuntimePlatform.WebGLPlayer)Debug.Log("SLOT_ACTION "+menuSlot+" "+id+" "+battle.Gold);}
  Texture2D BranchSheet(string name,int branch,Texture2D fallback){if(branch!=2)return fallback;Texture2D sheet;if(!blackSheets.TryGetValue(name,out sheet)){sheet=Resources.Load<Texture2D>(name+"Black");blackSheets[name]=sheet;}return sheet==null?fallback:sheet;}
  Texture2D MenuPortrait(int kind,int branch){string[] names={"WarriorIdle","ArcherIdle","Pawn","FrostIdle","SmithIdle"};Texture2D[] sheets={warriorIdleLevel[2],archerIdleLevel[2],pawnLevel[2],frostIdleLevel[2],smithIdleLevel[2]};return BranchSheet(names[kind],branch,sheets[kind]);}
  void DrawSlotPreview(){if(menuSlot<0)return;int id=menuPreview;if(id>=0&&id<5){Point p=battle.Field.Slots[menuSlot];RangeRing(p,battle.PlacementRange(id));if(Balance.Troops[id].MinRange>0)Ring(p,Balance.Troops[id].MinRange,new Color(1,.4f,.28f,.8f));}}
  void SlotMenuPanel(){ValidateSlotMenu();if(menuSlot<0)return;Rect panel=SlotMenuBounds();var actions=SlotActions(panel);UiPlate(panel);int old=hud.fontSize;bool wrap=hud.wordWrap;TextAnchor align=hud.alignment;hud.fontSize=13;hud.wordWrap=true;hud.alignment=TextAnchor.MiddleCenter;
   GUI.Label(new Rect(panel.x+8,panel.y+4,panel.width-16,24),(menuSlot+1)+"번 · "+(menuUnit==null?"병과 선택":Balance.Troops[menuUnit.Kind].Name+" Lv"+menuUnit.Level),hud);
   int preview=menuPreview;foreach(var action in actions){int id=action.Id;Rect r=action.Bounds;if(r.Contains(Event.current.mousePosition)&&Input.touchCount==0)preview=id;bool enabled=SlotActionEnabled(id);Color edge=id==11?new Color(.78f,.5f,1):id==12?new Color(.45f,.52f,.63f):uiGold;Box(r,enabled?edge:uiDisabled);Box(new Rect(r.x+2,r.y+2,r.width-4,r.height-4),uiPlate);Color color=GUI.color;if(!enabled)GUI.color=new Color(.5f,.5f,.5f);
    if(id<5){Texture2D[] icons={warriorIdle,archerIdle,pawn,frostIdle,smithIdle};TerrainView.Image(icons[id],new Rect(r.center.x-20,r.y+1,40,32),50,30,100,120);GUI.Label(new Rect(r.x+2,r.y+32,r.width-4,20),SlotActionLabel(id),hud);}
    else{if(id==11||id==12){TerrainView.Image(MenuPortrait(menuUnit.Kind,id-10),new Rect(r.center.x-21,r.y+1,42,34),50,30,100,120);GUI.Label(new Rect(r.x+3,r.y+34,r.width-6,28),SlotActionLabel(id),hud);}else GUI.Label(new Rect(r.x+4,r.y+4,r.width-8,r.height-8),SlotActionLabel(id),hud);}GUI.color=color;
   }
   if(menuUnit!=null&&menuUnit.Level<3){int k=menuUnit.Kind,l=menuUnit.Level-1;Texture2D[] current={warriorIdleLevel[l],archerIdleLevel[l],pawnLevel[l],frostIdleLevel[l],smithIdleLevel[l]};TerrainView.Image(current[k],new Rect(panel.center.x-26,panel.y+37,52,39),50,30,100,120);GUI.Label(new Rect(panel.center.x-32,panel.y+76,64,20),"Lv"+menuUnit.Level,hud);}
   menuPreview=preview;hud.fontSize=12;Rect detailRect=new Rect(panel.x+10,panel.y+202,panel.width-80,56);var detail=new GUIContent(SlotActionInfo(preview));while(hud.fontSize>10&&hud.CalcHeight(detail,detailRect.width)>detailRect.height)hud.fontSize--;if(Debug.isDebugBuild&&hud.CalcHeight(detail,detailRect.width)>detailRect.height+.1f)Debug.LogError("SLOT_DETAIL_OVERFLOW");GUI.Label(detailRect,detail,hud);hud.fontSize=old;hud.wordWrap=wrap;hud.alignment=align;
   if(Debug.isDebugBuild&&Application.platform==RuntimePlatform.WebGLPlayer){string state=menuSlot+"|"+(menuUnit==null?0:menuUnit.Level)+"|";foreach(var action in actions){Rect r=action.Bounds;state+=action.Id+","+r.x.ToString("F1",System.Globalization.CultureInfo.InvariantCulture)+","+r.y.ToString("F1",System.Globalization.CultureInfo.InvariantCulture)+","+r.width.ToString("F1",System.Globalization.CultureInfo.InvariantCulture)+","+r.height.ToString("F1",System.Globalization.CultureInfo.InvariantCulture)+";";}if(menuLogged!=state){menuLogged=state;Debug.Log("SLOT_MENU|"+state);}}
  }
 }
}
