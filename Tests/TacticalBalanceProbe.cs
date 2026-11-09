using System;
using System.Collections.Generic;
using System.Globalization;
using EmberBastion;
class TacticalBalanceProbe {
 static string F(double n){return Math.Round(n,2).ToString(CultureInfo.InvariantCulture);}
 static int Main(string[] args){try{
  string profile=args[0];int rules=profile=="R3"?3:4;
  Console.WriteLine("profile,seed,mode,field,policy,ember,result,wave,peak,seconds,over80_seconds,gold,invested,level3,cards,card_ids,wave_peaks,wave_gold");
  foreach(uint seed in new uint[]{29,41,57,73})for(int mode=0;mode<3;mode++)for(int field=0;field<Balance.Fields.Length;field++)
  foreach(string policy in new[]{"sword","archer","bomb","swordB","smart","smartB","bombA","bombB","sniper","smartBFrost","smartBSmith","smartBPrior"})foreach(bool ember in new[]{false,true}){
   var b=Battle.CreateForRules(seed,field,rules);b.SetDifficulty(mode);int ticks=0,swapped=-1;double over80=0;var cards=new List<int>();int[] peaks=new int[b.WaveCount+1],goldAtWave=new int[b.WaveCount+1];
   b.WaveStarted=x=>goldAtWave[x.Wave]=x.Gold;
   for(int slot=0;slot<b.Field.Slots.Length;slot++)if(Smart.Kind(field,slot)==1){swapped=slot;break;}
   while(b.Phase!=Phase.Won&&b.Phase!=Phase.Lost&&ticks<120000){
    if(b.ReinforcementPending){int card=b.Reinforcements.Choices()[0];cards.Add(card);if(!b.ChooseReinforcement(card))throw new Exception("Choice failed");continue;}
    if(ticks%60==0){
     for(int slot=0;slot<b.Field.Slots.Length;slot++)if(b.At(slot)==null){int kind=policy.StartsWith("smart")?Smart.Kind(field,slot):policy=="archer"||policy=="sniper"?1:policy.StartsWith("bomb")?2:0;
      if(slot==swapped&&policy=="smartBFrost")kind=3;if(slot==swapped&&policy=="smartBSmith")kind=4;
      if(b.Gold>=Balance.Troops[kind].Cost)b.Place(kind,slot);
     }
     foreach(var u in b.Units){if(policy=="smartBPrior"&&(u.Kind==1||u.Kind==3))b.SetPriority(u.Slot,2);
      if(u.Level==1&&b.Gold>=50)b.Upgrade(u.Slot);else if(u.Level==2&&b.Gold>=90){int branch=policy=="swordB"||(policy.StartsWith("smartB")&&u.Kind==0)||policy=="bombB"||policy=="sniper"?2:policy=="bombA"?1:0;if(branch>0)b.UpgradeBranch(u.Slot,branch);}
     }
    }
    if(b.Phase==Phase.Preparation&&!b.Start())throw new Exception("Start failed");
    if(ember&&b.EmberCD<=0&&b.Enemies.Count>=3)b.Ember(b.Enemies[b.Enemies.Count/2].Position);
    b.Step(1.0/60);ticks++;peaks[b.Wave]=Math.Max(peaks[b.Wave],b.Enemies.Count);if(b.Enemies.Count>=80)over80+=1.0/60;
   }
   int invested=0,level3=0;foreach(var u in b.Units){invested+=u.Invested;if(u.Level==3)level3++;}
   if(ticks==120000)throw new Exception("Stalled battle "+profile+" "+seed+" "+mode+" "+field+" "+policy);
   Console.WriteLine(profile+","+seed+","+mode+","+field+","+policy+","+(ember?1:0)+","+b.Phase+","+b.Wave+","+b.PeakAlive+","+F(b.Time)+","+F(over80)+","+b.Gold+","+invested+","+level3+","+cards.Count+","+string.Join(";",cards)+","+string.Join(";",peaks)+","+string.Join(";",goldAtWave));
  }
  return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
