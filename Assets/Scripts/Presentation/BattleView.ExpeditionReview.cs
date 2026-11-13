using System;
using UnityEngine;
namespace EmberBastion {
 public sealed partial class BattleView {
  bool ExpeditionReview {get{return Debug.isDebugBuild&&(Application.platform==RuntimePlatform.WebGLPlayer&&Application.absoluteURL.Contains("run-review=1")||Application.productName=="EmberBastionExpeditionQA");}}
  void ExpeditionRect(string name,Rect rect){if(!ExpeditionReview||Event.current.type!=EventType.Repaint)return;string data=name+"|"+rect.x+","+rect.y+","+rect.width+","+rect.height;if(debugRibbonLogged.Add("run-rect|"+data))Debug.Log("RUN_RECT|"+data);if(rect.x<0||rect.y<0||rect.xMax>Screen.width+.1f||rect.yMax>Screen.height+.1f)Debug.LogError("RUN_RECT_OUTSIDE|"+name);}
  void ExpeditionFit(Rect area,string text){if(ExpeditionReview&&Event.current.type==EventType.Repaint&&hud.CalcHeight(new GUIContent(text),area.width)>area.height+.5f)Debug.LogError("RUN_TEXT_OVERFLOW|"+text);}
  bool HandleExpeditionReview(Event e){if(!ExpeditionReview||e.type!=EventType.KeyDown)return false;if(e.keyCode==KeyCode.F6){paused=true;SaveExpedition();ExpeditionLog();}else if(e.keyCode==KeyCode.F7){battle.HP=Math.Max(1,battle.HP-15);paused=true;SaveExpedition();ExpeditionLog();}else if(e.keyCode==KeyCode.F9){var d=JsonUtility.FromJson<ExpeditionSessionData>(PlayerPrefs.GetString(ExpeditionSessionKey));d.started=true;d.combat="broken";d.preparation=null;PlayerPrefs.SetString(ExpeditionSessionKey,JsonUtility.ToJson(d));PlayerPrefs.Save();}else return false;e.Use();return true;}
#if DEVELOPMENT_BUILD || UNITY_EDITOR
  internal Expedition ReviewRun {get{return expedition;}}
  internal void ReviewRunStart(uint seed){PlayerPrefs.DeleteAll();PlayerPrefs.Save();expeditionLoaded=true;StartExpedition(seed);titleMenu=false;SimulationSpeed=0;tutorialStage=0;}
  internal void ReviewRunStartWave(){StartWave();SimulationSpeed=0;}
  internal void ReviewRunSave(){if(!SaveExpedition())throw new Exception("Run persistence failed");}
  internal void ReviewRunReload(){expedition=null;expeditionSavedBattle=null;expeditionLoaded=false;LoadExpedition();if(expedition==null||expedition.State!=Expedition.Status.Battle)throw new Exception("No resumable run");OpenExpeditionBattle();SimulationSpeed=0;titleMenu=false;}
  internal void ReviewRunFreeReturn(){ReviewRunSave();OpenField(0);OpenExpeditionBattle();SimulationSpeed=0;}
  internal void ReviewRunComplete(){UpdateExpedition();}
  internal void ReviewRunChoose(int boon,int field){if(!expedition.Choose(boon,field))throw new Exception("Camp choice failed");OpenExpeditionBattle();SimulationSpeed=0;}
  internal void ReviewRunAbandon(){AbandonExpedition();}
  internal void ReviewPreDoctrineSaves(){
   var old=Expedition.Start(29).Export();
   old.boons=new int[9];
   foreach(string key in new[]{ExpeditionSessionKey,ExpeditionKey}){
    PlayerPrefs.DeleteKey(ExpeditionSessionKey);
    PlayerPrefs.DeleteKey(ExpeditionKey);
    string payload=key==ExpeditionSessionKey?JsonUtility.ToJson(new ExpeditionSessionData{run=old}):JsonUtility.ToJson(old);
    PlayerPrefs.SetString(key,payload);PlayerPrefs.Save();
    expedition=null;expeditionSavedBattle=null;expeditionLoaded=false;
    LoadExpedition();
    if(expedition!=null||PlayerPrefs.HasKey(key)||!notice.Contains("교리 추가 전"))throw new Exception("Pre-doctrine expedition was not cleared: "+key);
    expeditionLoaded=false;LoadExpedition();
    if(expedition!=null)throw new Exception("Pre-doctrine run returned after a second load");
   }
   StartExpedition(30);
   if(expedition==null||expedition.State!=Expedition.Status.Battle||expedition.Seed!=30)throw new Exception("New run unavailable after pre-doctrine cleanup");
  }
  internal void ReviewRunForget(){expedition=null;expeditionSavedBattle=null;expeditionLoaded=false;PlayerPrefs.DeleteAll();PlayerPrefs.Save();}
#endif
 }
}
