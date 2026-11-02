using System;
using EmberBastion;
class FastPriorityChecks {
 static Enemy Make(int id,double speed,double travel,double slow=0,double freeze=0){return new Enemy{Id=id,Kind=0,HP=999,MaxHP=999,Speed=speed,Travel=travel,Position=Balance.Path(travel),SlowTime=slow>0?2:0,SlowPct=slow,FreezeTime=freeze};}
 static void Case(int kind,Enemy first,Enemy second,int expected,string name){var b=new Battle();if(!b.Place(kind,0)||!b.SetPriority(0,2)||!b.Start())throw new Exception("setup");b.Enemies.Add(first);b.Enemies.Add(second);b.Step(1.0/60);if(b.Shots.Count==0||b.Shots[0].Target!=expected)throw new Exception(name+" selected "+(b.Shots.Count==0?"nothing":b.Shots[0].Target.ToString()));}
 static void Main(){try{foreach(int kind in new[]{1,3}){Case(kind,Make(100,100,128+Balance.Perimeter*20),Make(101,101,128),101,"speed beats laps");Case(kind,Make(100,100,128+Balance.Perimeter*2),Make(101,100,128),100,"equal speed prefers ahead");Case(kind,Make(100,160,128,.5),Make(101,100,128),101,"slow changes effective speed");Case(kind,Make(100,160,128,0,1),Make(101,100,128),101,"freeze changes effective speed");}Console.WriteLine("PASS fast target priority across laps, ties, slow and freeze for two troops");}catch(Exception e){Console.Error.WriteLine(e);Environment.Exit(1);}}
}
