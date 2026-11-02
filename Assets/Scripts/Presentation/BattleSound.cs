using System;
using System.Collections.Generic;
using UnityEngine;
namespace EmberBastion {
 // Small original synthesized cues. No game simulation or random state is affected.
 public sealed class BattleSound : MonoBehaviour {
  const string VolumeKey="snomet.ember-bastion.unity-comparison.sound-volume.v2";const string MuteKey="snomet.ember-bastion.unity-comparison.sound-muted.v1";
  readonly Dictionary<string,AudioClip> clips=new Dictionary<string,AudioClip>();
  readonly Dictionary<string,float> lastPlayed=new Dictionary<string,float>();
  AudioSource source;bool muted;int volumeLevel;
  public bool Muted {get{return muted;}}public string VolumeLabel {get{return volumeLevel==0?"소리 끔":volumeLevel==1?"소리 작게":"소리 크게";}}
  void Awake(){source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=0;source.volume=.32f;try{volumeLevel=PlayerPrefs.HasKey(VolumeKey)?PlayerPrefs.GetInt(VolumeKey,2):(PlayerPrefs.GetInt(MuteKey,0)!=0?0:2);if(volumeLevel<0||volumeLevel>2)volumeLevel=2;}catch(Exception){volumeLevel=2;}muted=volumeLevel==0;source.volume=volumeLevel==1?.16f:.32f;
   clips["death"]=MakeNoise("death",.11f,145,.16f);clips["coin"]=MakeCoin("coin",.12f,.09f);clips["bosscoin"]=MakeCoin("bosscoin",.22f,.16f);clips["place"]=MakeNoise("place",.10f,110,.12f);clips["upgrade"]=Make("upgrade",.19f,330,880,.13f);clips["click"]=MakeNoise("click",.045f,450,.06f);clips["invalid"]=Make("invalid",.14f,190,110,.11f);clips["attack"]=Make("attack",.07f,620,390,.10f);clips["blast"]=Make("blast",.16f,160,65,.16f);
   clips["ember"]=Make("ember",.20f,380,110,.18f);clips["warn"]=Make("warn",.19f,530,670,.14f);
   clips["danger"]=Make("danger",.24f,700,360,.16f);clips["roar"]=Make("roar",.23f,165,95,.15f);clips["wave"]=Make("wave",.18f,440,660,.12f);
   clips["win"]=Make("win",.32f,470,790,.14f);clips["lose"]=Make("lose",.32f,330,140,.14f);
   if(Smoke()){var sample=new float[clips["attack"].samples];if(!clips["attack"].GetData(sample,0))throw new InvalidOperationException("Cannot read sound samples");float peak=0;foreach(float v in sample)peak=Mathf.Max(peak,Mathf.Abs(v));if(peak<.01f||peak>.3f)throw new InvalidOperationException("Invalid sound samples");Debug.Log("SOUND_READY muted="+muted+" peak="+peak);}
  }
  static AudioClip Make(string name,float seconds,float start,float end,float gain){const int rate=22050;int length=Mathf.CeilToInt(rate*seconds);var samples=new float[length];double phase=0;for(int i=0;i<length;i++){float t=(float)i/Mathf.Max(1,length-1);float frequency=Mathf.Lerp(start,end,t);phase+=2*Math.PI*frequency/rate;float envelope=Mathf.Min(1,t*30)*Mathf.Pow(1-t,1.8f);samples[i]=(float)(Math.Sin(phase)*.8+Math.Sin(phase*2)*.2)*gain*envelope;}var clip=AudioClip.Create("ember-"+name,length,1,rate,false);clip.SetData(samples,0);return clip;}
  static AudioClip MakeCoin(string name,float seconds,float gain){const int rate=22050;int length=Mathf.CeilToInt(rate*seconds);var samples=new float[length];double phase=0;for(int i=0;i<length;i++){float t=(float)i/Mathf.Max(1,length-1);float frequency=t<.5f?760:1140;phase+=2*Math.PI*frequency/rate;float local=t<.5f?t*2:(t-.5f)*2;float envelope=Mathf.Min(1,local*20)*Mathf.Pow(1-local,1.5f);samples[i]=(float)(Math.Sin(phase)*.75+Math.Sin(phase*2)*.25)*gain*envelope;}var clip=AudioClip.Create("ember-"+name,length,1,rate,false);clip.SetData(samples,0);return clip;}
  static AudioClip MakeNoise(string name,float seconds,float frequency,float gain){const int rate=22050;int length=Mathf.CeilToInt(rate*seconds);var samples=new float[length];uint random=1234567;double phase=0;for(int i=0;i<length;i++){float t=(float)i/Mathf.Max(1,length-1);random=1664525*random+1013904223;float noise=((random>>8)/(float)0xFFFFFF)*2-1;phase+=2*Math.PI*frequency/rate;float envelope=Mathf.Min(1,t*40)*Mathf.Pow(1-t,2);samples[i]=(float)(Math.Sin(phase)*.7+noise*.3)*gain*envelope;}var clip=AudioClip.Create("ember-"+name,length,1,rate,false);clip.SetData(samples,0);return clip;}
  static bool Smoke(){return Debug.isDebugBuild&&Application.platform==RuntimePlatform.WebGLPlayer&&(Application.absoluteURL.Contains("fx-smoke=sound")||Application.absoluteURL.Contains("fx-smoke=dangersound"));}
  public void Toggle(){volumeLevel=(volumeLevel+1)%3;muted=volumeLevel==0;source.volume=volumeLevel==1?.16f:.32f;if(muted&&source!=null)source.Stop();try{PlayerPrefs.SetInt(VolumeKey,volumeLevel);PlayerPrefs.SetInt(MuteKey,muted?1:0);PlayerPrefs.Save();}catch(Exception){}if(Smoke())Debug.Log("SOUND_TOGGLE muted="+muted+" level="+volumeLevel);}

  public void Play(string kind){if(muted||source==null||!Application.isFocused)return;AudioClip clip;if(!clips.TryGetValue(kind,out clip)||clip==null)return;float cooldown=kind=="click"?.05f:kind=="invalid"?.15f:kind=="attack"?.12f:kind=="blast"?.18f:kind=="coin"?.12f:kind=="death"?.08f:.3f,last;if(lastPlayed.TryGetValue(kind,out last)&&Time.unscaledTime-last<cooldown)return;lastPlayed[kind]=Time.unscaledTime;source.PlayOneShot(clip);if(Smoke())Debug.Log("SOUND_PLAY "+kind);}
  void OnDestroy(){foreach(var clip in clips.Values)if(clip!=null)Destroy(clip);}
 }
}
