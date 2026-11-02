using System;
using EmberBastion;
// Isolated check: the field values of a pre-r12 preparation save (no rulesVersion field) restore on the old rules and play to the end.
class RealSaveShapeProbe {
 static PreparationUnit U(int slot,double cd){return new PreparationUnit{slot=slot,kind=0,level=1,invested=65,prepSpent=0,priority=0,branch=0,cooldown=cd,stun=0};}
 static int Main(){
  var d=new PreparationData{version=1,field=0,wave=1,gold=68,hp=100,kills=6,rng=2399460287,time=8.833333333333352,emberCD=0,units=new[]{U(0,0),U(1,.8),U(2,.5166666666666658)}};
  var b=Battle.RestorePreparation(d);
  Console.WriteLine("restored rules="+b.RulesVersion+" phase="+b.Phase+" wave="+b.Wave+" gold="+b.Gold+" units="+b.Units.Count);
  if(b.RulesVersion!=0||b.Phase!=Phase.Preparation)return 1;
  if(!b.Start())return 2;
  int ticks=0,peak=0,first=0;while(b.Phase==Phase.Fighting&&ticks<120000){if(b.Wave==2&&first==0)first=b.Enemies.Count;if(ticks%60==0){for(int s=0;s<b.Field.Slots.Length;s++)if(b.At(s)==null&&b.Gold>=65)b.Place(0,s);foreach(var u in b.Units)if(u.Level==1&&b.Gold>=50)b.Upgrade(u.Slot);}b.Step(1.0/60);ticks++;peak=Math.Max(peak,b.Enemies.Count);}
  Console.WriteLine("end="+b.Phase+" wave="+b.Wave+" peak="+peak+" rules="+b.RulesVersion+" kills="+b.Kills);
  var fresh=new Battle(2399460287,0);Console.WriteLine("fresh rules="+fresh.RulesVersion);
  return b.Phase==Phase.Fighting?3:0;
 }
}
