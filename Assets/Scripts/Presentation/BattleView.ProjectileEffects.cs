using UnityEngine;
namespace EmberBastion {
 public sealed partial class BattleView {
  void ProjectileTrail(Shot shot, bool preview=false){
   if(battle.Enemies.Count>=70)return;
   Point position=shot.Kind==1?arrowFlights.Position(shot):shot.Position;
   Point target=shot.Destination;
   if(shot.Kind!=2){var enemy=battle.Enemies.Find(e=>e.Id==shot.Target);if(enemy!=null)target=enemy.Position;}
   float x=(float)position.X,y=(float)position.Y;
   if(shot.Kind==2){
    Color previous=GUI.color;GUI.color=new Color(.12f,.08f,.04f,.43f);
    GUI.DrawTexture(new Rect(x-15,y+17,30,11),projectileShadow??(projectileShadow=MakeProjectileShadow()));
    GUI.color=previous;
   }
   string name=shot.Kind==1?"TrailArrow":shot.Kind==2?"TrailPowder":"TrailFrost";
   var effect=AttackFx(name);
   if(effect==null)return;
   float distance=(float)Point.Distance(position,target);
   // Shorten the wake during the last part of its flight.
   float scale=Mathf.Clamp(distance/40,.25f,1);
   int branch=shot.Kind==1?arrowFlights.Branch(shot):0;
   Color tint=shot.Kind==1&&shot.BossMult>1?new Color(1,.48f,.4f):branch==1?new Color(1,1,.56f):Color.white;
   DrawProjectileSheet(effect,position,target,scale,tint);
   if(preview)ProjectileReview(name);
  }
  Texture2D projectileShadow;
  static Texture2D MakeProjectileShadow(){
   var tex=new Texture2D(16,16,TextureFormat.RGBA32,false){filterMode=FilterMode.Point};
   for(int y=0;y<16;y++)for(int x=0;x<16;x++){
    float dx=(x-7.5f)/8,dy=(y-7.5f)/8;
    tex.SetPixel(x,y,dx*dx+dy*dy<=1?Color.white:Color.clear);
   }
   tex.Apply();return tex;
  }
  void DrawProjectileSheet(AttackSheet effect,Point position,Point target,float widthScale,Color tint){
   var frame=effect.data.frames[1].frame;
   var pivot=effect.data.meta.pixelfx.pivot;
   var center=new Vector3((float)position.X,(float)position.Y,0);
   float angle=Mathf.Atan2((float)(target.Y-position.Y),(float)(target.X-position.X))*Mathf.Rad2Deg;
   Matrix4x4 previousMatrix=GUI.matrix;Color previousColor=GUI.color;
   GUI.matrix=previousMatrix*Matrix4x4.TRS(center,Quaternion.Euler(0,0,angle),Vector3.one)*Matrix4x4.Translate(-center);
   GUI.color=previousColor*tint;
   TerrainView.Image(effect.texture,new Rect(center.x-pivot[0]*widthScale,center.y-pivot[1],frame.w*widthScale,frame.h),frame.x,frame.y,frame.w,frame.h);
   GUI.color=previousColor;GUI.matrix=previousMatrix;
  }
  void DrawLandedArrow(VisualCue visual){
   if(battle.Enemies.Count>=30||visual.Age>=.6f||arrow==null)return;
   Point hit=visual.Cue.Position;
   float angle=Mathf.Atan2((float)(visual.Cue.Destination.Y-hit.Y),(float)(visual.Cue.Destination.X-hit.X))*Mathf.Rad2Deg;
   var pivot=new Vector3((float)hit.X,(float)hit.Y+9,0);
   Matrix4x4 previousMatrix=GUI.matrix;Color previousColor=GUI.color;
   GUI.matrix=previousMatrix*Matrix4x4.TRS(pivot,Quaternion.Euler(0,0,angle),Vector3.one)*Matrix4x4.Translate(-pivot);
   GUI.color=previousColor*new Color(1,1,1,Mathf.Clamp01((.6f-visual.Age)/.18f));
   TerrainView.Image(arrow,new Rect(pivot.x-24,pivot.y-14,31,31),0,0,64,64);
   GUI.matrix=previousMatrix;GUI.color=previousColor;
  }
  void ProjectileReview(string label){
   if(Event.current.type==EventType.Repaint&&debugRibbonLogged.Add("projectile-review|"+label))Debug.Log("PROJECTILE_FX|"+label);
  }
  void DrawProjectileReview(){
   if(!Debug.isDebugBuild||Application.platform!=RuntimePlatform.WebGLPlayer||!Application.absoluteURL.Contains("fx-smoke=attack-review"))return;
   bool fighting=battle.Phase==Phase.Fighting;
   string before=fighting?JsonUtility.ToJson(battle.ExportCombat()):JsonUtility.ToJson(battle.SavePreparation());
   for(int kind=1;kind<=3;kind++){
    Point at=new Point(400+kind*175,340),target=new Point(at.X+100,at.Y-30);
    var sample=new Shot{Kind=kind,Target=-1,Position=at,Destination=target};
    ProjectileTrail(sample,true);
    if(kind==1)DrawArrowTexture(at,target);
    else if(kind==2)TerrainView.Image(dynamite,new Rect((float)at.X-48,(float)at.Y-48,96,96),0,0,64,64);
    else Box(new Rect((float)at.X-5,(float)at.Y-5,10,10),new Color(.66f,.91f,1));
   }
   DrawLandedArrow(new VisualCue{Cue=new Cue("arrow-land",new Point(1070,560)){Destination=new Point(1120,590)},Age=.2f});
   ProjectileReview("arrow-land");
   string after=fighting?JsonUtility.ToJson(battle.ExportCombat()):JsonUtility.ToJson(battle.SavePreparation());
   if(before==after)ProjectileReview(fighting?"combat-state-unchanged":"state-unchanged");
   else Debug.LogError("PROJECTILE_DRAW_MUTATED_BATTLE");
  }
 }
}
