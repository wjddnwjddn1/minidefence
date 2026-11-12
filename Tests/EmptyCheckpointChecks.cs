using System;
using System.Reflection;
using EmberBastion;
class EmptyCheckpointChecks {
 static int checks;
 static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
 static int Main(){try{
  Check(CombatSaveData.IsAbsent(null),"null remains optional");Check(CombatSaveData.IsAbsent(new CombatSaveData()),"default inline object is absent");
  var empty=new CombatSaveData{units=new Unit[0],enemies=new Enemy[0],shots=new Shot[0],pending=new SpawnSave[0],reinforcement=new ReinforcementDraftData{owned=new int[0],offered=new int[0]},expedition=new ExpeditionBattleData{boons=new int[0]}};
  Check(CombatSaveData.IsAbsent(empty),"actual Unity inline empty arrays");empty.version=0;Check(CombatSaveData.IsAbsent(empty),"zero-initialized inline form");
  foreach(var field in typeof(CombatSaveData).GetFields(BindingFlags.Public|BindingFlags.Instance)){
   var d=new CombatSaveData();object value;
   if(field.FieldType==typeof(int))value=field.Name=="version"?2:1;
   else if(field.FieldType==typeof(uint))value=(uint)1;
   else if(field.FieldType==typeof(double))value=1.0;
   else if(field.FieldType==typeof(bool))value=true;
   else if(field.FieldType==typeof(Point))value=new Point(1,0);
   else if(field.FieldType.IsArray)value=Array.CreateInstance(field.FieldType.GetElementType(),1);
   else if(field.FieldType==typeof(ReinforcementDraftData))value=new ReinforcementDraftData{randomState=1};
   else if(field.FieldType==typeof(ExpeditionBattleData))value=new ExpeditionBattleData{threat=1};
   else throw new Exception("Add corruption coverage for "+field.Name);
   field.SetValue(d,value);Check(!CombatSaveData.IsAbsent(d),"populated "+field.Name+" is never silently discarded");
  }
  Check(!CombatSaveData.IsAbsent(new CombatSaveData{time=double.NaN}),"NaN is not absent");Check(!CombatSaveData.IsAbsent(new CombatSaveData{ember=new Point(0,1)}),"nonzero Ember coordinate is not absent");
  Check(!CombatSaveData.IsAbsent(new CombatSaveData{expedition=new ExpeditionBattleData{lapPoints=1}})&&!CombatSaveData.IsAbsent(new CombatSaveData{expedition=new ExpeditionBattleData{boons=new int[1]}}),"populated expedition data is not absent");
  bool rejected=false;try{Battle.ImportCombat(empty);}catch(ArgumentException){rejected=true;}Check(rejected,"empty CURRENT combat is still invalid");
  var b=new Battle(1);b.Place(0,0);b.Start();Check(!CombatSaveData.IsAbsent(b.ExportCombat()),"real checkpoints remain present");
  Console.WriteLine("PASS "+checks+" empty checkpoint and corruption boundary checks");return 0;
 }catch(Exception e){Console.Error.WriteLine(e.Message);return 1;}}
}
