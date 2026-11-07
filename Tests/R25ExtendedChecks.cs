using System;
using EmberBastion;
class R25ExtendedChecks {
 static int checks;
 static void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
 static void Main(){
  for(int f=0;f<3;f++){
   int old=f==0?10:8;var field=Balance.Fields[f];Check(field.Waves.Length==old+4,"four new waves");
   for(int wave=0;wave<field.Waves.Length;wave++)foreach(var g in field.Waves[wave])Check(g.Extended==(wave>=old),"only new groups flagged");
   int bosses=0;foreach(var g in field.Waves[old+3])if(g.Kind==4)bosses+=g.Count;Check(bosses==2,"two final bosses");
   foreach(int mode in new[]{0,1,2}){
    var b=new Battle(9,f);b.SetDifficulty(mode);Check(b.RulesVersion==3&&b.WaveCount==old+4,"new rules length");
    var original=field.Waves[0][0];var next=field.Waves[old][0];
    int expectedOriginal=mode==2?original.Count:(int)Math.Round(original.Count*1.25);
    int raw=next.Kind==4?next.Count:(int)Math.Round(next.Count*(mode==0?2.5:mode==1?1.4:1.7));int expectedNew=mode==2||next.Kind==4?raw:(int)Math.Round(raw*1.25);
    Check(b.SpawnCount(original)==expectedOriginal,"earlier density unchanged");Check(b.SpawnCount(next)==expectedNew,"new density");
    b.Gold=9999;Check(b.Place(0,0),"place");b.Wave=old;Check(b.Start(),"start extended wave");
    var restored=Battle.ImportCombat(b.ExportCombat());Check(restored.RulesVersion==3&&restored.Wave==old+1&&restored.WaveCount==old+4&&restored.Pending==b.Pending,"extended combat save");
   }
  }
  Check(new Battle(1,3).WaveCount==12,"marsh stays 12 waves");
  Console.WriteLine("PASS "+checks+" extended wave and save checks");
 }
}
