namespace EmberBastion {
 // Presentation-only diagnostic: pauses use game time, and progress re-arms the hint.
 public sealed class CombatProgressHint {
  const double Delay=30;
  double since,lastHealth;
  bool tracking,notified;
  public bool Visible {get{return tracking&&notified;}}
  public static string Message(bool hasAttackers){return hasAttackers?"피해 없음 · 사거리 확인":"피해 없음 · 공격 병력 필요";}
  public void Reset(){tracking=false;notified=false;since=0;lastHealth=0;}
  public bool Observe(double time,bool finalWaveActive,double remainingHealth){
   if(!finalWaveActive){Reset();return false;}
   if(!tracking||remainingHealth<lastHealth-0.000001||remainingHealth>lastHealth+0.000001||time<since){tracking=true;since=time;notified=false;}
   lastHealth=remainingHealth;
   if(!notified&&time-since>=Delay){notified=true;return true;}
   return false;
  }
 }
}
