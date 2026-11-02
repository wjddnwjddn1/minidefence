using System;
using EmberBastion;
using System.Collections.Generic;
static class Exp{public static double SlowAmp=1;public static double ShieldBlast=1,RunnerMelee=1;public static double Blast(int kind){return kind==2?ShieldBlast:1;}public static double Melee(int kind){return kind==1?RunnerMelee:1;}public static double ArcherVs=1;public static double Archer(int kind){return kind==1||kind==2?ArcherVs:1;}}
class FrostAmpProbe {
 static int Main(){try{var baseN=new System.Collections.Generic.Dictionary<Group,int>();var baseG=new System.Collections.Generic.Dictionary<Group,double>();foreach(var F in Balance.Fields)foreach(var W in F.Waves)foreach(var g in W){baseN[g]=g.Count;baseG[g]=g.Gap;}foreach(int seed in new[]{29,41,57,73,101,113,127,131})foreach(int resist in new[]{100,125,150})foreach(int scenario in new[]{100}){ Exp.SlowAmp=resist/100.0; Exp.ShieldBlast=.5;Exp.RunnerMelee=.5;Exp.ArcherVs=resist==4?1.5:1;foreach(var kv in baseN){kv.Key.Count=(int)Math.Round(kv.Value*scenario/100.0);kv.Key.Gap=baseG[kv.Key]*100.0/scenario;}foreach(bool fast in new[]{true})for(int field=0;field<Balance.Fields.Length;field++)foreach(int pattern in new[]{6,10,17,18,3})foreach(bool ember in new[]{false,true}){
  var b=new Battle((uint)seed,field);b.SetFastWaves(fast);int ticks=0,peak=0;var laps=new Dictionary<int,int>();string label=pattern==4?"mixed":pattern==5?"roles":pattern==6?"swordB":pattern==17?"smartB+F1":pattern==18?"smartB+F2":pattern==9?"smart":pattern==10?"smartB":pattern==7?"mixSBAA":pattern==8?"mixSABA":Balance.Troops[pattern].Name;
  while(b.Phase!=Phase.Won&&b.Phase!=Phase.Lost&&ticks<120000){
   if(ticks%60==0){for(int slot=0;slot<b.Field.Slots.Length;slot++)if(b.At(slot)==null){int kind=pattern==4?slot%4:pattern==5?new[]{0,0,1,2}[slot%4]:pattern==6?0:pattern>=17?Smart.KindFrost(field,slot,pattern-16):pattern>=9?Smart.Kind(field,slot):pattern==7?new[]{0,2,1,1}[slot%4]:pattern==8?new[]{0,1,2,1}[slot%4]:pattern;if(b.Gold>=Balance.Troops[kind].Cost)b.Place(kind,slot);}foreach(var u in b.Units){if(u.Level==1&&b.Gold>=50)b.Upgrade(u.Slot);else if((pattern==6||((pattern==7||pattern==8||pattern==10||pattern>=17)&&u.Kind==0))&&u.Level==2&&b.Gold>=90)b.UpgradeBranch(u.Slot,2);}}
   if(b.Phase==Phase.Preparation&&!b.Start())throw new Exception("start failed");
   if(ember&&b.EmberCD<=0&&b.Enemies.Count>=3)b.Ember(b.Enemies[b.Enemies.Count/2].Position);
   b.Step(1.0/60);ticks++;peak=Math.Max(peak,b.Enemies.Count);
  }
  string outcome=b.Phase==Phase.Won?"Won":b.Phase==Phase.Lost?"Lost":"Stalled";
  Console.WriteLine(seed+","+resist+","+scenario+","+(fast?"fast":"normal")+","+field+","+label+","+(ember?"ember":"no-ember")+","+outcome+","+b.Wave+","+peak+","+b.Kills);
 }}return 0;}catch(Exception ex){Console.Error.WriteLine(ex.Message);return 1;}}
}
static class Smart {
 static readonly System.Collections.Generic.Dictionary<int,int[]> cache=new System.Collections.Generic.Dictionary<int,int[]>();
 static Point P(int f,double d){return f==0?Balance.Path(d):Balance.Fields[f].Path(d);}
 static double Cover(int f,Point s,double range,double min){double per=f==0?Balance.Perimeter:Balance.Fields[f].Perimeter;int n=0,inside=0;for(double d=0;d<per;d+=16){n++;double dist=Point.Distance(P(f,d),s);if(dist<=range&&dist>=min)inside++;}return (double)inside/n;}
 // Swords on the half of slots that touch the most road within melee range; the rest alternate archer/bomb by long-range coverage.
 public static int KindFrost(int f,int slot,int frosts){int k=Kind(f,slot);if(k==0)return 0;var S=Balance.Fields[f].Slots;var c=new System.Collections.Generic.List<int>();for(int i=0;i<S.Length;i++)if(Kind(f,i)!=0)c.Add(i);c.Sort((x,y)=>Cover(f,S[y],180,0).CompareTo(Cover(f,S[x],180,0)));for(int i=0;i<frosts&&i<c.Count;i++)if(c[i]==slot)return 3;return k;}
 public static int Kind(int f,int slot){int[] a;if(!cache.TryGetValue(f,out a)){var S=Balance.Fields[f].Slots;int m=S.Length;var idx=new System.Collections.Generic.List<int>();for(int i=0;i<m;i++)idx.Add(i);idx.Sort((x,y)=>Cover(f,S[y],120,0).CompareTo(Cover(f,S[x],120,0)));a=new int[m];var rest=new System.Collections.Generic.List<int>();for(int i=0;i<m;i++){if(i<m/2)a[idx[i]]=0;else rest.Add(idx[i]);}rest.Sort((x,y)=>Cover(f,S[y],210,60).CompareTo(Cover(f,S[x],210,60)));for(int i=0;i<rest.Count;i++)a[rest[i]]=i%2==0?1:2;cache[f]=a;}return a[slot];}
}
