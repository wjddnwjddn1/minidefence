using System;
using EmberBastion;
class WaveVersionChecks {
 static void Check(bool ok,string what){if(!ok)throw new Exception(what);}
 static void Main(){
  // only the fields that existed under the legacy rules; the fourth field is rules-2 only (FieldFourChecks)
  for(int i=0;i<Balance.LegacyFieldCount;i++){
   var field=Balance.Fields[i];var original=field.Waves;int old=Battle.WaveCountFor(i,2);
   Check(old==(i==0?10:8),"legacy field length");
   var oldBattle=new Battle(42,i);oldBattle.Place(0,0);var prep=oldBattle.SavePreparation();prep.rulesVersion=1;prep.wave=old-1;
   var extended=new Group[original.Length+4][];Array.Copy(original,extended,original.Length);for(int j=original.Length;j<extended.Length;j++)extended[j]=original[old-1];field.Waves=extended;
   try{
    Check(Battle.WaveCountFor(i,0)==old&&Battle.WaveCountFor(i,1)==old&&Battle.WaveCountFor(i,2)==old&&Battle.WaveCountFor(i,3)==extended.Length,"versioned length");
    var restored=Battle.RestorePreparation(prep);Check(restored.WaveCount==old&&restored.Start(),"legacy last wave restore");
    var save=restored.ExportCombat();Check(Battle.ImportCombat(save).WaveCount==old,"legacy combat restore");
    save.pending=new SpawnSave[0];save.enemies=new Enemy[0];var last=Battle.ImportCombat(save);last.Step(.1);Check(last.Phase==Phase.Won,"legacy last wave completes without new waves");
    prep.rulesVersion=2;var middle=Battle.RestorePreparation(prep);Check(middle.WaveCount==old&&middle.Start(),"v2 last wave restore");var middleSave=middle.ExportCombat();middleSave.pending=new SpawnSave[0];middleSave.enemies=new Enemy[0];var middleLast=Battle.ImportCombat(middleSave);middleLast.Step(.1);Check(middleLast.Phase==Phase.Won,"v2 save ends before new waves");middleSave.wave=old+1;bool middleRejected=false;try{Battle.ImportCombat(middleSave);}catch(ArgumentException){middleRejected=true;}Check(middleRejected,"v2 extended wave rejected");
    var newer=oldBattle.SavePreparation();newer.rulesVersion=3;newer.wave=old;var newBattle=Battle.RestorePreparation(newer);Check(newBattle.WaveCount==extended.Length&&newBattle.Start(),"v3 extended wave starts");var newSave=newBattle.ExportCombat();Check(Battle.ImportCombat(newSave).RulesVersion==3,"v3 combat round trip");newer.rulesVersion=4;bool futureRejected=false;try{Battle.RestorePreparation(newer);}catch(ArgumentException){futureRejected=true;}Check(futureRejected,"future rules rejected");
    save.wave=old+1;bool rejected=false;try{Battle.ImportCombat(save);}catch(ArgumentException){rejected=true;}Check(rejected,"reject extended wave for legacy");
   } finally {field.Waves=original;}
  }
  var marsh=Balance.Fields[3];var originalMarsh=marsh.Waves;var longer=new Group[originalMarsh.Length+4][];Array.Copy(originalMarsh,longer,originalMarsh.Length);for(int i=originalMarsh.Length;i<longer.Length;i++)longer[i]=originalMarsh[0];marsh.Waves=longer;
  try{Check(Battle.WaveCountFor(3,0)==0&&Battle.WaveCountFor(3,1)==0&&Battle.WaveCountFor(3,2)==12&&Battle.WaveCountFor(3,3)==16,"marsh v2 preserved");}finally{marsh.Waves=originalMarsh;}
  Console.WriteLine("PASS v0/v1/v2 wave limits with extended field data");
 }
}
