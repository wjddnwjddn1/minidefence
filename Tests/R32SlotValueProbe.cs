using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using EmberBastion;
static class R32SlotValueProbe {
 static readonly int[] Seeds={29,41,57,73,101,137,181,223};
 static readonly int[] Cards={4,5,6,0,2,1};
 sealed class Result {public string Outcome,Picked;public int Wave,Peak,Gold,Kills,Level,Units;public double Time,Placed=-1;}
 static readonly Dictionary<string,Result> Cache=new Dictionary<string,Result>();
 static double Road(Battle b,int kind,int slot,int level){var u=new Unit{Kind=kind,Slot=slot,Level=level,Branch=level==3?1:0};double range=b.AttackRange(u),min=Balance.Troops[kind].MinRange,total=0;for(double d=0;d<b.Field.Perimeter;d+=16){Point p=b.FieldIndex==0?Balance.Path(d):b.Field.Path(d);double distance=Point.Distance(b.Field.Slots[slot],p);if(distance<=range&&distance>=min)total++;}return total;}
 // Favorable geometry: damage classes maximize road coverage; smith maximizes extra
 // non-smith allies beyond the baseline Lv3/branch1 support coverage (auras do not stack).
 static int Slot(Battle b,int[] plan,int kind,int level){int best=-1;double score=-1;for(int s=0;s<plan.Length;s++){if(plan[s]!=1)continue;double value=0;if(kind==4){double range=Battle.UnitRange(new Unit{Kind=4,Level=level,Branch=level==3?1:0});for(int i=0;i<plan.Length;i++){if(i==s||plan[i]==4||Point.Distance(b.Field.Slots[i],b.Field.Slots[s])>range)continue;bool alreadyCovered=false;for(int j=0;j<plan.Length;j++)if(j!=s&&plan[j]==4&&Point.Distance(b.Field.Slots[i],b.Field.Slots[j])<=220)alreadyCovered=true;if(!alreadyCovered)value+=100000+Road(b,plan[i],i,3);}}else value=Road(b,kind,s,level);if(value>score){score=value;best=s;}}if(best<0)throw new Exception("No Smart archer slot");return best;}
 static Result Run(int field,int mode,int seed,int[] plan,int slot,int kind,int level){var b=Battle.CreateForRules((uint)seed,field,4);b.SetDifficulty(mode);double placed=-1;var cards=new List<string>();
  for(int tick=0;tick<120000&&b.Phase!=Phase.Won&&b.Phase!=Phase.Lost;tick++){
   if(b.ReinforcementPending){int selected=-1;foreach(int c in Cards)if(Array.IndexOf(b.Reinforcements.Choices(),c)>=0){selected=c;break;}if(selected<0||!b.ChooseReinforcement(selected))throw new Exception("Card selection failed");cards.Add(selected.ToString());}
   if(tick%60==0){for(int i=0;i<plan.Length;i++)if(b.At(i)==null){int k=i==slot?kind:plan[i];if(b.Gold>=Balance.Troops[k].Cost&&b.Place(k,i)&&i==slot)placed=b.Time;}foreach(var u in b.Units){int cap=u.Slot==slot?level:3;if(u.Level>=cap)continue;if(u.Level==1&&b.Gold>=50)b.Upgrade(u.Slot);else if(u.Level==2&&b.Gold>=90)b.UpgradeBranch(u.Slot,1);}}
   if(b.Phase==Phase.Preparation&&!b.Start())throw new Exception("Cannot start");b.Step(Balance.Tick);
  }
  var target=b.At(slot);return new Result{Outcome=b.Phase==Phase.Won?"Won":b.Phase==Phase.Lost?"Lost":"Stalled",Wave=b.Wave,Peak=b.PeakAlive,Gold=b.Gold,Kills=b.Kills,Time=b.Time,Placed=placed,Level=target==null?0:target.Level,Units=b.Units.Count,Picked=string.Join(";",cards.ToArray())};
 }
 static int Main(string[] args){try{int field=int.Parse(args[0]),runs=0,rows=0;string path=Path.GetFullPath(args[1]);Directory.CreateDirectory(Path.GetDirectoryName(path));using(var writer=new StreamWriter(path,false,new UTF8Encoding(false))){writer.WriteLine("simulation_key,field,mode,seed,replacement_kind,target_level,target_slot,role,outcome,wave,peak,gold,kills,time,placed_at,actual_target_level,units,cards,cache_hit");
  for(int mode=0;mode<3;mode++)for(int level=1;level<=3;level++){var layout=Battle.CreateForRules(29,field,4);var plan=new int[layout.Field.Slots.Length];for(int s=0;s<plan.Length;s++)plan[s]=Smart.Kind(field,layout.Field,s);foreach(int kind in new[]{0,2,3,4}){int slot=Slot(layout,plan,kind,level);foreach(int seed in Seeds)foreach(string role in new[]{"baseline","replacement"}){int actual=role=="baseline"?1:kind;string key=field+":"+mode+":"+seed+":"+actual+":"+level+":"+slot;Result r;bool hit=Cache.TryGetValue(key,out r);if(!hit){r=Run(field,mode,seed,plan,slot,actual,level);Cache.Add(key,r);runs++;}writer.WriteLine(string.Join(",",new[]{key,field.ToString(),mode.ToString(),seed.ToString(),kind.ToString(),level.ToString(),slot.ToString(),role,r.Outcome,r.Wave.ToString(),r.Peak.ToString(),r.Gold.ToString(),r.Kills.ToString(),r.Time.ToString("F6",CultureInfo.InvariantCulture),r.Placed.ToString("F6",CultureInfo.InvariantCulture),r.Level.ToString(),r.Units.ToString(),r.Picked,hit?"1":"0"}));rows++;if(r.Outcome=="Stalled")throw new Exception("Stalled "+key);}writer.Flush();Console.WriteLine("field="+field+" mode="+mode+" level="+level+" kind="+kind+" target="+slot+" unique="+runs);}}
  }Console.WriteLine("PASS field="+field+" rows="+rows+" unique="+runs);return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
