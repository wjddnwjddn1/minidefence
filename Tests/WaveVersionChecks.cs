using System;
using EmberBastion;
class WaveVersionChecks {
 static void Check(bool ok,string what){if(!ok)throw new Exception(what);}
 static void Main(){
  // only the fields that existed under the legacy rules; the fourth field is rules-2 only (FieldFourChecks)
  for(int i=0;i<Balance.LegacyFieldCount;i++){
   var field=Balance.Fields[i];var original=field.Waves;int old=original.Length;
   Check(old==(i==0?10:8),"legacy field length");
   var oldBattle=new Battle(42,i);oldBattle.Place(0,0);var prep=oldBattle.SavePreparation();prep.rulesVersion=1;prep.wave=old-1;
   var extended=new Group[old+4][];Array.Copy(original,extended,old);for(int j=old;j<extended.Length;j++)extended[j]=original[old-1];field.Waves=extended;
   try{
    Check(Battle.WaveCountFor(i,0)==old&&Battle.WaveCountFor(i,1)==old&&Battle.WaveCountFor(i,2)==old+4,"versioned length");
    var restored=Battle.RestorePreparation(prep);Check(restored.WaveCount==old&&restored.Start(),"legacy last wave restore");
    var save=restored.ExportCombat();Check(Battle.ImportCombat(save).WaveCount==old,"legacy combat restore");
    save.pending=new SpawnSave[0];save.enemies=new Enemy[0];var last=Battle.ImportCombat(save);last.Step(.1);Check(last.Phase==Phase.Won,"legacy last wave completes without new waves");
    var newer=oldBattle.SavePreparation();newer.rulesVersion=2;newer.wave=old;var newBattle=Battle.RestorePreparation(newer);Check(newBattle.WaveCount==old+4&&newBattle.Start(),"v2 extended wave starts");var newSave=newBattle.ExportCombat();Check(Battle.ImportCombat(newSave).RulesVersion==2,"v2 combat round trip");newer.rulesVersion=3;bool futureRejected=false;try{Battle.RestorePreparation(newer);}catch(ArgumentException){futureRejected=true;}Check(futureRejected,"future rules rejected");
    save.wave=old+1;bool rejected=false;try{Battle.ImportCombat(save);}catch(ArgumentException){rejected=true;}Check(rejected,"reject extended wave for legacy");
   } finally {field.Waves=original;}
  }
  Console.WriteLine("PASS legacy wave limits with extended field data");
 }
}
