using System;
using System.Collections.Generic;
namespace EmberBastion {
 // Render-only launch anchors. No shot, damage, collision, RNG or save data is changed.
 public sealed class ArrowFlightView {
  sealed class Flight {public int Frame,Slot;public Point Origin,Offset,First;public double Distance;public bool Anchored;}
  readonly Dictionary<Shot,Flight> flights=new Dictionary<Shot,Flight>();
  readonly List<Shot> expired=new List<Shot>();readonly List<Cue> releases=new List<Cue>();readonly HashSet<int> represented=new HashSet<int>();
  int releaseFrame=-1;
  public void Clear(){flights.Clear();releases.Clear();represented.Clear();releaseFrame=-1;}
  public void Reset(Battle battle){Clear();foreach(var shot in battle.Shots)if(shot.Kind==1)flights.Add(shot,new Flight{Frame=-1,Slot=-1});}
  public static Point Muzzle(Cue cue){return new Point(cue.Position.X+cue.Facing*18,cue.Position.Y-20);}
  public static Point ReleaseCenter(Cue cue){Point muzzle=Muzzle(cue);double length=Point.Distance(muzzle,cue.Destination);return length<1?muzzle:new Point(muzzle.X+(cue.Destination.X-muzzle.X)*18/length,muzzle.Y+(cue.Destination.Y-muzzle.Y)*18/length);}
  public void Capture(Battle battle,IList<Cue> cues,int frame){
   expired.Clear();foreach(var pair in flights)if(!battle.Shots.Contains(pair.Key))expired.Add(pair.Key);foreach(var shot in expired)flights.Remove(shot);
   releases.Clear();represented.Clear();releaseFrame=frame;foreach(var cue in cues)if(cue.Kind=="attack"&&cue.Source==1)releases.Add(cue);
   foreach(var shot in battle.Shots){if(shot.Kind!=1||flights.ContainsKey(shot))continue;var flight=new Flight{Frame=frame,Slot=-1};double best=double.MaxValue;
    foreach(var cue in releases){Point origin=new Point(cue.Position.X,cue.Position.Y-12);double dx=shot.Destination.X-origin.X,dy=shot.Destination.Y-origin.Y,length=Math.Sqrt(dx*dx+dy*dy);if(length<1)continue;
     double along=((shot.Position.X-origin.X)*dx+(shot.Position.Y-origin.Y)*dy)/length,side=Math.Abs((shot.Position.X-origin.X)*dy-(shot.Position.Y-origin.Y)*dx)/length,error=side+Point.Distance(cue.Destination,shot.Destination);
     if(along<-.01||along>length+12||side>12||error>=best)continue;best=error;Point first=ReleaseCenter(cue);flight.Anchored=true;flight.Slot=cue.Slot;flight.Origin=origin;flight.First=first;flight.Offset=new Point(first.X-origin.X,first.Y-origin.Y);flight.Distance=length;
    }
    flights.Add(shot,flight);if(flight.Anchored)represented.Add(flight.Slot);
   }
  }
  public Point Position(Shot shot){Flight flight;if(!flights.TryGetValue(shot,out flight)||!flight.Anchored)return shot.Position;if(releaseFrame==flight.Frame)return flight.First;double amount=Math.Min(1,Point.Distance(shot.Position,shot.Destination)/flight.Distance);return new Point(shot.Position.X+flight.Offset.X*amount,shot.Position.Y+flight.Offset.Y*amount);}
  public IEnumerable<Cue> InstantReleases(){var seen=new HashSet<int>();foreach(var cue in releases)if(!represented.Contains(cue.Slot)&&seen.Add(cue.Slot))yield return cue;}
 }
}
