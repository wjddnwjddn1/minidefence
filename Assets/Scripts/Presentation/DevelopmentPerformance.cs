#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Unity.Profiling;
using UnityEngine;
namespace EmberBastion {
 [DefaultExecutionOrder(10000)]
 public sealed class DevelopmentPerformance : MonoBehaviour {
  const string Product="EmberBastionPerformanceQA";
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
  static void Boot(){if(UnityEngine.Debug.isDebugBuild&&Application.platform==RuntimePlatform.WindowsPlayer&&Application.productName==Product&&Array.IndexOf(Environment.GetCommandLineArgs(),"--performance-smoke")>=0)new GameObject("Isolated native performance diagnostic").AddComponent<DevelopmentPerformance>();}
  [Serializable] sealed class Metadata {
   public string unity,os,cpu,gpu,graphics,product,utc,limitations;
   public int cores,memoryMB,width,height,targetFps,vsync;
   public bool timingEnabled,windowVisible;
   public long totalAllocatedBytes,managedBytes;
  }
  struct Sample {public int frame,enemies,shots,guiCalls;public double delta,update,gui,core;public long allocated;}
  struct Timing {public ulong stamp;public double cpu,main,render,gpu,present;}
  [Serializable] sealed class StorageResult {public bool jsonEqual,stateEquivalent,replayEqual,pausedLoad,realCheckpointPreserved,partialCheckpointRejected;public int enemies,steps,jsonBytes;public double saveMs,loadMs,restoreMaxError,replayMaxError,checkpointMaxError;}
  [Serializable] sealed class InputSeries {public InputResult[] samples;}
  [Serializable] sealed class InputResult {public int enemiesAtSend;public string[] observedEvents;public bool windowFound,postSucceeded,received,pauseChanged;public double queueToEventMs,queueToFrameEndMs;public string scope="Synthetic WM_KEYDOWN to this QA window only; excludes device, display and human latency.";}
  readonly CultureInfo inv=CultureInfo.InvariantCulture;
  readonly FrameTiming[] frameTiming=new FrameTiming[1];
  string folder;
  IEnumerator Start(){
   folder=Path.Combine(Path.GetDirectoryName(Application.dataPath),"PerformanceEvidence",DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));Directory.CreateDirectory(folder);
   yield return null;
   var view=FindAnyObjectByType<BattleView>();if(view==null){Fail("No battle view");yield break;}
   Application.runInBackground=true;Application.targetFrameRate=-1;QualitySettings.vSyncCount=0;PerformanceCounters.Active=true;
   Battle snapshot=null;try{snapshot=PerformanceScenario.Capture(85);}catch(Exception e){Fail(e.Message);yield break;}
   File.WriteAllText(Path.Combine(folder,"snapshot.json"),JsonUtility.ToJson(snapshot.ExportCombat()),new UTF8Encoding(false));
   WriteMetadata("environment",0);MeasureLogic(snapshot);
   bool storageOnly=Array.IndexOf(Environment.GetCommandLineArgs(),"--storage-only")>=0;
   using(var allocations=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame")){
    foreach(int width in storageOnly?new int[0]:new[]{1280,1920})foreach(int selection in new[]{-1,1}){
     int height=width==1280?720:1080;Screen.SetResolution(width,height,FullScreenMode.Windowed);view.LoadPerformanceState(snapshot,selection);
     float warm=Time.realtimeSinceStartup+2;while(Time.realtimeSinceStartup<warm){if(view.SmokeState.Phase!=Phase.Fighting||view.SmokeState.Enemies.Count<80)view.LoadPerformanceState(snapshot,selection);yield return null;}
     var samples=new List<Sample>(900);var timings=new List<Timing>(900);ulong lastStamp=0;int safeFrames=0,resets=0;float timeout=Time.realtimeSinceStartup+90;
     while(samples.Count<900&&Time.realtimeSinceStartup<timeout){
      if(view.SmokeState.Phase!=Phase.Fighting||view.SmokeState.Enemies.Count<80||view.SmokeState.Enemies.Count>99||view.SmokeState.ReinforcementPending){view.LoadPerformanceState(snapshot,selection);safeFrames=0;resets++;}
      yield return new WaitForEndOfFrame();
      var b=view.SmokeState;
      bool eligible=b.Phase==Phase.Fighting&&b.Enemies.Count>=80&&b.Enemies.Count<=99&&!view.SmokePaused;
      safeFrames=eligible?safeFrames+1:0;
      FrameTimingManager.CaptureFrameTimings();
      // Exclude restart/warm-up frames and allow for delayed GPU timing delivery.
      if(safeFrames>=16){
       samples.Add(new Sample{frame=Time.frameCount,enemies=b.Enemies.Count,shots=b.Shots.Count,guiCalls=PerformanceCounters.GuiCalls,delta=Time.unscaledDeltaTime*1000,update=PerformanceCounters.Milliseconds(PerformanceCounters.UpdateTicks),gui=PerformanceCounters.Milliseconds(PerformanceCounters.GuiTicks),core=PerformanceCounters.Milliseconds(PerformanceCounters.CoreTicks),allocated=allocations.Valid?allocations.LastValue:-1});
       if(FrameTimingManager.GetLatestTimings(1,frameTiming)>0&&frameTiming[0].frameStartTimestamp!=lastStamp){var t=frameTiming[0];lastStamp=t.frameStartTimestamp;timings.Add(new Timing{stamp=t.frameStartTimestamp,cpu=t.cpuFrameTime,main=t.cpuMainThreadFrameTime,render=t.cpuRenderThreadFrameTime,gpu=t.gpuFrameTime,present=t.cpuMainThreadPresentWaitTime});}
      }
     }
     string label=width+(selection<0?"-battle":"-selected");WriteSamples(label,samples,timings,resets);
     if(samples.Count<900){Fail("Insufficient high-density frames: "+label);yield break;}
    }
   }
   view.LoadPerformanceState(snapshot);
   StorageResult storage=null;
   try{var b=view.SmokeState;string before=JsonUtility.ToJson(b.ExportCombat());File.WriteAllText(Path.Combine(folder,"archive-before.json"),JsonUtility.ToJson(new CombatArchive{current=b.ExportCombat(),checkpoint=null,speed=1}),new UTF8Encoding(false));var timer=Stopwatch.StartNew();CombatStore.Save(b,null,1);double save=timer.Elapsed.TotalMilliseconds;timer.Restart();Battle restored,checkpoint;int speed;CombatStore.Load(out restored,out checkpoint,out speed);double load=timer.Elapsed.TotalMilliseconds;File.WriteAllText(Path.Combine(folder,"restored-before-replay.json"),JsonUtility.ToJson(restored.ExportCombat()),new UTF8Encoding(false));bool equal=before==JsonUtility.ToJson(restored.ExportCombat());double restoreError,replayError;bool equivalent=RuntimeCombatComparison.Equivalent(b.ExportCombat(),restored.ExportCombat(),out restoreError);var control=b.CopyCombat();for(int i=0;i<120;i++){control.Step(1.0/60);restored.Step(1.0/60);}File.WriteAllText(Path.Combine(folder,"control-after-replay.json"),JsonUtility.ToJson(control.ExportCombat()),new UTF8Encoding(false));File.WriteAllText(Path.Combine(folder,"restored-after-replay.json"),JsonUtility.ToJson(restored.ExportCombat()),new UTF8Encoding(false));storage=new StorageResult{jsonEqual=equal,stateEquivalent=equivalent,restoreMaxError=restoreError,replayEqual=RuntimeCombatComparison.Equivalent(control.ExportCombat(),restored.ExportCombat(),out replayError),replayMaxError=replayError,enemies=b.Enemies.Count,steps=120,jsonBytes=Encoding.UTF8.GetByteCount(before),saveMs=save,loadMs=load};CombatStore.Save(b,b.CopyCombat(),1);Battle pairCurrent,pairCheckpoint;int pairSpeed;CombatStore.Load(out pairCurrent,out pairCheckpoint,out pairSpeed);double checkpointError=0;storage.realCheckpointPreserved=pairCheckpoint!=null&&RuntimeCombatComparison.Equivalent(b.ExportCombat(),pairCheckpoint.ExportCombat(),out checkpointError);storage.checkpointMaxError=checkpointError;PlayerPrefs.SetString("snomet.ember-bastion.unity-comparison.combat.v1",JsonUtility.ToJson(new CombatArchive{current=b.ExportCombat(),checkpoint=new CombatSaveData{gold=1},speed=1}));PlayerPrefs.Save();try{CombatStore.Load(out pairCurrent,out pairCheckpoint,out pairSpeed);}catch(ArgumentException){storage.partialCheckpointRejected=true;}CombatStore.Save(b,null,1);view.SmokeLoadTactical(Battle.ImportCombat(JsonUtility.FromJson<CombatSaveData>(before)));}catch(Exception e){Fail("Storage: "+e.Message);yield break;}
   double stopped=view.SmokeState.Time;yield return null;yield return null;storage.pausedLoad=view.SmokePaused&&view.SmokeState.Time==stopped;
   File.WriteAllText(Path.Combine(folder,"storage.json"),JsonUtility.ToJson(storage,true),new UTF8Encoding(false));
   if(!storage.stateEquivalent||!storage.replayEqual||!storage.pausedLoad||!storage.realCheckpointPreserved||!storage.partialCheckpointRejected){Fail("High-density save/replay mismatch");yield break;}
   view.LoadPerformanceState(snapshot);yield return null;
   var inputSamples=new List<InputResult>();
   for(int probe=0;probe<(storageOnly?1:30);probe++){
   if(!storageOnly&&(view.SmokeState.Phase!=Phase.Fighting||view.SmokeState.Enemies.Count<80||view.SmokeState.Enemies.Count>99||view.SmokeState.ReinforcementPending)){view.LoadPerformanceState(snapshot);yield return null;}
   IntPtr handle=OwnWindow();long sent=Stopwatch.GetTimestamp();bool wasPaused=view.SmokePaused;PerformanceCounters.InputTicks=0;PerformanceCounters.InputKey="";PerformanceCounters.InputEvents.Clear();PerformanceCounters.InputArmed=!storageOnly;
   var input=new InputResult{windowFound=handle!=IntPtr.Zero,enemiesAtSend=view.SmokeState.Enemies.Count};
   if(!storageOnly&&handle!=IntPtr.Zero){input.postSucceeded=PostMessage(handle,0x100,new IntPtr(0x50),new IntPtr(1));PostMessage(handle,0x101,new IntPtr(0x50),new IntPtr(unchecked((int)0xc0000001)));}
   float inputTimeout=Time.realtimeSinceStartup+2;
   while(!storageOnly&&Time.realtimeSinceStartup<inputTimeout&&PerformanceCounters.InputTicks==0)yield return new WaitForEndOfFrame();
   PerformanceCounters.InputArmed=false;input.observedEvents=PerformanceCounters.InputEvents.ToArray();
   if(storageOnly)input.scope="NR: storage-only hidden run; no input event sent.";
   input.received=!storageOnly&&PerformanceCounters.InputTicks>=sent&&PerformanceCounters.InputKey=="P";input.pauseChanged=view.SmokePaused!=wasPaused;
   input.queueToEventMs=input.received?PerformanceCounters.Milliseconds(PerformanceCounters.InputTicks-sent):-1;input.queueToFrameEndMs=input.received?PerformanceCounters.Milliseconds(Stopwatch.GetTimestamp()-sent):-1;
   inputSamples.Add(input);
   }
   File.WriteAllText(Path.Combine(folder,"input.json"),JsonUtility.ToJson(inputSamples[inputSamples.Count-1],true),new UTF8Encoding(false));
   if(!storageOnly)File.WriteAllText(Path.Combine(folder,"input-series.json"),JsonUtility.ToJson(new InputSeries{samples=inputSamples.ToArray()},true),new UTF8Encoding(false));
   File.WriteAllText(Path.Combine(folder,"result.txt"),"PASS native save/replay. Frame groups="+(storageOnly?0:4)+". Check visibility/GPU/input validity before interpreting performance.",new UTF8Encoding(false));
   File.WriteAllText(Path.Combine(folder,"story-render.csv"),view.StoryRenderEvidence,new UTF8Encoding(false));File.WriteAllText(Path.Combine(folder,"story-enabled.txt"),view.ReviewStoryAllowed.ToString(),new UTF8Encoding(false));
   PerformanceCounters.Active=false;PlayerPrefs.DeleteAll();PlayerPrefs.Save();Application.Quit(0);
  }
  void WriteSamples(string label,List<Sample> samples,List<Timing> timings,int resets){
   var rows=new StringBuilder("frame,enemies,shots,delta_ms,update_ms,gui_ms,core_ms,gui_calls,gc_allocated_bytes\n");foreach(var s in samples)rows.Append(s.frame).Append(',').Append(s.enemies).Append(',').Append(s.shots).Append(',').Append(F(s.delta)).Append(',').Append(F(s.update)).Append(',').Append(F(s.gui)).Append(',').Append(F(s.core)).Append(',').Append(s.guiCalls).Append(',').Append(s.allocated).Append('\n');File.WriteAllText(Path.Combine(folder,label+"-frames.csv"),rows.ToString(),new UTF8Encoding(false));
   rows=new StringBuilder("timestamp,cpu_ms,main_ms,render_ms,gpu_ms,present_wait_ms\n");foreach(var t in timings)rows.Append(t.stamp).Append(',').Append(F(t.cpu)).Append(',').Append(F(t.main)).Append(',').Append(F(t.render)).Append(',').Append(F(t.gpu)).Append(',').Append(F(t.present)).Append('\n');File.WriteAllText(Path.Combine(folder,label+"-timings.csv"),rows.ToString(),new UTF8Encoding(false));
   WriteMetadata(label,resets);
  }
  void WriteMetadata(string label,int resets){IntPtr hwnd=OwnWindow();var data=new Metadata{unity=Application.unityVersion,os=SystemInfo.operatingSystem,cpu=SystemInfo.processorType,cores=SystemInfo.processorCount,memoryMB=SystemInfo.systemMemorySize,gpu=SystemInfo.graphicsDeviceName,graphics=SystemInfo.graphicsDeviceType.ToString(),product=Application.productName,utc=DateTime.UtcNow.ToString("o"),width=Screen.width,height=Screen.height,targetFps=Application.targetFrameRate,vsync=QualitySettings.vSyncCount,timingEnabled=FrameTimingManager.IsFeatureEnabled(),windowVisible=hwnd!=IntPtr.Zero&&IsWindowVisible(hwnd)&&!IsIconic(hwnd),totalAllocatedBytes=UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong(),managedBytes=GC.GetTotalMemory(false),limitations="Development build on this PC. Snapshot replay resets="+resets+". Hidden window is not representative presented FPS. Timing samples are delayed and deduplicated; not exact per-enemy GPU attribution."};
   File.WriteAllText(Path.Combine(folder,label+"-metadata.json"),JsonUtility.ToJson(data,true),new UTF8Encoding(false));
  }
  void MeasureLogic(Battle snapshot){var rows=new StringBuilder("before_enemies,after_enemies,step_ms\n");var b=snapshot.CopyCombat();int count=0,resets=0;for(int attempt=0;attempt<100000&&count<10000;attempt++){if(b.Phase!=Phase.Fighting||b.Enemies.Count<80||b.Enemies.Count>99){b=snapshot.CopyCombat();resets++;}int before=b.Enemies.Count;long start=Stopwatch.GetTimestamp();b.Step(1.0/60);double elapsed=PerformanceCounters.Milliseconds(Stopwatch.GetTimestamp()-start);b.Cues.Clear();if(b.Phase!=Phase.Fighting||b.Enemies.Count<80||b.Enemies.Count>99)continue;rows.Append(before).Append(',').Append(b.Enemies.Count).Append(',').Append(F(elapsed)).Append('\n');count++;}File.WriteAllText(Path.Combine(folder,"logic.csv"),rows.ToString(),new UTF8Encoding(false));File.WriteAllText(Path.Combine(folder,"logic-scope.txt"),"Fixed 1/60s Battle.Step on native Unity Mono; replay copies excluded. Samples="+count+" resets="+resets+". Not render FPS, GUI, OS input or device latency.",new UTF8Encoding(false));}
  string F(double n){return n.ToString("F6",inv);}
  void Fail(string error){if(folder!=null)File.WriteAllText(Path.Combine(folder,"failure.txt"),error,new UTF8Encoding(false));UnityEngine.Debug.LogError(error);PlayerPrefs.DeleteAll();PlayerPrefs.Save();Application.Quit(1);}
  delegate bool EnumWindow(IntPtr hwnd,IntPtr data);
  [DllImport("user32.dll")]static extern bool EnumWindows(EnumWindow callback,IntPtr data);
  [DllImport("user32.dll")]static extern uint GetWindowThreadProcessId(IntPtr hwnd,out uint pid);
  [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int GetClassName(IntPtr hwnd,StringBuilder name,int count);
  [DllImport("user32.dll")]static extern bool IsWindowVisible(IntPtr hwnd);
  [DllImport("user32.dll")]static extern bool IsIconic(IntPtr hwnd);
  [DllImport("user32.dll",SetLastError=true)]static extern bool PostMessage(IntPtr hwnd,uint msg,IntPtr wParam,IntPtr lParam);
  static IntPtr OwnWindow(){uint own=(uint)Process.GetCurrentProcess().Id;IntPtr found=IntPtr.Zero;EnumWindows((hwnd,data)=>{uint pid;GetWindowThreadProcessId(hwnd,out pid);if(pid!=own)return true;var name=new StringBuilder(64);GetClassName(hwnd,name,name.Capacity);if(name.ToString()!="UnityWndClass")return true;found=hwnd;return false;},IntPtr.Zero);return found;}
 }
}
#endif
