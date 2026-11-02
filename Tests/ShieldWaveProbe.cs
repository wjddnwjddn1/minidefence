using System;
using EmberBastion;
using System.Collections.Generic;
class ShieldWaveProbe {
 static int Main(){try{foreach(var F in Balance.Fields)for(int w=2;w<F.Waves.Length;w+=3)foreach(var g in F.Waves[w])if(g.Kind==0)g.Kind=2;foreach(int seed in new[]{29,41,57,73})foreach(int scenario in new[]{16,24}){ Balance.Enemies[2].Armor=scenario;foreach(bool fast in new[]{false,true})for(int field=0;field<Balance.Fields.Length;field++)for(int pattern=0;pattern<7;pattern++)foreach(bool ember in new[]{false,true}){
  var b=new Battle((uint)seed,field);b.SetFastWaves(fast);int ticks=0,peak=0;var laps=new Dictionary<int,int>();string label=pattern==4?"mixed":pattern==5?"roles":pattern==6?"swordB":Balance.Troops[pattern].Name;
  while(b.Phase!=Phase.Won&&b.Phase!=Phase.Lost&&ticks<120000){
   if(ticks%60==0){for(int slot=0;slot<b.Field.Slots.Length;slot++)if(b.At(slot)==null){int kind=pattern==4?slot%4:pattern==5?new[]{0,0,1,2}[slot%4]:pattern==6?0:pattern;if(b.Gold>=Balance.Troops[kind].Cost)b.Place(kind,slot);}foreach(var u in b.Units){if(u.Level==1&&b.Gold>=50)b.Upgrade(u.Slot);else if(pattern==6&&u.Level==2&&b.Gold>=90)b.UpgradeBranch(u.Slot,2);}}
   if(b.Phase==Phase.Preparation&&!b.Start())throw new Exception("start failed");
   if(ember&&b.EmberCD<=0&&b.Enemies.Count>=3)b.Ember(b.Enemies[b.Enemies.Count/2].Position);
   b.Step(1.0/60);ticks++;peak=Math.Max(peak,b.Enemies.Count);
  }
  string outcome=b.Phase==Phase.Won?"Won":b.Phase==Phase.Lost?"Lost":"Stalled";
  Console.WriteLine(seed+","+scenario+","+(fast?"fast":"normal")+","+field+","+label+","+(ember?"ember":"no-ember")+","+outcome+","+b.Wave+","+peak+","+b.Kills);
 }}return 0;}catch(Exception ex){Console.Error.WriteLine(ex.Message);return 1;}}
}
