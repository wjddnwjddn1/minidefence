using System;
using System.Collections.Generic;
using UnityEngine;
namespace EmberBastion {
 public sealed partial class BattleView {
  List<WindowsAudioOutputs.Output> audioOutputs=new List<WindowsAudioOutputs.Output>();string audioOutputId="",audioOutputMessage="";bool audioOutputList;Vector2 audioOutputScroll;
  void RefreshAudioOutputs(){audioOutputMessage="";if(Application.platform!=RuntimePlatform.WindowsPlayer)return;try{audioOutputs=WindowsAudioOutputs.List();audioOutputId=WindowsAudioOutputs.Current();}catch(Exception){audioOutputs.Clear();audioOutputMessage="장치를 읽지 못했습니다. 새로고침해 주세요.";}}
  string AudioOutputName(){var item=audioOutputs.Find(x=>x.Id==audioOutputId);return audioOutputId==""?"기본 장치":item==null?"연결이 끊긴 장치 · 기본 사용":item.Name;}
  void PickAudioOutput(string id){try{WindowsAudioOutputs.Select(id);audioOutputId=id;audioOutputMessage="출력 장치 적용됨 · 테스트 소리로 확인하세요.";audioOutputList=false;sound.Play("upgrade");}catch(Exception){audioOutputMessage="전환 실패 · 기존 출력을 유지합니다. 새로고침해 주세요.";}}
  void AudioOutputChoices(float x,float y,float w,float h){
   int old=button.fontSize;button.fontSize=13;bool wrap=button.wordWrap;button.wordWrap=false;
   Rect area=new Rect(x,y,w,h);audioOutputScroll=GUI.BeginScrollView(area,audioOutputScroll,new Rect(0,0,w-22,(audioOutputs.Count+1)*40));
   if(MenuButton(new Rect(0,0,w-24,36),(audioOutputId==""?"✓ ":"")+"기본 장치 (Windows 설정 따름)"))PickAudioOutput("");
   for(int i=0;i<audioOutputs.Count;i++){var item=audioOutputs[i];string label=(item.Id==audioOutputId?"✓ ":"")+item.Name;while(button.fontSize>10&&MeasureText(button,label).x>w-42)button.fontSize--;if(MenuButton(new Rect(0,(i+1)*40,w-24,36),label))PickAudioOutput(item.Id);button.fontSize=13;}
   GUI.EndScrollView();button.fontSize=old;button.wordWrap=wrap;
  }
 }
}
