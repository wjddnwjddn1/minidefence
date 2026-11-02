using System;
using System.Collections.Generic;
using EmberBastion;
// Claude experiment: is calling the next wave early a real decision? Real engine, no game file changes. Smart mix, full auto-buy, ember on.
class EarlyCallProbe {
 static void Main(){Console.WriteLine("mode,field,seed,threshold,result,wave,peak,total,calls,earlyGold,goldLeft,spent");
  foreach(int mode in new[]{0,1,2})for(int field=0;field<Balance.Fields.Length;field++)foreach(uint seed in new uint[]{29,41,57,73})foreach(int th in new[]{-1,0,5,15,30,999}){
   var b=new Battle(seed,field);b.SetDifficulty(mode);int t=0,calls=0,early=0,spent=0;
   while(b.Phase!=Phase.Won&&b.Phase!=Phase.Lost&&t<240000){
    if(t%60==0){for(int s=0;s<b.Field.Slots.Length;s++)if(b.At(s)==null){int k=Smart.Kind(field,s);if(b.Gold>=Balance.Troops[k].Cost&&b.Place(k,s))spent+=Balance.Troops[k].Cost;}
     foreach(var u in b.Units){if(u.Level==1&&b.Gold>=50&&b.Upgrade(u.Slot))spent+=50;else if(u.Kind==0&&u.Level==2&&b.Gold>=90&&b.UpgradeBranch(u.Slot,2))spent+=90;}
     if(th>=0&&b.Phase==Phase.Fighting&&b.Enemies.Count<=th&&b.CanCallEarly()){int g=b.Gold;if(b.CallEarly()){calls++;early+=b.Gold-g;}}}
    if(b.Phase==Phase.Preparation&&b.Units.Count>0)b.Start();
    if(b.Phase==Phase.Fighting&&b.EmberCD<=0&&b.Enemies.Count>=3)b.Ember(b.Enemies[b.Enemies.Count/2].Position);
    b.Step(1.0/60);t++;}
   Console.WriteLine(mode+","+field+","+seed+","+th+","+(b.Phase==Phase.Won?"Won":b.Phase==Phase.Lost?"Lost":"Stalled")+","+b.Wave+","+b.PeakAlive+","+b.Time.ToString("F0")+","+calls+","+early+","+b.Gold+","+spent);
  }}
}
static class Smart {
 static readonly Dictionary<int,int[]> cache=new Dictionary<int,int[]>();
 static Point P(int f,double d){return f==0?Balance.Path(d):Balance.Fields[f].Path(d);}
 static double Cover(int f,Point s,double range,double min){double per=f==0?Balance.Perimeter:Balance.Fields[f].Perimeter;int n=0,inside=0;for(double d=0;d<per;d+=16){n++;double dist=Point.Distance(P(f,d),s);if(dist<=range&&dist>=min)inside++;}return (double)inside/n;}
 public static int Kind(int f,int slot){int[] a;if(!cache.TryGetValue(f,out a)){var S=Balance.Fields[f].Slots;int m=S.Length;var idx=new List<int>();for(int i=0;i<m;i++)idx.Add(i);idx.Sort((x,y)=>Cover(f,S[y],120,0).CompareTo(Cover(f,S[x],120,0)));a=new int[m];var rest=new List<int>();for(int i=0;i<m;i++){if(i<m/2)a[idx[i]]=0;else rest.Add(idx[i]);}rest.Sort((x,y)=>Cover(f,S[y],210,60).CompareTo(Cover(f,S[x],210,60)));for(int i=0;i<rest.Count;i++)a[rest[i]]=i%2==0?1:2;cache[f]=a;}return a[slot];}
}
