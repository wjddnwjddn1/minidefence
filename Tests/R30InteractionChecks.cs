using System;
using System.Collections.Generic;
using EmberBastion;
static class R30InteractionChecks {
 static int checks;static void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
 static bool Same(Point a,Point b){return Point.Distance(a,b)<1e-8;}
 static int Count(IEnumerable<Cue> cues){int count=0;foreach(var cue in cues)count++;return count;}
 static void ArrowChecks(){
  foreach(int facing in new[]{-1,1}){
   var b=new Battle();var cue=new Cue("attack",new Point(200,200)){Slot=0,Source=1,Facing=facing,Destination=new Point(200+facing*200,188)};
   var shot=new Shot{Kind=1,Position=new Point(200+facing*10,188),Destination=cue.Destination};b.Shots.Add(shot);var view=new ArrowFlightView();var original=shot.Position;
   view.Capture(b,new[]{cue},10);Point first=view.Position(shot);Check(Math.Abs(Point.Distance(first,ArrowFlightView.Muzzle(cue))-18)<1e-8,"first arrow is attached to bow tail");Check(Point.Distance(first,cue.Destination)>150,"first arrow is not at enemy");Check(Same(shot.Position,original),"render anchor never changes simulation");Check(Same(first,view.Position(shot)),"pause holds first arrow without simulation capture");
   shot.Position=new Point(200+facing*40,188);view.Capture(b,new Cue[0],11);Point second=view.Position(shot);Check(Point.Distance(second,cue.Destination)<Point.Distance(first,cue.Destination),"arrow flies toward target after birth");Check(Same(second,view.Position(shot)),"pause holds ongoing arrow");
   b.Shots.Clear();view.Capture(b,new[]{cue,cue},12);Check(Count(view.InstantReleases())==1,"instant impact still emits one bow release");view.Capture(b,new Cue[0],13);Check(Count(view.InstantReleases())==0,"instant release does not linger after its frame");
   b.Shots.Add(shot);view.Reset(b);view.Capture(b,new[]{cue},20);Check(Same(view.Position(shot),shot.Position),"restored mid-flight arrow does not snap to shooter");
  }
  var battle=new Battle();var arrow=new Shot{Kind=1,Position=new Point(600,300),Destination=new Point(650,300)};battle.Shots.Add(arrow);var tracker=new ArrowFlightView();tracker.Capture(battle,new Cue[0],1);Check(Same(tracker.Position(arrow),arrow.Position),"unmatched old projectile is preserved");
 }
 static void DraftChecks(){
  bool legacyEmber=false;
  for(uint seed=1;seed<=32;seed++)for(int mask=0;mask<8;mask++){
   bool sword=(mask&1)!=0,archer=(mask&2)!=0,bomb=(mask&4)!=0;
   var draft=new ReinforcementDraft(seed,3,true);Check(draft.Offer(sword,archer,bomb),"v4 offer including frost/support-only fallback");int[] choices=draft.Choices();Check(choices.Length==3&&new HashSet<int>(choices).Count==3&&Array.IndexOf(choices,3)<0,"three unique non-Ember choices");
   var copy=ReinforcementDraft.Restore(draft.Export(),3,true);if(draft.CanReroll(sword,archer,bomb)){Check(draft.Reroll(sword,archer,bomb)&&copy.Reroll(sword,archer,bomb),"restored non-Ember reroll succeeds");Check(string.Join(",",draft.Choices())==string.Join(",",copy.Choices())&&Array.IndexOf(copy.Choices(),3)<0,"reroll determinism and exclusion survive restore");}
   var old=new ReinforcementDraft(seed);old.Offer(sword,archer,bomb);legacyEmber|=Array.IndexOf(old.Choices(),3)>=0;
  }
  Check(legacyEmber,"legacy pool remains available for old rules");
  for(uint seed=1;seed<=12;seed++){
   var b=Battle.CreateForRules(seed,0,4);b.Gold=10000;b.Place(0,0);b.Place(1,1);b.Place(2,2);b.Start();int unchanged=b.Gold;Check(!b.Ember(new Point(300,300))&&!b.UpgradeEmber(1)&&b.Gold==unchanged,"candidate cannot use or buy retired skill");for(int i=0;i<2000&&!b.ReinforcementPending;i++){b.Enemies.Clear();b.Step(.25);}Check(b.ReinforcementPending,"real v4 pending offer");
   var restored=Battle.ImportCombat(b.ExportCombat());var copied=b.CopyCombat();for(int n=0;n<2;n++){Check(b.RerollReinforcement()&&restored.RerollReinforcement()&&copied.RerollReinforcement(),"real combat copies can reroll");Check(string.Join(",",b.Reinforcements.Choices())==string.Join(",",restored.Reinforcements.Choices())&&string.Join(",",b.Reinforcements.Choices())==string.Join(",",copied.Reinforcements.Choices()),"combat import and copy retain exclusion configuration");Check(Array.IndexOf(restored.Reinforcements.Choices(),3)<0,"combat import never restores Ember into new offers");}
  }
 }
 static int Main(){try{ArrowChecks();DraftChecks();Console.WriteLine("PASS "+checks+" arrow launch, pause, legacy and retired-card boundary checks");return 0;}catch(Exception e){Console.Error.WriteLine(e.Message);return 1;}}
}
