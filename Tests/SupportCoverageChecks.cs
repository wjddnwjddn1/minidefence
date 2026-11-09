using System;
using EmberBastion;
class SupportCoverageChecks {
 static int checks;
 static void Check(bool ok,string label){checks++;if(!ok)throw new Exception(label);}
 static void Main(){
  for(int f=0;f<Balance.Fields.Length;f++)for(int branch=1;branch<=2;branch++)for(int stun=0;stun<=1;stun++){
   var b=new Battle(29,f);b.Gold=10000;for(int i=0;i<b.Field.Slots.Length;i++)b.Place(i==0||i==2?4:i%4,i);
   b.Upgrade(0);b.UpgradeBranch(0,branch);b.Upgrade(2);b.UpgradeBranch(2,2);var smith=b.At(0);smith.Stun=stun;
   int covered,additional;b.SupportCoverage(smith,out covered,out additional);int actual=0,expectedCovered=0;
   foreach(var ally in b.Units.ToArray())if(ally.Kind!=4){if(Point.Distance(b.Field.Slots[smith.Slot],b.Field.Slots[ally.Slot])<=Battle.UnitRange(smith))expectedCovered++;
    double beforeH,beforeD,afterH,afterD;b.SupportBonuses(ally,out beforeH,out beforeD);
    int index=b.Units.IndexOf(smith);b.Units.RemoveAt(index);b.SupportBonuses(ally,out afterH,out afterD);b.Units.Insert(index,smith);
    if(beforeH>afterH||beforeD>afterD)actual++;
   }
   Check(covered==expectedCovered,"geometric coverage");Check(additional==actual,"additional count agrees with actual removal");
   if(stun>0)Check(additional==0,"stunned smith adds no effect");
   int c,n;b.SupportCoverage(new Unit{Kind=4,Slot=0},out c,out n);Check(c==0&&n==0,"foreign unit is not an active supporter");
  }
  Console.WriteLine("PASS "+checks+" support coverage, overlap, branch and stun checks");
 }
}
