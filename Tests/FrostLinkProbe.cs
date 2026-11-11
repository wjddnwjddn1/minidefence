using System;
using System.Collections.Generic;
using EmberBastion;
// Claude r31 decision 3: does the frost-link bundle (FrostMark, IceShard) make a frost build viable without dominating?
// Real engine, rules 4. Run through Tests/run-frost-link-probe.py.  args: field policies modes seeds
// Policies: bomb (all bombs), mixed (Smart.Kind), archerFrost / swordFrost (frost builds that take the new cards),
//           archerFrostNo / swordFrostNo (same placement, never take the new cards: isolates the cards' value).
class FrostLinkProbe {
 static FieldDef Layout(int field){return Battle.FieldFor(field,4);}
 static double Road(int field,int slot,double range){var f=Layout(field);double n=0;for(double d=0;d<f.Perimeter;d+=16){Point p=field==0?Balance.Path(d):f.Path(d);if(Point.Distance(p,f.Slots[slot])<=range)n++;}return n;}
 static int[] Army(string policy,int field){var f=Layout(field);int n=f.Slots.Length;var k=new int[n];for(int s=0;s<n;s++)k[s]=Smart.Kind(field,f,s);
  if(policy=="bomb"){for(int s=0;s<n;s++)k[s]=2;return k;}
  if(policy=="mixed")return k;
  if(policy.StartsWith("mixedFrost")){int best=-1;double bs=-1;for(int s=0;s<n;s++){if(k[s]!=1)continue;double v=Road(field,s,Balance.Troops[3].Range);if(v>bs){bs=v;best=s;}}if(best>=0)k[best]=3;return k;}
  // Frost goes to the two slots with the most road inside frost range.
  var order=new List<int>();for(int s=0;s<n;s++)order.Add(s);order.Sort((x,y)=>Road(field,y,Balance.Troops[3].Range).CompareTo(Road(field,x,Balance.Troops[3].Range)));
  bool archer=policy.StartsWith("archer");for(int s=0;s<n;s++)if(k[s]!=4)k[s]=archer?1:(k[s]==0?0:2);
  if(!archer){/* sword build: swords on melee slots, the rest stays bombs except frost */}
  k[order[0]]=3;k[order[1]]=3;return k;}
 static int Pick(Battle b,string policy){var c=b.Reinforcements.Choices();bool take=policy=="archerFrost"||policy=="swordFrost"||policy=="mixedFrost";
  int want=policy!="swordFrost"?ReinforcementDraft.FrostMark:ReinforcementDraft.IceShard,other=policy!="swordFrost"?ReinforcementDraft.IceShard:ReinforcementDraft.FrostMark;
  if(take){if(Array.IndexOf(c,want)>=0)return want;if(Array.IndexOf(c,other)>=0)return other;}
  foreach(int card in c)if(card<7)return card;return c[0];}
 static int Main(string[] a){int field=int.Parse(a[0]);string[] policies=a[1].Split(',');var modes=Array.ConvertAll(a[2].Split(','),int.Parse);var seeds=Array.ConvertAll(a[3].Split(','),uint.Parse);
  foreach(string policy in policies){var kinds=Army(policy,field);foreach(int mode in modes)foreach(uint seed in seeds){
   var b=Battle.CreateForRules(seed,field,4);b.SetDifficulty(mode);int ticks=0;string cards="";
   while(b.Phase!=Phase.Won&&b.Phase!=Phase.Lost&&ticks<200000){
    if(b.ReinforcementPending){int card=Pick(b,policy);cards+=card;b.ChooseReinforcement(card);continue;}
    if(ticks%60==0){for(int s=0;s<b.Field.Slots.Length;s++)if(b.At(s)==null&&b.Gold>=Balance.Troops[kinds[s]].Cost)b.Place(kinds[s],s);
     foreach(var u in new List<Unit>(b.Units)){if(u.Level==1&&b.Gold>=50)b.Upgrade(u.Slot);else if(u.Level==2&&b.Gold>=90&&u.Kind!=2&&u.Kind!=4)b.UpgradeBranch(u.Slot,1);}}
    if(b.Phase==Phase.Preparation&&!b.Start())throw new Exception("start");
    b.Step(1.0/60);ticks++;}
   int frostUnits=b.Units.FindAll(u=>u.Kind==3).Count;
   Console.WriteLine(policy+","+mode+","+seed+","+(b.Phase==Phase.Won?"W":"L")+","+b.Wave+","+b.PeakAlive+","+b.Gold+","+cards+","+frostUnits);}}
  return 0;}
}
