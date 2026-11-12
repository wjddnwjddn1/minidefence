using System;
using System.Collections.Generic;
using UnityEngine;
namespace EmberBastion {
 // Story layer (user request 2026-11-11). Display only: never changes the battle, prices, waves or saves.
 //  - Scenes (opening, first visit of each field, ending): a dialogue box during preparation/results that waits for click/Space; Esc skips.
 //  - Combat lines: short non-blocking speech strip (wave 1, middle, last, boss, roar, castle hit, crowding, win/loss).
 //  - Goblin barks: an occasional speech bubble over a walking enemy.
 // Off for automation: smoke/QA arguments, QA products, and debug Web builds unless the URL has story=1.
 public sealed partial class BattleView {
  const string StoryKey="snomet.ember-bastion.story.v1.";
  readonly HashSet<string> storySeen=new HashSet<string>();
  StoryLine[] sceneLines;int sceneIndex;string sceneId="";
  StoryLine sayLine;float sayUntil;StoryLine sayNext;float sayNextAt;
  Battle storyBattle;Phase storyPhase;int storyWave=-1,storyHP,storyBossWave=-1;bool storyRoar,storyCrowd;float storyHitAt=-99;
  int barkEnemy=-1;string barkText="";float barkUntil,nextBarkAt;
  bool? storyEnabled;
  bool storyMenu;bool? storySpeech;
  bool StorySpeech {get{if(!storySpeech.HasValue){bool enabled=true;if(PreferencesAllowed())try{enabled=PlayerPrefs.GetInt(StoryKey+"speech",1)!=0;}catch(Exception){}storySpeech=enabled;}return storySpeech.Value;}}
  void SetStorySpeech(bool enabled){storySpeech=enabled;if(!enabled){sayLine=null;sayNext=null;barkEnemy=-1;}if(PreferencesAllowed())try{PlayerPrefs.SetInt(StoryKey+"speech",enabled?1:0);PlayerPrefs.Save();}catch(Exception){}StoryLog("STORY_SETTING|"+(enabled?1:0)+"|"+PreferencesAllowed());}
  void StorySettingsPanel(){float w=Mathf.Min(420,Screen.width-24),h=Mathf.Min(230,Screen.height-24),x=(Screen.width-w)/2,y=(Screen.height-h)/2;Box(new Rect(0,0,Screen.width,Screen.height),new Color(0,0,0,.75f));UiPlate(new Rect(x,y,w,h));UiTitle(new Rect(x+12,y+5,w-24,36),"이야기 설정",smallTitle);StoryReviewControl("speech",new Rect(x+16,y+48,w-32,38));StoryReviewControl("replay",new Rect(x+16,y+94,w-32,38));StoryReviewControl("back",new Rect(x+16,y+h-42,w-32,32));if(MenuButton(new Rect(x+16,y+48,w-32,38),"이야기 대사: "+(StorySpeech?"켜짐":"꺼짐")))SetStorySpeech(!StorySpeech);if(MenuButton(new Rect(x+16,y+94,w-32,38),"오프닝 다시 보기",StoryEnabled&&battle.Phase==Phase.Preparation)){storyMenu=false;toolsMenu=false;OpenScene("opening",StoryScript.Opening);}if(MenuButton(new Rect(x+16,y+h-42,w-32,32),"뒤로"))storyMenu=false;}
  bool StoryEnabled {get{if(storyEnabled.HasValue)return storyEnabled.Value;bool on=true;
   if(AutomationSmoke()||Application.productName.Contains("QA"))on=false;
   foreach(string arg in Environment.GetCommandLineArgs())if(arg.StartsWith("--")&&(arg.EndsWith("-smoke")||arg.EndsWith("-qa")||arg=="--storage-only"))on=false;
   if(Debug.isDebugBuild&&Application.platform==RuntimePlatform.WebGLPlayer)on=Application.absoluteURL.Contains("story=1");
   if(StoryPerformance)on=true;storyEnabled=on;return on;}}
  bool StorySceneOpen {get{return sceneLines!=null;}}
  bool StorySeen(string id){if(storySeen.Contains(id))return true;if(!PreferencesAllowed())return false;try{return PlayerPrefs.GetInt(StoryKey+id,0)!=0;}catch(Exception){return false;}}
  void MarkStory(string id){storySeen.Add(id);if(!PreferencesAllowed())return;try{PlayerPrefs.SetInt(StoryKey+id,1);PlayerPrefs.Save();}catch(Exception){}}
  void OpenScene(string id,StoryLine[] lines){if(lines==null||lines.Length==0)return;sceneId=id;sceneLines=lines;sceneIndex=0;sceneTyping.Reset(lines[0].Text);sayLine=null;sayNext=null;held.Clear();pressed=false;MarkStory(id);StoryLog("STORY_SCENE|"+id+"|0");}
  void AdvanceScene(bool skip){if(!StorySceneOpen)return;if(!skip&&!sceneTyping.Finished){sceneTyping.Complete();StoryLog("STORY_TYPE_REVEAL|"+sceneId+"|"+sceneIndex);return;}sceneIndex++;if(skip||sceneIndex>=sceneLines.Length){if(skip&&sceneId=="opening")MarkStory("field"+battle.FieldIndex);StoryLog("STORY_SCENE_END|"+sceneId+(skip?"|skip":""));sceneLines=null;sceneId="";return;}sceneTyping.Reset(sceneLines[sceneIndex].Text);StoryLog("STORY_SCENE|"+sceneId+"|"+sceneIndex);}
  void Say(StoryLine line){if(line==null)return;sayLine=line;speechTypingText=line.Text;speechTyping.Reset(line.Text);sayUntil=Time.unscaledTime+4.5f;StoryLog("STORY_SAY|"+line.Who+"|"+line.Text);}
  void StoryLog(string line){if(Debug.isDebugBuild&&(Application.platform==RuntimePlatform.WebGLPlayer||StoryReview))Debug.Log(line);}
  static T Pick<T>(T[] list,int index){return list[((index%list.Length)+list.Length)%list.Length];}

