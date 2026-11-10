using System;
using EmberBastion;
class CandidateWaveIsolationChecks {
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 static void Main(){for(int f=0;f<Balance.Fields.Length;f++){
  var legacy=new Battle(1,f);var normal=Battle.CreateForRules(1,f,4);var reinforcement=Battle.CreateForRules(1,f,4);Check(reinforcement.EnableReinforcements(),"enable reinforcement");
  for(int w=0;w<normal.WaveCount;w++){var source=legacy.WaveGroups(w);var candidate=normal.WaveGroups(w);var other=reinforcement.WaveGroups(w);Check(!object.ReferenceEquals(source,candidate),"wave copy");Check(object.ReferenceEquals(source,other),"reinforcement uses legacy");Check(source.Length==candidate.Length,"group count");for(int i=0;i<source.Length;i++){var a=source[i];var b=candidate[i];Check(!object.ReferenceEquals(a,b),"group copy");Check(a.Kind==b.Kind&&a.Count==b.Count&&a.Gap==b.Gap&&a.Delay==b.Delay&&a.Gate==b.Gate&&a.Multiplier==b.Multiplier&&a.Extended==b.Extended,"copy values");}}
  var copy=normal.WaveGroups(0)[0];var original=legacy.WaveGroups(0)[0];int old=copy.Count;copy.Count++;Check(original.Count!=copy.Count,"independent mutation");copy.Count=old;
 }Console.WriteLine("PASS candidate wave isolation across four fields and modes");}
}
