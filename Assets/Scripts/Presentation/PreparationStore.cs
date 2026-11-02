using System;
using UnityEngine;
namespace EmberBastion {
 // Explicit user action only; product/company identity is exclusive to this Unity comparison.
 public static class PreparationStore {
  const string Key="snomet.ember-bastion.unity-comparison.preparation.v1";
  public static bool Exists {get{return PlayerPrefs.HasKey(Key);}}
  public static void Save(Battle battle){string json=JsonUtility.ToJson(battle.SavePreparation());PlayerPrefs.SetString(Key,json);PlayerPrefs.Save();}
  public static Battle Load(){if(!Exists)throw new InvalidOperationException("No preparation saved");var data=JsonUtility.FromJson<PreparationData>(PlayerPrefs.GetString(Key));return Battle.RestorePreparation(data);}
 }
}
