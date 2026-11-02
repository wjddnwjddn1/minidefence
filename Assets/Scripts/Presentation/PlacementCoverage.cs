using System;
using EmberBastion;
namespace EmberBastion {
 public static class PlacementCoverage {
  static readonly int[][][] cache=new int[Balance.Fields.Length][][];
  static readonly bool[][][] recommendations=new bool[Balance.Fields.Length][][];
  public static int Grade(int field,int kind,int slot){if(field<=0||field>=Balance.Fields.Length||kind<0||kind>3||slot<0||slot>=Balance.Fields[field].Slots.Length)return -1;if(cache[field]==null)cache[field]=new int[4][];if(cache[field][kind]==null)Build(field,kind);return cache[field][kind][slot];}
  public static bool Recommended(int field,int kind,int slot){if(Grade(field,kind,slot)<0)return false;return recommendations[field][kind][slot];}
  static void Build(int field,int kind){var f=Balance.Fields[field];int n=f.Slots.Length;var values=new int[n];double range=Balance.Troops[kind].Range,min=Balance.Troops[kind].MinRange;for(double d=0;d<f.Perimeter;d+=16){var p=f.Path(d);for(int i=0;i<n;i++){double dist=Point.Distance(p,f.Slots[i]);if(dist<=range&&dist>=min)values[i]++;}}var sorted=(int[])values.Clone();Array.Sort(sorted);int[] grades=new int[n];for(int i=0;i<n;i++){int lower=0,greater=0;for(int j=0;j<n;j++){if(values[j]<values[i])lower++;if(values[j]>values[i])greater++;}grades[i]=greater<n/3?2:lower<n/3?0:1;}cache[field][kind]=grades;if(recommendations[field]==null)recommendations[field]=new bool[4][];var order=new int[n];for(int i=0;i<n;i++)order[i]=i;Array.Sort(order,(a,b)=>values[a]==values[b]?a.CompareTo(b):values[b].CompareTo(values[a]));var chosen=new bool[n];for(int i=0;i<Math.Min(3,n);i++)chosen[order[i]]=true;recommendations[field][kind]=chosen;}
 }
}
