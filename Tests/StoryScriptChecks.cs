// Claude: story script shape (every field has an intro/victory/defeat line, lines fit the dialogue box, barks for 4 goblin kinds).
using System;
using EmberBastion;
static class StoryScriptChecks {
 static int pass;
 static void Check(bool ok,string name){if(!ok)throw new Exception("FAIL "+name);pass++;}
 static void Lines(StoryLine[] lines,string name,int max){Check(lines!=null&&lines.Length>0,name+" present");foreach(var l in lines){Check(l!=null&&!string.IsNullOrEmpty(l.Text),name+" text");Check(l.Text.Length<=max,name+" length "+l.Text.Length);}}
 static int Main(){
  int fields=Balance.Fields.Length;
  Lines(StoryScript.Opening,"opening",90);Lines(StoryScript.Ending,"ending",90);
  Check(StoryScript.FieldIntro.Length==fields,"intro per field");foreach(var f in StoryScript.FieldIntro)Lines(f,"intro",90);
  Check(StoryScript.Victory.Length==fields&&StoryScript.Defeat.Length==fields,"result line per field");
  Lines(StoryScript.Victory,"victory",40);Lines(StoryScript.Defeat,"defeat",40);Lines(StoryScript.FirstWave,"first",40);Lines(StoryScript.MidWave,"mid",40);Lines(StoryScript.BossArrive,"boss",40);
  Lines(new[]{StoryScript.LastWave,StoryScript.BossRoar,StoryScript.BossRoarReply,StoryScript.CastleHit,StoryScript.Crowded},"events",40);
  Check(StoryScript.Barks.Length==4,"barks per goblin kind");foreach(var b in StoryScript.Barks){Check(b.Length>0,"bark list");foreach(var t in b)Check(t.Length>0&&t.Length<=14,"bark length");}
  foreach(Speaker s in Enum.GetValues(typeof(Speaker)))Check(s==Speaker.Narrator||StoryScript.Name(s).Length>0,"speaker name "+s);
  Console.WriteLine("PASS "+pass+" story script checks");return 0;}
}
