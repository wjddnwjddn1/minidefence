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
  public static void Scene(){Directory.CreateDirectory("Assets/Scenes");var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);new GameObject("Battle").AddComponent<BattleView>();var camera=new GameObject("Camera").AddComponent<Camera>();camera.orthographic=true;camera.backgroundColor=new Color(.08f,.13f,.12f);EditorSceneManager.SaveScene(scene,ScenePath);EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};}
  [MenuItem("Ember Bastion/Build Windows comparison")]
  public static void Windows(){Build(BuildTarget.StandaloneWindows64,"Builds/Windows/EmberBastion.exe");}
  [MenuItem("Ember Bastion/Build Windows playable")]
  public static void WindowsPlayable(){Build(BuildTarget.StandaloneWindows64,"Builds/Playable/EmberBastion.exe",false);}
  [MenuItem("Ember Bastion/Build Web comparison")]
  public static void Web(){PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Disabled;Build(BuildTarget.WebGL,"Builds/Web");}
  static void Build(BuildTarget target,string output,bool development=true){if(!File.Exists(ScenePath))Scene();PlayerSettings.companyName="SnometComparison";PlayerSettings.productName="EmberBastionUnityComparison";PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;PlayerSettings.runInBackground=false;var report=BuildPipeline.BuildPlayer(new[]{ScenePath},output,target,development?BuildOptions.Development:BuildOptions.None);if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Comparison build failed: "+report.summary.result);}
 }
}
