using System;
using System.Collections.Generic;
namespace EmberBastion {
 // Isolated experiment only: Exp-SupplyCardProbe.ps1 patches a temp copy of Battle.cs. Not in the product.
 public static class Card {
  public static double ArcherHaste=1,BombRadius=1,EmberCool=1,GoldMult=1,RangeMult=1,SwordDamage=1;public static int SwordTargets=0;public static bool Tuned=false;
  public static void Reset(){ArcherHaste=1;BombRadius=1;EmberCool=1;GoldMult=1;RangeMult=1;SwordDamage=1;SwordTargets=0;}
  public static readonly string[] Names={"none","archerHaste20","bombRadius20","swordTarget1","emberCool30","gold20","range10","swordDmg20"};
  public static void Apply(int c){switch(c){case 1:ArcherHaste=1.2;break;case 2:BombRadius=Tuned?1.3:1.2;break;case 3:SwordTargets++;break;case 4:EmberCool=.7;break;case 5:GoldMult=Tuned?1.15:1.2;break;case 6:RangeMult=Tuned?1.07:1.1;break;case 7:SwordDamage=Tuned?1.15:1.2;break;}}
 }
 class SupplyCardProbe {
  static readonly string[] Pol={"smartB","swordB","archer","bomb","roles"};
  static string Run(int seed,int field,int pol,int c1,int c2){
   Card.Reset();var b=new Battle((uint)seed,field);b.SetFastWaves(true);int ticks=0,peak=0;bool a1=false,a2=false;
   while(b.Phase!=Phase.Won&&b.Phase!=Phase.Lost&&ticks<120000){
    if(!a1&&b.Wave>=3){a1=true;Card.Apply(c1);}if(!a2&&b.Wave>=6){a2=true;Card.Apply(c2);}
    if(ticks%60==0){for(int slot=0;slot<b.Field.Slots.Length;slot++)if(b.At(slot)==null){int kind=pol==0?Smart.Kind(field,slot):pol==1?0:pol==2?1:pol==3?2:new[]{0,0,1,2}[slot%4];if(b.Gold>=Balance.Troops[kind].Cost)b.Place(kind,slot);}foreach(var u in b.Units){if(u.Level==1&&b.Gold>=50)b.Upgrade(u.Slot);else if((pol==1||(pol==0&&u.Kind==0))&&u.Level==2&&b.Gold>=90)b.UpgradeBranch(u.Slot,2);}}
    if(b.Phase==Phase.Preparation&&!b.Start())throw new Exception("start failed");
    if(b.EmberCD<=0&&b.Enemies.Count>=3)b.Ember(b.Enemies[b.Enemies.Count/2].Position);
    b.Step(1.0/60);ticks++;peak=Math.Max(peak,b.Enemies.Count);
   }
   return (b.Phase==Phase.Won?"Won":b.Phase==Phase.Lost?"Lost":"Stalled")+","+b.Wave+","+peak+","+b.Gold;
  }
  static int Main(string[] args){if(args.Length>0){int density=int.Parse(args[0]);Card.Tuned=args.Length>1&&args[1]=="tuned";foreach(var F in Balance.Fields)foreach(var W in F.Waves)foreach(var g in W){if(g.Kind==4)continue;g.Count=(int)Math.Round(g.Count*density/100.0);g.Gap=g.Gap*100.0/density;}}Console.WriteLine("seed,field,policy,card1,card2,outcome,wave,peak,gold");
   foreach(int seed in new[]{29,41,57,73})for(int field=0;field<Balance.Fields.Length;field++){
    for(int pol=0;pol<Pol.Length;pol++)for(int c=0;c<Card.Names.Length;c++)Console.WriteLine(seed+","+field+","+Pol[pol]+","+Card.Names[c]+",none,"+Run(seed,field,pol,c,0));
    for(int c1=1;c1<Card.Names.Length;c1++)for(int c2=c1;c2<Card.Names.Length;c2++)if(c2!=c1)Console.WriteLine(seed+","+field+",smartB,"+Card.Names[c1]+","+Card.Names[c2]+","+Run(seed,field,0,c1,c2));
   }return 0;}
 }
 static class Smart {
  static readonly Dictionary<int,int[]> cache=new Dictionary<int,int[]>();
  static Point P(int f,double d){return f==0?Balance.Path(d):Balance.Fields[f].Path(d);}
  static double Cover(int f,Point s,double range,double min){double per=f==0?Balance.Perimeter:Balance.Fields[f].Perimeter;int n=0,inside=0;for(double d=0;d<per;d+=16){n++;double dist=Point.Distance(P(f,d),s);if(dist<=range&&dist>=min)inside++;}return (double)inside/n;}
  public static int Kind(int f,int slot){int[] a;if(!cache.TryGetValue(f,out a)){var S=Balance.Fields[f].Slots;int m=S.Length;var idx=new List<int>();for(int i=0;i<m;i++)idx.Add(i);idx.Sort((x,y)=>Cover(f,S[y],120,0).CompareTo(Cover(f,S[x],120,0)));a=new int[m];var rest=new List<int>();for(int i=0;i<m;i++){if(i<m/2)a[idx[i]]=0;else rest.Add(idx[i]);}rest.Sort((x,y)=>Cover(f,S[y],210,60).CompareTo(Cover(f,S[x],210,60)));for(int i=0;i<rest.Count;i++)a[rest[i]]=i%2==0?1:2;cache[f]=a;}return a[slot];}
 }
}
