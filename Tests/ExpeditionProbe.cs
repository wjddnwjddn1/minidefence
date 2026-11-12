using System;
using System.Collections.Generic;
using System.Globalization;
using EmberBastion;
// r37 (Claude) measurement tool for the roguelite run, on the real engine. Automatic policies, not human play.
//   ExpeditionProbe laps            how many laps enemies finish in ordinary rules-4 battles (to price warmth)
//   ExpeditionProbe run [seeds]     whole expeditions under several buying and boon policies
static class Policy {
 static readonly Dictionary<int,int[]> cache=new Dictionary<int,int[]>();
 static double Cover(FieldDef f,Point s,double range,double min){int n=0,inside=0;for(double d=0;d<f.Perimeter;d+=16){n++;double dist=Point.Distance(f.Path(d),s);if(dist<=range&&dist>=min)inside++;}return (double)inside/n;}
 // Swords on the half of the slots that touch the most road in melee reach; the rest alternate archer, bomb and frost by long reach.
 public static int Kind(Battle b,int slot){int[] a;int key=b.FieldIndex*10+(int)b.Mode;if(!cache.TryGetValue(key,out a)){var f=b.Field;var S=f.Slots;int m=S.Length;var idx=new List<int>();for(int i=0;i<m;i++)idx.Add(i);idx.Sort((x,y)=>{int c=Cover(f,S[y],120,0).CompareTo(Cover(f,S[x],120,0));return c!=0?c:x.CompareTo(y);});a=new int[m];var rest=new List<int>();for(int i=0;i<m;i++){if(i<m/2)a[idx[i]]=0;else rest.Add(idx[i]);}rest.Sort((x,y)=>{int c=Cover(f,S[y],210,60).CompareTo(Cover(f,S[x],210,60));return c!=0?c:x.CompareTo(y);});int[] cycle={1,2,1,3};for(int i=0;i<rest.Count;i++)a[rest[i]]=cycle[i%cycle.Length];cache[key]=a;}return a[slot];}
 // skill 0: three level-1 troops and nothing else. 1: fill and upgrade to level 2. 2: also take level-3 branches.
 public static void Buy(Battle b,int skill){
  int placed=b.Units.Count;
  for(int slot=0;slot<b.Field.Slots.Length;slot++)if(b.At(slot)==null){if(skill==0&&placed>=3)break;int kind=Kind(b,slot);if(b.Gold>=Balance.Troops[kind].Cost&&b.Place(kind,slot))placed++;}
  if(skill==0)return;
  foreach(var u in b.Units){if(u.Level==1&&b.Gold>=50)b.Upgrade(u.Slot);else if(skill>=2&&u.Level==2&&b.Gold>=90)b.UpgradeBranch(u.Slot,u.Kind==0?2:1);}
 }
 // Plays one battle. Counts finished laps from outside so that ordinary battles can be measured too.
 public static void Play(Battle b,int skill,out int laps,out int bossLaps){
  laps=0;bossLaps=0;var seen=new Dictionary<int,int>();int ticks=0;
  while(b.Phase!=Phase.Won&&b.Phase!=Phase.Lost&&ticks<600000){
   if(ticks%60==0)Buy(b,skill);
   if(b.Phase==Phase.Preparation&&!b.Start())throw new Exception("start failed");
   if(b.ReinforcementPending)b.ChooseReinforcement(b.Reinforcements.Choices()[0]);
   b.Step(Balance.Tick);ticks++;
   foreach(var e in b.Enemies){int old;seen.TryGetValue(e.Id,out old);if(e.Laps>old){if(e.Kind==4)bossLaps+=e.Laps-old;else laps+=e.Laps-old;seen[e.Id]=e.Laps;}}
  }
 }
 // Camp choice. 0 first offer, 1 damage for the classes in use, 2 keep the ember warm, 3 economy.
 public static int Camp(Expedition run,int style){
  var offers=run.Offers();int[] order;
  if(style==1)order=new[]{Boon.SwordDrill,Boon.StrongBow,Boon.WarDrum,Boon.PowderMix,Boon.FrostCore,Boon.Spyglass,Boon.Rekindle,Boon.Spoils,Boon.WarChest};
  else if(style==2)order=run.Warmth<=80?new[]{Boon.Rekindle,Boon.FrostCore,Boon.WarDrum,Boon.SwordDrill,Boon.StrongBow,Boon.PowderMix,Boon.Spyglass,Boon.Spoils,Boon.WarChest}:new[]{Boon.FrostCore,Boon.WarDrum,Boon.SwordDrill,Boon.StrongBow,Boon.PowderMix,Boon.Spyglass,Boon.Spoils,Boon.WarChest,Boon.Rekindle};
  else if(style==3)order=new[]{Boon.Spoils,Boon.WarChest,Boon.WarDrum,Boon.SwordDrill,Boon.StrongBow,Boon.PowderMix,Boon.FrostCore,Boon.Spyglass,Boon.Rekindle};
  else return offers[0];
  foreach(int boon in order)if(Array.IndexOf(offers,boon)>=0)return boon;return offers[0];
 }
}
class ExpeditionProbe {
 static string F(double v){return v.ToString("0.#",CultureInfo.InvariantCulture);}
 static int Main(string[] args){
  try{
   string command=args.Length>0?args[0]:"run";
   if(command=="laps"){
    Console.WriteLine("field,skill,seed,result,wave,peak,kills,laps,bossLaps,seconds");
    for(int field=0;field<Balance.Fields.Length;field++)foreach(int skill in new[]{0,1,2})foreach(uint seed in new uint[]{29,41,57,73}){
     var b=Battle.CreateForRules(seed,field,4);int laps,boss;Policy.Play(b,skill,out laps,out boss);
     Console.WriteLine(field+","+skill+","+seed+","+b.Phase+","+b.Wave+","+b.PeakAlive+","+b.Kills+","+laps+","+boss+","+F(b.Time));
    }
    return 0;
   }
   int seeds=args.Length>1?int.Parse(args[1]):8;
   Console.WriteLine("seed,skill,camp,result,stage,warmth,kills,boons,stage0,stage1,stage2,stage3");
   for(uint seed=1;seed<=seeds;seed++)foreach(int skill in new[]{0,1,2})foreach(int camp in new[]{0,1,2,3}){
    var run=Expedition.Start(seed*7919);var stages=new List<string>();
    while(run.State==Expedition.Status.Battle){
     var b=run.CreateBattle();int before=run.Warmth,laps,boss;Policy.Play(b,skill,out laps,out boss);
     if(!run.Complete(b))throw new Exception("complete failed");
     stages.Add(b.Phase.ToString().Substring(0,1)+"/w"+b.Wave+"/p"+b.PeakAlive+"/-"+(before-run.Warmth)+"/"+F(b.Time)+"s");
     if(run.State==Expedition.Status.Camp&&!run.Choose(Policy.Camp(run,camp)))throw new Exception("choose failed");
    }
    string boons="";for(int i=0;i<Boon.Count;i++)boons+=run.Count(i);
    while(stages.Count<4)stages.Add("");
    Console.WriteLine(seed+","+skill+","+camp+","+run.State+","+run.Stage+","+run.Warmth+","+run.Kills+","+boons+","+string.Join(",",stages.ToArray()));
   }
   return 0;
  }catch(Exception e){Console.Error.WriteLine(e);return 1;}
 }
}
