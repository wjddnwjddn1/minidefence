using System.Collections.Generic;
using UnityEngine;
namespace EmberBastion {
 // Spawn forecast (user request 2026-11-11: "어디서 적이 소환될지 직전 몇 초 전에 표시").
 // Display only: reads public wave data; never changes the battle.
 //  - Preparation: which gate the next wave uses, with what.
 //  - Combat: a gate pulses with a countdown SpawnLead seconds before a group starts there
 //    (later groups of the current wave, and the next wave's groups).
 public sealed partial class BattleView {
  const double SpawnLead=4;
  struct GateForecast {public int Gate,Kind,Count;public int[] Kinds;public double Eta;}
  readonly List<GateForecast> gateForecasts=new List<GateForecast>();
  readonly HashSet<string> spawnWarnLogged=new HashSet<string>();
  void AddForecast(int gate,int kind,int count,double eta){
   for(int i=0;i<gateForecasts.Count;i++){var f=gateForecasts[i];if(f.Gate!=gate)continue;if(eta<f.Eta){f.Eta=eta;f.Kind=kind;}f.Count+=count;f.Kinds[kind]+=count;gateForecasts[i]=f;return;}
   var kinds=new int[Balance.Enemies.Length];kinds[kind]=count;gateForecasts.Add(new GateForecast{Gate=gate,Kind=kind,Count=count,Kinds=kinds,Eta=eta});}
  // Per kind, most numerous first: "횃불병 11 · 질주병 6".
  static string ForecastKinds(GateForecast f){var parts=new List<string>();var used=new bool[f.Kinds.Length];for(int n=0;n<f.Kinds.Length;n++){int best=-1;for(int k=0;k<f.Kinds.Length;k++)if(!used[k]&&f.Kinds[k]>0&&(best<0||f.Kinds[k]>f.Kinds[best]))best=k;if(best<0)break;used[best]=true;parts.Add(Balance.Enemies[best].Name+" "+f.Kinds[best]);}return string.Join(" · ",parts.ToArray());}
  void BuildForecasts(){
   gateForecasts.Clear();
   if(battle.ReinforcementPending)return;
   if(battle.Phase==Phase.Preparation){if(battle.Wave<battle.WaveCount)foreach(var g in battle.WaveGroups(battle.Wave))AddForecast(g.Gate,g.Kind,battle.SpawnCount(g),-1);return;}
   if(battle.Phase!=Phase.Fighting)return;
   if(battle.Wave>=1)foreach(var g in battle.WaveGroups(battle.Wave-1)){double eta=g.Delay-battle.WaveTime;if(eta>0&&eta<=SpawnLead)AddForecast(g.Gate,g.Kind,battle.SpawnCount(g),eta);}
   double next=battle.NextWaveIn;
   if(battle.Wave<battle.WaveCount&&next>0)foreach(var g in battle.WaveGroups(battle.Wave)){double eta=next+g.Delay;if(eta<=SpawnLead)AddForecast(g.Gate,g.Kind,battle.SpawnCount(g),eta);}
  }
  Point GatePoint(int gate){double d=battle.Field.Gates[gate];return battle.FieldIndex==0?Balance.Path(d):battle.Field.Path(d);}
  void SpawnWarnings(){
   if(battle.Phase!=Phase.Preparation&&battle.Phase!=Phase.Fighting)return;
   if(branchMenu||toolsMenu||fieldMenu||guideMenu)return;
   BuildForecasts();if(gateForecasts.Count==0)return;
   float boardWidth=1280*scale,boardHeight=768*scale;
   foreach(var f in gateForecasts){
    if(f.Gate<0||f.Gate>=battle.Field.Gates.Length)continue;
    Point p=GatePoint(f.Gate);float cx=ox+(float)p.X*scale,cy=oy+(float)p.Y*scale;bool soon=f.Eta>=0;
    float pulse=soon?Mathf.PingPong(Time.unscaledTime*(f.Eta<1.5?4:2),1):0;
    Color ring=soon?new Color(1f,.28f,.18f,.7f+.3f*pulse):new Color(1f,.6f,.2f,.9f);
    float r=Mathf.Max(16,(soon?36+8*pulse:32)*scale);int dots=soon?32:24;float dot=Mathf.Max(5,8*scale);Color edge=new Color(.08f,.04f,.03f,.9f);
    for(int i=0;i<dots;i++){float a=i*Mathf.PI*2/dots,dx=cx+Mathf.Cos(a)*r,dy=cy+Mathf.Sin(a)*r;Box(new Rect(dx-dot/2-1.5f,dy-dot/2-1.5f,dot+3,dot+3),edge);Box(new Rect(dx-dot/2,dy-dot/2,dot,dot),ring);}
    string line=(soon?Mathf.CeilToInt((float)f.Eta)+"초 · ":(battle.Wave+1)+"웨이브 · ")+ForecastKinds(f);
    int prior=hud.fontSize;TextAnchor align=hud.alignment;hud.fontSize=Screen.width<480?11:13;hud.alignment=TextAnchor.MiddleCenter;
    float w=MeasureText(hud,line).x+14,h=20;float x=Mathf.Clamp(cx-w/2,ox+2,ox+boardWidth-w-2),y=cy-r-h-2;if(y<oy+2)y=cy+r+2;y=Mathf.Clamp(y,oy+2,oy+boardHeight-h-2);
    Box(new Rect(x,y,w,h),soon?new Color(.35f,.06f,.04f,.92f):new Color(.12f,.1f,.04f,.85f));GUI.Label(new Rect(x,y+1,w,h-2),line,hud);
    hud.fontSize=prior;hud.alignment=align;
    if(soon&&Debug.isDebugBuild&&Application.platform==RuntimePlatform.WebGLPlayer&&spawnWarnLogged.Add(battle.Wave+"|"+f.Gate+"|"+Mathf.CeilToInt((float)(battle.Time+f.Eta))))
     Debug.Log("SPAWN_WARN|"+battle.Wave+"|"+f.Gate+"|"+f.Eta.ToString("F1",System.Globalization.CultureInfo.InvariantCulture)+"|"+f.Kind+"|"+f.Count);
   }
  }
 }
}
