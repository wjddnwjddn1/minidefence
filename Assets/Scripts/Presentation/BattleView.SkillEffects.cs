using UnityEngine;

namespace EmberBastion {
 public sealed partial class BattleView {
  const float SkillEffectSeconds=.45f;
  static readonly string[] SkillSheets={"SkillSword","SkillArrow","SkillPowder","SkillFrost","SkillForge"};

  void DrawSkillEffect(VisualCue visual,bool preview=false){
   if(!ExpeditionBattle&&!preview)return;
   int kind=visual.Cue.Source;
   if(kind<0||kind>=SkillSheets.Length||(kind<=1&&visual.Age>=SkillEffectSeconds))return;
   // the sword skill is a big blade of air running the whole rift, with two smaller ones behind it (r45)
   if(kind==0){var caster=visual.Cue.Slot>=0?battle.At(visual.Cue.Slot):null;int branch=caster==null?0:caster.Branch;Color tint=branch==1?new Color(.86f,.7f,1):branch==2?new Color(1,.78f,.45f):new Color(.75f,.9f,1);
    Point a=visual.Cue.Position,b=visual.Cue.Destination;float deg=Mathf.Atan2((float)(b.Y-a.Y),(float)(b.X-a.X))*Mathf.Rad2Deg,run=visual.Age/SkillEffectSeconds;var from=new Vector2((float)a.X,(float)a.Y+10);
    DrawSwordWind(from,deg,run-.24f,20,(float)Boon.RiftLength-30,1.1f,tint);DrawSwordWind(from,deg,run-.12f,20,(float)Boon.RiftLength-12,1.4f,tint);DrawSwordWind(from,deg,run,20,(float)Boon.RiftLength,1.9f,tint);
    if(preview)ProjectileReview(SkillSheets[kind]);return;}
   // the piercing arrow is a white-hot streak down the whole line it goes through (r45)
   if(kind==1){Point a=visual.Cue.Position,b=visual.Cue.Destination;float dx=(float)(b.X-a.X),dy=(float)(b.Y-a.Y),far=Mathf.Sqrt(dx*dx+dy*dy);
    DrawSwordWind(new Vector2((float)a.X,(float)a.Y-18),Mathf.Atan2(dy,dx)*Mathf.Rad2Deg,visual.Age/SkillEffectSeconds,40,Mathf.Max(120,far),1.9f,new Color(1f,.92f,.7f),"PierceShot");
    if(preview)ProjectileReview(SkillSheets[kind]);return;}
   var sheet=AttackFx(SkillSheets[kind]);
   if(sheet==null)return;
   // big blast, frost nova and hammer signal are the library's explosion, nova and hammer smash, each played whole (r45)
   float seconds=sheet.data.frames.Length/Mathf.Max(1f,sheet.data.meta.pixelfx.fps);if(visual.Age>=seconds)return;
   int index=Mathf.Min(sheet.data.frames.Length-1,(int)(visual.Age/seconds*sheet.data.frames.Length));
   var frame=sheet.data.frames[index].frame;
   var origin=sheet.data.meta.pixelfx.pivot;
   Point at=visual.Cue.Position,target=visual.Cue.Destination;
   var pivot=new Vector3((float)at.X,(float)at.Y+(kind==4?30:kind==3?14:10),0);
   Matrix4x4 previous=GUI.matrix;
   if(kind<=1){
    float angle=Mathf.Atan2((float)(target.Y-at.Y),(float)(target.X-at.X))*Mathf.Rad2Deg;
    GUI.matrix=previous*Matrix4x4.TRS(pivot,Quaternion.Euler(0,0,angle),Vector3.one)*Matrix4x4.Translate(-pivot);
   }
   float scale=kind==2?1.1f:kind==3?.9f:1.4f;if(preview&&FixtureInt("fx-k",0)>0)scale*=FixtureInt("fx-k",100)/100f;
   // the sword rift takes its branch colour, like the basic rift (blue, violet for the sweep branch, orange for the breaker)
   Color before=GUI.color;if(kind==0&&visual.Cue.Slot>=0){var caster=battle.At(visual.Cue.Slot);int branch=caster==null?0:caster.Branch;GUI.color=branch==1?new Color(.86f,.7f,1,before.a):branch==2?new Color(1,.78f,.45f,before.a):new Color(.75f,.9f,1,before.a);}
   TerrainView.Image(sheet.texture,new Rect(pivot.x-origin[0]*scale,pivot.y-origin[1]*scale,frame.w*scale,frame.h*scale),frame.x,frame.y,frame.w,frame.h);
   GUI.color=before;
   GUI.matrix=previous;
   if(preview)ProjectileReview(SkillSheets[kind]);
  }

  // Charge comes from Battle.SkillCharge and Boon.SkillEvery; the last pip lights as the skill fires, so a full row is never shown.
  void DrawSkillPips(Point unit,int charged,int required){
   if(required<2||required>5)return;
   float x=(float)unit.X-(required*10-3)/2f,y=(float)unit.Y+22;
   for(int i=0;i<required;i++){
    Box(new Rect(x+i*10,y,7,7),new Color(.09f,.08f,.09f,.94f));
    if(i<charged)Box(new Rect(x+i*10+1,y+1,5,5),charged>=required?uiGold:new Color(.6f,.86f,.78f));
   }
  }

  void DrawSkillReview(){
   if(!Debug.isDebugBuild||Application.platform!=RuntimePlatform.WebGLPlayer||!Application.absoluteURL.Contains("skill-preview=1"))return;
   for(int kind=0;kind<SkillSheets.Length;kind++){
    Point at=new Point(240+kind%3*390,250+kind/3*290);
    var cue=new Cue("skill",at){Source=kind,Destination=new Point(at.X+130,at.Y+15)};
    DrawSkillEffect(new VisualCue{Cue=cue,Age=FixtureInt("fx-age",150)/1000f},true);
    DrawSkillPips(new Point(at.X,at.Y+44),kind==4?3:kind+1,kind==4?3:5);
   }
  }
 }
}
