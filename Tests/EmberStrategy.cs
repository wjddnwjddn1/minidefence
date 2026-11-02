using System;
using EmberBastion;
class EmberStrategy {
 static void Main(){Console.WriteLine("mode,field,seed,branch,result,wave,peak,kills,gold,time");foreach(int mode in new[]{0,1,2})for(int field=0;field<3;field++)foreach(uint seed in new uint[]{1,13,42})for(int branch=0;branch<=2;branch++){
  var b=new Battle(seed,field);b.SetDifficulty(mode);int ticks=0;bool chose=false;while(b.Phase!=Phase.Won&&b.Phase!=Phase.Lost&&ticks<120000){
   if(ticks%60==0){if(!chose&&branch!=0&&b.Units.Count>=2&&b.Gold>=100){chose=b.UpgradeEmber(branch);}for(int slot=0;slot<b.Field.Slots.Length;slot++)if(b.At(slot)==null){int kind=slot%4;if(b.Gold>=Balance.Troops[kind].Cost)b.Place(kind,slot);}foreach(var unit in b.Units)if(unit.Level==1&&b.Gold>=50)b.Upgrade(unit.Slot);}
   if(b.Phase==Phase.Preparation&&!b.Start())throw new Exception("cannot start");
   if(b.Phase==Phase.Fighting&&b.EmberCD<=0&&b.Enemies.Count>=3){Enemy best=null;int bestCount=0;foreach(var candidate in b.Enemies){int count=0;foreach(var e in b.Enemies)if(Point.Distance(candidate.Position,e.Position)<=b.EmberRadius)count++;if(count>bestCount){best=candidate;bestCount=count;}}if(best!=null&&bestCount>=3)b.Ember(best.Position);}
   b.Step(1.0/60);ticks++;
  }
  if(b.Phase!=Phase.Won&&b.Phase!=Phase.Lost)throw new Exception("simulation timeout");if(branch!=0&&!chose)throw new Exception("branch not purchased");Console.WriteLine((mode==2?"hard":mode==1?"fast":"normal")+","+field+","+seed+","+branch+","+b.Phase+","+b.Wave+","+b.PeakAlive+","+b.Kills+","+b.Gold+","+ticks);
 }}
}
