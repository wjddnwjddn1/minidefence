using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEditor.Build.Reporting;
namespace EmberBastion.Editor {
 public static class ComparisonBuild {
  const string ScenePath="Assets/Scenes/Comparison.unity";
  [MenuItem("Ember Bastion/Create comparison scene")]
  public static void Scene(){Directory.CreateDirectory("Assets/Scenes");var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);new GameObject("Battle").AddComponent<BattleView>();var camera=new GameObject("Camera").AddComponent<Camera>();camera.gameObject.AddComponent<AudioListener>();camera.orthographic=true;camera.backgroundColor=new Color(.08f,.13f,.12f);EditorSceneManager.SaveScene(scene,ScenePath);EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};}
  [MenuItem("Ember Bastion/Build Windows comparison")]
  public static void Windows(){Build(BuildTarget.StandaloneWindows64,"Builds/Windows/EmberBastion.exe");}
  public static void RulesMigrationQA(){Build(BuildTarget.StandaloneWindows64,"Tests/bin/RulesMigrationQA/EmberBastion.exe",true,"EmberBastionRulesMigrationQA");}
  public static void R36LocalValidation(){PerformanceQA();StoryReview();TacticalCandidate();WindowsPlayable();}
  public static void StoryReview(){Build(BuildTarget.StandaloneWindows64,"Tests/bin/StoryReview/EmberBastion.exe",true,"EmberBastionStoryReview");}
  public static void PerformanceQA(){Build(BuildTarget.StandaloneWindows64,"Tests/bin/PerformanceQA/EmberBastion.exe",true,"EmberBastionPerformanceQA");}
  public static void TacticalWeb(){PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Disabled;Build(BuildTarget.WebGL,"Tests/bin/TacticalWeb",true,"EmberBastionTacticalCandidate");}
  [MenuItem("Ember Bastion/Build tactical candidate (development)")]
  public static void TacticalCandidate(){Build(BuildTarget.StandaloneWindows64,"Tests/bin/TacticalCandidate/EmberBastion.exe",true,"EmberBastionTacticalCandidate");File.WriteAllText("Tests/bin/TacticalCandidate/Playtest.cmd","@echo off\r\n\"%~dp0EmberBastion.exe\" --tactical-playtest\r\n",new System.Text.UTF8Encoding(false));}
  [MenuItem("Ember Bastion/Build Windows playable")]
  public static void WindowsPlayable(){Build(BuildTarget.StandaloneWindows64,"Builds/Playable/EmberBastion.exe",false);}
#if UNITY_EDITOR_OSX
  [MenuItem("Ember Bastion/Build Mac playable (Universal)")]
  public static void MacPlayable(){UnityEditor.OSXStandalone.UserBuildSettings.architecture=UnityEditor.Build.OSArchitecture.x64ARM64;string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../실행용/EmberBastion.app"));Directory.CreateDirectory(Path.GetDirectoryName(output));Build(BuildTarget.StandaloneOSX,output,false);CodeSign(output,"--force --deep --sign -");CodeSign(output,"--verify --deep --strict");}
  static void CodeSign(string output,string arguments){var start=new System.Diagnostics.ProcessStartInfo("/usr/bin/codesign",arguments+" \""+output.Replace("\\","\\\\").Replace("\"","\\\"")+"\"");start.UseShellExecute=false;using(var process=System.Diagnostics.Process.Start(start)){process.WaitForExit();if(process.ExitCode!=0)throw new Exception("Mac app signing failed: "+process.ExitCode);}}
#endif
  [MenuItem("Ember Bastion/Build Web comparison")]
  public static void Web(){PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Disabled;Build(BuildTarget.WebGL,"Builds/Web");}
  static void Build(BuildTarget target,string output,bool development=true,string productName="EmberBastionUnityComparison"){if(!File.Exists(ScenePath))Scene();PlayerSettings.companyName="SnometComparison";PlayerSettings.productName=productName;PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;PlayerSettings.runInBackground=false;var report=BuildPipeline.BuildPlayer(new[]{ScenePath},output,target,development?BuildOptions.Development:BuildOptions.None);if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Comparison build failed: "+report.summary.result);}
 }
}
