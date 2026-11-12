using System.Collections.Generic;
using UnityEngine;
using EmberBastion.PixelFX;
namespace EmberBastion {
 public sealed partial class BattleView {
  sealed class AttackSheet {public Texture2D texture;public Sheet data;}
  readonly Dictionary<string,AttackSheet> attackSheets=new Dictionary<string,AttackSheet>();
  AttackSheet AttackFx(string name){AttackSheet sheet;if(attackSheets.TryGetValue(name,out sheet))return sheet;var tex=Resources.Load<Texture2D>("PixelFX/"+name);var json=Resources.Load<TextAsset>("PixelFX/"+name);sheet=tex==null||json==null?null:new AttackSheet{texture=tex,data=SheetReader.Read(json,tex)};attackSheets[name]=sheet;return sheet;}
  void AttackEffect(VisualCue v){
   bool review=Debug.isDebugBuild&&Application.platform==RuntimePlatform.WebGLPlayer&&Application.absoluteURL.Contains("fx-smoke=attack-review");float age=review?.12f:v.Age;
   if(age>.42f||v.Cue.Source<0||v.Cue.Source>3)return;
   // Dense battles keep short release flashes but skip late smoke/trails. No simulation or damage changes.
   if(battle.Enemies.Count>=70&&age>.18f)return;
   int kind=v.Cue.Source,branch=v.Branch;string name=kind==0?(branch==1?"SwordSweep":branch==2?"SwordBreaker":"SwordSlash"):kind==1?"HitArrow":kind==3?"HitFrost":branch==2?"Burn":"HitSteel";
   var sheet=AttackFx(name);if(sheet==null)return;float duration=kind==0?.4f:.2f;if(age>=duration)return;
   int frame=Mathf.Min(sheet.data.frames.Length-1,(int)(age/duration*sheet.data.frames.Length));var r=sheet.data.frames[frame].frame;var pivot=sheet.data.meta.pixelfx.pivot;
   float k=kind==0?(branch==1?1.5f:1.25f):branch==1?1.05f:branch==2?.95f:.8f;float x=(float)v.Cue.Position.X,y=(float)v.Cue.Position.Y;
   if(review){Texture2D[] bodies={warriorIdle,archerIdle,pawnLevel[0],frostIdle};var body=bodies[kind];if(body!=null){int bodyFrame=(int)(Time.unscaledTime/.16f)%Mathf.Max(1,body.width/192);TerrainView.Image(body,new Rect(x-72,y-100,144,144),bodyFrame*192,0,192,192);}}
   if(kind!=0){x+=v.Cue.Facing*20;y-=18;}else y+=8;
   Color before=GUI.color;if(kind==2)GUI.color=new Color(1,.7f,.3f,before.a);else if(kind==1&&branch==2)GUI.color=new Color(1,.6f,.45f,before.a);else if(kind==3&&branch==2)GUI.color=new Color(.8f,.6f,1,before.a);
   Matrix4x4 matrix=GUI.matrix;if(v.Cue.Facing<0)GUIUtility.ScaleAroundPivot(new Vector2(-1,1),new Vector2(x,y));
   TerrainView.Image(sheet.texture,new Rect(x-pivot[0]*k,y-pivot[1]*k,r.w*k,r.h*k),r.x,r.y,r.w,r.h);
   if(kind==1&&branch==1)TerrainView.Image(sheet.texture,new Rect(x-pivot[0]*k-8,y-pivot[1]*k-12,r.w*k,r.h*k),r.x,r.y,r.w,r.h);
   if(kind==3&&branch==1){Color ring=GUI.color;ring.a*=Mathf.Clamp01(1-age/duration);Ring(new Point(x,y),24+age*70,ring);}else if(kind==3&&branch==2){Box(new Rect(x-2,y-22,4,44),new Color(.8f,.65f,1,.8f));Box(new Rect(x-18,y-2,36,4),Color.white);}
   GUI.matrix=matrix;GUI.color=before;
   if(Debug.isDebugBuild&&Application.platform==RuntimePlatform.WebGLPlayer&&Application.absoluteURL.Contains("attack-fx-review=1")&&Event.current.type==EventType.Repaint&&debugRibbonLogged.Add("attack-fx|"+kind+"|"+branch+"|"+frame))Debug.Log("ATTACK_FX|"+kind+"|"+branch+"|"+name+"|"+frame);
  }
  void ReviewAttackEffects(){if(!Debug.isDebugBuild||Application.platform!=RuntimePlatform.WebGLPlayer||!Application.absoluteURL.Contains("fx-smoke=attack-review"))return;
   if(visuals.Exists(v=>v.Cue.Kind=="attack"))return;for(int kind=0;kind<4;kind++)for(int branch=0;branch<3;branch++)visuals.Add(new VisualCue{Cue=new Cue("attack",new Point(180+kind*280,220+branch*185)){Source=kind,Facing=kind%2==0?1:-1},Branch=branch});}
  void ProjectileTrail(Shot shot){if(battle.Enemies.Count>=70)return;float dx=(float)(shot.Destination.X-shot.Position.X),dy=(float)(shot.Destination.Y-shot.Position.Y),length=Mathf.Sqrt(dx*dx+dy*dy);if(length<1)return;dx/=length;dy/=length;Color c=shot.Kind==1?(shot.BossMult>1?new Color(1,.45f,.25f):new Color(1,.85f,.45f)):shot.Kind==3?(shot.Freezing?new Color(.8f,.6f,1):new Color(.45f,.85f,1)):new Color(1,.55f,.2f);for(int i=1;i<=4;i++){c.a=(5-i)*.14f;float size=shot.Kind==1?3:5;Box(new Rect((float)shot.Position.X-dx*i*5-size/2,(float)shot.Position.Y-dy*i*5-size/2,size,size),c);}}
 }
}
