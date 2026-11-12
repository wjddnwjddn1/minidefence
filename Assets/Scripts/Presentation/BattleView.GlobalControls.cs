using UnityEngine;
namespace EmberBastion {
 public sealed partial class BattleView {
  void GlobalControls(){if(LowLandscape){CompactControls();return;}
   bool ended=battle.Phase==Phase.Won||battle.Phase==Phase.Lost,prep=battle.Phase==Phase.Preparation;
   float y=Screen.height-64;UiPlate(new Rect(2,y,Screen.width-4,62));int old=button.fontSize;button.fontSize=Screen.width<600?13:16;
   bool retry=!ExpeditionBattle&&battle.Phase==Phase.Lost&&(checkpoint!=null||waveCheckpoint!=null),next=!ExpeditionBattle&&ended&&battle.Phase==Phase.Won&&battle.FieldIndex<Balance.Fields.Length-1,early=!prep&&!ended&&EarlyAvailable();
   float width=retry?Mathf.Min(300,(Screen.width-32)/2):Mathf.Min(300,Screen.width-((battle.Phase==Phase.Fighting||Screen.width<700&&prep&&battle.Wave==0)?132:24));Rect primary=new Rect(Screen.width-width-12,y+10,width,44);
   if(battle.Phase==Phase.Fighting&&Button(new Rect(12,y+10,100,44),paused?"이어가기 (P)":"전투 정지 (P)"))TogglePause();
   if(Screen.width<700&&prep&&battle.Wave==0)ExpeditionEntry(y);
   if(retry&&Button(new Rect(12,y+10,width,44),"웨이브 다시"))RetryCheckpoint();
   else if(!retry&&Screen.width>=700){float entry=ExpeditionEntry(y);GUI.Label(new Rect((battle.Phase==Phase.Fighting?128:16)+entry,y+18,Screen.width-width-160-entry,30),ExpeditionBattle&&!prep?ExpeditionHint():"빈 자리: 병과 선택   ·   병력 자리: 강화 / 판매",hud);}
   string action=ended?(ExpeditionBattle?"원정 화면":next?"다음 전장":"재도전"):prep?"전투 시작":battle.Decided?(quickClear?"원래 속도":"빠른 소탕"):early?EarlyLabel():"다음 호출 대기";
   if(PrimaryButton(primary,action,ended||(prep?battle.Units.Exists(unit=>unit.Kind!=4):battle.Decided?!paused:early))){if(ended&&ExpeditionBattle)OpenExpeditionMenu();else if(next)OpenField(battle.FieldIndex+1);else if(ended)ResetRun();else if(prep)StartWave();else if(battle.Decided)ToggleQuickClear();else EarlyAction();}
   button.fontSize=old;
  }
  int[] ActiveTacticalChoices(){return System.Array.FindAll(battle.Reinforcements.Choices(),card=>card!=ReinforcementDraft.EmberCooldown);}
  bool audioMenu;
  void OpenAudioMenu(){audioMenu=true;audioOutputList=false;RefreshAudioOutputs();if(Debug.isDebugBuild&&Application.platform==RuntimePlatform.WebGLPlayer&&Application.absoluteURL.Contains("review-flow=1"))Debug.Log("AUDIO_OPEN");}
  void AudioPanel(){
   float width=Mathf.Min(560,Screen.width-24),height=Mathf.Min(audioOutputList?440:310,Screen.height-24),x=(Screen.width-width)/2,y=(Screen.height-height)/2;Box(new Rect(0,0,Screen.width,Screen.height),new Color(0,0,0,.75f));UiPlate(new Rect(x,y,width,height));UiTitle(new Rect(x+12,y+5,width-24,36),"소리 · 출력 장치",smallTitle);
   bool windows=Application.platform==RuntimePlatform.WindowsPlayer;
   if(audioOutputList){if(MenuButton(new Rect(x+width-112,y+8,96,30),"새로고침"))RefreshAudioOutputs();AudioOutputChoices(x+16,y+48,width-32,height-104);}
   else{
    float row=height<290?38:46,bh=row-6;
    if(MenuButton(new Rect(x+16,y+48,width-32,bh),sound.VolumeLabel+" · 음량 변경")){sound.Toggle();if(Debug.isDebugBuild&&Application.platform==RuntimePlatform.WebGLPlayer&&Application.absoluteURL.Contains("review-flow=1"))Debug.Log("AUDIO_VOLUME|"+sound.VolumeLabel);}
    if(MenuButton(new Rect(x+16,y+48+row,width-32,bh),"테스트 소리",!sound.Muted))sound.Play("upgrade");
    int old=button.fontSize;button.fontSize=12;if(MenuButton(new Rect(x+16,y+48+row*2,width-32,bh),windows?"출력: "+AudioOutputName()+" ▾":"출력 장치 · Windows 실행판에서 선택",windows)){RefreshAudioOutputs();audioOutputList=true;}button.fontSize=old;
    bool wrap=menuNotice.wordWrap;int size=menuNotice.fontSize;menuNotice.fontSize=12;menuNotice.wordWrap=true;GUI.Label(new Rect(x+16,y+48+row*3,width-32,40),audioOutputMessage.Length>0?audioOutputMessage:"이 게임의 출력만 바꿉니다. 장치를 연결한 뒤 목록을 새로고침하세요.",menuNotice);menuNotice.wordWrap=wrap;menuNotice.fontSize=size;
   }
   if(MenuButton(new Rect(x+16,y+height-42,width-32,32),"뒤로")){if(audioOutputList)audioOutputList=false;else audioMenu=false;}
  }
  void TacticalCardIcon(int card,Rect rect){
   if(card==ReinforcementDraft.EmberCooldown){if(emberRound!=null)GUI.DrawTexture(rect,emberRound,ScaleMode.ScaleToFit,true);}
   else if(card==ReinforcementDraft.SwordDamage||card==ReinforcementDraft.SwordTargets){if(iconSword!=null)GUI.DrawTexture(rect,iconSword,ScaleMode.ScaleToFit,true);}
   else if(card==ReinforcementDraft.GoldIncome){if(coin!=null)GUI.DrawTexture(rect,coin,ScaleMode.ScaleToFit,true);}
   else if(card==ReinforcementDraft.BombRadius)DrawRoundBomb(rect,false);
   else if(card==ReinforcementDraft.ArcherHaste)TerrainView.Image(arrow,rect,0,0,64,64);
   else {Color color=uiGold;float cx=rect.center.x,cy=rect.center.y;Box(new Rect(cx-15,cy-1,30,2),color);Box(new Rect(cx-1,cy-15,2,30),color);Box(new Rect(cx-8,cy-8,16,2),color);Box(new Rect(cx-8,cy+6,16,2),color);Box(new Rect(cx-8,cy-8,2,16),color);Box(new Rect(cx+6,cy-8,2,16),color);}
  }
 }
}
