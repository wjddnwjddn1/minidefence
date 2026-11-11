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
  List<SpawnForecast.Entry> gateForecasts=new List<SpawnForecast.Entry>();
  readonly HashSet<string> spawnWarnLogged=new HashSet<string>();
  // Per kind, most numerous first: "횃불병 11 · 질주병 6".
  static string ForecastKinds(SpawnForecast.Entry f){var parts=new List<string>();var used=new bool[f.Kinds.Length];for(int n=0;n<f.Kinds.Length;n++){int best=-1;for(int k=0;k<f.Kinds.Length;k++)if(!used[k]&&f.Kinds[k]>0&&(best<0||f.Kinds[k]>f.Kinds[best]))best=k;if(best<0)break;used[best]=true;parts.Add(Balance.Enemies[best].Name+" "+f.Kinds[best]);}return string.Join(" · ",parts.ToArray());}
  void BuildForecasts(){gateForecasts=SpawnForecast.Build(battle,SpawnLead);if(WarningFixture){var rows=new List<string>();foreach(var f in gateForecasts)rows.Add(f.Gate+","+f.Eta.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+","+f.Count);string row=battle.Wave+"|"+string.Join(";",rows.ToArray());if(debugRibbonLogged.Add("forecast|"+row))Debug.Log("FORECAST_STATE|"+row);}}
  Point GatePoint(int gate){double d=battle.Field.Gates[gate];return battle.FieldIndex==0?Balance.Path(d):battle.Field.Path(d);}
  void SpawnWarnings(){
   if(battle.Phase!=Phase.Preparation&&battle.Phase!=Phase.Fighting)return;
   if(branchMenu||toolsMenu||fieldMenu||guideMenu||menuSlot>=0||battle.ReinforcementPending)return;
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
    bool oldWrap=hud.wordWrap;hud.wordWrap=true;float maxWidth=Mathf.Max(80,boardWidth-8),w=Mathf.Min(MeasureText(hud,line).x+14,maxWidth),h=Mathf.Clamp(hud.CalcHeight(new GUIContent(line),w-12)+4,20,56);Rect label=PlaceWarning(new Rect(cx-w/2,cy-r-h-2,w,h));
    if(label.width>0){ReserveWarning("spawn-"+f.Gate+"-"+f.Eta.ToString("F1",System.Globalization.CultureInfo.InvariantCulture),label);Box(label,soon?new Color(.35f,.06f,.04f,.92f):new Color(.12f,.1f,.04f,.85f));GUI.Label(new Rect(label.x+6,label.y+2,label.width-12,label.height-4),line,hud);}hud.wordWrap=oldWrap;
    hud.fontSize=prior;hud.alignment=align;
    if(soon&&Debug.isDebugBuild&&Application.platform==RuntimePlatform.WebGLPlayer&&spawnWarnLogged.Add(battle.Wave+"|"+f.Gate+"|"+(battle.Time+f.Eta).ToString("F2",System.Globalization.CultureInfo.InvariantCulture)))
     Debug.Log("SPAWN_WARN|"+battle.Wave+"|"+f.Gate+"|"+f.Eta.ToString("F1",System.Globalization.CultureInfo.InvariantCulture)+"|"+f.Kind+"|"+f.Count+"|"+ForecastKinds(f));
   }
  }
 }
}
