using System;
using System.Linq;
using EmberBastion;
class ReinforcementDraftChecks {
 static int checks;static void Check(bool value,string label){checks++;if(!value)throw new Exception(label);}
 static void Main(){
  for(uint seed=0;seed<256;seed++)for(int mask=0;mask<8;mask++){
   var draft=new ReinforcementDraft(seed);Check(draft.Offer((mask&1)!=0,(mask&2)!=0,(mask&4)!=0),"first offer");var choices=draft.Choices();Check(choices.Length==3&&choices.Distinct().Count()==3,"distinct");foreach(int c in choices){Check(c>=3||c==0&&(mask&2)!=0||c==1&&(mask&4)!=0||c==2&&(mask&1)!=0,"eligible");if(c==6)Check((mask&1)!=0,"sword damage eligible");}
   var copy=ReinforcementDraft.Restore(draft.Export());Check(choices.SequenceEqual(copy.Choices()),"pending restored");Check(!draft.Offer(true,true,true),"no redraw");Check(!draft.Choose(7),"invalid choice");Check(draft.Choose(choices[0])&&copy.Choose(choices[0]),"choice");Check(draft.Offer(true,true,true)&&copy.Offer(true,true,true),"second offer");Check(draft.Choices().SequenceEqual(copy.Choices()),"rng restored");Check(draft.Choose(draft.Choices()[0])&&draft.SelectedCount==2&&!draft.Offer(true,true,true),"two choices max");
  }
  var isolated=new ReinforcementDraft(42);isolated.Offer(true,true,true);var snapshot=isolated.Export();snapshot.offered[0]=99;Check(isolated.Choices()[0]!=99,"export copies");bool rejected=false;try{ReinforcementDraft.Restore(snapshot);}catch(ArgumentException){rejected=true;}Check(rejected,"corrupt offer rejected");
  Console.WriteLine("PASS "+checks+" reinforcement draft determinism, eligibility, isolation and validation checks");
 }
}
