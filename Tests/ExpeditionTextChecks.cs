using System;
using System.Globalization;
using EmberBastion;
// r39 (Claude): what the camp cards say must be what the rules do. Every number in a boon's description is
// derived here from the Core constants, so retuning a constant without rewording the card fails this check.
class ExpeditionTextChecks {
 static int checks;
 static void Check(bool ok,string what){checks++;if(!ok)throw new Exception(what);}
 static string Pct(double factor){return Math.Round(Math.Abs(factor-1)*100).ToString(CultureInfo.InvariantCulture)+"%";}
 static void Has(int boon,params string[] parts){string text=ExpeditionText.Effect(boon);foreach(string part in parts)Check(text.Contains(part),ExpeditionText.Name(boon)+" should say \""+part+"\": "+text);}
 static void Main(){
  Check(ExpeditionText.Count==Boon.Count,"one text per boon: "+ExpeditionText.Count+" vs "+Boon.Count);
  for(int boon=0;boon<Boon.Count;boon++){
   Check(ExpeditionText.Name(boon).Length>0&&ExpeditionText.Effect(boon).Length>0&&ExpeditionText.Quote(boon).Length>0,"boon "+boon+" has a name, an effect and a line");
   Check(StoryScript.Portrait(ExpeditionText.Proposer(boon))>=0&&StoryScript.Portrait(ExpeditionText.Proposer(boon))<5,"boon "+boon+" is proposed by a companion");
   Check(ExpeditionText.Effect(boon).StartsWith("교리 · ")==Boon.IsDoctrine(boon),"only doctrines are labelled as doctrines: "+ExpeditionText.Effect(boon));
   for(int other=0;other<boon;other++)Check(ExpeditionText.Name(other)!=ExpeditionText.Name(boon),"boon names are unique");
  }
  Has(Boon.SwordDrill,"+"+Pct(Boon.ClassDamage));Has(Boon.StrongBow,"+"+Pct(Boon.ClassDamage));Has(Boon.PowderMix,"+"+Pct(Boon.ClassDamage));
  Has(Boon.FrostCore,"+"+Boon.FrostSeconds.ToString(CultureInfo.InvariantCulture)+"초");
  Has(Boon.Spyglass,"+"+Pct(Boon.Range));Has(Boon.Spoils,"+"+Pct(Boon.Reward));Has(Boon.WarDrum,"+"+Pct(Boon.Haste));
  Has(Boon.Phalanx,"+"+Pct(1+Boon.PhalanxPerSword),"+"+Pct(1+Boon.PhalanxPerSword*Boon.PhalanxMax));
  Has(Boon.RapidFire,"+"+Pct(Boon.RapidHaste),"-"+Pct(Boon.RapidRange));
  Has(Boon.ChainBlast,Pct(1+Boon.ChainShare));
  Has(Boon.DeepFrost,"+50%","+"+Pct(Boon.DeepFrostTaken));
  Has(Boon.Shatter,Boon.ShatterSeconds.ToString(CultureInfo.InvariantCulture)+"초");
  Has(Boon.FieldForge,"+"+Pct(Boon.ForgeDamage));
  Has(Boon.EmberWard,Boon.PointsPerWarmth+"바퀴",(Boon.PointsPerWarmth+Boon.WardPoints)+"바퀴");
  Has(Boon.Rekindle,"+"+Boon.RekindleWarmth);Has(Boon.WarChest,"+"+Boon.WarChestGold);
  Console.WriteLine("PASS "+checks+" expedition text checks: every boon's card matches its rule");
 }
}
