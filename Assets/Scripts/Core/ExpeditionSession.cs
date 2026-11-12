using System;
namespace EmberBastion {
 [Serializable] public sealed class ExpeditionSessionData {
  public int version=1,speed=1;public ExpeditionData run;public bool started;public string combat,preparation;
 }
 public sealed class ExpeditionSession {
  public Expedition Run;public Battle Battle;public int Speed;
  public static ExpeditionSessionData Capture(Expedition run,Battle battle,int speed,Func<Battle,string> combat,Func<Battle,string> prep){
   if(run==null)throw new ArgumentException("Missing expedition");if(speed<1||speed>3)throw new ArgumentException("Invalid speed");
   var d=new ExpeditionSessionData{run=run.Export(),speed=speed};
   if(run.State==Expedition.Status.Battle){if(!run.Matches(battle))throw new ArgumentException("Foreign expedition battle");if(battle.Phase==Phase.Fighting){d.started=true;d.combat=combat(battle);}else if(battle.Phase==Phase.Preparation)d.preparation=prep(battle);else throw new ArgumentException("Unreported expedition result");}return d;
  }
  public static ExpeditionSession Restore(ExpeditionSessionData d,Func<string,Battle> combat,Func<string,Battle> prep){
   if(d==null||d.version!=1||d.speed<1||d.speed>3)throw new ArgumentException("Invalid expedition session");var run=Expedition.Restore(d.run);Battle b=null;
   bool c=!string.IsNullOrEmpty(d.combat),p=!string.IsNullOrEmpty(d.preparation);
   if(run.State==Expedition.Status.Battle){
    if(d.started){if(!c||p)throw new ArgumentException("Started expedition requires combat checkpoint");b=combat(d.combat);if(b.Phase!=Phase.Fighting)throw new ArgumentException("Invalid resumed combat");}
    else{if(c||!p)throw new ArgumentException("Preparation checkpoint missing");b=prep(d.preparation);if(b.Phase!=Phase.Preparation)throw new ArgumentException("Invalid resumed preparation");}
    if(!run.Matches(b)||b.HP<1||b.HP>100)throw new ArgumentException("Mismatched expedition checkpoint");
   }else if(c||p||d.started)throw new ArgumentException("Ended expedition has checkpoint");
   return new ExpeditionSession{Run=run,Battle=b,Speed=d.speed};
  }
 }
}
