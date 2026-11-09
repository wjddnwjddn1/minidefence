using System;
using EmberBastion;
class QuickClearChecks {
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 static void Main(){int cases=0;for(int field=0;field<Balance.Fields.Length;field++)for(int mode=0;mode<3;mode++)for(uint seed=1;seed<=4;seed++){
  var b=new Battle(seed,field);b.SetDifficulty(mode);b.Gold=5000;for(int i=0;i<b.Field.Slots.Length;i++){Check(b.Place(i%3,i),"place");Check(b.Upgrade(i),"upgrade");Check(b.UpgradeBranch(i,1),"branch");}Check(b.Start(),"start");Check(!b.Decided,"early decision");
  for(int i=0;i<120000&&b.Phase==Phase.Fighting&&!b.Decided;i++)b.Step(1.0/60);
  if(!b.Decided)continue;Check(b.Pending==0&&b.Wave==b.Field.Waves.Length,"last queue");var lowFrame=b.CopyCombat();for(int frame=0;frame<20&&lowFrame.Phase==Phase.Fighting;frame++)for(int part=0;part<2&&lowFrame.Phase==Phase.Fighting;part++)lowFrame.Step(.15);var reference=b.CopyCombat();for(int tick=0;tick<360&&reference.Phase==Phase.Fighting;tick++)reference.Step(1.0/60);Check(Math.Abs(lowFrame.Time-reference.Time)<1e-7&&lowFrame.Kills==reference.Kills&&lowFrame.Gold==reference.Gold,"20 FPS 6x timing mismatch");var a=b.CopyCombat();var fast=Battle.ImportCombat(b.ExportCombat());Check(fast.Decided,"saved decision");
  for(int i=0;i<120000&&a.Phase==Phase.Fighting;i++)a.Step(1.0/60);
  for(int i=0;i<20000&&fast.Phase==Phase.Fighting;i++)fast.Step(6.0/60);
  Check(a.Phase==Phase.Won&&fast.Phase==a.Phase,"outcome");Check(a.PeakAlive==fast.PeakAlive&&a.Kills==fast.Kills&&a.Gold==fast.Gold&&a.KillKinds==fast.KillKinds,"result mismatch");cases++;
 }Check(cases==Balance.Fields.Length*3*4,"too few decided fixtures");Console.WriteLine("PASS "+cases+" saved and resumed 1x/6x final-wave outcomes");}
}
