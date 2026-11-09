using System;
using System.Collections.Generic;
using System.Globalization;
using EmberBastion;
class TacticalBalanceProbe {
 static string F(double n){return Math.Round(n,2).ToString(CultureInfo.InvariantCulture);}
 static int BestSlot(int field,string policy){int best=-1;double bestScore=-1;var f=Balance.Fields[field];for(int slot=0;slot<f.Slots.Length;slot++){if(Smart.Kind(field,slot)!=1)continue;double score=0;if(policy.StartsWith("smith")){double range=policy=="smithWide"?220:160;for(int j=0;j<f.Slots.Length;j++)if(j!=slot&&Point.Distance(f.Slots[slot],f.Slots[j])<=range)score++;}else{for(double d=0;d<f.Perimeter;d+=16){Point p=field==0?Balance.Path(d):f.Path(d);if(Point.Distance(p,f.Slots[slot])<=180)score++;}}if(score>bestScore){bestScore=score;best=slot;}}return best;}
 static int Main(string[] args){try{
  string profile=args[0];int rules=profile=="R3"?3:4;bool roles=args.Length>1&&args[1]=="roles";
  string[] policies=roles?new[]{"smartB","fullFront","fullStrong","fullFast","fullBoss","rangedFirst","frostWide","frostFreeze","frostBoss","frostFirst","smithWide","smithDamage","smithFirst","smithCurrent"}:new[]{"sword","archer","bomb","swordB","smart","smartB","bombA","bombB","sniper","smartBFrost","smartBSmith","smartBPrior"};
  Console.WriteLine("profile,seed,mode,field,policy,ember,result,wave,peak,seconds,over80_seconds,gold,invested,level3,cards,card_ids,wave_peaks,wave_gold,swap_slot,support_samples,support_targets");
  foreach(uint seed in new uint[]{29,41,57,73})for(int mode=0;mode<3;mode++)for(int field=0;field<Balance.Fields.Length;field++)
  foreach(string policy in policies)foreach(bool ember in new[]{false,true}){
   var b=Battle.CreateForRules(seed,field,rules);b.SetDifficulty(mode);int ticks=0,swapped=-1,supportSamples=0,supportTargets=0;double over80=0;var cards=new List<int>();int[] peaks=new int[b.WaveCount+1],goldAtWave=new int[b.WaveCount+1];
   b.WaveStarted=x=>goldAtWave[x.Wave]=x.Gold;
   for(int slot=0;slot<b.Field.Slots.Length;slot++)if(Smart.Kind(field,slot)==1){swapped=slot;break;}
   if(roles&&policy!="smithCurrent"&&(policy.StartsWith("frost")||policy.StartsWith("smith")))swapped=BestSlot(field,policy);
   while(b.Phase!=Phase.Won&&b.Phase!=Phase.Lost&&ticks<120000){
    if(b.ReinforcementPending){int card=b.Reinforcements.Choices()[0];cards.Add(card);if(!b.ChooseReinforcement(card))throw new Exception("Choice failed");continue;}
    if(ticks%60==0){
     for(int slot=0;slot<b.Field.Slots.Length;slot++)if(b.At(slot)==null){int kind=roles||policy.StartsWith("smart")?Smart.Kind(field,slot):policy=="archer"||policy=="sniper"?1:policy.StartsWith("bomb")?2:0;
      if(slot==swapped&&(policy=="smartBFrost"||policy.StartsWith("frost")))kind=3;if(slot==swapped&&(policy=="smartBSmith"||policy.StartsWith("smith")))kind=4;
      if(b.Gold>=Balance.Troops[kind].Cost)b.Place(kind,slot);
     }
     var order=new List<Unit>(b.Units);if(roles&&policy!="smartB")order.Sort((a,c)=>a.Slot.CompareTo(c.Slot));if(roles&&(policy=="rangedFirst"||policy=="frostFirst"||policy=="smithFirst"))order.Sort((a,c)=>{int ak=policy=="rangedFirst"?(a.Kind==1?0:1):(a.Kind==(policy=="frostFirst"?3:4)?0:1),ck=policy=="rangedFirst"?(c.Kind==1?0:1):(c.Kind==(policy=="frostFirst"?3:4)?0:1);return ak!=ck?ak.CompareTo(ck):a.Slot.CompareTo(c.Slot);});
     foreach(var u in order){if(roles&&policy!="smartB"&&(u.Kind==1||u.Kind==3))b.SetPriority(u.Slot,policy=="fullFront"?0:policy=="fullStrong"?1:policy=="fullBoss"||policy=="frostBoss"?3:2);if(policy=="smartBPrior"&&(u.Kind==1||u.Kind==3))b.SetPriority(u.Slot,2);
      if(u.Level==1&&b.Gold>=50)b.Upgrade(u.Slot);else if(u.Level==2&&b.Gold>=90){int branch=policy=="swordB"||(policy.StartsWith("smartB")&&u.Kind==0)||policy=="bombB"||policy=="sniper"?2:policy=="bombA"?1:0;if(roles&&policy!="smartB")branch=u.Kind==0?2:u.Kind==3?(policy=="frostWide"||policy=="frostFirst"?1:2):u.Kind==4?(policy=="smithWide"?1:2):1;if(branch>0)b.UpgradeBranch(u.Slot,branch);}
     }
    }
    if(ticks%60==0&&roles&&policy.StartsWith("smith")){supportSamples++;foreach(var u in b.Units){double haste,bonus;b.SupportBonuses(u,out haste,out bonus);if(haste>0||bonus>0)supportTargets++;}}
    if(b.Phase==Phase.Preparation&&!b.Start())throw new Exception("Start failed");
    if(ember&&b.EmberCD<=0&&b.Enemies.Count>=3)b.Ember(b.Enemies[b.Enemies.Count/2].Position);
    b.Step(1.0/60);ticks++;peaks[b.Wave]=Math.Max(peaks[b.Wave],b.Enemies.Count);if(b.Enemies.Count>=80)over80+=1.0/60;
   }
   int invested=0,level3=0;foreach(var u in b.Units){invested+=u.Invested;if(u.Level==3)level3++;}
   if(ticks==120000)throw new Exception("Stalled battle "+profile+" "+seed+" "+mode+" "+field+" "+policy);
   Console.WriteLine(profile+","+seed+","+mode+","+field+","+policy+","+(ember?1:0)+","+b.Phase+","+b.Wave+","+b.PeakAlive+","+F(b.Time)+","+F(over80)+","+b.Gold+","+invested+","+level3+","+cards.Count+","+string.Join(";",cards)+","+string.Join(";",peaks)+","+string.Join(";",goldAtWave)+","+swapped+","+supportSamples+","+supportTargets);
  }
  return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
