using System;
using EmberBastion;
class CandidateSlotChecks {
 static int count;
 static void Check(bool ok,string why){count++;if(!ok)throw new Exception(why);}
 static void Reject(Action action,string why){bool rejected=false;try{action();}catch(ArgumentException){rejected=true;}Check(rejected,why);}
 static void Main(){try{Run();}catch(Exception e){Console.Error.WriteLine(e.GetType().Name+": "+e.Message);Environment.ExitCode=1;}}
 static void Run(){
  for(int f=0;f<Balance.Fields.Length;f++)for(int rules=0;rules<=4;rules++){
   if(Battle.WaveCountFor(f,rules)==0)continue;
   var b=Battle.CreateForRules(29,f,rules);var original=Balance.Fields[f];int n=original.Slots.Length;
   Check(b.Field.Slots.Length==n+(f==2&&rules==4?2:0),"normal layout length");
   for(int i=0;i<n;i++)Check(b.Field.Slots[i].X==original.Slots[i].X&&b.Field.Slots[i].Y==original.Slots[i].Y,"old slot index unchanged");
   if(f!=2||rules!=4)Check(object.ReferenceEquals(b.Field,original),"unchanged field identity");
  }
  var candidate=Battle.CreateForRules(29,2,4);var old=new Battle(29,2);
  Check(old.Field.Slots.Length==8&&candidate.Field.Slots.Length==10,"candidate-only extra slots");
  Check(!object.ReferenceEquals(old.Field.Slots,candidate.Field.Slots),"slot arrays isolated");
  Check(candidate.Field.Slots[8].X==640&&candidate.Field.Slots[8].Y==548&&candidate.Field.Slots[9].X==704&&candidate.Field.Slots[9].Y==452,"specified positions");
  for(int i=8;i<10;i++){var p=candidate.Field.Slots[i];var f=candidate.Field;Check(p.X+24<f.Castle.X||p.X-24>f.Castle.X+f.CastleWidth||p.Y+16<f.Castle.Y||p.Y-48>f.Castle.Y+f.CastleHeight,"new unit body clears castle image");}
  Check(old.Field.Castle.X==512&&old.Field.Castle.Y==400,"legacy castle position unchanged");
  candidate.Gold=5000;for(int i=0;i<10;i++)Check(candidate.Place(i%5,i),"all ten slots usable");
  Check(!candidate.Place(0,10)&&!old.Place(0,8),"invalid slots rejected");
  var prep=candidate.SavePreparation();var restored=Battle.RestorePreparation(prep);Check(restored.Field.Slots.Length==10&&restored.Units.Count==10&&restored.At(9).Kind==4,"ten-unit preparation restored");
  prep.mode=(int)BattleMode.Reinforcement;Reject(()=>Battle.RestorePreparation(prep),"reinforcement cannot import normal extras");
  prep.mode=(int)BattleMode.Normal;prep.rulesVersion=3;Reject(()=>Battle.RestorePreparation(prep),"old rules cannot import normal extras");
  int gold=candidate.Gold;Check(!candidate.EnableReinforcements()&&candidate.Mode==BattleMode.Normal&&!candidate.FastWaves&&candidate.Field.Slots.Length==10&&candidate.Gold==gold,"mode rejection has no side effects");
  Check(candidate.Start(),"ten-unit battle starts");candidate.Step(.5);var copy=candidate.CopyCombat();var combat=Battle.ImportCombat(candidate.ExportCombat());
  Check(copy.Field.Slots.Length==10&&copy.Units.Count==10&&combat.Field.Slots.Length==10&&combat.Units.Count==10,"copy and combat restore use candidate layout");
  for(int i=0;i<20;i++){candidate.Step(.1);copy.Step(.1);combat.Step(.1);}Check(candidate.Gold==copy.Gold&&copy.Gold==combat.Gold&&candidate.Kills==combat.Kills&&candidate.Enemies.Count==combat.Enemies.Count,"continued combat agrees");
  var mode=Battle.CreateForRules(29,2,4);Check(mode.EnableReinforcements()&&mode.Field.Slots.Length==8,"reinforcement layout");Check(!mode.Place(0,8),"reinforcement rejects extra slot");Check(mode.DisableReinforcements()&&mode.Field.Slots.Length==10,"normal layout restored on mode switch");
  for(int kind=0;kind<4;kind++){int recommended=0;for(int slot=0;slot<10;slot++){int grade=PlacementCoverage.Grade(2,candidate.Field,kind,slot);Check(grade>=0&&grade<=2,"all candidate slots graded");if(PlacementCoverage.Recommended(2,candidate.Field,kind,slot))recommended++;}Check(recommended==3,"three candidate recommendations");Check(PlacementCoverage.Grade(2,kind,8)==-1,"legacy coverage excludes extras");}
  Check(BadgeRules.Limit(2,false,false,false,3)==80&&BadgeRules.Limit(2,false,false,false,4)==80,"badge threshold retained");
  Console.WriteLine("PASS "+count+" candidate slot, mode, save, coverage and legacy checks");
 }
}
