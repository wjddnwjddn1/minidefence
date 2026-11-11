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
 // Layout follows the rule-specific field (r27 task 8: v4 normal pass has 10 slots).
 static int Rules=4;static FieldDef Layout(int field){return Battle.FieldFor(field,Rules);}
 static int SwapSlot(int field,int excluded=-1){var f=Layout(field);int best=-1;double bs=-1;for(int s=0;s<f.Slots.Length;s++){if(s==excluded||Smart.Kind(field,Layout(field),s)!=1)continue;double sc=0;for(double d=0;d<f.Perimeter;d+=16){Point p=field==0?Balance.Path(d):f.Path(d);if(Point.Distance(p,f.Slots[s])<=180)sc++;}if(sc>bs){bs=sc;best=s;}}return best;}
 static int Kind(string policy,int field,int slot,int swap,int second){
  if(policy=="frost")return 3;
  if(policy=="smartBFrost2"&&(slot==swap||slot==second))return 3;
  if(policy=="bombFrost2")return slot==swap||slot==second?3:2;
  if(policy=="bomb")return 2; if(policy=="archer")return 1;
  if(policy=="bombArcher")return slot==swap?1:2; if(policy=="bombFrost")return slot==swap?3:2; if(policy=="bombArcher2")return slot==swap||Smart.Kind(field,Layout(field),slot)==1&&slot%3==0?1:2;
  int k=Smart.Kind(field,Layout(field),slot); if(policy=="smartBFrost"&&slot==swap)return 3; return k;}
 static int Main(string[] a){
  int rules=int.Parse(a[0]),field=int.Parse(a[1]);Rules=rules;string spec=a[2];string[] policies=a[3].Split(',');
  var modes=Array.ConvertAll(a[4].Split(','),int.Parse);var seeds=Array.ConvertAll(a[5].Split(','),uint.Parse);
  // PASS_SLOTS="x,y;x,y" adds slots to the pass (field 2); PASS_START_GOLD overrides its start gold.
  { var ps=Environment.GetEnvironmentVariable("PASS_SLOTS");var sg=Environment.GetEnvironmentVariable("PASS_START_GOLD");var pf=Balance.Fields[2];
    if(!string.IsNullOrEmpty(ps)){var list=new List<Point>(pf.Slots);foreach(var xy in ps.Split(';')){var v=xy.Split(',');list.Add(new Point(double.Parse(v[0],CultureInfo.InvariantCulture),double.Parse(v[1],CultureInfo.InvariantCulture)));}pf.Slots=list.ToArray();}
    if(!string.IsNullOrEmpty(sg))pf.StartGold=int.Parse(sg); }
  var waves=Balance.Fields[field].Waves;var targets=new List<int>();
  if(spec.Length>0)foreach(var part in spec.Split(';')){var kv=part.Split('=');int w=int.Parse(kv[0]);targets.Add(w);var gs=new List<Group>();foreach(var g in kv[1].Split('|')){var v=g.Split(':');var grp=new Group(int.Parse(v[0]),int.Parse(v[1]),double.Parse(v[2],CultureInfo.InvariantCulture),double.Parse(v[3],CultureInfo.InvariantCulture),int.Parse(v[4]),v.Length>5?double.Parse(v[5],CultureInfo.InvariantCulture):1);grp.Extended=waves[w-1].Length>0&&waves[w-1][0].Extended;gs.Add(grp);}waves[w-1]=gs.ToArray();}
  if(Environment.GetEnvironmentVariable("EXT_RUNNERS")=="1"){var f=Balance.Fields[field];foreach(var wave in f.Waves)foreach(var g in wave)if(g.Extended&&g.Kind==0){g.Kind=1;g.Count=(int)Math.Round(g.Count*.8);g.Gap*=2;}}
  var bi=Environment.GetEnvironmentVariable("BOMB_INTERVAL");if(bi!=null)Balance.Troops[2].Interval=double.Parse(bi,CultureInfo.InvariantCulture);
  var ms=Environment.GetEnvironmentVariable("MARSH_SCALE");if(ms!=null&&field==3&&(Environment.GetEnvironmentVariable("MARSH_BASE_ONLY")!="1"||a[4]=="0")){double k=double.Parse(ms,CultureInfo.InvariantCulture);int n=0;foreach(var wave in Balance.Fields[3].Waves){n++;if(n<4)continue;foreach(var g in wave)if(g.Kind!=4){g.Count=(int)Math.Round(g.Count*k);g.Gap/=k;}}}
  int swap=SwapSlot(field),second=SwapSlot(field,swap);
  foreach(string policy in policies){if(Array.IndexOf(new[]{"bomb","archer","bombArcher","bombFrost","bombArcher2","smartB","smartBFrost","smartBFrost2","bombFrost2","frost"},policy)<0)throw new ArgumentException("Unknown policy: "+policy);if((policy=="smartBFrost2"||policy=="bombFrost2")&&(swap<0||second<0||swap==second))throw new Exception("Two distinct frost positions required");}
  foreach(string policy in policies)foreach(int mode in modes)foreach(uint seed in seeds)foreach(bool ember in new[]{false,true}){
   if(rules>=4&&ember&&a.Length>6&&a[6]=="unique")continue;
   var b=Battle.CreateForRules(seed,field,rules);b.SetDifficulty(mode);int ticks=0;var peaks=new int[b.WaveCount+1];double over80=0;
   while(b.Phase!=Phase.Won&&b.Phase!=Phase.Lost&&ticks<200000){
    if(b.ReinforcementPending){b.ChooseReinforcement(b.Reinforcements.Choices()[0]);continue;}
    if(ticks%60==0){for(int s=0;s<b.Field.Slots.Length;s++)if(b.At(s)==null){int k=Kind(policy,field,s,swap,second);if(b.Gold>=Balance.Troops[k].Cost)b.Place(k,s);}
     foreach(var u in new List<Unit>(b.Units)){if(u.Level==1&&b.Gold>=50)b.Upgrade(u.Slot);else if(u.Level==2&&b.Gold>=90){int br=policy.StartsWith("smartB")&&u.Kind==0?2:0;if(br>0)b.UpgradeBranch(u.Slot,br);}}}
    if(b.Phase==Phase.Preparation&&!b.Start())throw new Exception("start");
    if(ember&&b.EmberCD<=0&&b.Enemies.Count>=3)b.Ember(b.Enemies[b.Enemies.Count/2].Position);
    b.Step(1.0/60);ticks++;peaks[b.Wave]=Math.Max(peaks[b.Wave],b.Enemies.Count);if(b.Enemies.Count>=80)over80+=1.0/60;}
   var tp=new List<string>();foreach(int w in targets)tp.Add(w<peaks.Length?peaks[w].ToString():"-");
   Console.WriteLine(policy+","+mode+","+seed+","+(ember?1:0)+","+(b.Phase==Phase.Won?"W":b.Phase==Phase.Lost?"L":"S")+","+b.Wave+","+b.PeakAlive+","+string.Join("/",tp)+","+Math.Round(over80,1).ToString(CultureInfo.InvariantCulture)+","+b.Gold+","+string.Join(";",peaks));
  }
  return 0;}
}
