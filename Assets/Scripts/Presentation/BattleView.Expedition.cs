using System;
using UnityEngine;
namespace EmberBastion {
 // r37 (Claude): the roguelite run on screen. The rules are in Core (Battle.cs, Expedition.cs); this file only
 // stores the run, starts its battles, and shows the camp, the status and the end of a run.
 // The panel is a sub-mode of the field menu (fieldMenu && expeditionMenu), so every existing input guard treats it as modal.
 public sealed partial class BattleView {
  const string ExpeditionKey="snomet.ember-bastion.unity-comparison.expedition.v1",ExpeditionBestKey="snomet.ember-bastion.unity-comparison.expedition.best.v1";
  Expedition expedition;bool expeditionLoaded,expeditionMenu,expeditionFixture;float expeditionOpenAt=-1,expeditionAbandonUntil;
  bool ExpeditionBattle {get{return battle.Mode==BattleMode.Expedition;}}
  Expedition Run {get{LoadExpedition();return expedition;}}
  bool RunActive {get{var run=Run;return run!=null&&(run.State==Expedition.Status.Battle||run.State==Expedition.Status.Camp);}}
  static int RunCleared(Expedition run){return run.ClearedStages;}

  // Best finished run: stages cleared first, then warmth left.
  int ExpeditionBest {get{if(!PreferencesAllowed())return 0;try{return PlayerPrefs.GetInt(ExpeditionBestKey,0);}catch(Exception){return 0;}}}
  bool RecordExpeditionBest(Expedition run){int score=RunCleared(run)*1000+run.Warmth;if(score<=ExpeditionBest||!PreferencesAllowed())return false;try{PlayerPrefs.SetInt(ExpeditionBestKey,score);PlayerPrefs.Save();return true;}catch(Exception){return false;}}
  void ExpeditionLog(){if(Debug.isDebugBuild&&Application.platform==RuntimePlatform.WebGLPlayer){var run=expedition;Debug.Log("EXPEDITION|"+(run==null?"None":run.State.ToString())+"|"+(run==null?0:run.Stage)+"|"+(run==null?0:run.Warmth)+"|"+(run==null?"":string.Join(",",Array.ConvertAll(run.Offers(),o=>o.ToString())))+"|"+battle.Mode+"|"+battle.FieldIndex+"|"+battle.HP+"|"+battle.Gold+"|"+BoonDigits(run)+"|"+battle.Phase+"|"+battle.Time.ToString("F4",System.Globalization.CultureInfo.InvariantCulture)+"|"+battle.Wave+"|"+paused);}}
  static string BoonDigits(Expedition run){string digits="";if(run!=null)for(int i=0;i<Boon.Count;i++)digits+=run.Count(i);return digits;}

  void StartExpedition(uint seed=0){
   if(seed==0){unchecked{seed=(uint)DateTime.UtcNow.Ticks^(uint)(Time.realtimeSinceStartup*1000)^0x9E3779B9u;}if(seed==0)seed=1;}
   pendingCampBoon=-1;campDoctrineCounts=null;expedition=Expedition.Start(seed);expeditionLoaded=true;expeditionSavedBattle=null;OpenExpeditionBattle();
   if(StoryEnabled&&StorySpeech){if(!StorySeen("expedition"))OpenScene("expedition",ExpeditionText.Intro);else if(StorySpeech)Say(ExpeditionText.Again);}
  }
  // Starts the current stage of the run. OpenField resets every piece of view state the usual way; then the run's battle replaces it.
  void OpenExpeditionBattle(){
   if(!RefreshExpeditionCheckpoint())return;var next=expeditionSavedBattle??expedition.CreateBattle();expeditionSavedBattle=null;OpenField(next.FieldIndex);battle=next;BindBattle();paused=next.Phase==Phase.Fighting;SimulationSpeed=expeditionSavedSpeed;SaveExpedition();expeditionMenu=false;expeditionOpenAt=-1;
   notice="원정 "+(expedition.Stage+1)+"/"+expedition.Stages+" · "+battle.Field.Name+" · 온기 "+battle.HP+" · 시작 "+battle.Gold+"G"+(battle.Threat>0?" · 적 체력 +"+Mathf.RoundToInt((float)(Boon.ThreatHealth*battle.Threat*100))+"%":"");
   ExpeditionLog();
  }
  void OpenExpeditionMenu(){expeditionMenu=true;fieldMenu=true;expeditionAbandonUntil=0;held.Clear();pressed=false;}
  void CloseExpeditionMenu(){pendingCampBoon=-1;expeditionMenu=false;fieldMenu=false;}
  void AbandonExpedition(){if(expedition!=null){expedition.Abandon();RecordExpeditionBest(expedition);}SaveExpedition();CloseExpeditionMenu();OpenField(0);notice="원정을 접었습니다. 자유 전투로 돌아왔습니다.";ExpeditionLog();}

