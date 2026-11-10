using System;using EmberBastion;
class P{static void C(bool ok,string m){Console.WriteLine((ok?"PASS ":"FAIL ")+m);}
static void Main(){
 var b=Battle.CreateForRules(5,2,4);C(b.Field.Slots.Length==10,"v4 normal pass has 10 slots");
 C(Battle.CreateForRules(5,2,3).Field.Slots.Length==8,"v3 pass keeps 8");
 b.Gold=5000;C(b.Place(1,9)&&b.Place(0,8),"place in new slots 8,9");
 var r=Battle.RestorePreparation(b.SavePreparation());C(r.At(9)!=null&&r.At(8)!=null&&r.Field.Slots.Length==10,"prep save keeps slots 8,9");
 C(!r.EnableReinforcements(),"reinforcement refused with new slots occupied");
 C(r.Start(),"start");for(int i=0;i<60*20;i++)r.Step(1.0/60);
 var c=Battle.ImportCombat(r.ExportCombat());C(c.At(9)!=null&&c.Field.Slots.Length==10&&c.Enemies.Count==r.Enemies.Count,"combat save keeps slots and enemies");
 var e=Battle.CreateForRules(5,2,4);C(e.EnableReinforcements()&&e.Field.Slots.Length==8,"v4 reinforcement pass 8 slots");
 bool threw=false;try{var d=r.SavePreparation();}catch{threw=true;}C(threw,"(sanity) prep save refused mid-fight");
 var x=Battle.CreateForRules(5,2,4);x.Gold=5000;x.Place(1,9);var data=x.SavePreparation();data.rulesVersion=3;threw=false;try{Battle.RestorePreparation(data);}catch{threw=true;}C(threw,"v3 save with slot 9 rejected");
}}