  void UpdateStory(){
   ReviewFrostVisual();if(!StoryEnabled)return;
   ReviewPerformanceStory();if(storyBattle!=battle){storyBattle=battle;storyPhase=battle.Phase;storyWave=battle.Wave;storyHP=battle.HP;storyBossWave=-1;storyRoar=false;storyCrowd=false;barkEnemy=-1;sayLine=null;sayNext=null;}
   bool menus=titleMenu||branchMenu||toolsMenu||fieldMenu||guideMenu||audioMenu;
   int field=battle.FieldIndex;
   // Scenes: only while nothing else is open and the battle is not running.
   if(StorySpeech&&!StorySceneOpen&&!menus&&battle.Phase==Phase.Preparation&&battle.Wave==0){
    if(!StorySeen("opening"))OpenScene("opening",StoryScript.Opening);
    else if(field<StoryScript.FieldIntro.Length&&!StorySeen("field"+field))OpenScene("field"+field,StoryScript.FieldIntro[field]);}
   if(StorySpeech&&!StorySceneOpen&&!menus&&battle.RulesVersion>=4&&!StorySeen("frost-intro")&&battle.Units.Exists(u=>u.Kind==3)){Say(StoryScript.FrostIntro);MarkStory("frost-intro");}
   if(!StorySpeech){storyPhase=battle.Phase;storyWave=battle.Wave;storyHP=battle.HP;return;}
   if(sayNext!=null&&Time.unscaledTime>=sayNextAt){Say(sayNext);sayNext=null;}
   Phase phase=battle.Phase;
   if(phase!=storyPhase){
    if(phase==Phase.Won){Say(Pick(StoryScript.Victory,field));if(field==StoryScript.FieldIntro.Length-1&&!StorySeen("ending"))OpenScene("ending",StoryScript.Ending);}
    else if(phase==Phase.Lost)Say(Pick(StoryScript.Defeat,field));
    storyPhase=phase;}
   if(phase!=Phase.Fighting){storyWave=battle.Wave;storyHP=battle.HP;barkEnemy=-1;return;}
   if(battle.Wave!=storyWave){storyWave=battle.Wave;
    if(battle.Wave==1)Say(Pick(StoryScript.FirstWave,field));
    else if(battle.Wave==battle.WaveCount)Say(StoryScript.LastWave);
    else if(battle.Wave==(battle.WaveCount+1)/2)Say(Pick(StoryScript.MidWave,field));}
   foreach(var e in battle.Enemies){if(e.Kind!=4||e.HP<=0)continue;
    if(storyBossWave!=battle.Wave){storyBossWave=battle.Wave;storyRoar=false;Say(Pick(StoryScript.BossArrive,field));}
    if(e.Roared&&!storyRoar){storyRoar=true;Say(StoryScript.BossRoar);sayNext=StoryScript.BossRoarReply;sayNextAt=Time.unscaledTime+2.6f;}
    break;}
   if(battle.HP<storyHP&&Time.unscaledTime-storyHitAt>20){storyHitAt=Time.unscaledTime;Say(StoryScript.CastleHit);}storyHP=battle.HP;
   if(!storyCrowd&&battle.Enemies.Count>=70){storyCrowd=true;Say(StoryScript.Crowded);}
   // Barks: presentation-only randomness (UnityEngine.Random), never the battle RNG.
   if(barkEnemy>=0&&(Time.unscaledTime>barkUntil||battle.Enemies.Find(x=>x.Id==barkEnemy&&x.HP>0)==null))barkEnemy=-1;
   if(!paused&&!battle.ReinforcementPending&&Time.unscaledTime>=nextBarkAt&&battle.Enemies.Count>=3){
    nextBarkAt=Time.unscaledTime+UnityEngine.Random.Range(5f,9f);
    var pool=battle.Enemies.FindAll(x=>x.HP>0&&x.Kind<4&&x.Position.X>40&&x.Position.X<1240&&x.Position.Y>60&&x.Position.Y<740);
    if(pool.Count>0){var e=pool[UnityEngine.Random.Range(0,pool.Count)];barkEnemy=e.Id;barkText=StoryScript.Barks[e.Kind][UnityEngine.Random.Range(0,StoryScript.Barks[e.Kind].Length)];barkTypingText=barkText;barkTyping.Reset(barkText);barkUntil=Time.unscaledTime+2.4f;StoryLog("STORY_BARK|"+e.Kind+"|"+barkText);}}
  }

