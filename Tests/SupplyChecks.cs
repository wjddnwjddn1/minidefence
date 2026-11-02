using System;
using EmberBastion;
class SupplyChecks {
 static int checks;
 static void Check(bool ok,string reason){checks++;if(!ok)throw new Exception(reason);}
 static void Main(){foreach(int field in new[]{0,1,2})foreach(int amount in new[]{20,40,60}){
  var battle=new Battle(42,field);Check(!battle.GrantSupply(0)&&!battle.GrantSupply(10)&&!battle.GrantSupply(80),"invalid grant");Check(battle.GrantSupply(amount),"valid grant");Check(battle.Gold==battle.Field.StartGold+amount&&battle.SupplyBonus==amount,"amount");Check(!battle.GrantSupply(amount),"duplicate grant");
  var prep=Battle.RestorePreparation(battle.SavePreparation());Check(prep.Gold==battle.Gold&&prep.SupplyBonus==amount,"preparation roundtrip");Check(!prep.GrantSupply(amount),"restored duplicate");Check(prep.Place(0,0)&&prep.Start(),"start");Check(!prep.GrantSupply(amount),"combat grant");var copy=prep.CopyCombat();Check(copy.SupplyBonus==amount&&copy.Gold==prep.Gold,"checkpoint");var combat=Battle.ImportCombat(prep.ExportCombat());Check(combat.SupplyBonus==amount&&combat.Gold==prep.Gold,"combat roundtrip");Check(!combat.GrantSupply(amount),"combat restored grant");
  var invalid=battle.SavePreparation();invalid.supplyBonus=15;bool failed=false;try{Battle.RestorePreparation(invalid);}catch(ArgumentException){failed=true;}Check(failed,"reject invalid");
  var old=battle.SavePreparation();old.supplyBonus=0;Check(Battle.RestorePreparation(old).SupplyBonus==0,"legacy default");
  var spent=new Battle(1,field);Check(spent.Place(0,0),"place");Check(!spent.GrantSupply(amount),"no grant after placement");
 }Console.WriteLine("PASS "+checks+" supply eligibility, duplicate, legacy and save checks");}
}
