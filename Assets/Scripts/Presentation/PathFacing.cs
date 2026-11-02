using System;
namespace EmberBastion {
 // Vertical segments retain the direction of the most recent horizontal segment.
 internal static class PathFacing {
  internal static bool Left(FieldDef field,Enemy enemy){
   double distance=((field.Gates[enemy.Gate]+enemy.Travel)%field.Perimeter+field.Perimeter)%field.Perimeter;
   int segment=0;
   for(;segment<field.Points.Length-1;segment++){
    double length=Point.Distance(field.Points[segment],field.Points[(segment+1)%field.Points.Length]);
    if(distance<length)break;
    distance-=length;
   }
   for(int n=0;n<field.Points.Length;n++){
    int i=(segment-n+field.Points.Length)%field.Points.Length;
    double dx=field.Points[(i+1)%field.Points.Length].X-field.Points[i].X;
    if(Math.Abs(dx)>.001)return dx<0;
   }
   return false;
  }
 }
}
