using System;
using System.Collections.Generic;
using System.Globalization;
using EmberBastion;
// r37 (Claude) measurement tool for the roguelite run, on the real engine. Automatic policies, not human play.
//   ExpeditionProbe laps            how many laps enemies finish in ordinary rules-4 battles (to price warmth)
//   ExpeditionProbe run [seeds]     whole expeditions under several buying and boon policies
//   ExpeditionProbe doctrine [seeds] one battle per field with one doctrine and one army shape: does the doctrine change the best army?
//   ExpeditionProbe build [seeds]    the same with three-boon builds
static class Policy {
 static readonly Dictionary<int,int[]> cache=new Dictionary<int,int[]>();
 static double Cover(FieldDef f,Point s,double range,double min){int n=0,inside=0;for(double d=0;d<f.Perimeter;d+=16){n++;double dist=Point.Distance(f.Path(d),s);if(dist<=range&&dist>=min)inside++;}return (double)inside/n;}
 // Swords on the half of the slots that touch the most road in melee reach; the rest alternate archer, bomb and frost by long reach.
 public static int Kind(Battle b,int slot){int[] a;int key=b.FieldIndex*10+(int)b.Mode;if(!cache.TryGetValue(key,out a)){var f=b.Field;var S=f.Slots;int m=S.Length;var idx=new List<int>();for(int i=0;i<m;i++)idx.Add(i);idx.Sort((x,y)=>{int c=Cover(f,S[y],120,0).CompareTo(Cover(f,S[x],120,0));return c!=0?c:x.CompareTo(y);});a=new int[m];var rest=new List<int>();for(int i=0;i<m;i++){if(i<m/2)a[idx[i]]=0;else rest.Add(idx[i]);}rest.Sort((x,y)=>{int c=Cover(f,S[y],210,60).CompareTo(Cover(f,S[x],210,60));return c!=0?c:x.CompareTo(y);});int[] cycle={1,2,1,3};for(int i=0;i<rest.Count;i++)a[rest[i]]=cycle[i%cycle.Length];cache[key]=a;}return a[slot];}
 // Army shapes for the doctrine experiment. 0 mix, 1 swords, 2 archers, 3 powder, 4 frost, 5 smith-centred.
 public static readonly string[] Shapes={"mix","swords","archers","powder","frost","smith"};
 static readonly Dictionary<int,int[]> shapeCache=new Dictionary<int,int[]>();
 public static int ShapeKind(Battle b,int shape,int slot){
  int[] a;int key=b.FieldIndex*100+shape;if(shapeCache.TryGetValue(key,out a))return a[slot];
  var f=b.Field;var S=f.Slots;int m=S.Length;a=new int[m];var free=new List<int>();for(int i=0;i<m;i++)free.Add(i);
  if(shape==5){var byNeighbours=new List<int>(free);byNeighbours.Sort((x,y)=>{int nx=0,ny=0;for(int i=0;i<m;i++){if(i!=x&&Point.Distance(S[x],S[i])<=160)nx++;if(i!=y&&Point.Distance(S[y],S[i])<=160)ny++;}return ny!=nx?ny.CompareTo(nx):x.CompareTo(y);});for(int i=0;i<2&&i<byNeighbours.Count;i++){a[byNeighbours[i]]=4;free.Remove(byNeighbours[i]);}}
  free.Sort((x,y)=>{int c=Cover(f,S[y],120,0).CompareTo(Cover(f,S[x],120,0));return c!=0?c:x.CompareTo(y);});
  int swords=shape==1?free.Count*3/4:shape==0||shape==5?free.Count/2:shape==4?free.Count/3:free.Count/4;var rest=new List<int>();for(int i=0;i<free.Count;i++){if(i<swords)a[free[i]]=0;else rest.Add(free[i]);}
  rest.Sort((x,y)=>{int c=Cover(f,S[y],210,60).CompareTo(Cover(f,S[x],210,60));return c!=0?c:x.CompareTo(y);});
  int[] cycle=shape==1||shape==2?new[]{1}:shape==3?new[]{2,2,1}:shape==4?new[]{3,1,1}:new[]{1,2,1,3};for(int i=0;i<rest.Count;i++)a[rest[i]]=cycle[i%cycle.Length];
  shapeCache[key]=a;return a[slot];
 }
 public static void BuyShape(Battle b,int shape){
  // attackers first: smiths join once four attackers stand, so the opening is never a smith with nobody to help
  for(int pass=0;pass<2;pass++)for(int slot=0;slot<b.Field.Slots.Length;slot++)if(b.At(slot)==null){int kind=ShapeKind(b,shape,slot);if((kind==4)!=(pass==1))continue;if(kind==4&&b.Units.FindAll(u=>u.Kind!=4).Count<4)continue;if(b.Gold>=Balance.Troops[kind].Cost)b.Place(kind,slot);}
  foreach(var u in b.Units){if(u.Level==1&&b.Gold>=50)b.Upgrade(u.Slot);else if(u.Level==2&&b.Gold>=90)b.UpgradeBranch(u.Slot,u.Kind==0?2:1);}
 }
 public static void PlayShape(Battle b,int shape){int ticks=0;while(b.Phase!=Phase.Won&&b.Phase!=Phase.Lost&&ticks<600000){if(ticks%60==0)BuyShape(b,shape);if(b.Phase==Phase.Preparation&&!b.Start())throw new Exception("start failed");if(b.ReinforcementPending)b.ChooseReinforcement(b.Reinforcements.Choices()[0]);b.Step(Balance.Tick);ticks++;}}
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
   if(command=="doctrine"){
    Console.WriteLine("field,doctrine,shape,seed,result,wave,peak,warmthLost,kills,seconds");
    for(int field=0;field<Balance.Fields.Length;field++)for(int doctrine=-1;doctrine<Boon.BattleCount;doctrine++){if(doctrine>=0&&!Boon.IsDoctrine(doctrine))continue;
     for(int shape=0;shape<Policy.Shapes.Length;shape++)for(uint seed=1;seed<=seeds;seed++){
      var boons=new int[Boon.BattleCount];if(doctrine>=0)boons[doctrine]=1;var b=Battle.CreateExpedition(seed*7919+(uint)field,field,100,boons,field,0);Policy.PlayShape(b,shape);
      Console.WriteLine(field+","+doctrine+","+Policy.Shapes[shape]+","+seed+","+b.Phase+","+b.Wave+","+b.PeakAlive+","+(b.Phase==Phase.Won?100-b.HP:100)+","+b.Kills+","+F(b.Time));
     }}
    return 0;
   }
   if(command=="order"){
    // whole runs in each of the six field orders: does the order change how a run ends?
    int[][] orders={new[]{1,2,3},new[]{1,3,2},new[]{2,1,3},new[]{2,3,1},new[]{3,1,2},new[]{3,2,1}};
    Console.WriteLine("order,skill,seed,result,cleared,warmth,losses");
    foreach(var order in orders)foreach(int skill in new[]{1,2})for(uint seed=1;seed<=seeds;seed++){
     var run=Expedition.Start(seed*7919);string losses="";
     while(run.State==Expedition.Status.Battle){var b=run.CreateBattle();int before=run.Warmth,laps,boss;Policy.Play(b,skill,out laps,out boss);if(!run.Complete(b))throw new Exception("complete failed");losses+=(losses.Length>0?"/":"")+(before-run.Warmth);
      if(run.State==Expedition.Status.Camp&&!run.Choose(Policy.Camp(run,1),order[run.Stage]))throw new Exception("choose failed");}
     Console.WriteLine(""+order[0]+order[1]+order[2]+","+skill+","+seed+","+run.State+","+run.ClearedStages+","+run.Warmth+","+losses);
    }
    return 0;
   }
   if(command=="build"){
    // three-boon builds a run can end with, against every army shape: does a build have its own best army?
    string[] names={"none","swordBuild","archerBuild","powderBuild","frostBuild","smithBuild"};
    int[][] builds={new int[0],new[]{Boon.Phalanx,Boon.SwordDrill,Boon.WarDrum},new[]{Boon.RapidFire,Boon.StrongBow,Boon.Spyglass},new[]{Boon.ChainBlast,Boon.PowderMix,Boon.Spoils},new[]{Boon.DeepFrost,Boon.Shatter,Boon.FrostCore},new[]{Boon.FieldForge,Boon.WarDrum,Boon.SwordDrill}};
    Console.WriteLine("field,doctrine,shape,seed,result,wave,peak,warmthLost,kills,seconds");
    for(int field=0;field<Balance.Fields.Length;field++)for(int build=0;build<builds.Length;build++)for(int shape=0;shape<Policy.Shapes.Length;shape++)for(uint seed=1;seed<=seeds;seed++){
     var boons=new int[Boon.BattleCount];foreach(int boon in builds[build])boons[boon]++;var b=Battle.CreateExpedition(seed*7919+(uint)field,field,100,boons,field,0);Policy.PlayShape(b,shape);
     Console.WriteLine(field+","+names[build]+","+Policy.Shapes[shape]+","+seed+","+b.Phase+","+b.Wave+","+b.PeakAlive+","+(b.Phase==Phase.Won?100-b.HP:100)+","+b.Kills+","+F(b.Time));
    }
    return 0;
   }
   // EXPEDITION_GRADE=0..5 measures an ember grade (r43).
   int grade=0;int.TryParse(Environment.GetEnvironmentVariable("EXPEDITION_GRADE")??"0",out grade);
   // EXPEDITION_ROAD=0 plain, 1 supply, 2 hard: the road this policy takes whenever a camp offers it (r43).
   int roadWanted=0;int.TryParse(Environment.GetEnvironmentVariable("EXPEDITION_ROAD")??"0",out roadWanted);
   Console.WriteLine("seed,skill,camp,result,stage,warmth,kills,boons,stage0,stage1,stage2,stage3");
   for(uint seed=1;seed<=seeds;seed++)foreach(int skill in new[]{0,1,2})foreach(int camp in new[]{0,1,2,3}){
    var run=Expedition.Start(seed*7919,grade);var stages=new List<string>();string roads="";
    while(run.State==Expedition.Status.Battle){
     var b=run.CreateBattle();int before=run.Warmth,laps,boss;Policy.Play(b,skill,out laps,out boss);
     if(!run.Complete(b))throw new Exception("complete failed");
     stages.Add(b.Phase.ToString().Substring(0,1)+"/w"+b.Wave+"/p"+b.PeakAlive+"/-"+(before-run.Warmth)+"/"+F(b.Time)+"s");
     if(run.State==Expedition.Status.Camp){int[] open=run.NextFields();int go=open[0];foreach(int f in open)if(run.NextRoad(f)==roadWanted){go=f;break;}if(roadWanted==0)foreach(int f in open)if(run.NextRoad(f)==0){go=f;break;}roads+=run.NextRoad(go);if(!run.Choose(Policy.Camp(run,camp),go))throw new Exception("choose failed");}
    }
    string boons="";for(int i=0;i<Boon.Count;i++)boons+=run.Count(i);
    while(stages.Count<4)stages.Add("");
    Console.WriteLine(seed+","+skill+","+camp+","+run.State+","+run.Stage+","+run.Warmth+","+run.Kills+","+boons+","+string.Join(",",stages.ToArray())+",roads "+roads);
   }
   return 0;
  }catch(Exception e){Console.Error.WriteLine(e);return 1;}
 }
}
