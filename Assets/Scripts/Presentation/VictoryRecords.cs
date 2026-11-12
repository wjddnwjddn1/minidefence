using System;
namespace EmberBastion {
 public sealed class VictoryRecordResult {public bool NewBest,NewRecord,NewSupply,NewReinforcement;}
 public static class VictoryRecords {
  public static VictoryRecordResult Apply(Battle b){
   if(b==null||b.Phase!=Phase.Won)throw new ArgumentException("Battle has not won");
   var r=new VictoryRecordResult();
   // A run's battles are recorded by the run, not as free-battle clears, badges or bests.
   if(b.Mode==BattleMode.Expedition)return r;
   if(b.Mode==BattleMode.Reinforcement){r.NewBest=ProgressStore.RecordReinforcementBest(b.FieldIndex,b.PeakAlive,b.SupplyBonus,b.RulesVersion);return r;}
   bool unlockedBefore=false;for(int f=0;f<Balance.Fields.Length;f++)if(ProgressStore.Cleared(f))unlockedBefore=true;
   bool first=!ProgressStore.Cleared(b.FieldIndex)&&!ProgressStore.Cleared(b.FieldIndex,true)&&!ProgressStore.Cleared(b.FieldIndex,true,true);
   ProgressStore.SnapshotPreviousClear(b.FieldIndex,b.FastWaves,b.HardWaves);
   bool newUnlock=ProgressStore.Record(b.FieldIndex,b.FastWaves,b.HardWaves);
   r.NewRecord=ProgressStore.RecordClearForRules(b.FieldIndex,b.FastWaves,b.HardWaves,b.RulesVersion);
   r.NewReinforcement=!b.FastWaves&&!unlockedBefore&&newUnlock;r.NewSupply=first&&newUnlock;
   if(b.RulesVersion>0||b.HardWaves){ProgressStore.RecordBadges(b.FieldIndex,b.FastWaves,b.HardWaves,BadgeRules.Evaluate(b),b.RulesVersion);r.NewBest=ProgressStore.RecordBest(b.FieldIndex,b.FastWaves,b.HardWaves,b.PeakAlive,b.SupplyBonus,b.RulesVersion);}
   return r;
  }
 }

}
