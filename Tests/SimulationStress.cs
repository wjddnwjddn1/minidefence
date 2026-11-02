using System;
using EmberBastion;
class SimulationStress {
 static void Require(bool ok,string detail){if(!ok)throw new Exception(detail);}
 static bool Finite(double x){return !double.IsNaN(x)&&!double.IsInfinity(x);}
 static int Main(string[] args){try{
  int won=0,lost=0,maxAlive=0;
  for(int field=0;field<Balance.Fields.Length;field++)for(int seed=1;seed<=12;seed++){
   var b=new Battle((uint)seed,field);if(args.Length>0&&args[0]=="fast")b.SetDifficulty(1);else if(args.Length>0&&args[0]=="hard")b.SetDifficulty(2);else if(args.Length>0&&args[0]!="normal")throw new ArgumentException("Expected normal, fast or hard");int ticks=0;
   while(b.Phase!=Phase.Won&&b.Phase!=Phase.Lost&&ticks<240000){
    if(b.Phase==Phase.Preparation){
     if(b.HP<=45&&b.Gold>=120)b.Repair();
     for(int slot=0;slot<b.Field.Slots.Length;slot++)if(b.At(slot)==null){int kind=(slot+seed)%4;int cost=Balance.Troops[kind].Cost;if(b.Gold>=cost)b.Place(kind,slot);}
     foreach(var unit in b.Units)if(unit.Level==1&&b.Gold>=50)b.Upgrade(unit.Slot);
     if(b.Units.Count==0&&b.Gold>=65)b.Place(0,0);
     Require(b.Start(),"could not start field="+field+" seed="+seed);
    }
    if(b.Phase==Phase.Fighting&&ticks%60==0){for(int slot=0;slot<b.Field.Slots.Length;slot++)if(b.At(slot)==null){int kind=(slot+seed)%4;if(b.Gold>=Balance.Troops[kind].Cost)b.Place(kind,slot);}foreach(var unit in b.Units)if(unit.Level==1&&b.Gold>=50)b.Upgrade(unit.Slot);}
    if(b.EmberCD<=0&&b.Enemies.Count>=3)b.Ember(b.Enemies[b.Enemies.Count/2].Position);
    b.Step(1.0/60);ticks++;maxAlive=Math.Max(maxAlive,b.Enemies.Count);
    if(ticks%60==0){
     Require(b.HP>=0&&b.HP<=100&&b.Gold>=0&&b.Wave>=1&&b.Wave<=b.Field.Waves.Length&&Finite(b.Time)&&Finite(b.EmberCD),"invalid battle state");
     foreach(var e in b.Enemies)Require(Finite(e.Position.X)&&Finite(e.Position.Y)&&Finite(e.HP)&&Finite(e.MaxHP)&&Finite(e.Travel)&&e.HP>0&&e.MaxHP>0&&e.Travel>=0,"invalid enemy state");
     foreach(var shot in b.Shots)Require(Finite(shot.Position.X)&&Finite(shot.Position.Y)&&Finite(shot.Destination.X)&&Finite(shot.Destination.Y)&&Finite(shot.Damage)&&Finite(shot.Speed)&&shot.Speed>0,"invalid projectile state");
     foreach(var u in b.Units)Require(Finite(u.Cooldown)&&Finite(u.Stun)&&u.Cooldown>=0&&u.Stun>=0,"invalid unit state");
    }
   }
   Require(b.Phase==Phase.Won||b.Phase==Phase.Lost,"timed out field="+field+" seed="+seed);
   if(b.Phase==Phase.Won)won++;else lost++;
  }
  Console.WriteLine("PASS "+(args.Length>0?args[0]:"normal")+" 36 natural-resource simulations: won="+won+" lost="+lost+"; peak alive="+maxAlive+"; no timeout or invalid sampled state");return 0;
 }catch(Exception ex){Console.Error.WriteLine("FAIL "+ex.Message);return 1;}}
}
