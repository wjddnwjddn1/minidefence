using System;
using EmberBastion;
class CombatProgressHintChecks {
 static void Assert(bool value,string text){if(!value)throw new Exception(text);}
 static void Main(){try{var h=new CombatProgressHint();Assert(!h.Observe(0,false,0),"inactive");Assert(!h.Observe(10,true,100),"start");Assert(!h.Observe(39.99,true,100),"before threshold");Assert(h.Observe(40,true,100),"threshold");Assert(h.Visible,"warning visible");Assert(!h.Observe(50,true,100),"notify once");Assert(!h.Observe(51,true,99),"damage rearms");Assert(!h.Visible,"damage clears warning");Assert(!h.Observe(80.9,true,99),"after damage before threshold");Assert(h.Observe(81,true,99),"notify after new stall");Assert(!h.Observe(82,false,0),"inactive resets");Assert(!h.Observe(100,true,100),"new final wave resets");Assert(!h.Observe(90,true,100),"rewind resets");Assert(!h.Observe(90,true,100),"pause time remains unchanged");Console.WriteLine("PASS final-wave progress hint threshold, damage, pause and reset");}catch(Exception e){Console.Error.WriteLine(e);Environment.Exit(1);}}
}
