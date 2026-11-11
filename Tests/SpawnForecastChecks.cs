using System;
using EmberBastion;
class SpawnForecastChecks {
 static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
 static SpawnForecast.GroupPlan G(int gate,int kind,int count,double delay,double gap=0){return new SpawnForecast.GroupPlan{Gate=gate,Kind=kind,Count=count,Delay=delay,Gap=gap};}
 static int Main(){try{
  var s=new SpawnForecast.Schedule{Phase=Phase.Preparation,HasNext=true};s.Next.Add(G(0,0,11,0));s.Next.Add(G(0,1,6,0));s.Next.Add(G(1,2,5,0));var r=SpawnForecast.Calculate(s);Check(r.Count==2&&r[0].Kinds[0]==11&&r[0].Kinds[1]==6&&r[1].Count==5,"preparation kinds grouped per gate without collapsing names");
  s=new SpawnForecast.Schedule{Phase=Phase.Fighting,Clock=8,Interval=32,HasNext=true};s.Current.Add(G(0,1,4,9));s.Current.Add(G(0,2,5,11));r=SpawnForecast.Calculate(s);Check(r.Count==2&&r[0].Eta==1&&r[1].Eta==3&&r[0].Count==4&&r[1].Count==5,"different arrival times at one gate are not falsely merged");
  s=new SpawnForecast.Schedule{Phase=Phase.Fighting,Clock=28,Interval=32,HasNext=true};s.Next.Add(G(0,0,10,0));s.Next.Add(G(1,2,5,0));r=SpawnForecast.Calculate(s);Check(r.Count==2&&r[0].Eta==4&&r[1].Eta==4,"both next gates at inclusive four-second boundary");s.Clock=27.99;Check(SpawnForecast.Calculate(s).Count==0,"outside four seconds hidden");s.Blocked=true;Check(SpawnForecast.Calculate(s).Count==0,"card choice hides forecast");
  s=new SpawnForecast.Schedule{Phase=Phase.Fighting,Clock=30,Interval=32,HasNext=true};s.Current.Add(G(0,0,50,0,1));s.Next.Add(G(0,1,10,0));Check(SpawnForecast.Calculate(s).Count==0,"do not predict next wave before current spawning finishes");s.Clock=46;r=SpawnForecast.Calculate(s);Check(r.Count==1&&r[0].Eta==3,"next forecast follows actual final spawn rather than nominal interval");
  for(int field=0;field<4;field++)for(int mode=0;mode<3;mode++){var b=Battle.CreateForRules(29,field,4);b.SetDifficulty(mode);int gold=b.Gold;uint rng=b.SavePreparation().rng;var before=b.SavePreparation();r=SpawnForecast.Build(b);int expected=0;foreach(var g in b.WaveGroups(0))expected+=b.SpawnCount(g);int actual=0;foreach(var e in r)actual+=e.Count;Check(expected==actual,"real engine preparation counts");Check(b.Gold==gold&&b.SavePreparation().rng==rng&&b.Wave==before.wave&&b.Time==before.time,"display preserves real state");}
  Console.WriteLine("PASS "+checks+" spawn forecast boundary checks");return 0;
 }catch(Exception e){Console.Error.WriteLine(e.Message);return 1;}}
}
