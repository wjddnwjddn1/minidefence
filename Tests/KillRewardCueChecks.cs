using System;
using EmberBastion;
class KillRewardCueChecks {
 static void Check(int kind,int reward){var b=new Battle();if(!b.Place(0,0)||!b.Start())throw new Exception("setup");int before=b.Gold;b.Enemies.Add(new Enemy{Id=900,Kind=kind,HP=1,MaxHP=1,Speed=0,Travel=128,Position=Balance.Path(128)});b.Step(1.0/60);if(b.Gold!=before+reward)throw new Exception("incorrect reward");int count=0;foreach(var cue in b.Cues)if(cue.Kind=="death"&&cue.Text=="+"+reward+"G")count++;if(count!=1)throw new Exception("reward cue mismatch");b.Step(1.0/60);if(b.Gold!=before+reward)throw new Exception("duplicate reward");}
 static void Main(){try{Check(0,3);Check(4,40);Console.WriteLine("PASS regular and boss rewards: one gold award and matching cue");}catch(Exception e){Console.Error.WriteLine(e);Environment.Exit(1);}}
}
