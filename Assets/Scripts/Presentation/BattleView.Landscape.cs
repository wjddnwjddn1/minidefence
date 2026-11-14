using UnityEngine;
namespace EmberBastion {
 public sealed partial class BattleView {
  bool LowLandscape {get{return Screen.height<480&&!PortraitWindow();}}
  float HeaderHeight {get{return LowLandscape?44:72;}}
  float FooterHeight {get{return LowLandscape?44:64;}}
  float rotationReviewAt;
  void RotationReview(){if(Debug.isDebugBuild&&Application.platform==RuntimePlatform.WebGLPlayer&&Application.absoluteURL.Contains("landscape-review=1")&&PortraitWindow()&&Event.current.type==EventType.Repaint&&Time.unscaledTime>=rotationReviewAt){rotationReviewAt=Time.unscaledTime+.5f;Debug.Log("ROTATE_TIME|"+battle.Time.ToString("R",System.Globalization.CultureInfo.InvariantCulture));}}
  void CompactHeader(){
   UiPlate(new Rect(2,0,Screen.width-4,HeaderHeight));int old=hud.fontSize;bool wrap=hud.wordWrap;hud.wordWrap=false;hud.fontSize=Screen.width<600?11:14;
   string info=(Screen.width>=600?ShortFieldName(battle.FieldIndex)+" · ":"")+(ExpeditionBattle?"온기 "+battle.HP+" · ":"")+battle.Gold+"G · "+battle.Wave+"/"+battle.WaveCount+" · 적 "+battle.Enemies.Count+"/100";
   float available=Screen.width-204;while(hud.fontSize>9&&MeasureText(hud,info).x>available)hud.fontSize--;GUI.Label(new Rect(8,9,available,26),info,hud);hud.fontSize=old;hud.wordWrap=wrap;
   Rect middle=new Rect(Screen.width-184,4,72,36),menu=new Rect(Screen.width-104,4,96,36);int prior=button.fontSize;button.fontSize=13;
   if(ExpeditionBattle){ExpeditionRect("entry",middle);if(Button(middle,"원정 상태"))OpenExpeditionMenu();}
   else if(battle.Phase==Phase.Preparation&&battle.Wave==0){ExpeditionRect("entry",middle);if(Button(middle,RunActive?"원정 계속":"불씨 원정",battle.Units.Count==0)){if(RunActive){if(Choosing(Run))OpenExpeditionMenu();else OpenExpeditionBattle();}else if(ExpeditionGradeOpen>0)OpenExpeditionMenu();else StartExpedition();}} // like ExpeditionEntry: once a grade is open, the panel picks it first (Codex repro r47)
   else if(battle.Phase==Phase.Lost&&(checkpoint!=null||waveCheckpoint!=null)){button.fontSize=11;if(Button(middle,"웨이브복원"))RetryCheckpoint();}else if(Button(middle,SimulationSpeed+"배속",SpeedAvailable()))CycleSpeed();
   if(Button(menu,"메뉴 (Esc)"))OpenTools();button.fontSize=prior;
   if(Debug.isDebugBuild&&Application.platform==RuntimePlatform.WebGLPlayer&&Application.absoluteURL.Contains("landscape-review=1")&&Event.current.type==EventType.Repaint&&debugRibbonLogged.Add("compact|"+Screen.width+"x"+Screen.height))Debug.Log("COMPACT_BARS|"+HeaderHeight+"|"+FooterHeight+"|"+scale+"|"+ox+"|"+oy);
  }
  void CompactControls(){
   bool ended=battle.Phase==Phase.Won||battle.Phase==Phase.Lost,prep=battle.Phase==Phase.Preparation;float y=Screen.height-FooterHeight;UiPlate(new Rect(2,y,Screen.width-4,FooterHeight-2));float width=Mathf.Min(300,Screen.width-24);Rect r=new Rect(Screen.width-width-12,y+2,width,40);
   bool next=ended&&!ExpeditionBattle&&battle.Phase==Phase.Won&&battle.FieldIndex<Balance.Fields.Length-1;
   string action=ended?(ExpeditionBattle?"원정 화면":next?"다음 전장":"재도전"):prep?"전투 시작":battle.Decided?(quickClear?"원래 속도":"빠른 소탕"):EarlyAvailable()?EarlyLabel():"다음 호출 대기";
   if(PrimaryButton(r,action,ended||(prep?battle.Units.Exists(u=>u.Kind!=4):battle.Decided?!paused:EarlyAvailable()))){if(ended&&ExpeditionBattle)OpenExpeditionMenu();else if(next)OpenField(battle.FieldIndex+1);else if(ended)ResetRun();else if(prep)StartWave();else if(battle.Decided)ToggleQuickClear();else EarlyAction();}
  }
 }
}
