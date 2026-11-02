using System;
using EmberBastion;
// Claude experiment: how few units win? Real engine, no game file changes. Buys only the first N slots (cycle sword,sword,archer,bomb), optional upgrades, no ember.
class MinUnitsProbe {
 static void Main(){Console.WriteLine("mode,field,seed,units,upgrade,result,wave,peak,goldLeft");
  foreach(int mode in new[]{0,1,2})for(int field=0;field<Balance.Fields.Length;field++)foreach(uint seed in new uint[]{29,41,57,73})for(int n=1;n<=8;n++)foreach(bool up in new[]{false,true}){
   var b=new Battle(seed,field);b.SetDifficulty(mode);int t=0;int[] kinds={0,0,1,2};
   while(b.Phase!=Phase.Won&&b.Phase!=Phase.Lost&&t<120000){
    if(t%60==0){int placed=b.Units.Count;for(int s=0;s<b.Field.Slots.Length&&placed<n;s++)if(b.At(s)==null){int k=kinds[placed%4];if(b.Gold>=Balance.Troops[k].Cost&&b.Place(k,s))placed++;}
     if(up)foreach(var u in b.Units)if(u.Level==1&&b.Gold>=50)b.Upgrade(u.Slot);}
    if(b.Phase==Phase.Preparation&&b.Units.Count>0)b.Start();
    b.Step(1.0/60);t++;}
   Console.WriteLine(mode+","+field+","+seed+","+n+","+up+","+(b.Phase==Phase.Won?"Won":b.Phase==Phase.Lost?"Lost":"Stalled")+","+b.Wave+","+b.PeakAlive+","+b.Gold);
  }}
}