  // Returns true when the event belongs to an open scene (blocks map clicks and shortcuts underneath).
  bool HandleStoryInput(Event e){
   if(!StorySceneOpen)return false;
   if(e.type==EventType.KeyDown){if(e.keyCode==KeyCode.Escape)AdvanceScene(true);else if(e.keyCode==KeyCode.Space||e.keyCode==KeyCode.Return||e.keyCode==KeyCode.KeypadEnter)AdvanceScene(false);e.Use();return true;}
   if(e.type==EventType.MouseDown||e.type==EventType.TouchDown){AdvanceScene(SceneSkipRect().Contains(e.mousePosition));e.Use();return true;}
   if(e.type==EventType.MouseUp||e.type==EventType.MouseDrag||e.type==EventType.TouchUp||e.type==EventType.TouchMove){e.Use();return true;}
   return false;}

  Rect SceneRect(){float w=Mathf.Min(780,Screen.width-16),h=Mathf.Clamp(Screen.height*.2f,108,140);return new Rect((Screen.width-w)/2,Mathf.Max(8,Screen.height-h-72),w,h);}
  Rect SceneSkipRect(){Rect r=SceneRect();return new Rect(r.xMax-112,r.y-34,108,30);}
  Color SpeakerColor(Speaker s){return s==Speaker.Grnak||s==Speaker.Goblin?new Color(1f,.45f,.35f):s==Speaker.Narrator?uiText:uiAccent;}
  void StoryPortrait(Speaker s,Rect area){int p=StoryScript.Portrait(s);if(p<0)return;
   Box(area,new Color(.06f,.07f,.08f,.85f));
   if(p==14&&enemyBoss!=null){float height=area.height-4,width=height*88/96;int frame=(int)(Time.unscaledTime/.16f)%6;StoryIdleLog(s,frame);TerrainView.Image(enemyBoss,new Rect(area.center.x-width/2,area.y+2,width,height),frame*192+48,44,88,96);}
   else if(p<10){Texture2D[][] levels={warriorIdleLevel,archerIdleLevel,pawnLevel,frostIdleLevel,smithIdleLevel};var tex=levels[p]!=null&&levels[p].Length>0?levels[p][0]:null;if(tex!=null){int frame=(int)(Time.unscaledTime/.16f)%Mathf.Max(1,tex.width/192);StoryIdleLog(s,frame);TerrainView.Image(tex,new Rect(area.x+2,area.y+2,area.width-4,area.height-4),frame*192+58,44,84,84);}}
   else{Texture2D tex=p==14?enemyBoss:torch;if(tex!=null){int frame=(int)(Time.unscaledTime/.16f)%6;StoryIdleLog(s,frame);TerrainView.Image(tex,new Rect(area.x+2,area.y+2,area.width-4,area.height-4),frame*192+40,232,112,112);}}}