  // Every frame: notice the end of the run's battle, report it to the run and open the camp or the result after a short beat.
  void UpdateExpedition(){
   if(!expeditionFixture){expeditionFixture=true;ExpeditionFixtures();}
   var run=Run;if(run==null)return;if(ExpeditionBattle&&battle.Phase==Phase.Fighting&&Time.unscaledTime>=expeditionSaveAt){expeditionSaveAt=Time.unscaledTime+1;SaveExpedition();}
   if(run.State==Expedition.Status.Battle&&ExpeditionBattle&&(battle.Phase==Phase.Won||battle.Phase==Phase.Lost)&&run.Matches(battle)&&run.Complete(battle)){
    if(run.State==Expedition.Status.Camp)CaptureCampComposition(battle);
    SaveExpedition();expeditionOpenAt=Time.unscaledTime+(battle.Phase==Phase.Won?1.4f:2f);
    if(run.State==Expedition.Status.Won||run.State==Expedition.Status.Lost)RecordExpeditionBest(run);
    if(run.State==Expedition.Status.Lost)notice=battle.HP<=0?"잔불이 꺼졌습니다. 원정이 끝났습니다.":"전장을 내줬습니다. 원정이 끝났습니다.";
    else notice=run.State==Expedition.Status.Won?"원정 성공! 불씨 단지를 끝까지 지켰습니다.":"전장 방어 성공 · 야영지에서 준비 하나를 고르세요.";
    if(StoryEnabled&&StorySpeech){if(run.State==Expedition.Status.Lost)Say(battle.HP<=0?ExpeditionText.LostCold:ExpeditionText.LostCrowd);else if(run.State==Expedition.Status.Camp)Say(ExpeditionText.Camp[Mathf.Clamp(run.Stage,0,ExpeditionText.Camp.Length-1)]);}
    ExpeditionLog();
   }
   if(expeditionOpenAt>=0&&Time.unscaledTime>=expeditionOpenAt&&!StorySceneOpen&&!toolsMenu&&!titleMenu){expeditionOpenAt=-1;if(run.State==Expedition.Status.Won&&StoryEnabled&&StorySpeech&&!StorySeen("expedition-won"))OpenScene("expedition-won",ExpeditionText.Won);OpenExpeditionMenu();}
  }

  // Bottom bar, left side, during the first preparation: start or resume a run, or show the run's status. Returns the width used.
  float ExpeditionEntry(float y){
   if(battle.Phase!=Phase.Preparation||battle.Wave!=0)return 0;
   var run=Run;float w=Screen.width<700?100:Screen.width>=1000?210:170;Rect r=new Rect(12,y+10,w,44);ExpeditionRect("entry",r);
   if(ExpeditionBattle){if(Button(r,(Screen.width<700?"원정 상태":"원정 "+(run!=null&&run.Matches(battle)?(run.Stage+1)+"/"+run.Stages:"")+" · 온기 "+battle.HP)))OpenExpeditionMenu();return w+12;}
   if(RunActive){if(Button(r,(Screen.width<700?"원정 계속":"원정 이어가기 "+(run.Stage+1)+"/"+run.Stages),battle.Units.Count==0)){if(run.State==Expedition.Status.Camp)OpenExpeditionMenu();else OpenExpeditionBattle();}return w+12;}
   if(Button(r,"불씨 원정",battle.Units.Count==0))StartExpedition();return w+12;
  }
  // Bottom bar hint while a run's battle is on screen.
  string ExpeditionHint(){var run=Run;return "원정 "+(run!=null&&run.Matches(battle)?(run.Stage+1)+"/"+run.Stages+" · ":"")+"잔불 온기 "+battle.HP+" · 적이 성채를 한 바퀴 돌면 온기를 잃습니다";}

