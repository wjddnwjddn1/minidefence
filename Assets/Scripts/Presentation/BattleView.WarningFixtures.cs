using System;
using UnityEngine;
namespace EmberBastion {
 public sealed partial class BattleView {
  bool WarningFixture {get{return Debug.isDebugBuild&&Application.platform==RuntimePlatform.WebGLPlayer&&(FixtureInt("crisis",0)==1||FixtureText("spawn-case","")!=""||StoryReview);}}
  bool warningBoss;string warningCase="";
  string FixtureText(string key,string fallback){foreach(string item in Application.absoluteURL.Split('?','&')){string[] pair=item.Split('=');if(pair.Length==2&&pair[0]==key)return pair[1];}return fallback;}
  void SetWarningEnemies(int count){battle.Enemies.Clear();for(int i=0;i<count;i++){double travel=i*31;Point point=battle.FieldIndex==0?Balance.Path(travel):battle.Field.Path(travel);battle.Enemies.Add(new Enemy{Id=1000+i,Kind=i==0&&warningBoss?4:0,HP=100,MaxHP=100,Speed=0,Travel=travel,Position=i==0&&warningBoss?new Point(640,140):point});}battle.PeakAlive=Math.Max(battle.PeakAlive,count);}
  double LastSpawn(int index){double last=0;foreach(var g in battle.WaveGroups(index))last=Math.Max(last,g.Delay+Math.Max(0,battle.SpawnCount(g)-1)*battle.SpawnGap(g));return last;}
  void InitWarningFixture(){if(!WarningFixture||StoryReview)return;warningCase=FixtureText("crisis-case","build");string spawn=FixtureText("spawn-case","");int field=FixtureInt("field",2);battle=Battle.CreateForRules(29,field,4);battle.Gold=20000;titleMenu=false;tutorialStage=0;selected=-1;paused=false;SimulationSpeed=0;
   if(spawn=="prep"){BindBattle();Debug.Log("WARNING_FIXTURE|prep");return;}
   battle.Place(0,0);battle.Start();if(warningCase=="upgrade"){for(int i=1;i<battle.Field.Slots.Length;i++)battle.Place(0,i);}else battle.Units.Clear();
   int wave=1;double clock=10;
   if(spawn=="next"||spawn=="early"||spawn=="dual"){if(spawn=="dual"){wave=-1;for(int index=1;index<battle.WaveCount;index++){bool a=false,b=false;foreach(var g in battle.WaveGroups(index)){if(g.Delay>3)continue;a|=g.Gate==0;b|=g.Gate==1;}if(a&&b){wave=index;break;}}if(wave<1)throw new Exception("No overlapping dual-gate warning window");}clock=Math.Max(battle.WaveInterval,LastSpawn(wave-1))-(spawn=="dual"?1:3);}
   if(spawn=="delayed"){wave=-1;for(int index=0;index<battle.WaveCount&&wave<0;index++)foreach(var g in battle.WaveGroups(index))if(g.Delay>0){wave=index+1;clock=Math.Max(0,g.Delay-Math.Min(3,g.Delay/2));break;}if(wave<1)throw new Exception("No delayed group fixture");}
   warningBoss=warningCase=="overlap";if(warningBoss){wave=battle.WaveCount;clock=0;decidedSeen=true;decidedUntil=Time.unscaledTime+30;}
   int count=spawn!=""||warningCase=="damage"?5:warningCase=="sound"?75:65;SetWarningEnemies(count);
   var data=battle.ExportCombat();data.wave=wave;data.waveTime=clock;data.time=clock+(wave-1)*32;data.pending=new SpawnSave[0];data.nextId=1000+count;data.peakAlive=count;data.gold=warningCase=="poor"?0:1000;data.reinforcement.owned=new int[ReinforcementDraft.CardCount];data.reinforcement.owned[ReinforcementDraft.GoldIncome]=wave<3?0:wave<6?1:wave>=9?3:2;
   battle=Battle.ImportCombat(data);if(spawn=="card"){battle.Wave=2;battle.Reinforcements.Offer(true,true,true);}BindBattle();
   Debug.Log("WARNING_FIXTURE|"+(spawn!=""?spawn:warningCase)+"|"+battle.FieldIndex+"|"+battle.Wave+"|"+battle.WaveTime);
  }
  bool HandleWarningFixture(Event e){if(!WarningFixture||e.type!=EventType.KeyDown)return false;if(e.keyCode==KeyCode.F6){SetWarningEnemies(battle.Enemies.Count>=60?59:65);e.Use();return true;}if(e.keyCode==KeyCode.F7){battle.HP=Math.Max(1,battle.HP-15);e.Use();return true;}if(e.keyCode==KeyCode.F8){battle.Wave=Math.Min(battle.WaveCount,battle.Wave+1);e.Use();return true;}return false;}
 }
}
