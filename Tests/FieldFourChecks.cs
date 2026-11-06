using System;
using EmberBastion;
// r26 (Claude): the fourth field "습지 나루" is rules-2 only. Checks its data, legacy refusal, layout, placement hints,
// badge limits, a deterministic full playthrough and a combat save round trip in the middle of it.
class FieldFourChecks {
 static int checks;
 static void Check(bool ok,string what){checks++;if(!ok)throw new Exception(what);}
 static double SegDistance(Point p,Point a,Point b){double dx=b.X-a.X,dy=b.Y-a.Y,t=Math.Max(0,Math.Min(1,((p.X-a.X)*dx+(p.Y-a.Y)*dy)/(dx*dx+dy*dy)));return Point.Distance(p,new Point(a.X+t*dx,a.Y+t*dy));}
 static double RoadDistance(FieldDef f,Point p){double d=double.MaxValue;for(int i=0;i<f.Points.Length;i++)d=Math.Min(d,SegDistance(p,f.Points[i],f.Points[(i+1)%f.Points.Length]));return d;}
 static Battle Army(uint seed){var b=new Battle(seed,3);b.Gold=20000;for(int s=0;s<b.Field.Slots.Length;s++)Check(b.Place(s%3==2?3:s%2,s),"place "+s);foreach(var u in b.Units.ToArray())for(int k=0;k<2;k++)b.Upgrade(u.Slot);return b;}
 static string Run(Battle b,int saveAtWave,out string resumed){resumed=null;Check(b.Start(),"start");bool saved=false;while(b.Phase!=Phase.Won&&b.Phase!=Phase.Lost){b.Step(Balance.Tick);if(b.Phase==Phase.Preparation)Check(b.Start(),"next wave");if(!saved&&b.Wave==saveAtWave&&b.Phase==Phase.Fighting){saved=true;var copy=Battle.ImportCombat(b.ExportCombat());Check(copy.RulesVersion==2&&copy.FieldIndex==3&&copy.WaveCount==12,"combat save keeps rules 2 and 12 waves");string dummy;resumed=Finish(copy,out dummy);}}return b.Phase+" wave "+b.Wave+" peak "+b.PeakAlive+" kills "+b.Kills+" gold "+b.Gold;}
 static string Finish(Battle b,out string unused){unused=null;while(b.Phase!=Phase.Won&&b.Phase!=Phase.Lost){b.Step(Balance.Tick);if(b.Phase==Phase.Preparation)Check(b.Start(),"next wave after restore");}return b.Phase+" wave "+b.Wave+" peak "+b.PeakAlive+" kills "+b.Kills+" gold "+b.Gold;}
 static void Main(){
  Check(Balance.Fields.Length==4&&Balance.LegacyFieldCount==3,"four fields, three legacy");
  var f=Balance.Fields[3];
  Check(f.Name=="습지 나루"&&f.Waves.Length==12&&f.StartGold==360&&f.Slots.Length==12&&f.Gates.Length==2,"field data");
  Check(Math.Abs(f.Perimeter-2816)<1e-9&&Math.Abs(f.Gates[1]-1408)<1e-9,"perimeter and second gate");
  // rules: new battles are rules 2 with all twelve waves; the legacy rules cannot open this field
  var fresh=new Battle(1,3);Check(fresh.RulesVersion==2&&fresh.WaveCount==12,"new battle rules 2, 12 waves");
  Check(Battle.WaveCountFor(3,0)==0&&Battle.WaveCountFor(3,1)==0&&Battle.WaveCountFor(3,2)==12,"legacy rules see no waves here");
  for(int legacy=0;legacy<=1;legacy++){var prep=fresh.SavePreparation();prep.rulesVersion=legacy;bool refused=false;try{Battle.RestorePreparation(prep);}catch(ArgumentException){refused=true;}Check(refused,"legacy "+legacy+" preparation refused");}
  var ok2=Battle.RestorePreparation(fresh.SavePreparation());Check(ok2.RulesVersion==2&&ok2.FieldIndex==3,"rules 2 preparation restores");
  var started=Army(5);Check(started.Start(),"start for save");var combat=started.ExportCombat();combat.rulesVersion=1;bool refusedCombat=false;try{Battle.ImportCombat(combat);}catch(ArgumentException){refusedCombat=true;}Check(refusedCombat,"legacy combat save refused");
  // legacy fields keep their legacy lengths
  for(int i=0;i<Balance.LegacyFieldCount;i++)Check(Battle.WaveCountFor(i,1)==(i==0?10:8),"legacy field "+i+" length");
  // layout: slots clear of the road (64 px tiles centred on the path), of each other, of the castle and of the shore
  for(int i=0;i<f.Slots.Length;i++){
   var s=f.Slots[i];Check(RoadDistance(f,s)>=84,"slot "+i+" road distance "+RoadDistance(f,s));
   Check(s.X>=96&&s.X<=1184&&s.Y>=96&&s.Y<=672,"slot "+i+" on the ground");
   bool castle=s.X>f.Castle.X-40&&s.X<f.Castle.X+f.CastleWidth+40&&s.Y>f.Castle.Y-40&&s.Y<f.Castle.Y+f.CastleHeight+40;Check(!castle,"slot "+i+" clear of the castle");
   for(int j=i+1;j<f.Slots.Length;j++)Check(Point.Distance(s,f.Slots[j])>=96,"slots "+i+","+j+" apart");
  }
  foreach(var c in new[]{new Point(f.Castle.X,f.Castle.Y),new Point(f.Castle.X+f.CastleWidth,f.Castle.Y),new Point(f.Castle.X,f.Castle.Y+f.CastleHeight),new Point(f.Castle.X+f.CastleWidth,f.Castle.Y+f.CastleHeight)})Check(RoadDistance(f,c)>=32,"castle corner clear of the road");
  // every path point is within an archer's reach of some slot
  for(double d=0;d<f.Perimeter;d+=16){var p=f.Path(d);double best=double.MaxValue;foreach(var s in f.Slots)best=Math.Min(best,Point.Distance(p,s));Check(best<=Balance.Troops[1].Range,"path point "+d+" uncovered by archers");}
  // placement hints and badge limits exist for the new field
  for(int kind=0;kind<4;kind++){int recommended=0;for(int s=0;s<f.Slots.Length;s++){int g=PlacementCoverage.Grade(3,kind,s);Check(g>=0&&g<=2,"grade");if(PlacementCoverage.Recommended(3,kind,s))recommended++;}Check(recommended==3,"three recommended slots for kind "+kind);}
  Check(BadgeRules.Limit(3,false,false)>0&&BadgeRules.Limit(3,true,false)>0&&BadgeRules.Limit(3,true,true)>0,"badge limits");
  // supply stays within what saves accept even with four fields cleared
  Check(Balance.MaxSupplyFields*20==60,"supply cap 60 G");var supply=new Battle(2,3);Check(!supply.GrantSupply(80)&&supply.GrantSupply(60),"grant accepts 60, refuses 80");
  // a full game is deterministic, and a combat save taken in wave 6 finishes the same way
  string resumedA,resumedB;string a=Run(Army(11),6,out resumedA),b=Run(Army(11),6,out resumedB);
  Check(a==b,"deterministic playthrough: "+a+" | "+b);
  Check(resumedA==a,"combat save in wave 6 resumes to the same end: "+resumedA+" | "+a);
  Check(a.StartsWith("Won wave 12"),"strong army wins all twelve waves: "+a);
  Console.WriteLine("PASS "+checks+" fourth-field checks; strong army: "+a);
 }
}