  string BoonSummary(Expedition run){string line="";for(int i=0;i<Boon.Count;i++){int n=run.Count(i);if(n>0)line+=(line.Length>0?" · ":"")+ExpeditionText.Name(i)+(n>1?" ×"+n:"");}return line.Length>0?line:"아직 없음";}

  void ExpeditionPanel(){
   if(StorySceneOpen)return;
   var run=Run;bool camp=run!=null&&run.State==Expedition.Status.Camp,over=run!=null&&(run.State==Expedition.Status.Won||run.State==Expedition.Status.Lost);
   bool wide=Screen.width>=640&&Screen.height>=420;
   float width=Math.Min(Screen.width-24,wide?700:480),height=Math.Min(Screen.height-16,camp?(wide?324:340):250),x=(Screen.width-width)/2,y=(Screen.height-height)/2,left=x+18,inner=width-36;
   Box(new Rect(0,0,Screen.width,Screen.height),new Color(0,0,0,.72f));UiPlate(new Rect(x,y,width,height));
   string heading=run==null?"불씨 원정":camp?(pendingCampBoon>=0?"다음 전장 선택":"야영지 · "+ShortFieldName(run.Field)+" 방어 성공"):run.State==Expedition.Status.Won?"원정 성공":run.State==Expedition.Status.Lost?"원정 실패":"불씨 원정 · "+(run.Stage+1)+"/"+run.Stages;
   UiTitle(new Rect(x+12,y+4,width-24,34),heading,title);ExpeditionRect("panel",new Rect(x,y,width,height));
   int oldHud=hud.fontSize,oldButton=button.fontSize;bool oldWrap=hud.wordWrap;TextAnchor oldAlign=hud.alignment;Color oldColor=hud.normal.textColor;
   hud.wordWrap=false;hud.alignment=TextAnchor.MiddleLeft;hud.fontSize=wide?16:12;
   if(run==null){GUI.Label(new Rect(left,y+50,inner,24),"진행 중인 원정이 없습니다.",hud);button.fontSize=wide?15:13;if(MenuButton(new Rect(left,y+height-44,inner,32),"닫기")){CloseExpeditionMenu();if(ExpeditionBattle)OpenField(0);}}
   else{
    string status=camp?"잔불 온기 "+run.Warmth+"/100 · "+(pendingCampBoon>=0?"선택한 준비: "+ExpeditionText.Name(pendingCampBoon):"준비 하나를 고르세요")
     :over?"통과한 전장 "+RunCleared(run)+"/"+run.Stages+" · 남은 온기 "+run.Warmth+" · 처치 "+run.Kills
     :"잔불 온기 "+(ExpeditionBattle&&run.Matches(battle)?battle.HP:run.Warmth)+"/100 · "+Balance.Fields[run.Field].Name+(run.Threat>0?" · 적 체력 +"+Mathf.RoundToInt((float)(Boon.ThreatHealth*run.Threat*100))+"%":"");
    while(hud.fontSize>10&&MeasureText(hud,status).x>inner)hud.fontSize--;GUI.Label(new Rect(left,y+44,inner,20),status,hud);
    hud.fontSize=wide?14:11;hud.normal.textColor=uiDisabled;hud.wordWrap=true;string taken="준비: "+BoonSummary(run);Rect takenRect=new Rect(left,y+66,inner,26);while(hud.fontSize>10&&hud.CalcHeight(new GUIContent(taken),inner)>takenRect.height)hud.fontSize--;ExpeditionFit(takenRect,taken);GUI.Label(takenRect,taken,hud);hud.wordWrap=false;hud.normal.textColor=oldColor;
    if(camp&&pendingCampBoon>=0){DrawCampRoutes(run,new Rect(left,y+94,inner,height-104),wide);}
    else if(camp){
     var offers=run.Offers();int picked=-1;float top=y+94,areaH=height-94-14;
     for(int i=0;i<offers.Length;i++){
      int boon=offers[i];Rect card=wide?new Rect(left+i*(inner+10)/3,top,(inner-20)/3,areaH):new Rect(left,top+i*(areaH+6)/3,inner,(areaH-12)/3);
      ExpeditionRect("card"+i,card);if(MenuButton(card,""))picked=boon;
      bool doctrine=Boon.IsDoctrine(boon);
      if(doctrine){Box(new Rect(card.x,card.y,card.width,2),uiGold);Box(new Rect(card.x,card.yMax-2,card.width,2),uiGold);Box(new Rect(card.x,card.y,2,card.height),uiGold);Box(new Rect(card.xMax-2,card.y,2,card.height),uiGold);}
      Speaker who=ExpeditionText.Proposer(boon);float ps=wide?56:Mathf.Min(48,card.height-10);StoryPortrait(who,new Rect(card.x+8,card.y+8,ps,ps));
      float tx=card.x+16+ps,tw=card.xMax-tx-8;
      hud.wordWrap=false;hud.alignment=TextAnchor.UpperLeft;hud.normal.textColor=uiGold;hud.fontSize=wide?18:14;string name=(doctrine?"[교리] ":"")+ExpeditionText.Name(boon)+(run.Count(boon)>0&&boon<Boon.BattleCount?" +"+run.Count(boon):"");while(hud.fontSize>10&&MeasureText(hud,name).x>tw)hud.fontSize--;GUI.Label(new Rect(tx,card.y+8,tw,26),name,hud);
      hud.normal.textColor=SpeakerColor(who);hud.fontSize=wide?12:10;GUI.Label(new Rect(tx,card.y+(wide?34:25),tw,18),doctrine?DoctrineComposition(boon):StoryScript.Name(who),hud);
      hud.wordWrap=true;hud.normal.textColor=uiText;hud.fontSize=wide?16:12;
      Rect effect=wide?new Rect(card.x+10,card.y+74,card.width-20,52):new Rect(tx,card.y+(card.height>=64?42:40),tw,card.height-44);
      while(hud.fontSize>10&&hud.CalcHeight(new GUIContent(ExpeditionText.Effect(boon)),effect.width)>effect.height)hud.fontSize--;ExpeditionFit(effect,ExpeditionText.Effect(boon));GUI.Label(effect,ExpeditionText.Effect(boon),hud);
      if(wide){hud.normal.textColor=uiDisabled;hud.fontSize=13;Rect quote=new Rect(card.x+10,card.y+130,card.width-20,card.height-140);string said="“"+ExpeditionText.Quote(boon)+"”";while(hud.fontSize>10&&hud.CalcHeight(new GUIContent(said),quote.width)>quote.height)hud.fontSize--;GUI.Label(quote,said,hud);}
     }
     if(picked>=0)ChooseCampPreparation(run,picked);
    }else{
     button.fontSize=wide?15:13;float by=y+height-56,half=(inner-8)/2;
     if(over){
      hud.fontSize=wide?14:11;hud.normal.textColor=uiDisabled;int best=ExpeditionBest;if(best>0)GUI.Label(new Rect(left,y+90,inner,22),"최고 기록: 전장 "+(best/1000)+"/"+run.Stages+" · 온기 "+(best%1000),hud);hud.normal.textColor=oldColor;
      string peaks="";for(int s=0;s<run.Stages&&s<=run.Stage;s++)if(run.Peak(s)>0)peaks+=(peaks.Length>0?" · ":"")+ShortFieldName(run.FieldAt(s))+" 최대 "+run.Peak(s);if(peaks.Length>0){hud.fontSize=wide?14:11;while(hud.fontSize>9&&MeasureText(hud,peaks).x>inner)hud.fontSize--;GUI.Label(new Rect(left,y+112,inner,22),peaks,hud);}
      ExpeditionRect("left",new Rect(left,by,half,44));ExpeditionRect("right",new Rect(left+half+8,by,half,44));if(MenuButton(new Rect(left,by,half,44),"새 원정"))StartExpedition();
      if(MenuButton(new Rect(left+half+8,by,half,44),"자유 전투"))AbandonExpedition();
     }else{
      hud.wordWrap=true;hud.fontSize=wide?14:11;hud.normal.textColor=uiDisabled;GUI.Label(new Rect(left,y+92,inner,56),"전장을 지킬 때마다 야영지에서 준비 하나를 고릅니다. 온기는 다음 전장으로 이어지고, 꺼지면 원정이 끝납니다. 원정에서는 '웨이브 다시'가 없습니다.",hud);hud.normal.textColor=oldColor;hud.wordWrap=false;
      bool armed=Time.unscaledTime<expeditionAbandonUntil;
      ExpeditionRect("left",new Rect(left,by,half,44));ExpeditionRect("right",new Rect(left+half+8,by,half,44));if(MenuButton(new Rect(left,by,half,44),armed?"한 번 더 누르면 포기":"원정 포기")){if(armed)AbandonExpedition();else expeditionAbandonUntil=Time.unscaledTime+3;}
      if(MenuButton(new Rect(left+half+8,by,half,44),"닫기"))CloseExpeditionMenu();
     }
    }
   }
   hud.fontSize=oldHud;hud.wordWrap=oldWrap;hud.alignment=oldAlign;hud.normal.textColor=oldColor;button.fontSize=oldButton;
  }

