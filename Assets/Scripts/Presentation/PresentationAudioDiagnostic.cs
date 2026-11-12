using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace EmberBastion {
 public sealed class PresentationAudioDiagnostic:MonoBehaviour {
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){if(Application.platform==RuntimePlatform.WindowsPlayer&&Array.IndexOf(Environment.GetCommandLineArgs(),"--audio-output-review")>=0)new GameObject("Audio output review").AddComponent<PresentationAudioDiagnostic>();}
  IEnumerator Start(){yield return new WaitForSecondsRealtime(1);int exit=0;string old=null;try{var outputs=WindowsAudioOutputs.List();old=WindowsAudioOutputs.Current();Debug.Log("AUDIO_NATIVE_OUTPUTS "+outputs.Count);foreach(var item in outputs)Debug.Log("AUDIO_NATIVE_NAME "+item.Name);if(outputs.Count==0)throw new Exception("No active outputs");foreach(var item in outputs){WindowsAudioOutputs.Select(item.Id);if(WindowsAudioOutputs.Current()!=item.Id)throw new Exception("Policy readback mismatch");Debug.Log("AUDIO_NATIVE_SELECT_PASS "+item.Name);}WindowsAudioOutputs.Select("");if(WindowsAudioOutputs.Current()!="")throw new Exception("Default readback mismatch");Debug.Log("AUDIO_NATIVE_DEFAULT_PASS");}catch(Exception e){Debug.LogError("AUDIO_NATIVE_FAIL "+e);exit=1;}finally{if(old!=null)try{WindowsAudioOutputs.Select(old);Debug.Log("AUDIO_NATIVE_RESTORED");}catch(Exception e){Debug.LogError(e);exit=1;}}Application.Quit(exit);}
 }
}
