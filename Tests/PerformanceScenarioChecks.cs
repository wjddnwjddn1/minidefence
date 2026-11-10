using System;
using EmberBastion;
class PerformanceScenarioChecks {
 static int checks;
 static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
 static int Main(){try{
  foreach(int density in new[]{80,85,95,99}){
   var b=PerformanceScenario.Capture(density);Check(b.Phase==Phase.Fighting&&b.RulesVersion==4&&b.Mode==BattleMode.Normal,"live normal candidate");
   Check(b.Enemies.Count>=density&&b.Enemies.Count<=99&&b.Wave>=9,"actual high density");Check(b.Kills>0&&b.Time>0&&b.Gold>=0,"natural economy and clock");
   Check(b.Units.Count==6,"six-unit mixed lineup");for(int k=0;k<5;k++)Check(b.Units.Exists(u=>u.Kind==k),"every unit kind represented");
   var a=b.CopyCombat();var restored=Battle.ImportCombat(b.ExportCombat());
   for(int i=0;i<120;i++){b.Step(1.0/60);a.Step(1.0/60);restored.Step(1.0/60);Check(b.Phase==a.Phase&&a.Phase==restored.Phase&&b.Gold==a.Gold&&a.Gold==restored.Gold&&b.Kills==restored.Kills&&b.Enemies.Count==restored.Enemies.Count,"replay after restore");}
  }
  bool rejected=false;try{PerformanceScenario.Capture(100);}catch(ArgumentOutOfRangeException){rejected=true;}Check(rejected,"reject loss threshold");Console.WriteLine("PASS "+checks+" natural performance snapshot and replay checks");return 0;
 }catch(Exception e){Console.Error.WriteLine(e.Message);return 1;}}
}
