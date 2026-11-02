using System;
using EmberBastion;
class WaveSaveRetryChecks {
 static void Assert(bool condition,string text){if(!condition)throw new Exception(text);}
 static void Main(){var retry=new WaveSaveRetry();Assert(retry.Ready(10),"initial save");retry.Failed(10);Assert(!retry.Ready(14.99),"no early retry");Assert(retry.Ready(15),"retry after failure");retry.Failed(15);Assert(!retry.Ready(19.99)&&retry.Ready(20),"repeated failure");retry.Success();Assert(retry.Ready(0),"success resets");retry.Failed(30);retry.Reset();Assert(retry.Ready(0),"new battle resets");int attempts=0;bool saved=false;Action save=()=>{attempts++;if(attempts==1)throw new InvalidOperationException("storage unavailable");saved=true;};Assert(!retry.TrySave(40,save)&&attempts==1&&!saved,"failure preserves unsaved state");Assert(!retry.TrySave(44,save)&&attempts==1,"no early attempt");Assert(retry.TrySave(45,save)&&attempts==2&&saved,"storage recovery succeeds");Console.WriteLine("PASS autosave failure, retry, recovery and reset");}
}
