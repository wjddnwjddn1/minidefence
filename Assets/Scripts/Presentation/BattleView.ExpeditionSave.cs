using System;
using UnityEngine;
namespace EmberBastion {
 public sealed partial class BattleView {
  const string ExpeditionSessionKey="snomet.ember-bastion.unity-comparison.expedition.session.v1";
  Battle expeditionSavedBattle;int expeditionSavedSpeed=1;float expeditionSaveAt;bool expeditionSaveFailed;
  static Battle DecodeRunCombat(string json){return Battle.ImportCombat(JsonUtility.FromJson<CombatSaveData>(json));}
  static Battle DecodeRunPreparation(string json){return Battle.RestorePreparation(JsonUtility.FromJson<PreparationData>(json));}
  void LoadExpedition(){
   if(expeditionLoaded)return;expeditionLoaded=true;if(!PreferencesAllowed())return;
   ExpeditionSessionData data=null;
   try{
    if(PlayerPrefs.HasKey(ExpeditionSessionKey)){
     data=JsonUtility.FromJson<ExpeditionSessionData>(PlayerPrefs.GetString(ExpeditionSessionKey));
     var restored=ExpeditionSession.Restore(data,DecodeRunCombat,DecodeRunPreparation);expedition=restored.Run;expeditionSavedBattle=restored.Battle;expeditionSavedSpeed=restored.Speed;campDoctrineCounts=data.campDoctrineCounts;
    }else if(PlayerPrefs.HasKey(ExpeditionKey)){
     expedition=Expedition.Restore(JsonUtility.FromJson<ExpeditionData>(PlayerPrefs.GetString(ExpeditionKey)));
     // Old run-only saves cannot prove whether this stage was already started. Never grant a fresh retry.
     if(expedition.State==Expedition.Status.Battle){expedition.Abandon();notice="이전 원정은 전투 저장이 없어 종료 처리했습니다.";RecordExpeditionBest(expedition);}
     SaveExpedition();
    }
   }catch(Exception){
    expeditionSavedBattle=null;
    try{expedition=data==null?null:Expedition.Restore(data.run);if(expedition!=null&&expedition.State==Expedition.Status.Battle){expedition.Abandon();RecordExpeditionBest(expedition);SaveExpedition();}}catch(Exception){expedition=null;}
    notice="원정 저장이 손상되어 전장을 처음부터 다시 시작하지 않습니다.";
   }
  }
  bool RefreshExpeditionCheckpoint(){
   if(expeditionSavedBattle!=null||!PreferencesAllowed()||!PlayerPrefs.HasKey(ExpeditionSessionKey))return true;
   try{var data=JsonUtility.FromJson<ExpeditionSessionData>(PlayerPrefs.GetString(ExpeditionSessionKey));
   if(data==null||data.run==null||data.run.seed!=expedition.Seed||data.run.stage!=expedition.Stage||data.run.state!=(int)Expedition.Status.Battle)return true;
   var saved=ExpeditionSession.Restore(data,DecodeRunCombat,DecodeRunPreparation);expedition=saved.Run;expeditionSavedBattle=saved.Battle;expeditionSavedSpeed=saved.Speed;return true;}
   catch(Exception){expedition.Abandon();RecordExpeditionBest(expedition);SaveExpedition();OpenExpeditionMenu();notice="전투 저장이 손상되어 원정을 종료했습니다.";return false;}
  }
  bool SaveExpedition(){
   if(!PreferencesAllowed())return true;
   try{
    if(expedition==null){PlayerPrefs.DeleteKey(ExpeditionSessionKey);PlayerPrefs.DeleteKey(ExpeditionKey);}
    else{
     if(expedition.State==Expedition.Status.Battle&&!ExpeditionBattle)return true;
     Battle current=ExpeditionBattle&&expedition.Matches(battle)?battle:expeditionSavedBattle;
     if(current!=null&&expedition.State==Expedition.Status.Battle&&(current.Phase==Phase.Won||current.Phase==Phase.Lost)){expedition.Complete(current);if(expedition.State==Expedition.Status.Camp)CaptureCampComposition(current);RecordExpeditionBest(expedition);}
     if(expedition.State==Expedition.Status.Battle&&current==null)return true; // A free battle never overwrites the saved expedition checkpoint.
     var data=ExpeditionSession.Capture(expedition,current,Mathf.Clamp((int)SimulationSpeed,1,3),b=>JsonUtility.ToJson(b.ExportCombat()),b=>JsonUtility.ToJson(b.SavePreparation()));
     if(expedition.State==Expedition.Status.Camp)data.campDoctrineCounts=campDoctrineCounts;
     string json=JsonUtility.ToJson(data);ExpeditionSession.Restore(JsonUtility.FromJson<ExpeditionSessionData>(json),DecodeRunCombat,DecodeRunPreparation);
     PlayerPrefs.SetString(ExpeditionSessionKey,json);PlayerPrefs.DeleteKey(ExpeditionKey);
    }
    PlayerPrefs.Save();expeditionSaveFailed=false;return true;
   }catch(Exception){expeditionSaveFailed=true;notice="원정 저장 실패 · 메뉴의 저장을 다시 시도하세요.";return false;}
  }
  void OnApplicationQuit(){if(expeditionLoaded)SaveExpedition();}
  void ExpeditionHeader(float width){
   int old=hud.fontSize;hud.fontSize=Screen.width<700?13:15;string line="원정 "+(Run.Stage+1)+"/"+Run.Stages+" · 온기 "+battle.HP+" · "+battle.Gold+"G · "+battle.Wave+"/"+battle.WaveCount+" · 적 "+battle.Enemies.Count+"/100"+(expeditionSaveFailed?" · 저장 실패":"");while(hud.fontSize>9&&MeasureText(hud,line).x>width-32)hud.fontSize--;GUI.Label(new Rect(16,40,width-32,24),line,hud);if(ExpeditionReview&&Event.current.type==EventType.Repaint&&debugRibbonLogged.Add("run-hud|"+battle.HP))Debug.Log("RUN_HUD|"+battle.HP+"|"+Screen.width);hud.fontSize=old;
  }
 }
}
