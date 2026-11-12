#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
namespace EmberBastion {
 // Pure, deterministic diagnostic setup. Uses the real economy and waves.
 public static class PerformanceScenario {
  public static Battle CaptureChainBlast(){
   var boons=new int[Boon.BattleCount];
   boons[Boon.ChainBlast]=1;
   // Fixed QA budget for four powder troops. Enemies and chain hits come from the real engine.
   var battle=Battle.CreateExpedition(1,0,100,boons,1,500);
   for(int slot=0;slot<4;slot++)battle.Place(2,slot);
   battle.Start();
   for(int tick=0;tick<60000&&battle.Phase==Phase.Fighting;tick++){
    if(battle.ReinforcementPending)battle.ChooseReinforcement(battle.Reinforcements.Choices()[0]);
    battle.Step(Balance.Tick);
    if(tick%10!=0||battle.Enemies.Count<80||battle.Enemies.Count>95)continue;
    var sample=battle.CopyCombat();
    int chainHits=0,firstChainTick=-1;
    bool sustained=true;
    for(int i=0;i<180;i++){
     sample.Step(Balance.Tick);
     if(sample.Phase!=Phase.Fighting||sample.Enemies.Count<80||sample.Enemies.Count>99||sample.ReinforcementPending){sustained=false;break;}
     foreach(var cue in sample.Cues)if(cue.Kind=="chain"){if(firstChainTick<0)firstChainTick=i;chainHits++;}
    }
    if(sustained&&chainHits>=2&&firstChainTick>=0&&firstChainTick<=18)return battle.CopyCombat();
   }
   throw new InvalidOperationException("No dense chain blast scenario found");
  }
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
