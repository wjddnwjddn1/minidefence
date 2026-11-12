using System;
using System.Collections.Generic;
namespace EmberBastion {
 // r37 (Claude): the roguelite run. An expedition is a chain of battles on one ember: warmth (castle HP) carries from
 // battle to battle, a camp between battles offers three boons, and each stage raises the enemy threat.
 // Pure rules: no Unity, display, input, persistence or wall-clock dependencies.
 // The per-battle part (Boon, ExpeditionBattleData, Battle.CreateExpedition) lives in Battle.cs so that Battle.cs
 // still compiles on its own.

 [Serializable] public sealed class ExpeditionData {
  public int version=1,stage,warmth,state,kills,pendingGold;public bool abandonedCamp;
  public uint seed,randomState;
  public int[] boons,offered,peaks;
 }

 public sealed class Expedition {
  public enum Status {Battle=0,Camp=1,Won=2,Lost=3}
  // v1 route follows the story: gate, forest road, rocky pass, marsh ferry.
  public static readonly int[] Route={0,1,2,3};
  public const int OffersPerCamp=3;
  public uint Seed {get;private set;}
  public int Stage {get;private set;}
  public int Warmth {get;private set;}
  public int Kills {get;private set;}
  public int PendingGold {get;private set;}
  public Status State {get;private set;}
  bool abandonedCamp;uint randomState;readonly int[] boons=new int[Boon.Count];int[] offered=new int[0];readonly int[] peaks=new int[Route.Length];
  Expedition(){}
  public static Expedition Start(uint seed){return new Expedition{Seed=seed,Warmth=100,State=Status.Battle,randomState=seed^0x5EEDCA4Fu};}
  public int Stages {get{return Route.Length;}}
  public int ClearedStages {get{return State==Status.Won?Stages:Stage+(State==Status.Camp||abandonedCamp?1:0);}}
  public int Field {get{return Route[Math.Min(Stage,Route.Length-1)];}}
  public int Threat {get{return Stage;}}
  public int Count(int boon){if(boon<0||boon>=Boon.Count)throw new ArgumentOutOfRangeException("boon");return boons[boon];}
  public int Peak(int stage){if(stage<0||stage>=peaks.Length)throw new ArgumentOutOfRangeException("stage");return peaks[stage];}
  public int[] Offers(){return (int[])offered.Clone();}
  uint Next(){unchecked{uint t=(randomState+=0x6D2B79F5u);t=(t^(t>>15))*(t|1);t^=t+((t^(t>>7))*(t|61));return t^(t>>14);}}
  // Each stage fights on its own seed, so card offers and enemy pace differ from stage to stage and run to run.
  public uint StageSeed(int stage){unchecked{uint t=Seed+0x9E3779B9u*(uint)(stage+1);t=(t^(t>>16))*0x85EBCA6Bu;t=(t^(t>>13))*0xC2B2AE35u;t^=t>>16;return t==0?1u:t;}}
  int[] BattleBoons(){var list=new int[Boon.BattleCount];Array.Copy(boons,list,Boon.BattleCount);return list;}
  public Battle CreateBattle(){if(State!=Status.Battle)throw new InvalidOperationException("No battle to fight");return Battle.CreateExpedition(StageSeed(Stage),Field,Warmth,BattleBoons(),Threat,PendingGold);}
  // True when the battle is this run's current stage with this run's boons. A finished or foreign battle never matches.
  public bool Matches(Battle b){if(b==null||State!=Status.Battle||b.Mode!=BattleMode.Expedition||b.RulesVersion!=4||b.FieldIndex!=Field||b.Threat!=Threat)return false;for(int i=0;i<Boon.BattleCount;i++)if(b.BoonCount(i)!=boons[i])return false;return true;}
  // Report the end of the current stage. A win moves to the camp (or ends the run after the last stage); a loss ends the run.
  public bool Complete(Battle b){
   if(!Matches(b)||(b.Phase!=Phase.Won&&b.Phase!=Phase.Lost))return false;
   Kills+=b.Kills;peaks[Stage]=b.PeakAlive;PendingGold=0;
   if(b.Phase==Phase.Lost){Warmth=Math.Max(0,b.HP);State=Status.Lost;return true;}
   Warmth=b.HP;
   if(Stage==Route.Length-1){State=Status.Won;return true;}
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
  public bool Choose(int boon){
   if(State!=Status.Camp||Array.IndexOf(offered,boon)<0)return false;
   boons[boon]++;if(boon==Boon.Rekindle)Warmth=Math.Min(100,Warmth+Boon.RekindleWarmth);if(boon==Boon.WarChest)PendingGold=Boon.WarChestGold;
   offered=new int[0];Stage++;State=Status.Battle;return true;
  }
  public ExpeditionData Export(){return new ExpeditionData{seed=Seed,stage=Stage,abandonedCamp=abandonedCamp,warmth=Warmth,state=(int)State,kills=Kills,pendingGold=PendingGold,randomState=randomState,boons=(int[])boons.Clone(),offered=Offers(),peaks=(int[])peaks.Clone()};}
  public static Expedition Restore(ExpeditionData d){
   if(d==null||d.version!=1||d.boons==null||d.boons.Length!=Boon.Count||d.offered==null||d.peaks==null||d.peaks.Length!=Route.Length||d.state<0||d.state>3||d.stage<0||d.stage>=Route.Length||d.kills<0||d.warmth<0||d.warmth>100)throw new ArgumentException("Invalid expedition");
   var status=(Status)d.state;int taken=0;
   for(int i=0;i<Boon.Count;i++){if(d.boons[i]<0||d.boons[i]>Boon.MaxStacks(i))throw new ArgumentException("Invalid expedition boons");taken+=d.boons[i];}
   // One boon per camp passed, in every state: the boons taken always equal the stage number.
   if(taken!=d.stage)throw new ArgumentException("Invalid expedition boon count");
   // Only a lost run may have a cold ember (a run can also be lost to a crowd with warmth left).
   if(status!=Status.Lost&&d.warmth<1)throw new ArgumentException("Invalid expedition warmth");
   if(d.abandonedCamp&&(status!=Status.Lost||d.peaks[d.stage]<=0||d.warmth<=0))throw new ArgumentException("Invalid camp abandonment");
   if(status==Status.Won&&d.stage!=Route.Length-1)throw new ArgumentException("Invalid expedition end");
   if(status==Status.Camp&&d.stage>=Route.Length-1)throw new ArgumentException("Invalid expedition camp");
   if((status==Status.Camp)!=(d.offered.Length==OffersPerCamp)||(status!=Status.Camp&&d.offered.Length!=0))throw new ArgumentException("Invalid expedition offer");
   var seen=new HashSet<int>();foreach(int boon in d.offered)if(boon<0||boon>=Boon.Count||!seen.Add(boon)||d.boons[boon]>=Boon.MaxStacks(boon))throw new ArgumentException("Invalid expedition offer");
   if(d.pendingGold!=0&&(d.pendingGold!=Boon.WarChestGold||status!=Status.Battle||d.boons[Boon.WarChest]==0))throw new ArgumentException("Invalid expedition gold");
   for(int i=0;i<d.peaks.Length;i++)if(d.peaks[i]<0||d.peaks[i]>Balance.MaxAlive||(i>d.stage&&d.peaks[i]!=0))throw new ArgumentException("Invalid expedition record");
   var e=new Expedition{Seed=d.seed,Stage=d.stage,abandonedCamp=d.abandonedCamp,Warmth=d.warmth,State=status,Kills=d.kills,PendingGold=d.pendingGold,randomState=d.randomState,offered=(int[])d.offered.Clone()};
   Array.Copy(d.boons,e.boons,Boon.Count);Array.Copy(d.peaks,e.peaks,Route.Length);return e;
  }
 }
}
