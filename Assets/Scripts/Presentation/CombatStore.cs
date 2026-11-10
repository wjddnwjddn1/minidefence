using System;
using UnityEngine;
namespace EmberBastion {
 [Serializable] public sealed class CombatArchive {public int version=1,speed=1;public CombatSaveData current,checkpoint;}
 // Separate from preparation and progress keys. Only changes on an explicit save.
 public static class CombatStore {
  const string Key="snomet.ember-bastion.unity-comparison.combat.v1";
  const string AutoKey="snomet.ember-bastion.unity-comparison.combat.auto.v1";
  public static bool Exists {get{return PlayerPrefs.HasKey(Key);}}
  public static bool AutoExists {get{return PlayerPrefs.HasKey(AutoKey);}}
  public static void DeleteAuto(){PlayerPrefs.DeleteKey(AutoKey);PlayerPrefs.Save();}
  static void ValidatePair(Battle current,Battle checkpoint){if(current==null)throw new ArgumentException("Missing combat");if(checkpoint==null)return;if(checkpoint.Mode!=current.Mode||(checkpoint.Reinforcements==null)!=(current.Reinforcements==null))throw new ArgumentException("Invalid reinforcement checkpoint");if(current.Reinforcements!=null&&checkpoint.Reinforcements.Rerolls>current.Reinforcements.Rerolls)throw new ArgumentException("Invalid checkpoint rerolls");if(current.Reinforcements!=null)for(int i=0;i<ReinforcementDraft.CardCount;i++)if(checkpoint.Reinforcements.Count(i)!=current.Reinforcements.Count(i))throw new ArgumentException("Invalid reinforcement checkpoint cards");if(checkpoint.RulesVersion!=current.RulesVersion||checkpoint.FieldIndex!=current.FieldIndex||checkpoint.Wave!=current.Wave||checkpoint.FastWaves!=current.FastWaves||checkpoint.HardWaves!=current.HardWaves||checkpoint.SupplyBonus!=current.SupplyBonus||checkpoint.Time>current.Time||(checkpoint.EmberBranch!=0&&checkpoint.EmberBranch!=current.EmberBranch))throw new ArgumentException("Invalid checkpoint");}
  public static void Save(Battle current,Battle checkpoint,int speed){SaveAt(Key,current,checkpoint,speed);}
  public static void SaveAuto(Battle current,Battle checkpoint,int speed){SaveAt(AutoKey,current,checkpoint,speed);}
  static void SaveAt(string key,Battle current,Battle checkpoint,int speed){if(speed<1||speed>3)throw new ArgumentException("Invalid speed");ValidatePair(current,checkpoint);var data=new CombatArchive{current=current.ExportCombat(),checkpoint=checkpoint==null?null:checkpoint.ExportCombat(),speed=speed};string json=JsonUtility.ToJson(data);var checkedData=JsonUtility.FromJson<CombatArchive>(json);if(checkedData==null||checkedData.version!=1||checkedData.speed!=speed)throw new ArgumentException("Invalid serialized archive");var verified=Battle.ImportCombat(checkedData.current);var verifiedCheckpoint=CombatSaveData.IsAbsent(checkedData.checkpoint)?null:Battle.ImportCombat(checkedData.checkpoint);ValidatePair(verified,verifiedCheckpoint);PlayerPrefs.SetString(key,json);PlayerPrefs.Save();}
  public static void Load(out Battle current,out Battle checkpoint,out int speed){LoadAt(Key,out current,out checkpoint,out speed);}
  public static void LoadAuto(out Battle current,out Battle checkpoint,out int speed){LoadAt(AutoKey,out current,out checkpoint,out speed);}
  static void LoadAt(string key,out Battle current,out Battle checkpoint,out int speed){if(!PlayerPrefs.HasKey(key))throw new InvalidOperationException("No combat save");var data=JsonUtility.FromJson<CombatArchive>(PlayerPrefs.GetString(key));if(data==null||data.version!=1||data.speed<1||data.speed>3)throw new ArgumentException("Invalid archive");var restored=Battle.ImportCombat(data.current);var restoredCheckpoint=CombatSaveData.IsAbsent(data.checkpoint)?null:Battle.ImportCombat(data.checkpoint);ValidatePair(restored,restoredCheckpoint);current=restored;checkpoint=restoredCheckpoint;speed=data.speed;}
 }
}
