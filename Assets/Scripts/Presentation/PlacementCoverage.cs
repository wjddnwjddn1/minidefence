using System;
using EmberBastion;
namespace EmberBastion {
 public static class PlacementCoverage {
  sealed class Coverage {public readonly int[][] Grades=new int[4][];public readonly bool[][] Recommendations=new bool[4][];}
  static readonly System.Collections.Generic.Dictionary<FieldDef,Coverage> cache=new System.Collections.Generic.Dictionary<FieldDef,Coverage>();
  public static int Grade(int field,int kind,int slot){return field<0||field>=Balance.Fields.Length?-1:Grade(field,Balance.Fields[field],kind,slot);}
  public static int Grade(int field,FieldDef layout,int kind,int slot){if(field<=0||field>=Balance.Fields.Length||layout==null||kind<0||kind>3||slot<0||slot>=layout.Slots.Length)return -1;Coverage c;if(!cache.TryGetValue(layout,out c)){c=new Coverage();cache.Add(layout,c);}if(c.Grades[kind]==null)Build(layout,kind,c);return c.Grades[kind][slot];}
  public static bool Recommended(int field,int kind,int slot){return field>=0&&field<Balance.Fields.Length&&Recommended(field,Balance.Fields[field],kind,slot);}
  public static bool Recommended(int field,FieldDef layout,int kind,int slot){if(Grade(field,layout,kind,slot)<0)return false;return cache[layout].Recommendations[kind][slot];}
  static void Build(FieldDef f,int kind,Coverage c){int n=f.Slots.Length;var values=new int[n];double range=Balance.Troops[kind].Range,min=Balance.Troops[kind].MinRange;for(double d=0;d<f.Perimeter;d+=16){var p=f.Path(d);for(int i=0;i<n;i++){double dist=Point.Distance(p,f.Slots[i]);if(dist<=range&&dist>=min)values[i]++;}}var sorted=(int[])values.Clone();Array.Sort(sorted);int[] grades=new int[n];for(int i=0;i<n;i++){int lower=0,greater=0;for(int j=0;j<n;j++){if(values[j]<values[i])lower++;if(values[j]>values[i])greater++;}grades[i]=greater<n/3?2:lower<n/3?0:1;}c.Grades[kind]=grades;var order=new int[n];for(int i=0;i<n;i++)order[i]=i;Array.Sort(order,(a,b)=>values[a]==values[b]?a.CompareTo(b):values[b].CompareTo(values[a]));var chosen=new bool[n];for(int i=0;i<Math.Min(3,n);i++)chosen[order[i]]=true;c.Recommendations[kind]=chosen;}
 }
}
