using System;
namespace EmberBastion {
 // Uses unscaled time so retry timing does not depend on the selected game speed.
 public sealed class WaveSaveRetry {
  double retryAt;
  public bool Ready(double now){return now>=retryAt;}
  public bool TrySave(double now,Action save){if(!Ready(now))return false;try{save();Success();return true;}catch(Exception){Failed(now);return false;}}
  public void Failed(double now){retryAt=now+5;}
  public void Success(){retryAt=0;}
  public void Reset(){retryAt=0;}
 }
}
