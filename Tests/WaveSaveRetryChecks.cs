using System;
using EmberBastion;
class WaveSaveRetryChecks {
 static void Assert(bool condition,string text){if(!condition)throw new Exception(text);}
 static void Main(){var retry=new WaveSaveRetry();Assert(retry.Ready(10),"initial save");retry.Failed(10);Assert(!retry.Ready(14.99),"no early retry");Assert(retry.Ready(15),"retry after failure");retry.Failed(15);Assert(!retry.Ready(19.99)&&retry.Ready(20),"repeated failure");retry.Success();Assert(retry.Ready(0),"success resets");retry.Failed(30);retry.Reset();Assert(retry.Ready(0),"new battle resets");Console.WriteLine("PASS autosave retry timing and reset");}
}
