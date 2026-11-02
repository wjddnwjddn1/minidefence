using System;
using System.Collections.Generic;
using EmberBastion;
class PressureProbe {
 static int Main(){try{var counts=new Dictionary<Group,int>();foreach(var field in Balance.Fields)foreach(var wave in field.Waves)foreach(var group in wave)if(!counts.ContainsKey(group))counts.Add(group,group.Count);
 Console.WriteLine("factor,seed,mode,field,pattern,ember,result,wave,peak,kills");
 foreach(double factor in new[]{1.0,1.25,1.5,1.75})foreach(uint seed in new uint[]{13,29,41}){foreach(var pair in counts)pair.Key.Count=pair.Key.Kind==4?pair.Value:(int)Math.Ceiling(pair.Value*factor);
 foreach(int mode in new[]{1,2})for(int field=0;field<Balance.Fields.Length;field++)foreach(int pattern in new[]{0,1,2,4,5})foreach(bool ember in new[]{false,true}){
 var b=new Battle(seed,field);b.SetDifficulty(mode);int ticks=0;while(b.Phase!=Phase.Won&&b.Phase!=Phase.Lost&&ticks<120000){
 if(ticks%60==0){for(int slot=0;slot<b.Field.Slots.Length;slot++)if(b.At(slot)==null){int kind=pattern==4?slot%4:pattern==5?new[]{0,0,1,2}[slot%4]:pattern;if(b.Gold>=Balance.Troops[kind].Cost)b.Place(kind,slot);}foreach(var u in b.Units)if(u.Level==1&&b.Gold>=50)b.Upgrade(u.Slot);}
 if(b.Phase==Phase.Preparation&&!b.Start())throw new Exception("start failed");if(ember&&b.EmberCD<=0&&b.Enemies.Count>=3)b.Ember(b.Enemies[b.Enemies.Count/2].Position);b.Step(1.0/60);ticks++;}
 string outcome=b.Phase==Phase.Won?"Won":b.Phase==Phase.Lost?"Lost":"Stalled";Console.WriteLine(factor.ToString(System.Globalization.CultureInfo.InvariantCulture)+","+seed+","+mode+","+field+","+pattern+","+(ember?1:0)+","+outcome+","+b.Wave+","+b.PeakAlive+","+b.Kills);
 }}return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
