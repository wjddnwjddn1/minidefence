using System;
using System.Collections.Generic;
using EmberBastion;
namespace UnityEngine {
 public static class PlayerPrefs {
  static readonly Dictionary<string,int> data=new Dictionary<string,int>();
  public static int GetInt(string key,int fallback){int value;return data.TryGetValue(key,out value)?value:fallback;}
  public static void SetInt(string key,int value){data[key]=value;}
  public static void Save(){}
 }
}
class ProgressChecks {
 static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
 static int Main(){try{
  Check(!ProgressStore.Cleared(0)&&!ProgressStore.Cleared(0,true),"initial records");
  Check(ProgressStore.Record(0),"normal clear");
  Check(ProgressStore.Cleared(0)&&!ProgressStore.Cleared(0,true),"normal and challenge independent");
  Check(ProgressStore.Record(0,true)&&ProgressStore.Cleared(0,true),"challenge clear");
  Check(!ProgressStore.Record(0)&&!ProgressStore.Record(0,true),"repeat records");
  Check(!ProgressStore.Cleared(1)&&!ProgressStore.Cleared(1,true),"other field independent");
  Check(!ProgressStore.Cleared(0,true,true)&&ProgressStore.Record(0,true,true)&&ProgressStore.Cleared(0,true,true),"hard record independent");
  Check(!ProgressStore.Record(0,true,true),"hard repeat record");
  Check(!ProgressStore.BestPeak(0).HasValue,"no initial best");
  Check(ProgressStore.RecordBest(0,false,false,60)&&ProgressStore.BestPeak(0)==60,"first best");
  Check(!ProgressStore.RecordBest(0,false,false,80)&&!ProgressStore.RecordBest(0,false,false,60)&&ProgressStore.BestPeak(0)==60,"worse and tied records ignored");
  Check(ProgressStore.RecordBest(0,false,false,42)&&ProgressStore.BestPeak(0)==42,"better peak saved");
  Check(!ProgressStore.BestPeak(0,true).HasValue&&!ProgressStore.BestPeak(0,true,true).HasValue,"mode best independent");
  UnityEngine.PlayerPrefs.SetInt("snomet.ember-bastion.unity-comparison.best-peak.v2.fast.0.0",51);
  Check(ProgressStore.PreviousFastBestPeak(0)==50&&!ProgressStore.BestPeak(0,true).HasValue,"old challenge record kept separate");
  Check(ProgressStore.RecordBest(0,true,false,80)&&ProgressStore.BestPeak(0,true)==80&&ProgressStore.PreviousFastBestPeak(0)==50,"new challenge record keeps previous v2");
  Check(!ProgressStore.RecordBest(0,true,false,80)&&ProgressStore.RecordBest(0,true,true,95)&&ProgressStore.BestPeak(0)==42&&ProgressStore.BestPeak(0,true)==80&&ProgressStore.BestPeak(0,true,true)==95,"separate best records");
  UnityEngine.PlayerPrefs.SetInt("snomet.ember-bastion.unity-comparison.best-peak.v1.normal.0",36);
  Check(ProgressStore.LegacyBestPeak(0)==35&&ProgressStore.BestPeak(0)==42,"legacy kept separate");
  Check(!ProgressStore.BestPeak(0,false,false,20).HasValue&&ProgressStore.RecordBest(0,false,false,50,20),"supply record created");
  Check(ProgressStore.RecordBest(0,false,false,30,40)&&ProgressStore.BestPeak(0,false,false,20)==50&&ProgressStore.BestPeak(0,false,false,40)==30&&ProgressStore.BestPeak(0)==42,"supply tiers independent");
  Check(ProgressStore.LegacyBestPeak(0)==35,"legacy remains untouched");
  ProgressStore.RecordBadges(0,true,false,7);ProgressStore.RecordBadges(0,true,false,7);
  Check(ProgressStore.HasBadge(0,true,false,2)&&ProgressStore.HasBadge(0,true,false,4),"badges persist idempotently");
  Check(!ProgressStore.HasBadge(0,false,false,2)&&!ProgressStore.HasBadge(1,true,false,4)&&!ProgressStore.HasBadge(0,true,true,2),"badges independent by field and difficulty");
  for(int mode=0;mode<3;mode++){
   bool fast=mode>0,hard=mode==2;int field=2;
   Check(ProgressStore.RecordBest(field,fast,hard,60,20,1),"v1 best set");
   Check(!ProgressStore.BestPeak(field,fast,hard,20,2).HasValue,"v2 starts separate");
   Check(ProgressStore.RecordBest(field,fast,hard,70,20,2),"v2 best set");
   Check(ProgressStore.BestPeak(field,fast,hard,20,1)==60&&ProgressStore.BestPeak(field,fast,hard,20,2)==70,"versioned best preserved");
   ProgressStore.RecordBadges(field,fast,hard,3,1);Check(!ProgressStore.HasBadge(field,fast,hard,2,2),"v2 badge separate");
   ProgressStore.RecordBadges(field,fast,hard,5,2);Check(ProgressStore.HasBadge(field,fast,hard,2,1)&&!ProgressStore.HasBadge(field,fast,hard,4,1)&&ProgressStore.HasBadge(field,fast,hard,4,2),"badges preserve versions");
  }
  Check(ProgressStore.Cleared(0)&&!ProgressStore.ClearedForRules(0,false,false,2),"global unlock does not imply v2 win");
  Check(ProgressStore.RecordClearForRules(0,false,false,2)&&!ProgressStore.RecordClearForRules(0,false,false,2)&&ProgressStore.Cleared(0),"v2 clear and global unlock distinct");
  Check(ProgressStore.RecordReinforcementBest(2,55,0,1)&&!ProgressStore.ReinforcementBest(2,0,2).HasValue,"reinforcement v2 separate");
  Check(ProgressStore.RecordReinforcementBest(2,70,0,2)&&ProgressStore.ReinforcementBest(2,0,1)==55&&ProgressStore.ReinforcementBest(2,0,2)==70,"reinforcement records preserved");
  bool invalidSupply=false;try{ProgressStore.RecordBest(0,false,false,20,30);}catch(ArgumentException){invalidSupply=true;}Check(invalidSupply,"invalid supply rejected");
  bool invalidPeak=false;try{ProgressStore.RecordBest(0,false,false,100);}catch(ArgumentException){invalidPeak=true;}Check(invalidPeak,"invalid peak rejected");

  bool rejected=false;try{ProgressStore.Record(-1,true);}catch(ArgumentException){rejected=true;}Check(rejected,"invalid field rejected");
  Console.WriteLine("PASS difficulty, supply and legacy peak records are independent and validated");return 0;
 }catch(Exception e){Console.Error.WriteLine("FAIL "+e.Message);return 1;}}
}
