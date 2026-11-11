using System;
using System.Collections.Generic;
namespace EmberBastion {
 // Claude r31 decision 2: when combat turns dangerous, point at one slot the player can act on now.
 // Pure logic (no Unity) so Tests/CrisisAdviceChecks.cs can compile it with Core.
 public static class CrisisAdvice {
  public const int AliveThreshold=60,PerWave=2;
  static readonly Dictionary<FieldDef,int[][]> roads=new Dictionary<FieldDef,int[][]>();
  // Path samples (every 16 px) within kind's attack band of each slot.
  public static int Road(Battle b,int kind,int slot){
   var f=b.Field;int[][] table;if(!roads.TryGetValue(f,out table)){table=new int[4][];roads.Add(f,table);}
   if(table[kind]==null){var values=new int[f.Slots.Length];double range=Balance.Troops[kind].Range,min=Balance.Troops[kind].MinRange;
    for(double d=0;d<f.Perimeter;d+=16){Point p=b.FieldIndex==0?Balance.Path(d):f.Path(d);for(int i=0;i<values.Length;i++){double dist=Point.Distance(p,f.Slots[i]);if(dist<=range&&dist>=min)values[i]++;}}
    table[kind]=values;}
   return table[kind][slot];}
  // Returns the slot to highlight, or -1. build=true: an empty slot the player can afford a troop for;
  // build=false: an attacking unit the player can afford to upgrade now. Never buys anything.
  public static int Suggest(Battle b,out bool build){
   build=false;if(b==null||b.Phase!=Phase.Fighting||b.ReinforcementPending)return -1;
   int best=-1,score=-1;
   for(int s=0;s<b.Field.Slots.Length;s++){if(b.At(s)!=null)continue;
    for(int k=0;k<4;k++){if(b.Gold<Balance.Troops[k].Cost)continue;int v=Road(b,k,s);if(v>score||(v==score&&s<best)){score=v;best=s;}}}
   if(best>=0&&score>0){build=true;return best;}
   best=-1;score=-1;
   foreach(var u in b.Units){if(u.Kind>3||u.Level>=3)continue;if(u.Level==1&&b.Gold<50||u.Level==2&&b.Gold<90)continue;int v=Road(b,u.Kind,u.Slot);if(v>score||(v==score&&u.Slot<best)){score=v;best=u.Slot;}}
   return score>0?best:-1;}
 }
}
