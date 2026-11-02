using System;
using EmberBastion;
class RoleMixProbe {
 static int Main(){try{foreach(int scenario in new[]{0,1,2,3}){ Balance.Enemies[2].Armor=scenario==0?16:scenario==1?24:scenario==2?30:24;Balance.Enemies[1].Speed=scenario==0?165:scenario==3?240:200;foreach(bool fast in new[]{false,true})for(int field=0;field<Balance.Fields.Length;field++)for(int pattern=0;pattern<6;pattern++)foreach(bool ember in new[]{false,true}){
  var b=new Battle(13,field);b.SetFastWaves(fast);int ticks=0,peak=0;string label=pattern==4?"mixed":pattern==5?"roles":Balance.Troops[pattern].Name;
  while(b.Phase!=Phase.Won&&b.Phase!=Phase.Lost&&ticks<120000){
   if(ticks%60==0){for(int slot=0;slot<b.Field.Slots.Length;slot++)if(b.At(slot)==null){int kind=pattern==4?slot%4:pattern==5?new[]{0,0,1,2}[slot%4]:pattern;if(b.Gold>=Balance.Troops[kind].Cost)b.Place(kind,slot);}foreach(var u in b.Units)if(u.Level==1&&b.Gold>=50)b.Upgrade(u.Slot);}
   if(b.Phase==Phase.Preparation&&!b.Start())throw new Exception("start failed");
   if(ember&&b.EmberCD<=0&&b.Enemies.Count>=3)b.Ember(b.Enemies[b.Enemies.Count/2].Position);
   b.Step(1.0/60);ticks++;peak=Math.Max(peak,b.Enemies.Count);
  }
  string outcome=b.Phase==Phase.Won?"Won":b.Phase==Phase.Lost?"Lost":"Stalled";
  Console.WriteLine(scenario+","+(fast?"fast":"normal")+","+field+","+label+","+(ember?"ember":"no-ember")+","+outcome+","+b.Wave+","+peak+","+b.Kills);
 }}return 0;}catch(Exception ex){Console.Error.WriteLine(ex.Message);return 1;}}
}
