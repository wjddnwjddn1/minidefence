using System;
using System.Collections.Generic;
namespace EmberBastion {
 // r37 (Claude): the roguelite run. An expedition is a chain of battles on one ember: warmth (castle HP) carries from
 // battle to battle, a camp between battles offers three boons, and each stage raises the enemy threat.
 // Pure rules: no Unity, display, input, persistence or wall-clock dependencies.
 // The per-battle part (Boon, ExpeditionBattleData, Battle.CreateExpedition) lives in Battle.cs so that Battle.cs
 // still compiles on its own.

 [Serializable] public sealed class ExpeditionData {
  public int version=1,stage,warmth,state,kills,pendingGold,grade;public bool abandonedCamp;
  // True for runs started with roads (r43). Runs saved before that keep plain roads, so their battles still match.
  public bool roads;
  // True for runs with the last stand (r44): a fifth battle back at the gate. Runs saved before that end after four.
  public bool finale;
  // Fields in the order this run takes them (r40). Empty in saves made before the route choice: those follow the story order.
  public int[] route;
  public uint seed,randomState;
  public int[] boons,offered,peaks;
 }

 public sealed class Expedition {
  public enum Status {Battle=0,Camp=1,Won=2,Lost=3}
  // The story order: gate, forest road, rocky pass, marsh ferry. A run always starts at the gate; from each camp the
  // player picks the next field among those not yet fought (r40), so this is only the default order.
  public static readonly int[] Route={0,1,2,3};
  public const int OffersPerCamp=3;
  public uint Seed {get;private set;}
  public int Stage {get;private set;}
  public int Warmth {get;private set;}
  public int Kills {get;private set;}
  public int PendingGold {get;private set;}
  public Status State {get;private set;}
  bool abandonedCamp;uint randomState;readonly int[] boons=new int[Boon.Count];int[] offered=new int[0];int[] peaks=new int[Route.Length],route={Route[0],-1,-1,-1};
  Expedition(){}
  // Roads (r41/r43): the way to each field offered at a camp is plain, a supply road or a hard road, so that the choice
  // of the next field is a trade and not only an order. Supply road: more gold for the battle, the detour costs warmth.
  // Hard road: the enemy is one threat step stronger there, but the shelter on the way warms the ember.
  public const int RoadPlain=0,RoadSupply=1,RoadHard=2;
  public const int SupplyGold=100,SupplyWarmth=6,HardWarmth=15;
  bool roads;
  // A pure function of the seed and of the fields fought before `stage`: nothing extra is saved. With one field left the road is plain.
  int RoadFor(int stage,int field){
   if(!roads||stage<1||stage>=Route.Length)return RoadPlain;
   var left=new List<int>();foreach(int f in Route){bool used=false;for(int i=0;i<stage;i++)if(route[i]==f)used=true;if(!used)left.Add(f);}
   int index=left.IndexOf(field);if(left.Count<2||index<0)return RoadPlain;
   uint h=StageSeed(stage+7);int special=(int)(h%(uint)left.Count),kind=((h>>8)&1)==0?RoadSupply:RoadHard;
   // one option always has a special road; each other option has the other special road one time in three
   if(index==special)return kind;
   return ((h>>(12+index*4))&15)%3==0?(kind==RoadSupply?RoadHard:RoadSupply):RoadPlain;
  }
  // At a camp: the road to an offered field. Plain for anything else.
  public int NextRoad(int field){return State==Status.Camp&&Array.IndexOf(NextFields(),field)>=0?RoadFor(Stage+1,field):RoadPlain;}
  // The road taken to a stage already chosen.
  public int RoadAt(int stage){if(stage<0||stage>=route.Length)throw new ArgumentOutOfRangeException("stage");return route[stage]<0?RoadPlain:RoadFor(stage,route[stage]);}
  // At a camp: enemy health on an offered field, as a multiple of the same field in a free battle.
  public double NextHealth(int field){return (1+Boon.ThreatHealth*(Stage+1+(NextRoad(field)==RoadHard?1:0)))*(1+Boon.GradeHealth*Grade);}
  // Ember grade (r43): a harder run for players who have won (Boon.GradeHealth more enemy health per grade, in every battle).
  public const int MaxGrade=Boon.MaxGrade;
  public int Grade {get;private set;}
  public static Expedition Start(uint seed){return Start(seed,0);}
  // The last stand (r44): after the four fields the run comes home and holds the gate once more, at the highest threat.
  // It is one more camp (a fourth boon) and one more battle, so a build has a place to pay off.
  // Coming home warms the ember before the last battle.
  public const int FinaleField=0,FinaleWarmth=20;
  bool finale;
  public bool HasFinale {get{return finale;}}
  // True while the run stands in (or has finished on) its last stand.
  public bool InFinale {get{return finale&&Stage==Route.Length;}}
  // At a camp: true when the only way on is the last stand.
  public bool FinaleNext {get{return finale&&State==Status.Camp&&Stage==Route.Length-1;}}
  public static Expedition Start(uint seed,int grade){if(grade<0||grade>MaxGrade)throw new ArgumentOutOfRangeException("grade");var e=new Expedition{Seed=seed,Grade=grade,roads=true,finale=true,Warmth=100,State=Status.Battle,randomState=seed^0x5EEDCA4Fu};e.peaks=new int[e.Stages];e.route=new int[e.Stages];for(int i=1;i<e.Stages;i++)e.route[i]=-1;e.route[0]=Route[0];return e;}
  // Enemy health of a stage of this run, as a multiple of the same field in a free battle.
  public double HealthAt(int stage){if(stage<0||stage>=Stages)throw new ArgumentOutOfRangeException("stage");return (1+Boon.ThreatHealth*(stage+(RoadAt(stage)==RoadHard?1:0)))*(1+Boon.GradeHealth*Grade);}
  public int Stages {get{return Route.Length+(finale?1:0);}}
  public int ClearedStages {get{return State==Status.Won?Stages:Stage+(State==Status.Camp||abandonedCamp?1:0);}}
  public int Field {get{return route[Math.Min(Stage,Stages-1)];}}
  // The field fought at a stage, or -1 when that stage has not been chosen yet.
  public int FieldAt(int stage){if(stage<0||stage>=route.Length)throw new ArgumentOutOfRangeException("stage");return route[stage];}
  // At a camp: the fields this run has not fought yet, in story order; after the fourth field, the gate for the last stand. Empty otherwise.
  public int[] NextFields(){if(State!=Status.Camp)return new int[0];if(FinaleNext)return new[]{FinaleField};var list=new List<int>();foreach(int field in Route){bool used=false;for(int i=0;i<=Stage;i++)if(route[i]==field)used=true;if(!used)list.Add(field);}return list.ToArray();}
  public int Threat {get{return Stage+(RoadAt(Stage)==RoadHard?1:0);}}
  public int Count(int boon){if(boon<0||boon>=Boon.Count)throw new ArgumentOutOfRangeException("boon");return boons[boon];}
  public int Peak(int stage){if(stage<0||stage>=peaks.Length)throw new ArgumentOutOfRangeException("stage");return peaks[stage];}
  public int[] Offers(){return (int[])offered.Clone();}
  uint Next(){unchecked{uint t=(randomState+=0x6D2B79F5u);t=(t^(t>>15))*(t|1);t^=t+((t^(t>>7))*(t|61));return t^(t>>14);}}
  // Each stage fights on its own seed, so card offers and enemy pace differ from stage to stage and run to run.
  public uint StageSeed(int stage){unchecked{uint t=Seed+0x9E3779B9u*(uint)(stage+1);t=(t^(t>>16))*0x85EBCA6Bu;t=(t^(t>>13))*0xC2B2AE35u;t^=t>>16;return t==0?1u:t;}}
  int[] BattleBoons(){var list=new int[Boon.BattleCount];Array.Copy(boons,list,Boon.BattleCount);return list;}
  public Battle CreateBattle(){if(State!=Status.Battle)throw new InvalidOperationException("No battle to fight");return Battle.CreateExpedition(StageSeed(Stage),Field,Warmth,BattleBoons(),Threat,PendingGold,Grade);}
  // True when the battle is this run's current stage with this run's boons. A finished or foreign battle never matches.
  public bool Matches(Battle b){if(b==null||State!=Status.Battle||b.Mode!=BattleMode.Expedition||b.RulesVersion!=4||b.FieldIndex!=Field||b.Threat!=Threat||b.Grade!=Grade)return false;for(int i=0;i<Boon.BattleCount;i++)if(b.BoonCount(i)!=boons[i])return false;return true;}
  // Report the end of the current stage. A win moves to the camp (or ends the run after the last stage); a loss ends the run.
  public bool Complete(Battle b){
   if(!Matches(b)||(b.Phase!=Phase.Won&&b.Phase!=Phase.Lost))return false;
   Kills+=b.Kills;peaks[Stage]=b.PeakAlive;PendingGold=0;
   if(b.Phase==Phase.Lost){Warmth=Math.Max(0,b.HP);State=Status.Lost;return true;}
   Warmth=b.HP;
   if(Stage==Stages-1){State=Status.Won;return true;}
   State=Status.Camp;Offer();return true;
  }
  public bool Abandon(){if(State==Status.Won||State==Status.Lost)return false;abandonedCamp=State==Status.Camp;State=Status.Lost;offered=new int[0];PendingGold=0;return true;}
  void Offer(){
   var pool=new List<int>();var doctrines=new List<int>();for(int i=0;i<Boon.Count;i++){if(boons[i]>=Boon.MaxStacks(i))continue;if(i==Boon.Rekindle&&Warmth>=100)continue;pool.Add(i);if(Boon.IsDoctrine(i))doctrines.Add(i);}
   offered=new int[OffersPerCamp];int first=0;
   // Every camp shows at least one doctrine while any is left, so every camp can change the build.
   if(doctrines.Count>0){int doctrine=doctrines[(int)(Next()%(uint)doctrines.Count)];offered[0]=doctrine;pool.Remove(doctrine);first=1;}
   for(int i=first;i<offered.Length;i++){int pick=(int)(Next()%(uint)pool.Count);offered[i]=pool[pick];pool.RemoveAt(pick);}
  }
  // Takes a boon and marches on in story order.
  public bool Choose(int boon){var next=NextFields();return next.Length>0&&Choose(boon,next[0]);}
  // Takes a boon and marches to the chosen field. Threat follows the stage number, so a field fought later is harder.
  public bool Choose(int boon,int nextField){
   if(State!=Status.Camp||Array.IndexOf(offered,boon)<0||Array.IndexOf(NextFields(),nextField)<0)return false;
   int road=RoadFor(Stage+1,nextField);route[Stage+1]=nextField;
   boons[boon]++;if(boon==Boon.Rekindle)Warmth=Math.Min(100,Warmth+Boon.RekindleWarmth);if(boon==Boon.WarChest)PendingGold=Boon.WarChestGold;
   if(road==RoadSupply){PendingGold+=SupplyGold;Warmth=Math.Max(1,Warmth-SupplyWarmth);}else if(road==RoadHard)Warmth=Math.Min(100,Warmth+HardWarmth);
   if(finale&&Stage+1==Route.Length)Warmth=Math.Min(100,Warmth+FinaleWarmth);
   offered=new int[0];Stage++;State=Status.Battle;return true;
  }
  public ExpeditionData Export(){return new ExpeditionData{seed=Seed,stage=Stage,abandonedCamp=abandonedCamp,warmth=Warmth,state=(int)State,kills=Kills,pendingGold=PendingGold,randomState=randomState,grade=Grade,roads=roads,finale=finale,boons=(int[])boons.Clone(),offered=Offers(),peaks=(int[])peaks.Clone(),route=(int[])route.Clone()};}
  public static Expedition Restore(ExpeditionData d){
   int stages=d==null?0:Route.Length+(d.finale?1:0);
   if(d==null||d.version!=1||d.boons==null||d.boons.Length!=Boon.Count||d.offered==null||d.peaks==null||d.peaks.Length!=stages||d.state<0||d.state>3||d.stage<0||d.stage>=stages||d.kills<0||d.warmth<0||d.warmth>100||d.grade<0||d.grade>MaxGrade)throw new ArgumentException("Invalid expedition");
   var status=(Status)d.state;int taken=0;
   for(int i=0;i<Boon.Count;i++){if(d.boons[i]<0||d.boons[i]>Boon.MaxStacks(i))throw new ArgumentException("Invalid expedition boons");taken+=d.boons[i];}
   // One boon per camp passed, in every state: the boons taken always equal the stage number.
   if(taken!=d.stage)throw new ArgumentException("Invalid expedition boon count");
   // Only a lost run may have a cold ember (a run can also be lost to a crowd with warmth left).
   if(status!=Status.Lost&&d.warmth<1)throw new ArgumentException("Invalid expedition warmth");
   if(d.abandonedCamp&&(status!=Status.Lost||d.peaks[d.stage]<=0||d.warmth<=0))throw new ArgumentException("Invalid camp abandonment");
   if(status==Status.Won&&d.stage!=stages-1)throw new ArgumentException("Invalid expedition end");
   if(status==Status.Camp&&d.stage>=stages-1)throw new ArgumentException("Invalid expedition camp");
   if((status==Status.Camp)!=(d.offered.Length==OffersPerCamp)||(status!=Status.Camp&&d.offered.Length!=0))throw new ArgumentException("Invalid expedition offer");
   var seen=new HashSet<int>();foreach(int boon in d.offered)if(boon<0||boon>=Boon.Count||!seen.Add(boon)||d.boons[boon]>=Boon.MaxStacks(boon))throw new ArgumentException("Invalid expedition offer");
   for(int i=0;i<d.peaks.Length;i++)if(d.peaks[i]<0||d.peaks[i]>Balance.MaxAlive||(i>d.stage&&d.peaks[i]!=0))throw new ArgumentException("Invalid expedition record");
   var e=new Expedition{Seed=d.seed,Grade=d.grade,roads=d.roads,finale=d.finale,peaks=new int[stages],route=new int[stages],Stage=d.stage,abandonedCamp=d.abandonedCamp,Warmth=d.warmth,State=status,Kills=d.kills,PendingGold=d.pendingGold,randomState=d.randomState,offered=(int[])d.offered.Clone()};
   Array.Copy(d.boons,e.boons,Boon.Count);Array.Copy(d.peaks,e.peaks,stages);e.route[0]=Route[0];for(int i=1;i<stages;i++)e.route[i]=-1;
   // Route: stage 0 is the gate, every stage reached so far has its own field, later stages are not chosen yet.
   if(d.route==null||d.route.Length==0){if(d.finale)throw new ArgumentException("Invalid expedition route");for(int i=1;i<Route.Length;i++)e.route[i]=i<=d.stage?Route[i]:-1;}
   else{if(d.route.Length!=stages||d.route[0]!=Route[0])throw new ArgumentException("Invalid expedition route");
    for(int i=1;i<stages;i++){int field=d.route[i];if(i>d.stage){if(field!=-1)throw new ArgumentException("Invalid expedition route");continue;}if(i==Route.Length){if(field!=FinaleField)throw new ArgumentException("Invalid expedition route");e.route[i]=field;continue;}if(Array.IndexOf(Route,field)<0)throw new ArgumentException("Invalid expedition route");for(int j=0;j<i;j++)if(d.route[j]==field)throw new ArgumentException("Invalid expedition route");e.route[i]=field;}}
   // Gold waiting for the battle: a supply road's share is always there during that battle; the war chest's share only if one was ever taken.
   int roadGold=status==Status.Battle&&e.RoadAt(d.stage)==RoadSupply?SupplyGold:0,rest=d.pendingGold-roadGold;
   if((d.pendingGold!=0&&status!=Status.Battle)||!(rest==0||(rest==Boon.WarChestGold&&d.boons[Boon.WarChest]>0)))throw new ArgumentException("Invalid expedition gold");
   return e;
  }
 }
}
