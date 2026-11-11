// Claude r31: crisis suggestion rule (CLAUDE-DIRECTION r31 decision 2) (frost-link cards of decision 3 were measured and removed).
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
  Console.WriteLine("PASS "+pass+" crisis advice checks");return 0;}
}
