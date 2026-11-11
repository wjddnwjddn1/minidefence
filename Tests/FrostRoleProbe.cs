using System;
using System.Collections.Generic;
using EmberBastion;
// Claude r33: frost / smith role candidates, paired against keeping the archer (same slot, seed, difficulty).
// Compiled by Tests/run-frost-role-probe.py against a PATCHED COPY of Battle.cs (FROST_SPLASH, FROST_BRITTLE knobs).
// args: field swapKind(3|4) modes seeds   env: FROST_SPLASH, FROST_SPLASH_DAMAGE, FROST_BRITTLE, FROST_DAMAGE, FROST_INTERVAL, FROST_COST, SMITH_COST
class FrostRoleProbe {
 static FieldDef Layout(int field){return Battle.FieldFor(field,4);}
 static double Road(int field,int slot,double range,double min){var f=Layout(field);double n=0;for(double d=0;d<f.Perimeter;d+=16){Point p=field==0?Balance.Path(d):f.Path(d);double dist=Point.Distance(p,f.Slots[slot]);if(dist<=range&&dist>=min)n++;}return n;}
 static int SwapSlot(int field,int[] k,int swap){var f=Layout(field);int best=-1;double bs=-1;for(int s=0;s<k.Length;s++){if(k[s]!=1)continue;double v;
   if(swap==4){v=0;for(int j=0;j<k.Length;j++)if(j!=s&&k[j]!=4&&Point.Distance(f.Slots[s],f.Slots[j])<=Balance.Troops[4].Range)v++;}else v=Road(field,s,Balance.Troops[swap].Range,Balance.Troops[swap].MinRange);
   if(v>bs){bs=v;best=s;}}return best;}
 static double Env(string k,double d){var v=Environment.GetEnvironmentVariable(k);return v==null?d:double.Parse(v,System.Globalization.CultureInfo.InvariantCulture);}
 static int Main(string[] a){int field=int.Parse(a[0]),swap=int.Parse(a[1]);var modes=Array.ConvertAll(a[2].Split(','),int.Parse);var seeds=Array.ConvertAll(a[3].Split(','),uint.Parse);
  Balance.Troops[3].Damage=Env("FROST_DAMAGE",Balance.Troops[3].Damage);Balance.Troops[3].Interval=Env("FROST_INTERVAL",Balance.Troops[3].Interval);Balance.Troops[3].Cost=(int)Env("FROST_COST",Balance.Troops[3].Cost);Balance.Troops[4].Cost=(int)Env("SMITH_COST",Balance.Troops[4].Cost);
  var f=Layout(field);var baseKinds=new int[f.Slots.Length];for(int s=0;s<baseKinds.Length;s++)baseKinds[s]=Smart.Kind(field,f,s);int slot=SwapSlot(field,baseKinds,swap);
  foreach(int mode in modes)foreach(uint seed in seeds)foreach(bool swapped in new[]{false,true}){
   var kinds=(int[])baseKinds.Clone();if(swapped&&slot>=0)kinds[slot]=swap;
   var b=Battle.CreateForRules(seed,field,4);b.SetDifficulty(mode);int ticks=0;
   while(b.Phase!=Phase.Won&&b.Phase!=Phase.Lost&&ticks<200000){
    if(b.ReinforcementPending){b.ChooseReinforcement(b.Reinforcements.Choices()[0]);continue;}
    if(ticks%60==0){for(int s=0;s<b.Field.Slots.Length;s++)if(b.At(s)==null&&b.Gold>=Balance.Troops[kinds[s]].Cost)b.Place(kinds[s],s);
     foreach(var u in new List<Unit>(b.Units)){if(u.Level==1&&b.Gold>=50)b.Upgrade(u.Slot);else if(u.Level==2&&b.Gold>=90)b.UpgradeBranch(u.Slot,1);}}
    if(b.Phase==Phase.Preparation&&!b.Start())throw new Exception("start");
    b.Step(1.0/60);ticks++;}
   Console.WriteLine(mode+","+seed+","+(swapped?1:0)+","+(b.Phase==Phase.Won?"W":"L")+","+b.PeakAlive+","+b.Gold+","+slot);}
  return 0;}
}
