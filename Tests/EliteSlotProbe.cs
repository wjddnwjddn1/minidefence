// Claude r27 elite-slot experiment. Compiled only by Tests/run-elite-probe.py against a PATCHED COPY of
// Battle.cs (adds Battle.EliteSlot and the elite effects below). The game has no elite feature.
// Elite (initial design, CLAUDE-DIRECTION r27): one Lv3 unit per battle, from wave 6, 240G.
//   sword/archer damage x1.35, archer pen +8, bomb radius x1.2, frost slow 3s + boss x1.5, smith support range +60.
// args: rules field policies modes seeds   (C# 5)
using System;
using System.Collections.Generic;
using System.Globalization;
using EmberBastion;
class EliteSlotProbe {
 const int Cost=240,FromWave=6;
 static double Road(int field,int slot,double range){var f=Balance.Fields[field];double n=0;for(double d=0;d<f.Perimeter;d+=16){Point p=field==0?Balance.Path(d):f.Path(d);if(Point.Distance(p,f.Slots[slot])<=range)n++;}return n;}
 static int Best(int field,Func<int,bool> ok,Func<int,double> score){int best=-1;double bs=double.MinValue;for(int s=0;s<Balance.Fields[field].Slots.Length;s++){if(!ok(s))continue;double v=score(s);if(v>bs){bs=v;best=s;}}return best;}
 // Army: mixed placement (Smart.Kind), best long-range archer slot -> frost, best-connected sword slot -> smith.
 static int[] Army(int field){var f=Balance.Fields[field];int n=f.Slots.Length;var k=new int[n];for(int s=0;s<n;s++)k[s]=Smart.Kind(field,s);
  int frost=Best(field,s=>k[s]==1,s=>Road(field,s,180));if(frost>=0)k[frost]=3;
  int smith=Best(field,s=>k[s]==0,s=>{int c=0;for(int j=0;j<n;j++)if(j!=s&&k[j]!=0&&Point.Distance(f.Slots[s],f.Slots[j])<=160)c++;return c;});if(smith>=0)k[smith]=4;return k;}
 static int Target(Battle b,int[] kinds,string policy){int field=b.FieldIndex;
  if(policy=="none")return -1;
  if(policy=="first"){foreach(var u in b.Units)if(u.Level==3)return u.Slot;return -1;}
  int kind=policy=="sword"?0:policy=="archer"?1:policy=="bomb"?2:policy=="frost"?3:4;
  return Best(field,s=>kinds[s]==kind,s=>kind==4?0:Road(field,s,Balance.Troops[kind].Range));}
 static void BuyElite(Battle b,int[] kinds,string policy,ref int eliteWave){if(b.EliteSlot<0&&b.Wave>=FromWave&&b.Gold>=Cost&&b.Phase==Phase.Fighting){int t=Target(b,kinds,policy);var u=t<0?null:b.At(t);if(u!=null&&u.Level==3){b.Gold-=Cost;b.EliteSlot=t;eliteWave=b.Wave;}}}
 static int Main(string[] a){bool eliteFirst=Environment.GetEnvironmentVariable("ELITE_FIRST")=="1";
  int rules=int.Parse(a[0]),field=int.Parse(a[1]);string[] policies=a[2].Split(',');var modes=Array.ConvertAll(a[3].Split(','),int.Parse);var seeds=Array.ConvertAll(a[4].Split(','),uint.Parse);
  if(Environment.GetEnvironmentVariable("EXT_RUNNERS")=="1")foreach(var wave in Balance.Fields[field].Waves)foreach(var g in wave)if(g.Extended&&g.Kind==0){g.Kind=1;g.Count=(int)Math.Round(g.Count*.8);g.Gap*=2;}
  var kinds=Army(field);int[] branch={2,1,1,1,2};
  foreach(string policy in policies)foreach(int mode in modes)foreach(uint seed in seeds)foreach(bool ember in new[]{false,true}){
   var b=Battle.CreateForRules(seed,field,rules);b.SetDifficulty(mode);int ticks=0,eliteWave=0;
   while(b.Phase!=Phase.Won&&b.Phase!=Phase.Lost&&ticks<200000){
    if(b.ReinforcementPending){b.ChooseReinforcement(b.Reinforcements.Choices()[0]);continue;}
    if(ticks%60==0){for(int s=0;s<b.Field.Slots.Length;s++)if(b.At(s)==null&&b.Gold>=Balance.Troops[kinds[s]].Cost)b.Place(kinds[s],s);
     if(eliteFirst)BuyElite(b,kinds,policy,ref eliteWave);
     // ELITE_FIRST: once wave 6 arrives, save for the elite (bring its target to Lv3 first, hold other upgrades).
     bool saving=eliteFirst&&b.EliteSlot<0&&b.Wave>=FromWave&&policy!="none";int target=saving?Target(b,kinds,policy):-1;
     foreach(var u in new List<Unit>(b.Units)){if(saving&&u.Slot!=target&&target>=0&&b.At(target)!=null)continue;if(u.Level==1&&b.Gold>=50)b.Upgrade(u.Slot);else if(u.Level==2&&b.Gold>=90)b.UpgradeBranch(u.Slot,branch[u.Kind]);}
     if(!eliteFirst)BuyElite(b,kinds,policy,ref eliteWave);}
    if(b.Phase==Phase.Preparation&&!b.Start())throw new Exception("start");
    if(ember&&b.EmberCD<=0&&b.Enemies.Count>=3)b.Ember(b.Enemies[b.Enemies.Count/2].Position);
    b.Step(1.0/60);ticks++;}
   Console.WriteLine(policy+","+mode+","+seed+","+(ember?1:0)+","+(b.Phase==Phase.Won?"W":"L")+","+b.Wave+","+b.PeakAlive+","+eliteWave+","+b.Gold);
  }
  return 0;}
}
