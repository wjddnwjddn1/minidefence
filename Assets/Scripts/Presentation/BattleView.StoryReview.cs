using System;
using UnityEngine;
namespace EmberBastion {
 public sealed partial class BattleView {
  bool? cachedStoryReview,cachedStoryPerformance;
  bool StoryReview {get{if(!cachedStoryReview.HasValue)cachedStoryReview=Debug.isDebugBuild&&(Application.platform==RuntimePlatform.WebGLPlayer&&FixtureText("story-case","")!=""||Application.productName=="EmberBastionStoryReview"&&Array.IndexOf(Environment.GetCommandLineArgs(),"--story-review")>=0);return cachedStoryReview.Value;}}
  bool StoryPerformance {get{if(!cachedStoryPerformance.HasValue)cachedStoryPerformance=Debug.isDebugBuild&&Application.productName=="EmberBastionPerformanceQA"&&Array.IndexOf(Environment.GetCommandLineArgs(),"--story-performance")>=0;return cachedStoryPerformance.Value;}}
  readonly int[] storyRenderCounts=new int[5];
  void StoryRender(string kind){if(!Debug.isDebugBuild||Event.current.type!=EventType.Repaint||!StoryReview&&!StoryPerformance)return;int index=kind=="slow"?0:kind=="brittle"?1:kind=="frost"?2:kind=="speech"?3:4;storyRenderCounts[index]++;if(StoryReview&&debugRibbonLogged.Add("story-render|"+kind))StoryLog("STORY_RENDER|"+kind);}
  internal string StoryRenderEvidence {get{return "slow,brittle,frost,speech,bark\n"+string.Join(",",Array.ConvertAll(storyRenderCounts,x=>x.ToString()))+"\n";}}
  void ReviewFrostVisual(){if(!StoryReview||FixtureText("story-case","")!="frost")return;foreach(var v in visuals)if(v.Cue.Text=="QA frost")v.Age=0;if(!visuals.Exists(v=>v.Cue.Text=="QA frost"))visuals.Add(new VisualCue{Cue=new Cue("frost",new Point(480,200),"QA frost")});foreach(var v in visuals)if(v.Cue.Kind=="hit"&&v.Cue.Brittle)v.Age=0;if(!visuals.Exists(v=>v.Cue.Kind=="hit"&&v.Cue.Brittle))visuals.Add(new VisualCue{Cue=new Cue("hit",new Point(640,200),"15"){Brittle=true,Source=1}});}
  int storyPaints;string storyReviewScene="";
  void InitStoryReview(){if(!StoryReview)return;if(FixtureInt("story-reset",0)==1)ReviewStoryReset();int field=FixtureInt("story-field",0);ReviewStoryPrepare(field);int forgotten=FixtureInt("story-forget-field",-1);if(forgotten>=0){PlayerPrefs.DeleteKey(StoryKey+"field"+forgotten);PlayerPrefs.Save();}if(FixtureText("story-case","")=="events"||FixtureText("story-case","")=="frost"){MarkStory("opening");MarkStory("field"+field);battle.Place(0,0);battle.Start();SetWarningEnemies(FixtureText("story-case","")=="frost"?5:65);if(FixtureText("story-case","")=="frost"){battle.Units.Clear();battle.Gold=1000;battle.Place(3,0);battle.Enemies[0].SlowTime=2;battle.Enemies[0].SlowPct=.35;}else battle.Enemies[0].Kind=4;battle.Enemies[0].Position=new Point(640,140);BindBattle();}StoryLog("STORY_REVIEW_READY|"+StoryEnabled+"|"+PreferencesAllowed());}
  internal void ReviewStoryReset(){if(!StoryReview)return;foreach(string id in new[]{"opening","field0","field1","field2","field3","ending","frost-intro","speech"})PlayerPrefs.DeleteKey(StoryKey+id);PlayerPrefs.Save();storySeen.Clear();storySpeech=null;storyEnabled=null;sceneLines=null;}
  internal void ReviewStoryPrepare(int field){battle=Battle.CreateForRules(29,field,4);BindBattle();titleMenu=toolsMenu=fieldMenu=guideMenu=branchMenu=paused=false;tutorialStage=0;SimulationSpeed=0;sceneLines=null;storyBattle=null;}
  internal bool ReviewStorySceneOpen {get{return StorySceneOpen;}}
  internal string ReviewStorySceneId {get{return sceneId;}}
  internal int ReviewStoryPaints {get{return storyPaints;}}
  internal bool ReviewStoryAllowed {get{return StoryEnabled;}}
  internal bool ReviewStoryPrefs {get{return PreferencesAllowed();}}
  internal void ReviewStoryAdvance(bool skip){AdvanceScene(skip);}
  internal void ReviewStoryRefreshMemory(){storySeen.Clear();storySpeech=null;}
  internal bool ReviewStoryWasSeen(string id){return StorySeen(id);}
  void StoryReviewControl(string id,Rect rect){if(!StoryReview||Event.current.type!=EventType.Repaint)return;string row=id+"|"+rect.x+","+rect.y+","+rect.width+","+rect.height;if(debugRibbonLogged.Add("story-control|"+row))StoryLog("STORY_CONTROL|"+row);}
  void StoryReviewScene(Rect body,string line){if(!StoryReview||Event.current.type!=EventType.Repaint)return;storyPaints++;float required=hud.CalcHeight(new GUIContent(line),body.width);if(required>body.height+.5f)Debug.LogError("STORY_TEXT_OVERFLOW|"+sceneId+"|"+sceneIndex);string row=sceneId+"|"+sceneIndex;if(storyReviewScene!=row){storyReviewScene=row;StoryLog("STORY_PAINT|"+row+"|"+hud.fontSize);}}
  void ReviewPerformanceStory(){if(!StoryPerformance)return;storyBattle=battle;storyPhase=battle.Phase;storyWave=battle.Wave;storyHP=battle.HP;sceneLines=null;foreach(string id in new[]{"opening","field0","field1","field2","field3","frost-intro"})storySeen.Add(id);sayLine=StoryScript.BossRoarReply;sayUntil=Time.unscaledTime+2;sayNext=null;if(battle.Enemies.Count>0){barkEnemy=battle.Enemies[0].Id;barkText=StoryScript.Barks[0][0];barkUntil=Time.unscaledTime+2;nextBarkAt=Time.unscaledTime+10;}}
  bool HandleStoryReview(Event e){if(!StoryReview||e.type!=EventType.KeyDown)return false;if(e.keyCode==KeyCode.F9){battle.Enemies[0].Roared=true;}else if(e.keyCode==KeyCode.F10){battle.HP-=10;}else if(e.keyCode==KeyCode.F11){barkEnemy=battle.Enemies[battle.Enemies.Count-1].Id;barkText=StoryScript.Barks[0][0];barkUntil=Time.unscaledTime+10;nextBarkAt=Time.unscaledTime+20;}else if(e.keyCode==KeyCode.F12){battle.Phase=e.shift?Phase.Lost:Phase.Won;}else if(e.keyCode==KeyCode.F8){toolsMenu=true;storyMenu=true;}else return false;e.Use();return true;}
 }
}
#if DEVELOPMENT_BUILD || UNITY_EDITOR
namespace EmberBastion {
 public sealed class StoryReviewDiagnostic : MonoBehaviour {
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){if(Debug.isDebugBuild&&Application.platform==RuntimePlatform.WindowsPlayer&&Application.productName=="EmberBastionStoryReview"&&Array.IndexOf(Environment.GetCommandLineArgs(),"--story-review")>=0)new GameObject("Isolated native story validation").AddComponent<StoryReviewDiagnostic>();}
  System.Collections.IEnumerator Start(){Application.runInBackground=true;yield return null;var view=FindAnyObjectByType<BattleView>();string dir=System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.dataPath),"Evidence");System.IO.Directory.CreateDirectory(dir);string failure=null;
   view.ReviewStoryReset();view.ReviewStoryPrepare(0);yield return new WaitForSecondsRealtime(.5f);
   try{if(!view.ReviewStoryAllowed||!view.ReviewStoryPrefs||view.ReviewStorySceneId!="opening"||view.ReviewStoryPaints==0)throw new Exception("Native opening was not painted with real prefs");while(view.ReviewStorySceneOpen)view.ReviewStoryAdvance(false);}catch(Exception e){failure=e.Message;}
   yield return null;
   try{if(view.ReviewStorySceneId!="field0")throw new Exception("Opening did not lead to field intro");view.ReviewStoryAdvance(true);PlayerPrefs.Save();view.ReviewStoryRefreshMemory();if(!view.ReviewStoryWasSeen("opening")||!view.ReviewStoryWasSeen("field0"))throw new Exception("Story actual prefs did not persist");view.ReviewStoryPrepare(0);}catch(Exception e){failure=e.Message;}
   yield return new WaitForSecondsRealtime(.3f);
   if(view.ReviewStorySceneOpen)failure="Seen native intro reopened";
   System.IO.File.WriteAllText(System.IO.Path.Combine(dir,failure==null?"PASS.txt":"failure.txt"),failure??"PASS native opening painted, opening advanced to field0, actual isolated PlayerPrefs persisted, revisited intro stayed closed. Direct scene advancement; keyboard/pointer separately tested in Web.",new System.Text.UTF8Encoding(false));PlayerPrefs.DeleteAll();PlayerPrefs.Save();Application.Quit(failure==null?0:1);
  }
 }
}
#endif
