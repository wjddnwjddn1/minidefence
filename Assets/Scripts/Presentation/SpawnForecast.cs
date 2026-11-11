using System;
using System.Collections.Generic;
namespace EmberBastion {
 public static class SpawnForecast {
  public sealed class Entry {public int Gate;public int[] Kinds=new int[Balance.Enemies.Length];public double Eta;public int Kind {get{int best=0;for(int i=1;i<Kinds.Length;i++)if(Kinds[i]>Kinds[best])best=i;return best;}}public int Count {get{int count=0;foreach(int n in Kinds)count+=n;return count;}}}
  public sealed class GroupPlan {public int Gate,Kind,Count;public double Delay,Gap;}
  public sealed class Schedule {public Phase Phase;public double Clock,Interval;public bool Blocked,HasNext;public readonly List<GroupPlan> Current=new List<GroupPlan>(),Next=new List<GroupPlan>();}
  static void Add(List<Entry> result,GroupPlan group,double eta){if(group.Count<=0)return;foreach(var entry in result)if(entry.Gate==group.Gate&&Math.Abs(entry.Eta-eta)<1e-6){entry.Kinds[group.Kind]+=group.Count;return;}var next=new Entry{Gate=group.Gate,Eta=eta};next.Kinds[group.Kind]=group.Count;result.Add(next);}
  public static List<Entry> Calculate(Schedule state,double lead=4){var result=new List<Entry>();if(state.Blocked)return result;
   if(state.Phase==Phase.Preparation){if(state.HasNext)foreach(var g in state.Next)Add(result,g,-1);return result;}
   if(state.Phase!=Phase.Fighting)return result;double last=0;foreach(var g in state.Current){double eta=g.Delay-state.Clock;if(eta>0&&eta<=lead)Add(result,g,eta);if(g.Count>0)last=Math.Max(last,g.Delay+(g.Count-1)*g.Gap);}
   // An overdue interval cannot start the next wave while a current group is still spawning.
   double next=Math.Max(state.Interval,last)-state.Clock;if(state.HasNext&&next>0)foreach(var g in state.Next){double eta=next+g.Delay;if(eta<=lead)Add(result,g,eta);}
   result.Sort((a,b)=>{int order=a.Gate.CompareTo(b.Gate);return order!=0?order:a.Eta.CompareTo(b.Eta);});return result;
  }
  static void Read(Battle b,int index,List<GroupPlan> plans){foreach(var g in b.WaveGroups(index))plans.Add(new GroupPlan{Gate=g.Gate,Kind=g.Kind,Count=b.SpawnCount(g),Delay=g.Delay,Gap=b.SpawnGap(g)});}
  public static List<Entry> Build(Battle b,double lead=4){if(b==null)return new List<Entry>();var state=new Schedule{Phase=b.Phase,Clock=b.WaveTime,Interval=b.WaveInterval,Blocked=b.ReinforcementPending,HasNext=b.Wave<b.WaveCount};if(state.Phase==Phase.Fighting&&b.Wave>=1)Read(b,b.Wave-1,state.Current);if(state.HasNext)Read(b,b.Wave,state.Next);return Calculate(state,lead);}
 }
}
