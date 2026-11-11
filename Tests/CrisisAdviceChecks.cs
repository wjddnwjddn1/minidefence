// Claude r31: crisis suggestion rule (CLAUDE-DIRECTION r31 decision 2) and frost-link cards (decision 3).
using System;
using EmberBastion;
static class CrisisAdviceChecks {
 static int pass;
 static void Check(bool ok,string name){if(!ok)throw new Exception("FAIL "+name);pass++;}
 static Battle Fighting(int gold){var b=Battle.CreateForRules(29,0,4);b.Place(1,0);if(!b.Start())throw new Exception("start");b.Step(.05);SetGold(b,gold);return b;}
 static void SetGold(Battle b,int gold){b.Gold=gold;}
 static int Main(){
  bool build;
  // Not fighting: nothing.
  var prep=Battle.CreateForRules(29,0,4);Check(CrisisAdvice.Suggest(prep,out build)==-1,"preparation gives no advice");
  // Empty slot affordable: best-road empty slot, build.
  var b=Fighting(500);int s=CrisisAdvice.Suggest(b,out build);Check(build&&s>=0&&b.At(s)==null,"affordable empty slot suggested");
  int best=0;for(int i=0;i<b.Field.Slots.Length;i++)if(b.At(i)==null)for(int k=0;k<4;k++)best=Math.Max(best,CrisisAdvice.Road(b,k,i));
  int got=0;for(int k=0;k<4;k++)got=Math.Max(got,CrisisAdvice.Road(b,k,s));Check(got==best,"suggested slot has the most road");
  // No empty slots: upgrade the best affordable attacker.
  var full=Battle.CreateForRules(29,0,4);SetGold(full,5000);for(int i=0;i<full.Field.Slots.Length;i++)full.Place(i==1?4:0,i);full.Start();full.Step(.05);SetGold(full,60);
  s=CrisisAdvice.Suggest(full,out build);Check(!build&&s>=0&&full.At(s).Kind!=4&&full.At(s).Level==1,"upgrade suggested when slots are full");
  // Too poor for any troop or upgrade: none.
  SetGold(full,10);Check(CrisisAdvice.Suggest(full,out build)==-1,"no advice when nothing is affordable");
  // Legacy draft keeps 7 entries; rules-4 draft has 9 and pads old saves.
  var legacy=new ReinforcementDraft(1,2);Check(legacy.Export().owned.Length==7,"legacy draft size");
  var v4=new ReinforcementDraft(1,3,true);Check(v4.Export().owned.Length==9,"rules-4 draft size");
  var old=v4.Export();old.owned=new int[7];var padded=ReinforcementDraft.Restore(old,3,true);Check(padded.Export().owned.Length==9&&padded.Count(ReinforcementDraft.FrostMark)==0,"old rules-4 save padded");
  bool rejected=false;try{var bad=legacy.Export();bad.owned=new int[9];ReinforcementDraft.Restore(bad,2,false);}catch(ArgumentException){rejected=true;}Check(rejected,"legacy rejects 9-card save");
  // Frost cards only offered with frost (and swords for IceShard) in rules 4.
  bool seenMark=false,seenShard=false,legacyMark=false,noFrostMark=false;
  for(uint seed=1;seed<400;seed++){var d=new ReinforcementDraft(seed,3,true);d.Offer(true,true,true,true);foreach(int c in d.Choices()){if(c==ReinforcementDraft.FrostMark)seenMark=true;if(c==ReinforcementDraft.IceShard)seenShard=true;}
   var n=new ReinforcementDraft(seed,3,true);n.Offer(true,true,true,false);foreach(int c in n.Choices())if(c>=7)noFrostMark=true;
   var l=new ReinforcementDraft(seed,2);l.Offer(true,true,true,true);foreach(int c in l.Choices())if(c>=7)legacyMark=true;}
  Check(seenMark&&seenShard,"frost cards offered with frost");Check(!noFrostMark,"no frost cards without frost");Check(!legacyMark,"legacy never offers frost cards");
  Console.WriteLine("PASS "+pass+" crisis advice and frost-link draft checks");return 0;}
}
