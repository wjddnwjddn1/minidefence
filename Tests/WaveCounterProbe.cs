using System;
using System.Collections.Generic;
using System.Globalization;
using EmberBastion;
// Claude wave-composition probe (r27). Run through Tests/run-wave-counter-probe.py, which compiles a
// patched COPY of Battle.cs (GAP_EXP, ALL_COUNTERS) outside Assets/. Game sources are never modified.
// Knobs (environment): GAP_EXP, EXT_RUNNERS=1, ALL_COUNTERS=1, BOMB_INTERVAL, MARSH_SCALE (base difficulty only when MARSH_BASE_ONLY=1).
// args: rules field waveSpecs policies modes seeds
// waveSpecs: "" or "w=kind:count:gap:delay:gate[:mult]|kind:...;w2=..." (w is 1-based wave number)
class WaveCounterProbe {
 static int SwapSlot(int field){var f=Balance.Fields[field];int best=-1;double bs=-1;for(int s=0;s<f.Slots.Length;s++){if(Smart.Kind(field,s)!=1)continue;double sc=0;for(double d=0;d<f.Perimeter;d+=16){Point p=field==0?Balance.Path(d):f.Path(d);if(Point.Distance(p,f.Slots[s])<=180)sc++;}if(sc>bs){bs=sc;best=s;}}return best;}
 static int Kind(string policy,int field,int slot,int swap){
  if(policy=="bomb")return 2; if(policy=="archer")return 1;
  if(policy=="bombArcher")return slot==swap?1:2; if(policy=="bombFrost")return slot==swap?3:2; if(policy=="bombArcher2")return slot==swap||Smart.Kind(field,slot)==1&&slot%3==0?1:2;
  int k=Smart.Kind(field,slot); if(policy=="smartBFrost"&&slot==swap)return 3; return k;}
 static int Main(string[] a){
  int rules=int.Parse(a[0]),field=int.Parse(a[1]);string spec=a[2];string[] policies=a[3].Split(',');
  var modes=Array.ConvertAll(a[4].Split(','),int.Parse);var seeds=Array.ConvertAll(a[5].Split(','),uint.Parse);
  var waves=Balance.Fields[field].Waves;var targets=new List<int>();
  if(spec.Length>0)foreach(var part in spec.Split(';')){var kv=part.Split('=');int w=int.Parse(kv[0]);targets.Add(w);var gs=new List<Group>();foreach(var g in kv[1].Split('|')){var v=g.Split(':');var grp=new Group(int.Parse(v[0]),int.Parse(v[1]),double.Parse(v[2],CultureInfo.InvariantCulture),double.Parse(v[3],CultureInfo.InvariantCulture),int.Parse(v[4]),v.Length>5?double.Parse(v[5],CultureInfo.InvariantCulture):1);grp.Extended=waves[w-1].Length>0&&waves[w-1][0].Extended;gs.Add(grp);}waves[w-1]=gs.ToArray();}
  if(Environment.GetEnvironmentVariable("EXT_RUNNERS")=="1"){var f=Balance.Fields[field];foreach(var wave in f.Waves)foreach(var g in wave)if(g.Extended&&g.Kind==0){g.Kind=1;g.Count=(int)Math.Round(g.Count*.8);g.Gap*=2;}}
  var bi=Environment.GetEnvironmentVariable("BOMB_INTERVAL");if(bi!=null)Balance.Troops[2].Interval=double.Parse(bi,CultureInfo.InvariantCulture);
  var ms=Environment.GetEnvironmentVariable("MARSH_SCALE");if(ms!=null&&field==3&&(Environment.GetEnvironmentVariable("MARSH_BASE_ONLY")!="1"||a[4]=="0")){double k=double.Parse(ms,CultureInfo.InvariantCulture);int n=0;foreach(var wave in Balance.Fields[3].Waves){n++;if(n<4)continue;foreach(var g in wave)if(g.Kind!=4){g.Count=(int)Math.Round(g.Count*k);g.Gap/=k;}}}
  int swap=SwapSlot(field);
  foreach(string policy in policies)foreach(int mode in modes)foreach(uint seed in seeds)foreach(bool ember in new[]{false,true}){
   var b=Battle.CreateForRules(seed,field,rules);b.SetDifficulty(mode);int ticks=0;var peaks=new int[b.WaveCount+1];double over80=0;
   while(b.Phase!=Phase.Won&&b.Phase!=Phase.Lost&&ticks<200000){
    if(b.ReinforcementPending){b.ChooseReinforcement(b.Reinforcements.Choices()[0]);continue;}
    if(ticks%60==0){for(int s=0;s<b.Field.Slots.Length;s++)if(b.At(s)==null){int k=Kind(policy,field,s,swap);if(b.Gold>=Balance.Troops[k].Cost)b.Place(k,s);}
     foreach(var u in new List<Unit>(b.Units)){if(u.Level==1&&b.Gold>=50)b.Upgrade(u.Slot);else if(u.Level==2&&b.Gold>=90){int br=policy.StartsWith("smartB")&&u.Kind==0?2:0;if(br>0)b.UpgradeBranch(u.Slot,br);}}}
    if(b.Phase==Phase.Preparation&&!b.Start())throw new Exception("start");
    if(ember&&b.EmberCD<=0&&b.Enemies.Count>=3)b.Ember(b.Enemies[b.Enemies.Count/2].Position);
    b.Step(1.0/60);ticks++;peaks[b.Wave]=Math.Max(peaks[b.Wave],b.Enemies.Count);if(b.Enemies.Count>=80)over80+=1.0/60;}
   var tp=new List<string>();foreach(int w in targets)tp.Add(w<peaks.Length?peaks[w].ToString():"-");
   Console.WriteLine(policy+","+mode+","+seed+","+(ember?1:0)+","+(b.Phase==Phase.Won?"W":"L")+","+b.Wave+","+b.PeakAlive+","+string.Join("/",tp)+","+Math.Round(over80,1).ToString(CultureInfo.InvariantCulture)+","+b.Gold+","+string.Join(";",peaks));
  }
  return 0;}
}
