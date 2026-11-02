using System;
using System.Collections.Generic;
namespace EmberBastion {
 public static class LossEnemySummary {
  // A tied maximum is not attributed to an arbitrary enemy type.
  public static string Describe(IList<Enemy> enemies,string endCause){
   if(endCause!="적 100마리 도달"||enemies==null||enemies.Count==0)return "";
   int[] counts=new int[Balance.Enemies.Length];
   foreach(var enemy in enemies)if(enemy!=null&&enemy.HP>0&&enemy.Kind>=0&&enemy.Kind<counts.Length)counts[enemy.Kind]++;
   int best=0,kind=-1,ties=0;
   for(int i=0;i<counts.Length;i++){if(counts[i]>best){best=counts[i];kind=i;ties=1;}else if(counts[i]>0&&counts[i]==best)ties++;}
   if(kind<0)return "";
   return ties>1?"최다 적 동률 · 각 "+best+"명":"가장 많은 적 · "+Balance.Enemies[kind].Name+" "+best+"명";
  }
 }
}
