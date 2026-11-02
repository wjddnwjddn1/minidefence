using System;
using System.IO;
using UnityEngine;
namespace EmberBastion.Editor {
 public static class SaveCompatibilityCheck {
  public static void Run(){
   var battle=new Battle(); if(!battle.Place(1,0))throw new Exception("Fixture placement failed");
   string json=JsonUtility.ToJson(battle.SavePreparation());
   string oldJson=json.Replace(",\"hitCount\":0","");
   if(oldJson==json||oldJson.Contains("hitCount"))throw new Exception("Legacy fixture construction failed");
   var oldData=JsonUtility.FromJson<PreparationData>(oldJson);
   var restored=Battle.RestorePreparation(oldData);
   if(restored.At(0)==null||restored.At(0).Kind!=1||restored.At(0).HitCount!=0||restored.Gold!=115)throw new Exception("Legacy preparation failed");
   var frost=new Battle();if(!frost.Place(3,0)||!frost.SetPriority(0,2))throw new Exception("Frost fixture failed");frost.At(0).HitCount=4;
   var frostRestored=Battle.RestorePreparation(JsonUtility.FromJson<PreparationData>(JsonUtility.ToJson(frost.SavePreparation())));
   if(frostRestored.At(0)==null||frostRestored.At(0).HitCount!=4||frostRestored.At(0).Priority!=2||frostRestored.Gold!=110)throw new Exception("Frost round trip failed");
   File.WriteAllText("Docs/save-compatibility.txt","PASS legacy version-1 JSON without hitCount restores an archer and 115G\nPASS frost hit count and target priority round trip; no PlayerPrefs used\n",new System.Text.UTF8Encoding(false));
  }
 }
}
