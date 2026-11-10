#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
namespace EmberBastion {
 // Pure, deterministic diagnostic setup. Uses the real economy and waves.
 public static class PerformanceScenario {
  public static Battle Capture(int minimumEnemies=85){
   if(minimumEnemies<80||minimumEnemies>99)throw new ArgumentOutOfRangeException("minimumEnemies");
   var b=Battle.CreateForRules(29,0,4);b.Place(0,0);b.Place(0,10);b.Start();
   int[] kinds={1,2,3,4},slots={1,3,4,2};
   for(int tick=0;tick<60000&&b.Phase==Phase.Fighting;tick++){
    if(b.ReinforcementPending){b.ChooseReinforcement(b.Reinforcements.Choices()[0]);continue;}
    if(tick%60==0){bool placed=false;
     for(int i=0;i<slots.Length;i++)if(b.At(slots[i])==null){if(b.Gold>=Balance.Troops[kinds[i]].Cost){b.Place(kinds[i],slots[i]);placed=true;}break;}
     if(!placed)foreach(var u in b.Units){if(u.Level==1&&b.Gold>=50)b.Upgrade(u.Slot);else if(u.Level==2&&b.Gold>=90)b.UpgradeBranch(u.Slot,u.Kind==0||u.Kind==3||u.Kind==4?2:1);}
    }
    b.Step(1.0/60);b.Cues.Clear();
    if(b.Phase==Phase.Fighting&&b.Wave>=9&&b.Enemies.Count>=minimumEnemies&&b.Enemies.Count<=99)return b.CopyCombat();
   }
   throw new InvalidOperationException("Could not reach the requested natural density");
  }
 }
}
#endif
