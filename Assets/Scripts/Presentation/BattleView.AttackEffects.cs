using System.Collections.Generic;
using UnityEngine;
using EmberBastion.PixelFX;
namespace EmberBastion {
 public sealed partial class BattleView {
  sealed class AttackSheet {public Texture2D texture;public Sheet data;}
  readonly Dictionary<string,AttackSheet> attackSheets=new Dictionary<string,AttackSheet>();
  AttackSheet AttackFx(string name){AttackSheet sheet;if(attackSheets.TryGetValue(name,out sheet))return sheet;var tex=Resources.Load<Texture2D>("PixelFX/"+name);var json=Resources.Load<TextAsset>("PixelFX/"+name);sheet=tex==null||json==null?null:new AttackSheet{texture=tex,data=SheetReader.Read(json,tex)};attackSheets[name]=sheet;return sheet;}
  // The sword wind sheet faces right with its pivot at the blade; it flies `from+start` to `from+reach` along `angle` while `travel` runs 0..1 and fades at the end.
  void DrawSwordWind(Vector2 from,float angle,float travel,float start,float reach,float k,Color tint,string sheet="SwordWind"){
   if(travel<0||travel>=1)return;var wind=AttackFx(sheet);if(wind==null)return;
   float eased=1-(1-travel)*(1-travel),along=start+(reach-start)*eased,rad=angle*Mathf.Deg2Rad,x=from.x+Mathf.Cos(rad)*along,y=from.y+Mathf.Sin(rad)*along;
   var f=wind.data.frames[(int)(travel*.3f*wind.data.meta.pixelfx.fps*2)%wind.data.frames.Length].frame;var pivot=wind.data.meta.pixelfx.pivot;
   Matrix4x4 m=GUI.matrix;Color c=GUI.color;var at=new Vector3(x,y,0);GUI.matrix=m*Matrix4x4.TRS(at,Quaternion.Euler(0,0,angle),Vector3.one)*Matrix4x4.Translate(-at);
   GUI.color=new Color(tint.r,tint.g,tint.b,c.a*Mathf.Clamp01((1-travel)/.25f));
   TerrainView.Image(wind.texture,new Rect(x-pivot[0]*k,y-pivot[1]*k,f.w*k,f.h*k),f.x,f.y,f.w,f.h);GUI.color=c;GUI.matrix=m;
  }
  void AttackEffect(VisualCue v){
   bool review=Debug.isDebugBuild&&Application.platform==RuntimePlatform.WebGLPlayer&&Application.absoluteURL.Contains("fx-smoke=attack-review");float age=review?FixtureInt("fx-age",200)/1000f:v.Age;
   if(age>.55f||v.Cue.Source<0||v.Cue.Source>3)return;
   // Dense battles keep short release flashes but skip late smoke/trails. No simulation or damage changes.
   if(battle.Enemies.Count>=70&&age>.18f)return;
   int kind=v.Cue.Source,branch=v.Branch;string name=kind==0?(branch==1?"SwordSweep":branch==2?"SwordBreaker":"SwordSlash"):kind==1?"LaunchArrow":kind==3?"LaunchFrost":"LaunchPowder";
   // r45 (user: the attack tells looked cheap): the sheets are the library's hand-made cut, whirl, muzzle flash and glint, each played whole.
   var sheet=AttackFx(name);if(sheet==null)return;float duration=sheet.data.frames.Length/Mathf.Max(1f,sheet.data.meta.pixelfx.fps);if(age>=duration)return;
   int frame=Mathf.Min(sheet.data.frames.Length-1,(int)(age/duration*sheet.data.frames.Length));var r=sheet.data.frames[frame].frame;var pivot=sheet.data.meta.pixelfx.pivot;
   float k=kind==0?(branch==1?1.3f:branch==2?1.7f:1.5f):kind==1?1.05f:kind==3?(branch==0?1.2f:1.35f):2.2f;float x=(float)v.Cue.Position.X,y=(float)v.Cue.Position.Y;
   if(review){Texture2D[] bodies={warriorIdle,archerIdle,pawnLevel[0],frostIdle};var body=bodies[kind];if(body!=null){int bodyFrame=(int)(Time.unscaledTime/.16f)%Mathf.Max(1,body.width/192);TerrainView.Image(body,new Rect(x-72,y-100,144,144),bodyFrame*192,0,192,192);}}
   if(review&&FixtureInt("fx-k",0)>0)k*=FixtureInt("fx-k",100)/100f;
   if(kind!=0){x+=v.Cue.Facing*20;y-=18;}else y+=review?FixtureInt("fx-sy",34):34;
   // Sword cuts also travel (r42, user request): a blade of air flies from the swordsman towards the target (r45: the flat streaks looked cheap). Display only.
   if(kind==0&&age>=.08f){float px=(float)v.Cue.Position.X,py=(float)v.Cue.Position.Y+10,tx=(float)v.Cue.Destination.X-px,ty=(float)v.Cue.Destination.Y-py,far=Mathf.Sqrt(tx*tx+ty*ty);float angle=far<1?(v.Cue.Facing<0?180:0):Mathf.Atan2(ty,tx)*Mathf.Rad2Deg;
    DrawSwordWind(new Vector2(px,py),angle,(age-.08f)/.3f,30,Mathf.Clamp(far,60,150),branch==1?1.35f:1.15f,branch==1?new Color(.86f,.7f,1):branch==2?new Color(1,.78f,.45f):new Color(.75f,.9f,1));}
   Color before=GUI.color;if(kind==1&&branch==2)GUI.color=new Color(1,.6f,.45f,before.a);else if(kind==3&&branch==2)GUI.color=new Color(.8f,.6f,1,before.a);
   Matrix4x4 matrix=GUI.matrix;
   var release=new Vector3(x,y,0);
   if(kind==0&&v.Cue.Facing<0){
    GUI.matrix=matrix*Matrix4x4.TRS(release,Quaternion.identity,new Vector3(-1,1,1))*Matrix4x4.Translate(-release);
   }else if(kind!=0){
    float dx=(float)v.Cue.Destination.X-x,dy=(float)v.Cue.Destination.Y-y;
    float angle=dx*dx+dy*dy<1?(v.Cue.Facing<0?180:0):Mathf.Atan2(dy,dx)*Mathf.Rad2Deg;
    GUI.matrix=matrix*Matrix4x4.TRS(release,Quaternion.Euler(0,0,angle),Vector3.one)*Matrix4x4.Translate(-release);
    if(review&&Event.current.type==EventType.Repaint){
     if(Vector3.Distance(matrix.MultiplyPoint3x4(release),GUI.matrix.MultiplyPoint3x4(release))>.01f)Debug.LogError("LAUNCH_PIVOT_DRIFT");
     if(debugRibbonLogged.Add("launch-angle|"+kind))Debug.Log("LAUNCH_ANGLE|"+kind+"|"+Mathf.RoundToInt(angle));
    }
   }
   TerrainView.Image(sheet.texture,new Rect(x-pivot[0]*k,y-pivot[1]*k,r.w*k,r.h*k),r.x,r.y,r.w,r.h);
   if(kind==1&&branch==1)TerrainView.Image(sheet.texture,new Rect(x-pivot[0]*k-8,y-pivot[1]*k-12,r.w*k,r.h*k),r.x,r.y,r.w,r.h);
   if(kind==3&&branch==1){Color ring=GUI.color;ring.a*=Mathf.Clamp01(1-age/duration);Ring(new Point(x,y),24+age*70,ring);}else if(kind==3&&branch==2){Box(new Rect(x-2,y-22,4,44),new Color(.8f,.65f,1,.8f));Box(new Rect(x-18,y-2,36,4),Color.white);}
   GUI.matrix=matrix;GUI.color=before;
   if(Debug.isDebugBuild&&Application.platform==RuntimePlatform.WebGLPlayer&&Application.absoluteURL.Contains("attack-fx-review=1")&&Event.current.type==EventType.Repaint&&debugRibbonLogged.Add("attack-fx|"+kind+"|"+branch+"|"+frame))Debug.Log("ATTACK_FX|"+kind+"|"+branch+"|"+name+"|"+frame);
  }
  void ReviewAttackEffects(){if(!Debug.isDebugBuild||Application.platform!=RuntimePlatform.WebGLPlayer||!Application.absoluteURL.Contains("fx-smoke=attack-review"))return;
   if(visuals.Exists(v=>v.Cue.Kind=="attack"))return;for(int kind=0;kind<4;kind++)for(int branch=0;branch<3;branch++)visuals.Add(new VisualCue{Cue=new Cue("attack",new Point(180+kind*280,220+branch*185)){Source=kind,Facing=kind%2==0?1:-1,Destination=new Point(180+kind*280+(kind%2==0?110:-110),220+branch*185+40)},Branch=branch});}
 }
}
