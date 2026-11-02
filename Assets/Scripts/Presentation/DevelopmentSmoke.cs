#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace EmberBastion {
 // Opt-in development executable check. Never runs in a normal player session.
 public sealed class DevelopmentSmoke : MonoBehaviour {
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
  static void Boot(){var args=Environment.GetCommandLineArgs();if(Debug.isDebugBuild)Application.runInBackground=true;if(Array.IndexOf(args,"--comparison-smoke")>=0||Array.IndexOf(args,"--forest-smoke")>=0||Array.IndexOf(args,"--pass-smoke")>=0||Array.IndexOf(args,"--branch-smoke")>=0||Array.IndexOf(args,"--window-smoke")>=0)new GameObject("Opt-in development smoke").AddComponent<DevelopmentSmoke>();}
  IEnumerator Start(){yield return null;var view=FindAnyObjectByType<BattleView>();if(Array.IndexOf(Environment.GetCommandLineArgs(),"--window-smoke")>=0){bool initial=Screen.fullScreen;view.SmokeWindowMode();yield return new WaitForSecondsRealtime(2);bool changed=Screen.fullScreen;view.SmokeWindowMode();yield return new WaitForSecondsRealtime(2);bool restored=Screen.fullScreen;File.WriteAllText(Path.GetFullPath(Path.Combine(Application.dataPath,"../../../Docs/window-smoke.txt")),"initial="+initial+" changed="+changed+" restored="+restored,new System.Text.UTF8Encoding(false));Application.Quit();yield break;}bool forest=Array.IndexOf(Environment.GetCommandLineArgs(),"--forest-smoke")>=0,pass=Array.IndexOf(Environment.GetCommandLineArgs(),"--pass-smoke")>=0,branch=Array.IndexOf(Environment.GetCommandLineArgs(),"--branch-smoke")>=0;if(forest||pass)view.SmokeField(pass?2:1);string folder=Path.GetFullPath(Path.Combine(Application.dataPath,branch?"../../../Docs/runtime-smoke-branch":pass?"../../../Docs/runtime-smoke-pass":forest?"../../../Docs/runtime-smoke-forest":"../../../Docs/runtime-smoke"));Directory.CreateDirectory(folder);var log=new StreamWriter(Path.Combine(folder,"result.txt"),false,new System.Text.UTF8Encoding(false));log.AutoFlush=true;
   if(branch){var fixture=view.SmokeState;fixture.Gold=500;fixture.Place(1,0);fixture.Upgrade(0);view.SmokeBranch(0);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,"branch-options.png"));yield return null;log.WriteLine("fixture-level="+fixture.At(0).Level+" gold="+fixture.Gold+"; isolated memory only");log.Dispose();Application.Quit();yield break;}
   yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,"preparation.png"));yield return null;
   var b=view.SmokeState;b.Place(0,0);b.Place(1,1);b.Start();view.SimulationSpeed=15;
   while(b.WaveTime<2)yield return null;
   view.SmokePause();double stopped=b.Time;view.SmokeAim(new Point(400,128));yield return null;yield return null;log.WriteLine("pause-time-frozen="+(b.Time==stopped)+" cooldown-before-resume="+b.EmberCD);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,"queued-pause.png"));view.SmokePause();yield return null;log.WriteLine("resumed="+(!view.SmokePaused)+" ember-cooldown="+b.EmberCD);
   yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,"combat.png"));
   float deadline=Time.realtimeSinceStartup+120;bool gotNext=false;
   while(b.Phase!=Phase.Won&&b.Phase!=Phase.Lost&&Time.realtimeSinceStartup<deadline){
    if(b.Phase==Phase.Fighting){if(!gotNext&&b.Wave>=2){log.WriteLine("automatic-wave="+b.Wave+" gold="+b.Gold);gotNext=true;}int[] slots={4,9,5,10,12,3,8,6,11,13,2,7};foreach(int slot in slots)if(b.At(slot)==null&&b.Gold>=85)b.Place(1,slot);foreach(var u in b.Units)if(u.Level==1&&b.Gold>=50)b.Upgrade(u.Slot);}
    if(b.EmberCD<=0&&b.Enemies.Count>4)view.SmokeAim(b.Enemies[b.Enemies.Count/2].Position);yield return null;
   }
   log.WriteLine("outcome="+b.Phase+" wave="+b.Wave+" hp="+b.HP+" gold="+b.Gold+" kills="+b.Kills+" cause="+b.EndCause);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,"outcome.png"));yield return null;
   view.SmokeRetry();log.WriteLine("retry="+view.SmokeState.Phase+" field="+view.SmokeState.FieldIndex+" gold="+view.SmokeState.Gold+" units="+view.SmokeState.Units.Count);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,"retry.png"));yield return null;log.Dispose();Application.Quit();
  }
 }
}
#endif
