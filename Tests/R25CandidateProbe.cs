using System;
using System.Collections.Generic;
using EmberBastion;
static class Exp {public static double ShieldBlast=1,RunnerMelee=1,ArcherVs=1;public static double Blast(int k){return k==2?ShieldBlast:1;}public static double Melee(int k){return k==1?RunnerMelee:1;}public static double Archer(int k){return k==1||k==2?ArcherVs:1;}}
class GameplayDurationProbe {
 static int Main(string[] args){try{int mode=args.Length>0?int.Parse(args[0]):1;int density=args.Length>1?int.Parse(args[1]):100;bool enabled=args.Length>2&&args[2]=="on";Exp.ShieldBlast=Exp.RunnerMelee=enabled?.5:1;Exp.ArcherVs=enabled?1.5:1;
 int extra=args.Length>3?int.Parse(args[3]):0;if(extra!=0&&extra!=4)throw new ArgumentOutOfRangeException("extra");if(extra>0)CandidateWaves.Apply();
 var baseN=new Dictionary<Group,int>();var baseG=new Dictionary<Group,double>();foreach(var f in Balance.Fields)foreach(var wave in f.Waves)foreach(var g in wave){baseN[g]=g.Count;baseG[g]=g.Gap;}foreach(var x in baseN){x.Key.Count=(int)Math.Round(x.Value*density/100.0);x.Key.Gap=baseG[x.Key]*100.0/density;}
 bool idle=args.Length>4&&args[4]=="idle";
 Console.WriteLine("seed,mode,density,bundle,field,policy,ember,result,wave,peak,kills,seconds,level2,level3");foreach(uint seed in new uint[]{29,41,57,73})for(int field=0;field<3;field++)foreach(string policy in (idle?new[]{"idle"}:new[]{"sword","archer","bomb","swordB","smart","smartB","bombA","bombB","sniper"}))foreach(bool ember in (idle?new[]{false}:new[]{false,true})){
 var b=new Battle(seed,field);b.SetDifficulty(mode);int ticks=0,peak=0;while(b.Phase!=Phase.Won&&b.Phase!=Phase.Lost&&ticks<120000){if(ticks%60==0){if(idle){for(int slot=0;slot<3;slot++)if(b.At(slot)==null){int kind=slot==0?0:1;if(b.Gold>=Balance.Troops[kind].Cost)b.Place(kind,slot);}}else{for(int slot=0;slot<b.Field.Slots.Length;slot++)if(b.At(slot)==null){int kind=policy.StartsWith("smart")?Smart.Kind(field,slot):policy=="archer"||policy=="sniper"?1:policy.StartsWith("bomb")?2:0;if(b.Gold>=Balance.Troops[kind].Cost)b.Place(kind,slot);}foreach(var u in b.Units){if(u.Level==1&&b.Gold>=50)b.Upgrade(u.Slot);else if(u.Level==2&&b.Gold>=90){int branch=policy=="swordB"||(policy=="smartB"&&u.Kind==0)||policy=="bombB"||policy=="sniper"?2:policy=="bombA"?1:0;if(branch>0)b.UpgradeBranch(u.Slot,branch);}}}}
 if(b.Phase==Phase.Preparation&&!b.Start())throw new Exception("start failed");if(ember&&b.EmberCD<=0&&b.Enemies.Count>=3)b.Ember(b.Enemies[b.Enemies.Count/2].Position);b.Step(1.0/60);ticks++;peak=Math.Max(peak,b.Enemies.Count);}
 int level2=0,level3=0;foreach(var unit in b.Units){if(unit.Level==2)level2++;else if(unit.Level==3)level3++;}Console.WriteLine(seed+","+mode+","+density+","+(enabled?"on":"off")+","+field+","+policy+","+(ember?1:0)+","+(b.Phase==Phase.Won?"Won":b.Phase==Phase.Lost?"Lost":"Stalled")+","+b.Wave+","+peak+","+b.Kills+","+Math.Round(b.Time,2).ToString(System.Globalization.CultureInfo.InvariantCulture)+","+level2+","+level3);
 }return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
static class Smart {
 static readonly System.Collections.Generic.Dictionary<int,int[]> cache=new System.Collections.Generic.Dictionary<int,int[]>();
 static Point P(int f,double d){return f==0?Balance.Path(d):Balance.Fields[f].Path(d);}
 static double Cover(int f,Point s,double range,double min){double per=f==0?Balance.Perimeter:Balance.Fields[f].Perimeter;int n=0,inside=0;for(double d=0;d<per;d+=16){n++;double dist=Point.Distance(P(f,d),s);if(dist<=range&&dist>=min)inside++;}return (double)inside/n;}
 // Swords on the half of slots that touch the most road within melee range; the rest alternate archer/bomb by long-range coverage.
 public static int Kind(int f,int slot){int[] a;if(!cache.TryGetValue(f,out a)){var S=Balance.Fields[f].Slots;int m=S.Length;var idx=new System.Collections.Generic.List<int>();for(int i=0;i<m;i++)idx.Add(i);idx.Sort((x,y)=>{int comparison=Cover(f,S[y],120,0).CompareTo(Cover(f,S[x],120,0));return comparison!=0?comparison:x.CompareTo(y);});a=new int[m];var rest=new System.Collections.Generic.List<int>();for(int i=0;i<m;i++){if(i<m/2)a[idx[i]]=0;else rest.Add(idx[i]);}rest.Sort((x,y)=>{int comparison=Cover(f,S[y],210,60).CompareTo(Cover(f,S[x],210,60));return comparison!=0?comparison:x.CompareTo(y);});for(int i=0;i<rest.Count;i++)a[rest[i]]=i%2==0?1:2;cache[f]=a;}return a[slot];}
}

// Experimental additions exist only inside this process; production field definitions are untouched.
static class CandidateWaves {
 public static void Apply(){
  var additions=new Group[][][]{
   new[]{new[]{new Group(0,10,.7,0,0),new Group(1,8,.6,1,1),new Group(2,5,1,5,0)},new[]{new Group(2,8,.8,0,1),new Group(1,10,.5,1,0),new Group(3,5,1,5,1)},new[]{new Group(4,1,0,4,0,1.1),new Group(3,6,.9,0,1),new Group(0,12,.5,3,0)},new[]{new Group(4,1,0,3,0,1.2),new Group(4,1,0,8,1,1.1),new Group(2,7,.9,0,0),new Group(1,8,.5,2,1)}},
   new[]{new[]{new Group(0,10,.7,0,0),new Group(1,8,.6,1,1),new Group(2,5,1,5,0)},new[]{new Group(2,8,.8,0,1),new Group(1,10,.5,1,0),new Group(3,5,1,5,1)},new[]{new Group(4,1,0,4,0,1.1),new Group(3,6,.9,0,1),new Group(0,12,.5,3,0)},new[]{new Group(4,1,0,3,0,1.2),new Group(4,1,0,8,1,1.1),new Group(2,7,.9,0,0),new Group(1,8,.5,2,1)}},
   new[]{new[]{new Group(0,10,.7,0,0),new Group(1,8,.6,1,1),new Group(2,5,1,5,0)},new[]{new Group(2,8,.8,0,1),new Group(1,10,.5,1,0),new Group(3,5,1,5,1)},new[]{new Group(4,1,0,4,0,1.1),new Group(3,6,.9,0,1),new Group(0,12,.5,3,0)},new[]{new Group(4,1,0,3,0,1.2),new Group(4,1,0,8,1,1.1),new Group(2,7,.9,0,0),new Group(1,8,.5,2,1)}}};
  if(Environment.GetEnvironmentVariable("R25_CANDIDATE_VARIATION")=="1"){
   for(int wave=0;wave<4;wave++)foreach(var g in additions[1][wave]){g.Gate=1-g.Gate;if(g.Kind!=4)g.Delay+=1;}
   for(int wave=0;wave<4;wave++)for(int group=0;group<additions[2][wave].Length;group++){var g=additions[2][wave][group];if((wave+group)%2==0)g.Gate=1-g.Gate;if(g.Kind==1)g.Delay+=1;}
  }
  if(Environment.GetEnvironmentVariable("R25_CANDIDATE_TUNED")=="1"){
   foreach(var g in additions[0][3])if(g.Kind==4){g.Multiplier*=.7;g.Delay=Math.Max(0,g.Delay-3);}
   foreach(var g in additions[1][3])if(g.Kind==4)g.Multiplier*=1.5;
  }
  double factor=double.Parse(Environment.GetEnvironmentVariable("R25_CANDIDATE_FACTOR")??"1",System.Globalization.CultureInfo.InvariantCulture);foreach(var field in additions)foreach(var wave in field)foreach(var g in wave)if(g.Kind!=4){g.Count=(int)Math.Round(g.Count*factor);g.Gap/=factor;}
  for(int f=0;f<Math.Min(3,Balance.Fields.Length);f++){var original=Balance.Fields[f].Waves;var expanded=new Group[original.Length+4][];Array.Copy(original,expanded,original.Length);Array.Copy(additions[f],0,expanded,original.Length,4);Balance.Fields[f].Waves=expanded;}
 }
}
