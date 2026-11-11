using System;
using System.Reflection;
using EmberBastion;
class FrostReworkChecks {
 static void Check(bool ok,string label){if(!ok)throw new Exception(label);}
 static void Near(double a,double b,string label){Check(Math.Abs(a-b)<1e-6,label+" "+a+" != "+b);}
 static void Main(){foreach(int version in new[]{3,4}){
  var b=Battle.CreateForRules(29,0,version);double damage,interval;b.AttackStats(new Unit{Kind=3},out damage,out interval);Near(interval,version==4?.8:1.2,"interval");
  var e=new Enemy{Kind=0,HP=1000,MaxHP=1000,Shield=1,SlowTime=1};var hit=typeof(Battle).GetMethod("Hit",BindingFlags.NonPublic|BindingFlags.Instance);hit.Invoke(b,new object[]{e,100.0,0.0,false,1.0,true,1.0,1});Near(e.HP,version==4?925:950,"shield then brittle");
  b.Phase=Phase.Fighting;b.Wave=1;var target=new Enemy{Id=901,Kind=0,HP=1000,MaxHP=1000,Position=new Point(500,128),Travel=308};var near=new Enemy{Id=902,Kind=0,HP=1000,MaxHP=1000,Position=new Point(550,128),Travel=358};var far=new Enemy{Id=903,Kind=0,HP=1000,MaxHP=1000,Position=new Point(600,128),Travel=408};b.Enemies.Add(target);b.Enemies.Add(near);b.Enemies.Add(far);b.Shots.Add(new Shot{Kind=3,Target=901,Position=target.Position,Speed=100,Damage=6,SlowPct=.5,Freezing=true});b.Step(1.0/60);Check(target.SlowTime>0&&target.FreezeTime>0,"target slow and freeze");Check((near.SlowTime>0)==(version==4),"nearby slow rules isolation");Check(near.FreezeTime==0&&near.HP==1000,"splash does not freeze or damage");Check(far.SlowTime==0,"outside radius");
 }Console.WriteLine("PASS frost interval, radius, damage ordering and legacy isolation");}
}
