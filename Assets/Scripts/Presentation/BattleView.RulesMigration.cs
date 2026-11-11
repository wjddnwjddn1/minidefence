#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
namespace EmberBastion {
 public sealed partial class BattleView {
  bool ReviewTutorial {get{return Debug.isDebugBuild&&Application.platform==RuntimePlatform.WebGLPlayer&&Application.absoluteURL.Contains("review-tutorial=1");}}
  internal void MigrationLoadPrep(){LoadPrep();titleMenu=false;tutorialStage=0;}
  internal bool MigrationLoadCombat(){bool ok=LoadCombat(false);titleMenu=false;tutorialStage=0;return ok;}
  internal void MigrationPreference(){PlayerPrefs.SetInt(PreferenceKey,4);PlayerPrefs.Save();RestorePreference();}
 }
 public sealed class RulesMigrationDiagnostic : MonoBehaviour {
  [Serializable] sealed class Evidence {public int expectedFresh,initialFresh,resetFresh,preferredFresh,preparationRules,combatRules,preferredField;public bool prepWon,combatWon,pausedLoad;public string product,unity,limitations;}
  const string Product="EmberBastionRulesMigrationQA";
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){if(Debug.isDebugBuild&&Application.platform==RuntimePlatform.WindowsPlayer&&Application.productName==Product&&Array.IndexOf(Environment.GetCommandLineArgs(),"--rules-migration-qa")>=0)new GameObject("Isolated rules migration diagnostic").AddComponent<RulesMigrationDiagnostic>();}
  string folder;
  static Battle PreparedClassic(){var b=Battle.CreateForRules(29,0,3);b.Gold=20000;for(int slot=0;slot<b.Field.Slots.Length;slot++){if(!b.Place(slot%2==0?2:1,slot)||!b.Upgrade(slot)||!b.UpgradeBranch(slot,1))throw new Exception("Cannot prepare migration army");}return b;}
  static bool Finish(Battle b){if(b.Phase==Phase.Preparation&&!b.Start())throw new Exception("Migration start failed");for(int i=0;i<10000&&b.Phase==Phase.Fighting;i++){if(b.ReinforcementPending)b.ChooseReinforcement(b.Reinforcements.Choices()[0]);b.Step(.25);if(b.RulesVersion!=3)throw new Exception("Rules changed during migration playback");}return b.Phase==Phase.Won;}
  IEnumerator Start(){yield return null;folder=Path.Combine(Path.GetDirectoryName(Application.dataPath),"Evidence",DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));Directory.CreateDirectory(folder);var view=FindAnyObjectByType<BattleView>();var data=new Evidence{expectedFresh=Array.IndexOf(Environment.GetCommandLineArgs(),"--classic-rules")>=0?3:4,product=Application.productName,unity=Application.unityVersion,limitations="Isolated actual PlayerPrefs and view load paths. Boosted fixture army (20000G) verifies version continuity to victory, not natural economy or human play."};
   try{data.initialFresh=view.SmokeState.RulesVersion;var prep=PreparedClassic();PreparationStore.Save(prep);File.WriteAllText(Path.Combine(folder,"classic-preparation.json"),JsonUtility.ToJson(prep.SavePreparation()),new UTF8Encoding(false));view.MigrationLoadPrep();data.preparationRules=view.SmokeState.RulesVersion;data.prepWon=Finish(view.SmokeState);
    var combat=PreparedClassic();combat.Start();combat.Step(.25);CombatStore.Save(combat,combat.CopyCombat(),1);File.WriteAllText(Path.Combine(folder,"classic-combat.json"),JsonUtility.ToJson(combat.ExportCombat()),new UTF8Encoding(false));if(!view.MigrationLoadCombat())throw new Exception("Real manual combat load failed");data.combatRules=view.SmokeState.RulesVersion;
   }catch(Exception e){Fail(e.Message);yield break;}
   double stopped=view.SmokeState.Time;yield return null;yield return null;data.pausedLoad=view.SmokePaused&&view.SmokeState.Time==stopped;
   try{data.combatWon=Finish(view.SmokeState);view.SmokeRetry();data.resetFresh=view.SmokeState.RulesVersion;view.MigrationPreference();data.preferredFresh=view.SmokeState.RulesVersion;data.preferredField=view.SmokeState.FieldIndex;
    File.WriteAllText(Path.Combine(folder,"result.json"),JsonUtility.ToJson(data,true),new UTF8Encoding(false));if(data.initialFresh!=data.expectedFresh||data.resetFresh!=data.expectedFresh||data.preferredFresh!=data.expectedFresh||data.preferredField!=1||data.preparationRules!=3||data.combatRules!=3||!data.prepWon||!data.combatWon||!data.pausedLoad)throw new Exception("Rules migration acceptance failed");
    Cleanup();File.WriteAllText(Path.Combine(folder,"PASS.txt"),"PASS actual preparation/combat PlayerPrefs, classic continuation to victory, new/default/classic and preference rules.",new UTF8Encoding(false));Application.Quit(0);
   }catch(Exception e){Fail(e.Message);}
  }
  static void Cleanup(){PlayerPrefs.DeleteAll();PlayerPrefs.Save();}
  void Fail(string message){if(folder!=null)File.WriteAllText(Path.Combine(folder,"failure.txt"),message,new UTF8Encoding(false));Debug.LogError(message);Cleanup();Application.Quit(1);}
 }
}
#else
namespace EmberBastion {public sealed partial class BattleView {bool ReviewTutorial {get{return false;}}}}
#endif
