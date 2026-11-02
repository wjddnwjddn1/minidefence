using System;
using EmberBastion;
// Claude experiment: where does game time go? Real engine, no game file changes. Buys the first N slots (cycle sword,sword,archer,bomb), upgrades when affordable, no ember, never calls early.
class PacingProbe {
 static void Main(){Console.WriteLine("mode,field,seed,units,result,wave,peak,peakAt,total,spawnEndAt,tail,tailShare,enemiesAtSpawnEnd,tailMaxRise,idleShare,lowShare,goldLeft");
  foreach(int mode in new[]{0,1,2})for(int field=0;field<Balance.Fields.Length;field++)foreach(uint seed in new uint[]{29,41,57,73})foreach(int n in new[]{2,3,5,8,14}){
   var b=new Battle(seed,field);b.SetDifficulty(mode);int t=0;int[] kinds={0,0,1,2};double idle=0,low=0,lastAt=-1,peakAt=0;int peakSeen=0,atEnd=0,rise=0;var qf=typeof(Battle).GetField("queue",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
   while(b.Phase!=Phase.Won&&b.Phase!=Phase.Lost&&t<240000){
    if(t%60==0){int placed=b.Units.Count;for(int s=0;s<b.Field.Slots.Length&&placed<n;s++)if(b.At(s)==null){int k=kinds[placed%4];if(b.Gold>=Balance.Troops[k].Cost&&b.Place(k,s))placed++;}
     foreach(var u in b.Units)if(u.Level==1&&b.Gold>=50)b.Upgrade(u.Slot);}
    if(b.Phase==Phase.Preparation&&b.Units.Count>0)b.Start();
    if(b.Phase==Phase.Fighting&&b.Wave==b.Field.Waves.Length&&lastAt<0&&((System.Collections.ICollection)qf.GetValue(b)).Count==0){lastAt=b.Time;atEnd=b.Enemies.Count;}if(lastAt>=0)rise=Math.Max(rise,b.Enemies.Count-atEnd);if(b.Enemies.Count>peakSeen){peakSeen=b.Enemies.Count;peakAt=b.Time;}
    b.Step(1.0/60);t++;if(b.Phase==Phase.Fighting){if(b.Enemies.Count==0)idle+=1.0/60;if(b.Enemies.Count<=3)low+=1.0/60;}}
   double total=b.Time;Console.WriteLine(mode+","+field+","+seed+","+n+","+(b.Phase==Phase.Won?"Won":b.Phase==Phase.Lost?"Lost":"Stalled")+","+b.Wave+","+b.PeakAlive+","+peakAt.ToString("F0")+","+total.ToString("F0")+","+lastAt.ToString("F0")+","+(lastAt<0?0:total-lastAt).ToString("F0")+","+((lastAt<0?0:total-lastAt)/Math.Max(1,total)).ToString("F2")+","+atEnd+","+rise+","+(idle/Math.Max(1,total)).ToString("F2")+","+(low/Math.Max(1,total)).ToString("F2")+","+b.Gold);
  }}
}
