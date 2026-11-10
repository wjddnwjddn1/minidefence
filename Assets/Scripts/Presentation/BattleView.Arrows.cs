using System;
using UnityEngine;
namespace EmberBastion {
 public sealed partial class BattleView {
  readonly ArrowFlightView arrowFlights=new ArrowFlightView();
  void DrawArrow(Shot shot){var enemy=battle.Enemies.Find(e=>e.Id==shot.Target);DrawArrowTexture(arrowFlights.Position(shot),enemy==null?shot.Destination:enemy.Position);}
  bool ArrowFixture {get{return Debug.isDebugBuild&&Application.platform==RuntimePlatform.WebGLPlayer&&Application.absoluteURL.Contains("fx-smoke=arrowlaunch");}}
  bool arrowFixtureFrozen,arrowFixtureDrawn;
  void InitArrowFixture(){if(!ArrowFixture)return;
   int side=FixtureInt("side",1),level=FixtureInt("level",1),branch=FixtureInt("branch",1);battle=Battle.CreateForRules(29,2,4);battle.Gold=1000;int slot=-1;double travel=0,best=double.MaxValue;
   for(int i=0;i<battle.Field.Slots.Length;i++)for(double t=0;t<battle.Field.Perimeter;t+=8){Point p=battle.Field.Path(t),origin=battle.Field.Slots[i];double distance=Point.Distance(p,origin);if((p.X-origin.X)*side<20||distance<120||distance>190)continue;double error=Math.Abs(distance-150);if(error<best){best=error;slot=i;travel=t;}}
   if(slot<0){Debug.LogError("ARROW_FIXTURE_NO_TARGET");return;}battle.Place(1,slot);if(level>=2)battle.Upgrade(slot);if(level>=3)battle.UpgradeBranch(slot,branch);battle.At(slot).Priority=1;battle.Start();battle.Enemies.Add(new Enemy{Id=999,Kind=0,Gate=0,HP=100000,MaxHP=100000,Speed=0,Travel=travel,Position=battle.Field.Path(travel)});BindBattle();tutorialStage=0;titleMenu=false;SimulationSpeed=FixtureInt("speed",1);
  }
  void ObserveArrowFixture(){if(!ArrowFixture||arrowFixtureFrozen)return;var cue=frameCues.Find(c=>c.Kind=="attack"&&c.Source==1);if(cue==null)return;arrowFixtureFrozen=true;paused=true;Point muzzle=ArrowFlightView.Muzzle(cue);Debug.Log("ARROW_MUZZLE|"+muzzle.X+"|"+muzzle.Y+"|"+cue.Destination.X+"|"+cue.Destination.Y);}
  void DrawArrowTexture(Point position,Point destination){if(ArrowFixture&&!arrowFixtureDrawn){arrowFixtureDrawn=true;Debug.Log("ARROW_DRAW|"+position.X+"|"+position.Y);}
Vector2 center=new Vector2((float)position.X,(float)position.Y);Matrix4x4 matrix=GUI.matrix;float angle=Mathf.Atan2((float)(destination.Y-position.Y),(float)(destination.X-position.X))*Mathf.Rad2Deg;Vector3 pivot=new Vector3(center.x,center.y,0);GUI.matrix=matrix*Matrix4x4.TRS(pivot,Quaternion.Euler(0,0,angle),Vector3.one)*Matrix4x4.TRS(-pivot,Quaternion.identity,Vector3.one);
   if(ArrowFixture&&Event.current.type==EventType.Repaint&&debugRibbonLogged.Add("arrow-pivot|"+Screen.width+"x"+Screen.height)){Vector3 expected=matrix.MultiplyPoint3x4(pivot),actual=GUI.matrix.MultiplyPoint3x4(pivot);if(Vector3.Distance(expected,actual)>.01f)Debug.LogError("ARROW_PIVOT_DRIFT");Debug.Log("ARROW_PIVOT|"+expected.x+"|"+expected.y+"|"+actual.x+"|"+actual.y);}
   TerrainView.Image(arrow,new Rect(center.x-24,center.y-24,48,48),0,0,64,64);GUI.matrix=matrix;}
  void DrawInstantArrows(){foreach(var cue in arrowFlights.InstantReleases())DrawArrowTexture(ArrowFlightView.ReleaseCenter(cue),cue.Destination);}
 }
}
