using UnityEngine;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System.Diagnostics;
#endif
namespace EmberBastion {
 public sealed partial class BattleView {
  void Update(){
#if DEVELOPMENT_BUILD || UNITY_EDITOR
   if(PerformanceCounters.Active){PerformanceCounters.BeginFrame();long start=Stopwatch.GetTimestamp();try{UpdateBattleView();}finally{PerformanceCounters.UpdateTicks+=Stopwatch.GetTimestamp()-start;}return;}
#endif
   UpdateBattleView();
  }
  void OnGUI(){
#if DEVELOPMENT_BUILD || UNITY_EDITOR
   if(PerformanceCounters.Active){PerformanceCounters.BeginFrame();if(PerformanceCounters.InputArmed&&Event.current.type==EventType.KeyDown){long observed=Stopwatch.GetTimestamp();if(PerformanceCounters.InputEvents.Count<8)PerformanceCounters.InputEvents.Add(Event.current.keyCode+":"+(int)Event.current.character+":"+observed);if(Event.current.keyCode==KeyCode.P&&PerformanceCounters.InputTicks==0){PerformanceCounters.InputTicks=observed;PerformanceCounters.InputKey="P";}}long start=Stopwatch.GetTimestamp();try{DrawInterface();}finally{PerformanceCounters.GuiTicks+=Stopwatch.GetTimestamp()-start;PerformanceCounters.GuiCalls++;}return;}
#endif
   DrawInterface();
  }
  void AdvanceBattle(float delta,float speed){
#if DEVELOPMENT_BUILD || UNITY_EDITOR
   if(PerformanceCounters.Active){long start=Stopwatch.GetTimestamp();try{AdvanceBattleCore(delta,speed);}finally{PerformanceCounters.CoreTicks+=Stopwatch.GetTimestamp()-start;}return;}
#endif
   AdvanceBattleCore(delta,speed);
  }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
  internal void LoadPerformanceState(Battle state,int selection=-1){battle=state.CopyCombat();BindBattle();visuals.Clear();titleMenu=toolsMenu=fieldMenu=guideMenu=branchMenu=aiming=paused=false;queued=null;tutorialStage=0;selected=selection;SimulationSpeed=1;dangerBand=0;}
#endif
 }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
 internal static class PerformanceCounters {
  public static bool Active;
  public static long UpdateTicks,GuiTicks,CoreTicks,InputTicks;
  public static int GuiCalls;
  public static string InputKey="";
  public static bool InputArmed;
  public static readonly System.Collections.Generic.List<string> InputEvents=new System.Collections.Generic.List<string>();
  static int frame=-1;
  public static void BeginFrame(){if(frame==Time.frameCount)return;frame=Time.frameCount;UpdateTicks=GuiTicks=CoreTicks=0;GuiCalls=0;}
  public static double Milliseconds(long ticks){return ticks*1000.0/Stopwatch.Frequency;}
 }
#endif
}
