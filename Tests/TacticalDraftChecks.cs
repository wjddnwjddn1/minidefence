using System;
using EmberBastion;
static class TacticalDraftChecks {
 static int checks;
 static void Check(bool condition,string message){checks++;if(!condition)throw new Exception(message);}
 static string State(Battle b){var d=b.ExportCombat();var c=d.reinforcement;return d.gold+":"+d.rng+":"+c.randomState+":"+c.rerolls+":"+c.offerRerolls+":"+string.Join(",",c.offered)+":"+string.Join(",",c.owned);}
 static void Reject(Action action){bool rejected=false;try{action();}catch(ArgumentException){rejected=true;}Check(rejected,"corrupt save rejected");}
 static void Advance(Battle b){for(int i=0;i<10000&&!b.ReinforcementPending&&b.Phase==Phase.Fighting;i++){b.Enemies.Clear();b.Step(.25);}Check(b.ReinforcementPending,"card boundary reached");}
 static void Main(){
  for(int field=0;field<4;field++)for(int mode=0;mode<3;mode++){
   var b=Battle.CreateForRules(29,field,4);b.SetDifficulty(mode);b.Gold=10000;b.Place(0,0);b.Place(1,1);b.Place(2,2);b.Start();
   int total=0;
   for(int stage=0;stage<3;stage++){
    Advance(b);Check(b.Wave==2+stage*3,"three scheduled choices");
    var saved=b.ExportCombat();var copy=Battle.ImportCombat(saved);Check(State(copy)==State(b),"pending round trip");
    var before=b.ExportCombat();b.Gold=0;string zero=State(b);Check(!b.RerollReinforcement()&&zero==State(b),"no money changes nothing");b.Gold=before.gold;
    for(int roll=0;roll<2;roll++){
     int cost=b.RerollCost,gold=b.Gold;var old=b.ExportCombat();var offered=b.Reinforcements.Choices();
     bool changed=b.RerollReinforcement();Check(changed,"mixed lineup can reroll");total++;
     Check(cost==60+30*(total-1)&&b.Gold==gold-cost,"cumulative price and single payment");
     Check(b.ExportCombat().rng==old.rng,"combat RNG isolated");
     var next=b.Reinforcements.Choices();Array.Sort(offered);Array.Sort(next);Check(string.Join(",",offered)!=string.Join(",",next),"reroll changes set");
     var restored=Battle.ImportCombat(b.ExportCombat());Check(State(restored)==State(b),"reroll round trip");
    }
    string capped=State(b);Check(!b.RerollReinforcement()&&State(b)==capped,"two rerolls cap unchanged");
    var broken=b.ExportCombat();broken.reinforcement.rerolls=7;Reject(()=>Battle.ImportCombat(broken));
    broken=b.ExportCombat();broken.reinforcement.offerRerolls=3;Reject(()=>Battle.ImportCombat(broken));
    var future=Battle.ImportCombat(b.ExportCombat());int pick=b.Reinforcements.Choices()[0];b.ChooseReinforcement(pick);future.ChooseReinforcement(pick);
    Check(b.Reinforcements.OfferRerolls==0&&b.Reinforcements.Rerolls==total,"choice resets local count only");
    Check(State(b.CopyCombat())==State(b)&&State(future)==State(b),"post-choice checkpoint and restored path agree");
   }
  }
  var frost=Battle.CreateForRules(41,0,4);frost.Gold=1000;frost.Place(3,0);frost.Start();Advance(frost);string same=State(frost);Check(!frost.RerollReinforcement()&&State(frost)==same,"three-card pool cannot charge for a permutation");
  var oldBattle=new Battle(29);oldBattle.EnableReinforcements();oldBattle.Place(0,0);oldBattle.Start();Advance(oldBattle);same=State(oldBattle);Check(!oldBattle.RerollReinforcement()&&State(oldBattle)==same,"legacy reinforcement cannot reroll");
  Console.WriteLine("PASS "+checks+" tactical draft, reroll, corruption and deterministic save checks");
 }
}
