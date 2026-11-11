using UnityEngine;
namespace EmberBastion {
 // Claude r31 decision 2: crisis warning -> suggested slot. Display only; no pause, no purchase, no numbers.
 public sealed partial class BattleView {
  Battle crisisBattle;int crisisWave=-1,crisisShown,crisisHP,crisisSlot=-1;bool crisisBuild,crisisAbove;float crisisUntil;
  void UpdateCrisis(){
   if(crisisBattle!=battle){crisisBattle=battle;crisisWave=-1;crisisShown=0;crisisHP=battle.HP;crisisAbove=false;crisisUntil=0;crisisSlot=-1;}
   if(battle.Phase!=Phase.Fighting){crisisHP=battle.HP;crisisAbove=false;if(battle.Phase!=Phase.Fighting)crisisUntil=0;return;}
   if(battle.Wave!=crisisWave){crisisWave=battle.Wave;crisisShown=0;}
   bool above=battle.Enemies.Count>=CrisisAdvice.AliveThreshold,hit=battle.HP<crisisHP;
   bool trigger=(above&&!crisisAbove)||hit;crisisAbove=above;crisisHP=battle.HP;
   if(trigger&&crisisShown<CrisisAdvice.PerWave){crisisShown++;crisisSlot=CrisisAdvice.Suggest(battle,out crisisBuild);crisisUntil=Time.unscaledTime+6;
    if(Debug.isDebugBuild&&Application.platform==RuntimePlatform.WebGLPlayer)Debug.Log("CRISIS|"+battle.Wave+"|"+battle.Enemies.Count+"|"+battle.HP+"|"+crisisSlot+"|"+(crisisBuild?1:0));}
   // The suggestion goes stale once acted on (slot filled / unit upgraded past what we pointed at).
   if(crisisSlot>=0&&crisisUntil>Time.unscaledTime){bool b;if(CrisisAdvice.Suggest(battle,out b)!=crisisSlot||b!=crisisBuild)crisisSlot=-1;}
  }
  string CrisisLine(){
   string head=battle.Enemies.Count>=CrisisAdvice.AliveThreshold?"적 "+battle.Enemies.Count+"명":"성채 피해";
   if(crisisSlot<0)return head+" · 지금 살 수 있는 대응 없음";
   return head+" · "+(crisisSlot+1)+"번 자리 "+(crisisBuild?"배치":"강화")+" 추천";}
  void CrisisBanner(){
   if(battle.Phase!=Phase.Fighting||crisisUntil<=Time.unscaledTime||paused||menuSlot>=0||branchMenu||toolsMenu||fieldMenu||guideMenu)return;
   float boardWidth=1280*scale,width=Mathf.Min(360,Mathf.Max(150,boardWidth-24)),height=26,x=ox+(boardWidth-width)/2,y=oy+Mathf.Max(5,8*scale)+40;
   float alpha=.75f+.25f*Mathf.PingPong(Time.unscaledTime*2,1);
   Box(new Rect(x,y,width,height),new Color(.35f,.08f,.05f,.92f*alpha));
   int prior=hud.fontSize;TextAnchor align=hud.alignment;hud.fontSize=Screen.width<480?12:14;hud.alignment=TextAnchor.MiddleCenter;string line=CrisisLine();
   while(hud.fontSize>10&&MeasureText(hud,line).x>width-12)hud.fontSize--;GUI.Label(new Rect(x+6,y+2,width-12,height-4),line,hud);hud.fontSize=prior;hud.alignment=align;
   if(crisisSlot>=0&&crisisSlot<battle.Field.Slots.Length){Point p=battle.Field.Slots[crisisSlot];float r=(30+6*Mathf.PingPong(Time.unscaledTime*3,1))*scale,cx=ox+(float)p.X*scale,cy=oy+(float)p.Y*scale;
    Color c=new Color(1f,.55f,.2f,alpha);for(int i=0;i<28;i++){float a=i*Mathf.PI*2/28;Box(new Rect(cx+Mathf.Cos(a)*r-3,cy+Mathf.Sin(a)*r-3,6,6),c);}}
  }
 }
}
