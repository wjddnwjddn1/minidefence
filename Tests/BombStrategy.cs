using System;
using EmberBastion;
class BombStrategy {
 static int Main(){try{Console.WriteLine("mode,field,seed,branch,result,wave,peak,kills,branch_count");foreach(int mode in new[]{0,1,2})for(int field=0;field<3;field++)foreach(uint seed in new uint[]{1,13,42})foreach(int branch in new[]{1,2}){
  var b=new Battle(seed,field);b.SetDifficulty(mode);int ticks=0,bought=0;while(b.Phase!=Phase.Won&&b.Phase!=Phase.Lost&&ticks<120000){
   if(ticks%60==0){foreach(var unit in b.Units){if(unit.Kind!=2)continue;if(unit.Level==1&&b.Gold>=50)b.Upgrade(unit.Slot);if(unit.Level==2&&b.Gold>=90&&b.UpgradeBranch(unit.Slot,branch))bought++;}bool reserve=bought==0&&b.Units.Exists(u=>u.Kind==2&&u.Level<3);if(!reserve){for(int slot=0;slot<b.Field.Slots.Length;slot++)if(b.At(slot)==null){int kind=slot%4;if(b.Gold>=Balance.Troops[kind].Cost)b.Place(kind,slot);}foreach(var unit in b.Units)if(unit.Kind!=2&&unit.Level==1&&b.Gold>=50)b.Upgrade(unit.Slot);}}
   if(b.Phase==Phase.Preparation&&!b.Start())throw new Exception("cannot start");
   if(b.Phase==Phase.Fighting&&b.EmberCD<=0&&b.Enemies.Count>=3){Enemy best=null;int max=0;foreach(var e in b.Enemies){int count=0;foreach(var other in b.Enemies)if(Point.Distance(e.Position,other.Position)<=b.EmberRadius)count++;if(count>max){best=e;max=count;}}if(best!=null&&max>=3)b.Ember(best.Position);}
   b.Step(1.0/60);ticks++;
  }
  if(b.Phase!=Phase.Won&&b.Phase!=Phase.Lost)throw new Exception("timeout");Console.WriteLine((mode==0?"normal":mode==1?"fast":"hard")+","+field+","+seed+","+branch+","+b.Phase+","+b.Wave+","+b.PeakAlive+","+b.Kills+","+bought);
 }return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
