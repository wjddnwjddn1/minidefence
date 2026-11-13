#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using EmberBastion;
static class NativeExpeditionPolicy {
 static readonly Dictionary<int,int[]> cache=new Dictionary<int,int[]>();
 static double Cover(FieldDef f,Point s,double range,double min){int n=0,inside=0;for(double d=0;d<f.Perimeter;d+=16){n++;double dist=Point.Distance(f.Path(d),s);if(dist<=range&&dist>=min)inside++;}return (double)inside/n;}
 // Swords on the half of the slots that touch the most road in melee reach; the rest alternate archer, bomb and frost by long reach.
 public static int Kind(Battle b,int slot){int[] a;int key=b.FieldIndex*10+(int)b.Mode;if(!cache.TryGetValue(key,out a)){var f=b.Field;var S=f.Slots;int m=S.Length;var idx=new List<int>();for(int i=0;i<m;i++)idx.Add(i);idx.Sort((x,y)=>{int c=Cover(f,S[y],120,0).CompareTo(Cover(f,S[x],120,0));return c!=0?c:x.CompareTo(y);});a=new int[m];var rest=new List<int>();for(int i=0;i<m;i++){if(i<m/2)a[idx[i]]=0;else rest.Add(idx[i]);}rest.Sort((x,y)=>{int c=Cover(f,S[y],210,60).CompareTo(Cover(f,S[x],210,60));return c!=0?c:x.CompareTo(y);});int[] cycle={1,2,1,3};for(int i=0;i<rest.Count;i++)a[rest[i]]=cycle[i%cycle.Length];cache[key]=a;}return a[slot];}
 // skill 0: three level-1 troops and nothing else. 1: fill and upgrade to level 2. 2: also take level-3 branches.
 public static void Buy(Battle b,int skill){
  int placed=b.Units.Count;
  for(int slot=0;slot<b.Field.Slots.Length;slot++)if(b.At(slot)==null){if(skill==0&&placed>=3)break;int kind=Kind(b,slot);if(b.Gold>=Balance.Troops[kind].Cost&&b.Place(kind,slot))placed++;}
  if(skill==0)return;
  foreach(var u in b.Units){if(u.Level==1&&b.Gold>=50)b.Upgrade(u.Slot);else if(skill>=2&&u.Level==2&&b.Gold>=90)b.UpgradeBranch(u.Slot,u.Kind==0?2:1);else if(skill>=2&&u.Level==3&&u.Mastery==0)b.UpgradeMastery(u.Slot);}
 }
 // Plays one battle. Counts finished laps from outside so that ordinary battles can be measured too.
 public static void Play(Battle b,int skill,out int laps,out int bossLaps){
  laps=0;bossLaps=0;var seen=new Dictionary<int,int>();int ticks=0;
  while(b.Phase!=Phase.Won&&b.Phase!=Phase.Lost&&ticks<600000){
   if(ticks%60==0)Buy(b,skill);
   if(b.Phase==Phase.Preparation&&!b.Start())throw new Exception("start failed");
   if(b.ReinforcementPending)b.ChooseReinforcement(b.Reinforcements.Choices()[0]);
   b.Step(Balance.Tick);ticks++;
   foreach(var e in b.Enemies){int old;seen.TryGetValue(e.Id,out old);if(e.Laps>old){if(e.Kind==4)bossLaps+=e.Laps-old;else laps+=e.Laps-old;seen[e.Id]=e.Laps;}}
  }
 }
 // The smoke policy favors the standard route, then takes supply if no plain road is available.
 public static int NextField(Expedition run){
  int[] fields=run.NextFields();
  foreach(int field in fields)if(run.NextRoad(field)==Expedition.RoadPlain)return field;
  foreach(int field in fields)if(run.NextRoad(field)==Expedition.RoadSupply)return field;
  return fields[0];
 }
 // Camp choice. 0 first offer, 1 damage for the classes in use, 2 keep the ember warm, 3 economy.
 public static int Camp(Expedition run,int style){
  var offers=run.Offers();int[] order;
  if(style==1)order=new[]{Boon.Phalanx,Boon.RapidFire,Boon.FieldForge,Boon.SwordDrill,Boon.StrongBow,Boon.WarDrum,Boon.PowderMix,Boon.FrostCore,Boon.Spyglass,Boon.Rekindle,Boon.Spoils,Boon.WarChest};
  else if(style==2)order=run.Warmth<=80?new[]{Boon.Rekindle,Boon.EmberWard,Boon.DeepFrost,Boon.Shatter,Boon.FrostCore,Boon.WarDrum,Boon.SwordDrill,Boon.StrongBow,Boon.PowderMix,Boon.Spyglass,Boon.Spoils,Boon.WarChest}:new[]{Boon.DeepFrost,Boon.Shatter,Boon.ChainBlast,Boon.FieldForge,Boon.FrostCore,Boon.WarDrum,Boon.SwordDrill,Boon.StrongBow,Boon.PowderMix,Boon.Spyglass,Boon.Spoils,Boon.WarChest,Boon.Rekindle};
  else if(style==3)order=new[]{Boon.Spoils,Boon.WarChest,Boon.FieldForge,Boon.ChainBlast,Boon.WarDrum,Boon.SwordDrill,Boon.StrongBow,Boon.PowderMix,Boon.FrostCore,Boon.Spyglass,Boon.Rekindle};
  else return offers[0];
  foreach(int boon in order)if(Array.IndexOf(offers,boon)>=0)return boon;return offers[0];
 }
}
namespace EmberBastion {
 public sealed class ExpeditionDiagnostic : MonoBehaviour {
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){if(Debug.isDebugBuild&&Application.productName=="EmberBastionExpeditionQA"&&Application.platform==RuntimePlatform.WindowsPlayer&&Array.IndexOf(Environment.GetCommandLineArgs(),"--expedition-review")>=0)new GameObject("Isolated expedition checkpoint validation").AddComponent<ExpeditionDiagnostic>();}
  BattleView view;string folder;readonly UTF8Encoding utf8=new UTF8Encoding(false);
  void Require(bool ok,string message){if(!ok)throw new Exception(message);}
  void ReloadCheck(){var before=view.SmokeState.ExportCombat();view.ReviewRunSave();view.ReviewRunReload();double error;Require(RuntimeCombatComparison.Equivalent(before,view.SmokeState.ExportCombat(),out error),"Run checkpoint changed combat state");Require(view.SmokePaused&&view.SmokeState.Mode==BattleMode.Expedition&&view.SmokeState.ExportCombat().expedition!=null,"Run must resume paused with expedition payload");}
  IEnumerator Start(){Application.runInBackground=true;Application.targetFrameRate=-1;yield return null;view=FindAnyObjectByType<BattleView>();folder=Path.Combine(Path.GetDirectoryName(Application.dataPath),"Evidence");Directory.CreateDirectory(folder);
   if(Array.IndexOf(Environment.GetCommandLineArgs(),"--prepare")>=0){
    try{view.ReviewRunStart(7919);NativeExpeditionPolicy.Buy(view.SmokeState,2);view.ReviewRunStartWave();for(int i=0;i<660;i++){if(i%60==0)NativeExpeditionPolicy.Buy(view.SmokeState,2);view.SmokeState.Step(Balance.Tick);}view.ReviewRunSave();File.WriteAllText(Path.Combine(folder,"cold-checkpoint.json"),JsonUtility.ToJson(view.SmokeState.ExportCombat()),utf8);File.WriteAllText(Path.Combine(folder,"PREPARED.txt"),"Natural economy, 11 seconds, actual session PlayerPrefs; application now exits without deleting checkpoint.",utf8);Application.Quit(0);}catch(Exception e){Fail(e);}yield break;
   }
   try{view.ReviewRunReload();var before=JsonUtility.FromJson<CombatSaveData>(File.ReadAllText(Path.Combine(folder,"cold-checkpoint.json"),utf8));double error;Require(RuntimeCombatComparison.Equivalent(before,view.SmokeState.ExportCombat(),out error),"Cold application restart changed checkpoint");Require(view.SmokePaused,"Cold resume did not pause");view.ReviewRunFreeReturn();Require(RuntimeCombatComparison.Equivalent(before,view.SmokeState.ExportCombat(),out error),"Leaving to free battle reset expedition checkpoint");}catch(Exception e){Fail(e);yield break;}
   var rows=new StringBuilder("stage,field,road,outcome,warmth,peak,kills,seconds,masteries,skill_cues\n");
   int totalMasteries=0,totalSkillCues=0;bool masteryCheckpoint=false;
   int stages=view.ReviewRun.Stages;
   for(int stage=0;stage<stages;stage++){
    Battle b=view.SmokeState;if(b.Phase==Phase.Preparation){NativeExpeditionPolicy.Buy(b,2);view.ReviewRunStartWave();}
    int skillCues=0;
    for(int tick=0;tick<600000&&b.Phase==Phase.Fighting;tick++){
     if(tick%60==0)NativeExpeditionPolicy.Buy(b,2);if(b.ReinforcementPending)b.ChooseReinforcement(b.Reinforcements.Choices()[0]);b.Step(Balance.Tick);
     foreach(var cue in b.Cues)if(cue.Kind=="skill")skillCues++;
     bool checkMastery=!masteryCheckpoint&&b.Phase==Phase.Fighting&&b.Units.Exists(unit=>unit.Mastery>0&&b.SkillCharge(unit)>0);
     if(tick==600||checkMastery){
      try{
       ReloadCheck();
       b=view.SmokeState;
       if(checkMastery){
        Require(b.Units.Exists(unit=>unit.Mastery>0&&b.SkillCharge(unit)>0),"Mastery or charged skill lost on reload");
        masteryCheckpoint=true;
       }
      }catch(Exception e){Fail(e);yield break;}
     }
     if(tick%1800==0)yield return null;
    }
    try{
     Require(b.Phase==Phase.Won,"Natural expedition did not win stage "+stage+" · field "+b.FieldIndex+" · warmth "+b.HP);
     int masteries=b.Units.FindAll(unit=>unit.Mastery>0).Count;
     totalMasteries+=masteries;totalSkillCues+=skillCues;
     rows.Append(stage).Append(',').Append(b.FieldIndex).Append(',').Append(view.ReviewRun.RoadAt(stage)).Append(',')
      .Append(b.Phase).Append(',').Append(b.HP).Append(',').Append(b.PeakAlive).Append(',').Append(b.Kills).Append(',')
      .Append(b.Time.ToString("F4",System.Globalization.CultureInfo.InvariantCulture)).Append(',').Append(masteries).Append(',').Append(skillCues).Append('\n');
     view.ReviewRunComplete();
     if(stage==stages-1)Require(view.ReviewRun.State==Expedition.Status.Won,"Whole run did not finish");
     else{
      var run=view.ReviewRun;
      Require(run.State==Expedition.Status.Camp,"Camp missing");
      int field=NativeExpeditionPolicy.NextField(run);
      if(run.FinaleNext)Require(field==Expedition.FinaleField,"Finale must return to the gate");
      view.ReviewRunChoose(NativeExpeditionPolicy.Camp(run,2),field);
      Require(view.SmokeState.FieldIndex==field,"Chosen route did not start");
     }
    }catch(Exception e){File.WriteAllText(Path.Combine(folder,"natural-run.csv"),rows.ToString(),utf8);Fail(e);yield break;}
   }
   try{Require(totalMasteries>0&&totalSkillCues>0&&masteryCheckpoint,"Run did not exercise mastery and real skills");File.WriteAllText(Path.Combine(folder,"natural-run.csv"),rows.ToString(),utf8);File.WriteAllText(Path.Combine(folder,"won-run.json"),JsonUtility.ToJson(view.ReviewRun.Export(),true),utf8);view.ReviewRunSave();
    view.ReviewRunStart(29);NativeExpeditionPolicy.Buy(view.SmokeState,2);view.ReviewRunStartWave();view.ReviewRunAbandon();Require(view.ReviewRun.State==Expedition.Status.Lost,"Abandon was not a loss");bool rejected=false;try{view.ReviewRunReload();}catch(Exception){rejected=true;}Require(rejected&&view.ReviewRun.State==Expedition.Status.Lost,"Abandoned run resumed");
    view.ReviewPreDoctrineSaves();
    File.WriteAllText(Path.Combine(folder,"PASS.txt"),"PASS actual Windows two-process PlayerPrefs cold resume, exact discrete/tolerant-double combat state, paused reload at every stage and with an active mastery/skill charge, natural-economy whole-run win (four fields and the last stand), abandon persisted as loss, both pre-doctrine formats removed and new run available. Automated policy; not human play or performance.",utf8);view.ReviewRunForget();Application.Quit(0);
   }catch(Exception e){Fail(e);}
  }
  void Fail(Exception e){File.WriteAllText(Path.Combine(folder,"failure.txt"),e.ToString(),utf8);Debug.LogError(e);if(view!=null)view.ReviewRunForget();Application.Quit(1);}
 }
}
#endif
