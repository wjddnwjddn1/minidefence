using System;
using EmberBastion;
class RuntimeCombatComparisonChecks {
 static int checks;
 static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
 static int Main(){try{
  var b=PerformanceScenario.Capture();var original=b.ExportCombat();double error;
  Check(RuntimeCombatComparison.Equivalent(original,b.CopyCombat().ExportCombat(),out error)&&error==0,"exact state");
  var noise=b.ExportCombat();noise.enemies[0].Position.X+=1e-12;Check(RuntimeCombatComparison.Equivalent(original,noise,out error)&&error>0,"native JSON last-digit noise accepted and measured");
  var damage=b.ExportCombat();damage.enemies[0].HP+=1e-6;Check(!RuntimeCombatComparison.Equivalent(original,damage,out error),"real HP difference rejected");
  var discrete=b.ExportCombat();discrete.gold++;Check(!RuntimeCombatComparison.Equivalent(original,discrete,out error),"one gold difference rejected");
  var rng=b.ExportCombat();rng.rng++;Check(!RuntimeCombatComparison.Equivalent(original,rng,out error),"RNG exact");
  var cards=b.ExportCombat();cards.reinforcement.owned[0]++;Check(!RuntimeCombatComparison.Equivalent(original,cards,out error),"card count exact");
  var missing=b.ExportCombat();missing.enemies=new Enemy[0];Check(!RuntimeCombatComparison.Equivalent(original,missing,out error),"missing enemy rejected");
  var nan=b.ExportCombat();nan.enemies[0].HP=double.NaN;Check(!RuntimeCombatComparison.Equivalent(nan,nan,out error),"NaN never accepted");
  var infinite=b.ExportCombat();infinite.time=double.PositiveInfinity;Check(!RuntimeCombatComparison.Equivalent(infinite,infinite,out error),"infinity never accepted");
  Console.WriteLine("PASS "+checks+" native combat comparison boundary checks");return 0;
 }catch(Exception e){Console.Error.WriteLine(e.Message);return 1;}}
}
