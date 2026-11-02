using System;
using System.Collections.Generic;
using EmberBastion;
static class Exp {public static double ShieldBlast=1,RunnerMelee=1,ArcherVs=1;public static double Blast(int k){return k==2?ShieldBlast:1;}public static double Melee(int k){return k==1?RunnerMelee:1;}public static double Archer(int k){return k==1||k==2?ArcherVs:1;}}
class ComboProbe {
 static int Main(string[] args){try{int mode=args.Length>0?int.Parse(args[0]):1;int density=args.Length>1?int.Parse(args[1]):100;bool enabled=args.Length>2&&args[2]=="on";Exp.ShieldBlast=Exp.RunnerMelee=enabled?.5:1;Exp.ArcherVs=enabled?1.5:1;
 var baseN=new Dictionary<Group,int>();var baseG=new Dictionary<Group,double>();foreach(var f in Balance.Fields)foreach(var wave in f.Waves)foreach(var g in wave){baseN[g]=g.Count;baseG[g]=g.Gap;}foreach(var x in baseN){x.Key.Count=(int)Math.Round(x.Value*density/100.0);x.Key.Gap=baseG[x.Key]*100.0/density;}
 Console.WriteLine("seed,mode,density,bundle,field,policy,ember,result,wave,peak,kills");foreach(uint seed in new uint[]{29,41,57,73})for(int field=0;field<3;field++)foreach(string policy in new[]{"sword","archer","bomb","swordB","smart","smartB","bombA","bombB","sniper"})foreach(bool ember in new[]{false,true}){
 var b=new Battle(seed,field);b.SetDifficulty(mode);int ticks=0,peak=0;while(b.Phase!=Phase.Won&&b.Phase!=Phase.Lost&&ticks<120000){if(ticks%60==0){for(int slot=0;slot<b.Field.Slots.Length;slot++)if(b.At(slot)==null){int kind=policy.StartsWith("smart")?Smart.Kind(field,slot):policy=="archer"||policy=="sniper"?1:policy.StartsWith("bomb")?2:0;if(b.Gold>=Balance.Troops[kind].Cost)b.Place(kind,slot);}foreach(var u in b.Units){if(u.Level==1&&b.Gold>=50)b.Upgrade(u.Slot);else if(u.Level==2&&b.Gold>=90){int branch=policy=="swordB"||(policy=="smartB"&&u.Kind==0)||policy=="bombB"||policy=="sniper"?2:policy=="bombA"?1:0;if(branch>0)b.UpgradeBranch(u.Slot,branch);}}}
 if(b.Phase==Phase.Preparation&&!b.Start())throw new Exception("start failed");if(ember&&b.EmberCD<=0&&b.Enemies.Count>=3)b.Ember(b.Enemies[b.Enemies.Count/2].Position);b.Step(1.0/60);ticks++;peak=Math.Max(peak,b.Enemies.Count);}
 Console.WriteLine(seed+","+mode+","+density+","+(enabled?"on":"off")+","+field+","+policy+","+(ember?1:0)+","+(b.Phase==Phase.Won?"Won":b.Phase==Phase.Lost?"Lost":"Stalled")+","+b.Wave+","+peak+","+b.Kills);
 }return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
static class Smart {
 static readonly System.Collections.Generic.Dictionary<int,int[]> cache=new System.Collections.Generic.Dictionary<int,int[]>();
 static Point P(int f,double d){return f==0?Balance.Path(d):Balance.Fields[f].Path(d);}
 static double Cover(int f,Point s,double range,double min){double per=f==0?Balance.Perimeter:Balance.Fields[f].Perimeter;int n=0,inside=0;for(double d=0;d<per;d+=16){n++;double dist=Point.Distance(P(f,d),s);if(dist<=range&&dist>=min)inside++;}return (double)inside/n;}
 // Swords on the half of slots that touch the most road within melee range; the rest alternate archer/bomb by long-range coverage.
 public static int Kind(int f,int slot){int[] a;if(!cache.TryGetValue(f,out a)){var S=Balance.Fields[f].Slots;int m=S.Length;var idx=new System.Collections.Generic.List<int>();for(int i=0;i<m;i++)idx.Add(i);idx.Sort((x,y)=>Cover(f,S[y],120,0).CompareTo(Cover(f,S[x],120,0)));a=new int[m];var rest=new System.Collections.Generic.List<int>();for(int i=0;i<m;i++){if(i<m/2)a[idx[i]]=0;else rest.Add(idx[i]);}rest.Sort((x,y)=>Cover(f,S[y],210,60).CompareTo(Cover(f,S[x],210,60)));for(int i=0;i<rest.Count;i++)a[rest[i]]=i%2==0?1:2;cache[f]=a;}return a[slot];}
}
