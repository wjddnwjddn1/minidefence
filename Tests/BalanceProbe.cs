using System;
using System.Collections.Generic;
using System.Linq;
using EmberBastion;
class BalanceProbe {
 static int Main(string[] args){
  bool repair=Array.IndexOf(args,"--repair")>=0;
  for(int field=0;field<3;field++){
   int wins=0,furthest=0; string best="";
   for(int trial=0;trial<24;trial++){
    var random=new Random(3100+field*100+trial);var b=new Battle(42,field);int ticks=0;
    while(b.Phase!=Phase.Won&&b.Phase!=Phase.Lost&&ticks<360000){
     if(b.Phase==Phase.Preparation){
      if(repair&&b.HP<=60&&b.Gold>=120)b.Repair();
      int attempts=0;
      while(b.Gold>=50&&attempts++<40){
       bool upgraded=false;
       if(b.Units.Count>=2&&random.NextDouble()<.45){var choices=b.Units.Where(u=>u.Level<3).OrderBy(u=>random.Next()).ToArray();foreach(var u in choices){if(u.Level==1&&b.Gold>=50){upgraded=b.Upgrade(u.Slot);break;}if(u.Level==2&&b.Gold>=90){upgraded=b.UpgradeBranch(u.Slot,1+random.Next(2));break;}}}
       if(upgraded)continue;
       var slots=Enumerable.Range(0,b.Field.Slots.Length).Where(i=>b.At(i)==null).ToArray();if(slots.Length==0)break;
       int[] kinds={0,1,1,1,2,3,4};int kind=kinds[random.Next(kinds.Length)];if(b.Units.Count<2)kind=random.Next(2);if(b.Gold<Balance.Troops[kind].Cost){if(b.Gold<65)break;continue;}
       int selected=slots.OrderBy(i=>Score(b,i)+random.NextDouble()*70).First();b.Place(kind,selected);
      }
      if(b.Units.Count==0||!b.Start())break;
     }
     if(b.Phase==Phase.Fighting&&b.EmberCD<=0&&b.Enemies.Count>=3){Enemy bestEnemy=null;int max=0;foreach(var e in b.Enemies){int count=b.Enemies.Count(o=>Point.Distance(e.Position,o.Position)<=90);if(count>max){max=count;bestEnemy=e;}}if(bestEnemy!=null&&max>=3)b.Ember(bestEnemy.Position);}
     b.Step(1.0/60);ticks++;
    }
    if(b.Phase==Phase.Won)wins++;if(b.Wave>furthest){furthest=b.Wave;best="trial="+trial+" phase="+b.Phase+" hp="+b.HP+" gold="+b.Gold+" kills="+b.Kills;}
   }
   Console.WriteLine("repair="+repair+" field="+field+" trials=24 wins="+wins+" furthest="+furthest+" "+best);
  }
  return 0;
 }
 static double Score(Battle b,int slot){var p=b.Field.Slots[slot];double best=99999;for(double d=0;d<b.Field.Perimeter;d+=32)best=Math.Min(best,Point.Distance(p,b.Field.Path(d)));return best;}
}
