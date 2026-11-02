namespace EmberBastion {
 // Uses unscaled time so retry timing does not depend on the selected game speed.
 public sealed class WaveSaveRetry {
  double retryAt;
  public bool Ready(double now){return now>=retryAt;}
  public void Failed(double now){retryAt=now+5;}
  public void Success(){retryAt=0;}
  public void Reset(){retryAt=0;}
 }
}
