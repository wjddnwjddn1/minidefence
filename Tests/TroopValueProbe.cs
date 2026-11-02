using System;
using System.Collections.Generic;
using EmberBastion;
// Claude experiment: are frost and engineer worth a slot on the current rules? Real engine, no game file changes.
class TroopValueProbe {
 static void Main(){Console.WriteLine("mode,field,seed,variant,result,wave,peak,total");
  string[] names={"base","frost1","frost2","eng1","eng2","frost1eng1"};
  foreach(int mode in new[]{0,1,2})for(int field=0;field<Balance.Fields.Length;field++)foreach(uint seed in new uint[]{29,41,57,73})for(int v=0;v<names.Length;v++){
   int[] plan=Smart.Plan(field,v);var b=new Battle(seed,field);b.SetDifficulty(mode);int t=0;
   while(b.Phase!=Phase.Won&&b.Phase!=Phase.Lost&&t<240000){
    if(t%60==0){for(int s=0;s<b.Field.Slots.Length;s++)if(b.At(s)==null&&b.Gold>=Balance.Troops[plan[s]].Cost)b.Place(plan[s],s);
     foreach(var u in b.Units){if(u.Level==1&&b.Gold>=50)b.Upgrade(u.Slot);else if(u.Kind==0&&u.Level==2&&b.Gold>=90)b.UpgradeBranch(u.Slot,2);}}
    if(b.Phase==Phase.Preparation&&b.Units.Count>0)b.Start();
    if(b.Phase==Phase.Fighting&&b.EmberCD<=0&&b.Enemies.Count>=3)b.Ember(b.Enemies[b.Enemies.Count/2].Position);
    b.Step(1.0/60);t++;}
   Console.WriteLine(mode+","+field+","+seed+","+names[v]+","+(b.Phase==Phase.Won?"Won":b.Phase==Phase.Lost?"Lost":"Stalled")+","+b.Wave+","+b.PeakAlive+","+b.Time.ToString("F0"));
  }}
}
static class Smart {
 static Point P(int f,double d){return f==0?Balance.Path(d):Balance.Fields[f].Path(d);}
 static double Cover(int f,Point s,double range,double min){double per=f==0?Balance.Perimeter:Balance.Fields[f].Perimeter;int n=0,inside=0;for(double d=0;d<per;d+=16){n++;double dist=Point.Distance(P(f,d),s);if(dist<=range&&dist>=min)inside++;}return (double)inside/n;}
 // Base smart mix. Variants turn the best-covered non-sword slots into frost; engineers go on the non-sword slot nearest the most allies.
 public static int[] Plan(int f,int v){var S=Balance.Fields[f].Slots;int m=S.Length;var idx=new List<int>();for(int i=0;i<m;i++)idx.Add(i);idx.Sort((x,y)=>Cover(f,S[y],120,0).CompareTo(Cover(f,S[x],120,0)));var a=new int[m];var rest=new List<int>();for(int i=0;i<m;i++){if(i<m/2)a[idx[i]]=0;else rest.Add(idx[i]);}rest.Sort((x,y)=>Cover(f,S[y],210,60).CompareTo(Cover(f,S[x],210,60)));for(int i=0;i<rest.Count;i++)a[rest[i]]=i%2==0?1:2;
  int frost=v==1||v==5?1:v==2?2:0,eng=v==3||v==5?1:v==4?2:0;int r=0;for(int i=0;i<frost&&r<rest.Count;i++,r++)a[rest[r]]=3;
  var near=new List<int>(rest.GetRange(r,rest.Count-r));near.Sort((x,y)=>Allies(S,y,160).CompareTo(Allies(S,x,160)));for(int i=0;i<eng&&i<near.Count;i++)a[near[i]]=4;return a;}
 static int Allies(Point[] S,int i,double r){int n=0;for(int j=0;j<S.Length;j++)if(j!=i&&Point.Distance(S[i],S[j])<=r)n++;return n;}
}