  // Development fixtures for the debug Web build: ?fx-smoke=expedition[-camp|-won|-lost] starts a fixed-seed run in a known state.
  void ExpeditionFixtures(){
   if(!Debug.isDebugBuild||Application.platform!=RuntimePlatform.WebGLPlayer)return;string url=Application.absoluteURL;if(url.Contains("run-review=1")){tutorialStage=0;titleMenu=false;}
   if(!url.Contains("fx-smoke=expedition"))return;
   StartExpedition(2026);
   if(url.Contains("fx-smoke=expedition-camp")||url.Contains("fx-smoke=expedition-won")||url.Contains("fx-smoke=expedition-lost")){
    int wins=url.Contains("fx-smoke=expedition-won")?expedition.Stages:1;
    for(int i=0;i<wins;i++){battle.Kills=120+i;battle.PeakAlive=40+i;battle.HP=Mathf.Max(1,battle.HP-9);battle.Phase=url.Contains("fx-smoke=expedition-lost")?Phase.Lost:Phase.Won;if(battle.Phase==Phase.Lost)battle.HP=0;
     expedition.Complete(battle);if(expedition.State==Expedition.Status.Camp&&i<wins-1){expedition.Choose(expedition.Offers()[0]);var next=expedition.CreateBattle();battle=next;BindBattle();}}
    if(expedition.State==Expedition.Status.Camp){CaptureCampComposition(battle);SaveExpedition();}
    expeditionOpenAt=Time.unscaledTime;notice="원정 화면 격리 테스트";ExpeditionLog();
   }
   if(url.Contains("fx-smoke=expedition-play")){battle.Gold+=3000;for(int slot=0;slot<battle.Field.Slots.Length;slot++){battle.Place(slot%3==2?3:slot%2,slot);battle.Upgrade(slot);}StartWave();notice="원정 전투 격리 테스트";}
  }
 }
}
