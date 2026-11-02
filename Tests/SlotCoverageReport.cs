using System;
using EmberBastion;
// Claude experiment: share of the enemy route inside each slot's range (16-unit samples), for the placement hint design.
class SlotCoverageReport {
 static Point P(int f,double d){return f==0?Balance.Path(d):Balance.Fields[f].Path(d);}
 static double Cover(int f,Point s,double range,double min){double per=f==0?Balance.Perimeter:Balance.Fields[f].Perimeter;int n=0,inside=0;for(double d=0;d<per;d+=16){n++;double dist=Point.Distance(P(f,d),s);if(dist<=range&&dist>=min)inside++;}return 100.0*inside/n;}
 static void Main(){Console.WriteLine("field,slot,sword120,archer210,bomb210min60,archerSniper320");for(int f=0;f<Balance.Fields.Length;f++){var S=Balance.Fields[f].Slots;for(int i=0;i<S.Length;i++)Console.WriteLine(f+","+(i+1)+","+Cover(f,S[i],120,0).ToString("0")+","+Cover(f,S[i],210,0).ToString("0")+","+Cover(f,S[i],210,60).ToString("0")+","+Cover(f,S[i],320,0).ToString("0"));}}
}
