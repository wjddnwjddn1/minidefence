using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
namespace EmberBastion {
 public sealed partial class BattleView {
  readonly List<Rect> warningBlocks=new List<Rect>();readonly List<string> warningGeometry=new List<string>();string lastWarningGeometry="",warningStateBefore="";
  string WarningState(){return JsonUtility.ToJson(battle.Phase==Phase.Preparation?(object)battle.SavePreparation():battle.ExportCombat());}
  void ReserveWarning(string name,Rect rect){if(rect.width<=0)return;warningBlocks.Add(rect);if(WarningFixture)warningGeometry.Add(name+","+rect.x.ToString("F2",CultureInfo.InvariantCulture)+","+rect.y.ToString("F2",CultureInfo.InvariantCulture)+","+rect.width.ToString("F2",CultureInfo.InvariantCulture)+","+rect.height.ToString("F2",CultureInfo.InvariantCulture));}
  void BeginWarningLayout(){if(WarningFixture)warningStateBefore=WarningState();warningBlocks.Clear();warningGeometry.Clear();ReserveWarning("hud",new Rect(0,0,Screen.width,72));ReserveWarning("controls",new Rect(0,Screen.height-64,Screen.width,64));
   foreach(var e in battle.Enemies)if(e.Kind==4||e.Id==inspectedEnemy){float size=e.Kind==4?150:e.Kind==1?92:112,bar=battle.Enemies.Count>=45&&e.Kind!=4?22:56,x=Mathf.Clamp((float)e.Position.X-bar/2,4,1276-bar)-22,y=Mathf.Clamp((float)e.Position.Y-size*.3f-12,20,710)-27;ReserveWarning("name-"+e.Id,new Rect(ox+x*scale-3,oy+y*scale-3,130*scale+8,38*scale+8));}}
  Rect PlaceWarning(Rect wanted){Rect bounds=new Rect(ox+2,Mathf.Max(74,oy+2),Mathf.Max(1,1280*scale-4),Mathf.Max(1,Mathf.Min(Screen.height-66,oy+768*scale-2)-Mathf.Max(74,oy+2)));wanted.width=Mathf.Min(wanted.width,bounds.width);if(wanted.height>bounds.height)return Rect.zero;
   float[] xs={Mathf.Clamp(wanted.x,bounds.x,bounds.xMax-wanted.width),bounds.x,bounds.xMax-wanted.width,bounds.center.x-wanted.width/2};
   for(int step=0;step<40;step++)foreach(float x in xs){float offset=step==0?0:((step+1)/2)*28*(step%2==1?1:-1);Rect candidate=new Rect(x,Mathf.Clamp(wanted.y+offset,bounds.y,bounds.yMax-wanted.height),wanted.width,wanted.height);bool clear=true;foreach(var block in warningBlocks)if(new Rect(block.x-3,block.y-3,block.width+6,block.height+6).Overlaps(candidate)){clear=false;break;}if(clear)return candidate;}
   if(WarningFixture)Debug.LogError("WARNING_LAYOUT_NO_SPACE");return Rect.zero;
  }
  void LogWarningLayout(){if(!WarningFixture||Event.current.type!=EventType.Repaint)return;if(warningStateBefore!=WarningState())Debug.LogError("WARN_STATE_MUTATED");else if(debugRibbonLogged.Add("warn-read-only"))Debug.Log("WARN_READ_ONLY");string data=string.Join(";",warningGeometry.ToArray());if(data==lastWarningGeometry)return;lastWarningGeometry=data;Debug.Log("WARN_LAYOUT|"+data);}
 }
}
