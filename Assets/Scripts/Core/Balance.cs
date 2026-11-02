using System;
namespace EmberBastion {
 [Serializable] public struct Point {
  public double X,Y;
  public Point(double x,double y){X=x;Y=y;}
  public static double Distance(Point a,Point b){double x=a.X-b.X,y=a.Y-b.Y;return Math.Sqrt(x*x+y*y);}
 }
 public sealed class TroopDef {
  public string Name; public int Cost,Targets; public double Damage,Range,MinRange,Interval,Radius,Speed,Pen;
  public TroopDef(string name,int cost,double damage,double range,double interval,int targets,double pen,double radius=0,double min=0,double speed=600){Name=name;Cost=cost;Damage=damage;Range=range;Interval=interval;Targets=targets;Pen=pen;Radius=radius;MinRange=min;Speed=speed;}
 }
 public sealed class EnemyDef {
  public string Name;public double HP,Speed,Armor,BlastTaken,MeleeTaken;
  public EnemyDef(string name,double hp,double speed,double armor=0,double blastTaken=1,double meleeTaken=1){Name=name;HP=hp;Speed=speed;Armor=armor;BlastTaken=blastTaken;MeleeTaken=meleeTaken;}
 }
 public sealed class Group {
  public int Kind,Count,Gate;public double Gap,Delay,Multiplier;
  public Group(int kind,int count,double gap,double delay=0,int gate=0,double multiplier=1){Kind=kind;Count=count;Gap=gap;Delay=delay;Gate=gate;Multiplier=multiplier;}
 }
 public sealed class FieldDef {
  public string Name;public Point[] Points,Slots;public double[] Gates;public Group[][] Waves;public double Perimeter;public Point Castle;public double CastleWidth,CastleHeight;public int StartGold;
  public FieldDef(string name,Point[] points,Point[] slots,double[] gates,Group[][] waves,Point castle,double width,double height,int startGold=200){Name=name;Points=points;Slots=slots;Gates=gates;Waves=waves;Castle=castle;CastleWidth=width;CastleHeight=height;StartGold=startGold;for(int i=0;i<points.Length;i++)Perimeter+=Point.Distance(points[i],points[(i+1)%points.Length]);}
  public Point Path(double distance){double d=((distance%Perimeter)+Perimeter)%Perimeter;for(int i=0;i<Points.Length;i++){Point a=Points[i],b=Points[(i+1)%Points.Length];double length=Point.Distance(a,b);if(d<length)return new Point(a.X+(b.X-a.X)*d/length,a.Y+(b.Y-a.Y)*d/length);d-=length;}return Points[0];}
 }
 // Baseline: v2-campaign/rules/base-data.mjs and path.mjs, read 2026-10-27.
 // Distances use the original 1280 x 768 map, seconds use game time.
 public static class Balance {
  public const double Perimeter=2816,Tick=1.0/60,EmberRadius=90,EmberCooldown=25,AutoWaveInterval=32,FastWaveInterval=22,FastEnemyHealth=1.5,HardWaveInterval=18,HardEnemyHealth=1.8;
  public const int StartGold=200,MaxAlive=100;
  public const double ArcherFavoredMult=1.5;
  public static bool ArcherFavored(int kind){return kind==1||kind==2;}
  public static readonly TroopDef[] Troops={
   new TroopDef("검사",65,20,120,.8,3,0),
   new TroopDef("궁수",85,20,210,1,1,7),
   new TroopDef("폭탄병",105,36,210,2,1,6,55,60,420),
   new TroopDef("서리",90,6,180,1.2,1,0),
   new TroopDef("기술자",90,0,160,1,0,0)};
  public static readonly EnemyDef[] Enemies={new EnemyDef("횃불병",90,85),new EnemyDef("질주병",67,165,0,1,.5),new EnemyDef("방패병",128,72,16,.5,1),new EnemyDef("폭약병",84,85),new EnemyDef("고블린 대장",504,62)};
  public static readonly Point[] Slots={new Point(320,218),new Point(480,218),new Point(640,218),new Point(800,218),new Point(960,218),new Point(320,550),new Point(480,550),new Point(640,550),new Point(800,550),new Point(960,550),new Point(278,330),new Point(278,438),new Point(1002,330),new Point(1002,438)};
  public static readonly double[] Gates={0,1408};
  public static readonly Group[][] Waves={
   new[]{new Group(0,6,1.1)},
   new[]{new Group(0,6,1),new Group(1,3,.8,4)},
   new[]{new Group(0,6,.35),new Group(0,6,.35,7)},
   new[]{new Group(2,5,1.1),new Group(0,5,.9,2)},
   new[]{new Group(0,8,.9),new Group(4,1,0,5)},
   new[]{new Group(1,8,.7,0,1),new Group(0,8,.9,1)},
   new[]{new Group(3,6,.9),new Group(2,6,1,2,1)},
   new[]{new Group(0,6,.6),new Group(1,6,.6,3,1),new Group(2,5,.9,6),new Group(3,5,.9,8,1)},
   new[]{new Group(2,4,.35),new Group(0,10,.15,.5,0,.5),new Group(2,4,.35,1,1),new Group(0,10,.15,1.5,1,.5)},
   new[]{new Group(2,6,.8,0,1),new Group(1,6,.6,2),new Group(3,4,.8,5,1),new Group(4,1,0,6,0,1.4)}
  };
  public static Point Path(double distance){double d=((distance%Perimeter)+Perimeter)%Perimeter;if(d<896)return new Point(192+d,128);d-=896;if(d<512)return new Point(1088,128+d);d-=512;if(d<896)return new Point(1088-d,640);return new Point(192,640-(d-896));}
  public static readonly Group[][] ForestWaves={
   new[]{new Group(0,10,.9),new Group(1,4,1,9)},
   new[]{new Group(0,12,.8),new Group(2,3,2,6)},
   new[]{new Group(1,10,.75),new Group(3,3,2,4),new Group(0,6,.8,8)},
   new[]{new Group(4,1,1,5),new Group(0,12,.9),new Group(2,4,1.4,9)},
   new[]{new Group(0,10,.8),new Group(1,8,.8,2,1),new Group(2,4,1.2,8,1)},
   new[]{new Group(2,7,1.2),new Group(3,5,1.2,2,1),new Group(1,8,.8,8,1)},
   new[]{new Group(0,12,.7),new Group(2,7,1,1,1),new Group(1,9,.6,6)},
   new[]{new Group(4,1,1,6,1,1.25),new Group(2,7,1.1),new Group(3,4,1.5,2,1),new Group(1,8,.7,7)}
  };
  public static readonly Group[][] PassWaves={
   new[]{new Group(0,9,.8),new Group(1,5,.7,7)},
   new[]{new Group(1,10,.35),new Group(2,4,1.5,5)},
   new[]{new Group(2,6,1.2),new Group(3,3,2,5),new Group(1,7,.4,9)},
   new[]{new Group(4,1,1,4),new Group(1,12,.4),new Group(0,11,.4,7)},
   new[]{new Group(2,6,1),new Group(1,11,.35,2,1),new Group(0,10,.35,9)},
   new[]{new Group(3,5,1.5,0,1),new Group(2,7,1.1,2),new Group(1,11,.35,7,1)},
   new[]{new Group(2,8,1,0,1),new Group(3,4,1.6,3),new Group(0,14,.33,6)},
   new[]{new Group(4,1,1,5,1,1.3),new Group(1,12,.35),new Group(2,8,1,2,1),new Group(3,4,1.3,9)}
  };
  public static readonly FieldDef[] Fields={
   new FieldDef("성문 앞 방어선",new[]{new Point(192,128),new Point(1088,128),new Point(1088,640),new Point(192,640)},Slots,Gates,Waves,new Point(528,218),224,180),
   new FieldDef("숲길 굽이",new[]{new Point(192,128),new Point(512,128),new Point(512,384),new Point(768,384),new Point(768,128),new Point(1088,128),new Point(1088,640),new Point(192,640)},new[]{new Point(640,256),new Point(400,232),new Point(880,232),new Point(288,256),new Point(992,256),new Point(320,550),new Point(448,550),new Point(832,550),new Point(960,550),new Point(278,400),new Point(1002,400)},new[]{0.0,1088.0},ForestWaves,new Point(544,440),192,154),
   new FieldDef("바위 고개",new[]{new Point(192,128),new Point(1088,128),new Point(1088,320),new Point(832,320),new Point(832,640),new Point(448,640),new Point(448,448),new Point(192,448)},new[]{new Point(320,232),new Point(480,232),new Point(704,232),new Point(960,224),new Point(928,448),new Point(352,550),new Point(320,352),new Point(576,256)},new[]{0.0,1344.0},PassWaves,new Point(512,400),192,154,340)
  };
 }
}
