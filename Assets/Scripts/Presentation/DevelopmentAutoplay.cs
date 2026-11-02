#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System.Collections;
using UnityEngine;
namespace EmberBastion {
 // Explicit, isolated Web smoke: no real player save or victory record is written.
 public sealed class DevelopmentAutoplay : MonoBehaviour {
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
  static void Boot(){if(Debug.isDebugBuild&&Application.platform==RuntimePlatform.WebGLPlayer&&Application.absoluteURL.Contains("fx-smoke=autoplay"))new GameObject("Isolated autoplay").AddComponent<DevelopmentAutoplay>();}
  IEnumerator Start(){
   BattleView view=null;while(view==null){view=FindAnyObjectByType<BattleView>();yield return null;}
   var battle=view.SmokeState;view.SimulationSpeed=3;
   while(battle.Phase!=Phase.Won&&battle.Phase!=Phase.Lost){
    if(battle.Phase==Phase.Preparation){
     for(int slot=0;slot<battle.Field.Slots.Length;slot++)if(battle.At(slot)==null){int kind=(slot+1)%4;if(battle.Gold>=Balance.Troops[kind].Cost)battle.Place(kind,slot);}
     foreach(var unit in battle.Units)if(unit.Level==1&&battle.Gold>=50)battle.Upgrade(unit.Slot);
     view.SmokeStart();
     if(battle.Phase==Phase.Preparation){Debug.LogError("AUTOPLAY_STUCK");yield break;}
    }
    if(battle.Phase==Phase.Fighting){for(int slot=0;slot<battle.Field.Slots.Length;slot++)if(battle.At(slot)==null){int kind=(slot+1)%4;if(battle.Gold>=Balance.Troops[kind].Cost)battle.Place(kind,slot);}foreach(var unit in battle.Units)if(unit.Level==1&&battle.Gold>=50)battle.Upgrade(unit.Slot);}
    if(battle.EmberCD<=0&&battle.Enemies.Count>=3)view.SmokeAim(battle.Enemies[battle.Enemies.Count/2].Position);
    yield return null;
   }
   Debug.Log("AUTOPLAY_"+(battle.Phase==Phase.Won?"WIN":"LOSS")+" alive="+battle.Enemies.Count+" wave="+battle.Wave+" kills="+battle.Kills);
  }
 }
}
#endif
