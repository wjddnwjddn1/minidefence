using System;
using System.Collections.Generic;
using EmberBastion;
// Claude experiment: spawn density vs the fewest units that win (real engine, in-memory wave scaling only; game data unchanged). Lv2 upgrades, no ember. Boss groups unchanged.
class DifficultyCurveProbe {
 static void Main(){Console.WriteLine("mode,field,seed,countPct,units,result,wave,peak,goldLeft");
  var n0=new Dictionary<Group,int>();var g0=new Dictionary<Group,double>();foreach(var F in Balance.Fields)foreach(var W in F.Waves)foreach(var g in W){n0[g]=g.Count;g0[g]=g.Gap;}
  foreach(int pct in new[]{100,125,150,175}){foreach(var kv in n0){if(kv.Key.Kind==4)continue;kv.Key.Count=(int)Math.Round(kv.Value*pct/100.0);kv.Key.Gap=g0[kv.Key]*100.0/pct;}
   foreach(int mode in new[]{0,1,2})for(int field=0;field<Balance.Fields.Length;field++)foreach(uint seed in new uint[]{29,41})foreach(int n in new[]{1,2,3,4,5,6,7,8,10}){
    var b=new Battle(seed,field);b.SetDifficulty(mode);int t=0;int[] kinds={0,0,1,2};
    while(b.Phase!=Phase.Won&&b.Phase!=Phase.Lost&&t<120000){
     if(t%60==0){int placed=b.Units.Count;for(int s=0;s<b.Field.Slots.Length&&placed<n;s++)if(b.At(s)==null){int k=kinds[placed%4];if(b.Gold>=Balance.Troops[k].Cost&&b.Place(k,s))placed++;}foreach(var u in b.Units)if(u.Level==1&&b.Gold>=50)b.Upgrade(u.Slot);}
     if(b.Phase==Phase.Preparation&&b.Units.Count>0)b.Start();
     b.Step(1.0/60);t++;}
    Console.WriteLine(mode+","+field+","+seed+","+pct+","+n+","+(b.Phase==Phase.Won?"Won":b.Phase==Phase.Lost?"Lost":"Stalled")+","+b.Wave+","+b.PeakAlive+","+b.Gold);
   }}}
}
