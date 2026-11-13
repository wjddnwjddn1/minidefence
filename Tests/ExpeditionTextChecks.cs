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
  // skills (r43)
  for(int kind=0;kind<5;kind++){Check(ExpeditionText.SkillName(kind).Length>0,"skill "+kind+" has a name");string say=ExpeditionText.SkillEffect(kind);
   if(kind<4)Check(say.StartsWith("기본 공격 "+Boon.SkillEvery(kind)+"회마다"),"skill "+kind+" says how often: "+say);else Check(say.StartsWith(Boon.ForgeEvery.ToString(CultureInfo.InvariantCulture)+"초마다"),"smith skill says how often: "+say);
   for(int other=0;other<kind;other++)Check(ExpeditionText.SkillName(other)!=ExpeditionText.SkillName(kind),"skill names are unique");}
  Check(ExpeditionText.SkillEffect(0).Contains(Boon.RiftLength.ToString(CultureInfo.InvariantCulture)+" 거리")&&Boon.RiftDamage==1,"rift text");
  Check(ExpeditionText.SkillEffect(1).Contains("+"+Pct(Boon.PierceDamage)),"pierce text");
  Check(ExpeditionText.SkillEffect(2).Contains("반경 +"+Pct(Boon.BigBlastRadius))&&ExpeditionText.SkillEffect(2).Contains("피해 +"+Pct(Boon.BigBlastDamage)),"blast text");
  Check(ExpeditionText.SkillEffect(3).Contains(Boon.NovaFreeze.ToString(CultureInfo.InvariantCulture)+"초"),"nova text");
  // mastery (r43)
  for(int kind=0;kind<4;kind++)Check(ExpeditionText.MasteryEffect(kind).Contains("+"+Pct(Boon.MasteryDamage))&&ExpeditionText.MasteryEffect(kind).Contains(ExpeditionText.SkillName(kind)),"mastery text "+kind);
  Check(ExpeditionText.MasteryEffect(4).Contains("+"+Math.Round(Battle.SmithHasteLevel2*100)+"% → +"+Math.Round(Boon.MasteryHaste*100)+"%"),"smith mastery text");
  // the last stand (r44)
  Check(ExpeditionText.FinaleName.Length>0,"last stand text");
  Check(ExpeditionText.Camp.Length==Expedition.Route.Length,"a camp greeting for every camp");
  // roads (r43)
  for(int road=0;road<3;road++)Check(ExpeditionText.RoadName(road).Length>0&&ExpeditionText.RoadEffect(road).Length>0,"road "+road+" has words");
  Check(ExpeditionText.RoadEffect(Expedition.RoadSupply).Contains("+"+Expedition.SupplyGold)&&ExpeditionText.RoadEffect(Expedition.RoadSupply).Contains("-"+Expedition.SupplyWarmth),"supply road text");
  Check(ExpeditionText.RoadEffect(Expedition.RoadHard).Contains("+"+Expedition.HardWarmth),"hard road text");
  Console.WriteLine("PASS "+checks+" expedition text checks: every boon's card, every skill and every road matches its rule");
 }
}
