using System;
using System.Reflection;
using EmberBastion;
static class BattleModeChecks {
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);Console.WriteLine("PASS "+name);}
 static void Main(){
  var normal=new Battle(29);
  Check(normal.Mode==BattleMode.Normal,"new battle is normal");
  Check(normal.EnableReinforcements()&&normal.Mode==BattleMode.Reinforcement,"enable changes mode");
  Check(!normal.SetDifficulty(0),"reinforcement difficulty locked");
  Check(Battle.RestorePreparation(normal.SavePreparation()).Mode==BattleMode.Reinforcement,"legacy preparation infers reinforcement mode");
  Check(normal.DisableReinforcements()&&normal.Mode==BattleMode.Normal,"disable restores normal mode");
  Check(Battle.RestorePreparation(normal.SavePreparation()).Mode==BattleMode.Normal,"normal preparation retains mode");
  normal.EnableReinforcements();normal.Place(0,0);normal.Start();
  Check(normal.CopyCombat().Mode==BattleMode.Reinforcement,"retry copy retains reinforcement mode");
  Check(Battle.ImportCombat(normal.ExportCombat()).Mode==BattleMode.Reinforcement,"combat restore infers reinforcement mode");
  // Model a future ordinary draft without opening v4 creation or save support.
  var ordinary=new Battle(29);var baseline=new Battle(29);
  typeof(Battle).GetProperty("Reinforcements").GetSetMethod(true).Invoke(ordinary,new object[]{new ReinforcementDraft(29)});
  Check(ordinary.Mode==BattleMode.Normal&&ordinary.SetDifficulty(1),"draft existence does not lock normal difficulty");
  baseline.SetDifficulty(1);
  foreach(var wave in ordinary.Field.Waves)foreach(var group in wave)
   Check(ordinary.SpawnCount(group)==baseline.SpawnCount(group)&&ordinary.SpawnGap(group)==baseline.SpawnGap(group),"ordinary draft does not add reinforcement density");
  Console.WriteLine("PASS battle mode separation and legacy restoration");
 }
}
