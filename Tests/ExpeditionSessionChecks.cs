using System;
using EmberBastion;
class ExpeditionSessionChecks {
 static int checks;
 static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
 static void Reject(Action action,string why){checks++;try{action();}catch(Exception){return;}throw new Exception(why);}
 static ExpeditionSessionData Copy(ExpeditionSessionData d){return new ExpeditionSessionData{version=d.version,speed=d.speed,started=d.started,run=d.run,combat=d.combat,preparation=d.preparation};}
 static int Main(){try{
  for(uint seed=1;seed<=32;seed++){
   var run=Expedition.Start(seed);var b=run.CreateBattle();b.Place(0,0);b.Upgrade(0);
   var prep=b.SavePreparation();var p=ExpeditionSession.Capture(run,b,2,x=>"combat",x=>"prep");Check(!p.started&&p.combat==null&&p.preparation=="prep","preparation snapshot shape");
   var loaded=ExpeditionSession.Restore(p,x=>null,x=>Battle.RestorePreparation(prep));Check(loaded.Battle.Gold==b.Gold&&loaded.Battle.Units[0].Level==2&&loaded.Speed==2,"preparation retained");
   b.Start();b.Step(.25);b.HP=91;var combat=b.ExportCombat();var c=ExpeditionSession.Capture(run,b,3,x=>"combat",x=>"prep");Check(c.started&&c.combat=="combat"&&c.preparation==null,"start marks combat immediately");
   loaded=ExpeditionSession.Restore(c,x=>Battle.ImportCombat(combat),x=>null);Check(loaded.Battle.Phase==Phase.Fighting&&loaded.Battle.Wave==b.Wave&&loaded.Battle.Time==b.Time&&loaded.Battle.HP==91,"resume retains time, wave and warmth");
   var bad=Copy(c);bad.combat=null;Reject(()=>ExpeditionSession.Restore(bad,x=>b,x=>b),"missing combat must not become fresh preparation");
   bad=Copy(c);bad.preparation="prep";Reject(()=>ExpeditionSession.Restore(bad,x=>b,x=>b),"mixed payload rejected");
   bad=Copy(c);bad.started=false;Reject(()=>ExpeditionSession.Restore(bad,x=>b,x=>b),"started flag cannot reopen preparation with a combat payload");
   bad=Copy(c);bad.speed=0;Reject(()=>ExpeditionSession.Restore(bad,x=>b,x=>b),"speed boundary");
   Reject(()=>ExpeditionSession.Restore(c,x=>Battle.CreateForRules(1,1,4),x=>b),"foreign battle rejected");
   Reject(()=>ExpeditionSession.Restore(c,x=>run.CreateBattle(),x=>b),"started checkpoint cannot be preparation");
   Check(run.Abandon()&&run.State==Expedition.Status.Lost&&!run.Abandon(),"abandon records a single loss");
   var ended=ExpeditionSession.Capture(run,null,1,x=>null,x=>null);Check(ExpeditionSession.Restore(ended,x=>b,x=>b).Battle==null,"lost run has no replay battle");
   ended.started=true;ended.combat="combat";Reject(()=>ExpeditionSession.Restore(ended,x=>b,x=>b),"lost checkpoint cannot resume");
   run=Expedition.Start(seed);b=run.CreateBattle();b.Phase=Phase.Won;b.PeakAlive=45;b.HP=80;Check(run.Complete(b)&&run.ClearedStages==1,"camp cleared count");
   Check(run.Abandon()&&run.State==Expedition.Status.Lost&&run.ClearedStages==1,"abandoned camp keeps defended stage");
   Check(Expedition.Restore(run.Export()).ClearedStages==1,"abandoned camp record survives persistence");
  }
  Console.WriteLine("PASS "+checks+" expedition checkpoint, abandonment and resume boundary checks");return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