  void DrawStory(){
   if(!StoryEnabled)return;
   if(StorySceneOpen){var line=sceneLines[sceneIndex];Rect r=SceneRect();
    Box(new Rect(0,0,Screen.width,Screen.height),new Color(0,0,0,.45f));UiPlate(r);
    float pad=12,ps=StoryScript.Portrait(line.Who)>=0?r.height-2*pad:0;StoryPortrait(line.Who,new Rect(r.x+pad,r.y+pad,ps,ps));
    float tx=r.x+pad+(ps>0?ps+12:0),tw=r.xMax-tx-pad;
    int oldSize=hud.fontSize;bool oldWrap=hud.wordWrap;Color oldColor=hud.normal.textColor;TextAnchor oldAlign=hud.alignment;hud.alignment=TextAnchor.UpperLeft;
    string name=StoryScript.Name(line.Who);if(name.Length>0){hud.fontSize=Screen.width<480?13:16;hud.normal.textColor=SpeakerColor(line.Who);GUI.Label(new Rect(tx,r.y+pad-2,tw,24),name,hud);}
    hud.wordWrap=true;hud.normal.textColor=uiText;hud.fontSize=Screen.width<480?13:17;Rect body=new Rect(tx,r.y+pad+(name.Length>0?24:4),tw,r.height-pad*2-(name.Length>0?24:4)-18);
    while(hud.fontSize>11&&hud.CalcHeight(new GUIContent(line.Text),body.width)>body.height)hud.fontSize--;GUI.Label(body,TypedStory(sceneTyping,"scene"),hud);StoryReviewScene(body,line.Text);
    hud.wordWrap=false;hud.fontSize=12;hud.alignment=TextAnchor.LowerRight;hud.normal.textColor=uiDisabled;GUI.Label(new Rect(tx,r.yMax-22,tw,18),(sceneIndex+1)+"/"+sceneLines.Length+(sceneTyping.Finished?" · 클릭 또는 Space ▶":" · 클릭 또는 Space: 바로 표시"),hud);
    Rect skip=SceneSkipRect();Box(skip,new Color(.1f,.12f,.14f,.92f));hud.alignment=TextAnchor.MiddleCenter;hud.normal.textColor=uiText;hud.fontSize=13;GUI.Label(skip,"건너뛰기 (Esc)",hud);
    hud.fontSize=oldSize;hud.wordWrap=oldWrap;hud.normal.textColor=oldColor;hud.alignment=oldAlign;return;}
   if(!StorySpeech||menuSlot>=0||battle.ReinforcementPending||titleMenu||toolsMenu||fieldMenu||guideMenu||branchMenu)return;
   // Goblin bark bubble over the enemy.
   if(barkEnemy>=0&&!paused){var e=battle.Enemies.Find(x=>x.Id==barkEnemy&&x.HP>0);if(e!=null){int old=hud.fontSize;Color oc=hud.normal.textColor;TextAnchor oa=hud.alignment;hud.fontSize=Screen.width<480?10:12;hud.alignment=TextAnchor.MiddleCenter;hud.normal.textColor=new Color(.12f,.08f,.06f);
     float w=MeasureText(hud,barkText).x+12,h=20,cx=ox+(float)e.Position.X*scale,cy=oy+(float)(e.Position.Y-46)*scale;Rect b=PlaceWarning(new Rect(cx-w/2-1,cy-h-1,w+2,h+7),true);b=new Rect(b.x+1,b.y+1,b.width-2,b.height-7);if(b.width<=0){hud.fontSize=old;hud.normal.textColor=oc;hud.alignment=oa;return;}ReserveWarning("story-bark",new Rect(b.x-1,b.y-1,b.width+2,b.height+7));StoryRender("bark");
     Box(new Rect(b.x-1,b.y-1,b.width+2,b.height+2),new Color(.2f,.12f,.08f,.9f));Box(b,new Color(1f,.96f,.88f,.95f));Box(new Rect(Mathf.Clamp(cx-3,b.x+4,b.xMax-10),b.yMax,6,5),new Color(1f,.96f,.88f,.95f));if(barkTypingText!=barkText){barkTypingText=barkText;barkTyping.Reset(barkText);}GUI.Label(b,TypedStory(barkTyping,"bark"),hud);
     hud.fontSize=old;hud.normal.textColor=oc;hud.alignment=oa;}}
   // Combat speech strip at the bottom-left of the board.
   if(sayLine!=null&&Time.unscaledTime<sayUntil&&menuSlot<0){float fade=Mathf.Clamp01((sayUntil-Time.unscaledTime)/.5f);float bw=1280*scale,w=Mathf.Min(LowLandscape?350:420,bw-12),h=58;Rect r=PlaceWarning(new Rect(ox+6,Mathf.Min(oy+768*scale,Screen.height-FooterHeight)-h-12,w,h),true);if(r.width<=0)return;ReserveWarning("story-speech",r);StoryRender("speech");
    Color old=GUI.color;GUI.color=new Color(1,1,1,fade);Box(r,new Color(.05f,.07f,.08f,.9f));Box(new Rect(r.x,r.y,3,r.height),SpeakerColor(sayLine.Who));
    float ps=StoryScript.Portrait(sayLine.Who)>=0?h-10:0;StoryPortrait(sayLine.Who,new Rect(r.x+8,r.y+5,ps,ps));float tx=r.x+12+(ps>0?ps+6:0),tw=r.xMax-tx-8;
    int oldSize=hud.fontSize;bool oldWrap=hud.wordWrap;Color oc=hud.normal.textColor;TextAnchor oa=hud.alignment;hud.alignment=TextAnchor.UpperLeft;hud.fontSize=12;hud.normal.textColor=SpeakerColor(sayLine.Who);GUI.Label(new Rect(tx,r.y+4,tw,16),StoryScript.Name(sayLine.Who),hud);
    hud.wordWrap=true;hud.normal.textColor=uiText;hud.fontSize=Screen.width<480?12:13;Rect body=new Rect(tx,r.y+20,tw,h-22);while(hud.fontSize>10&&hud.CalcHeight(new GUIContent(sayLine.Text),body.width)>body.height)hud.fontSize--;if(speechTypingText!=sayLine.Text){speechTypingText=sayLine.Text;speechTyping.Reset(sayLine.Text);}GUI.Label(body,TypedStory(speechTyping,"speech"),hud);
    hud.fontSize=oldSize;hud.wordWrap=oldWrap;hud.normal.textColor=oc;hud.alignment=oa;GUI.color=old;}
  }
 }
}
