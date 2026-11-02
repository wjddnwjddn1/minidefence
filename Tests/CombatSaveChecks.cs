using System;
using EmberBastion;
class CombatSaveChecks {
 static int checks;static void Check(bool ok,string what){if(!ok)throw new Exception(what);checks++;}
 static bool Near(double a,double b){return Math.Abs(a-b)<1e-8;}
 static void Same(Battle a,Battle b){Check(a.Phase==b.Phase&&a.Wave==b.Wave&&a.Gold==b.Gold&&a.Kills==b.Kills&&a.PeakAlive==b.PeakAlive&&a.Pending==b.Pending&&a.Enemies.Count==b.Enemies.Count&&a.Shots.Count==b.Shots.Count&&Near(a.Time,b.Time)&&Near(a.WaveTime,b.WaveTime),"battle differs");for(int i=0;i<a.Enemies.Count;i++){var x=a.Enemies[i];var y=b.Enemies[i];Check(x.Id==y.Id&&Near(x.HP,y.HP)&&Near(x.Travel,y.Travel)&&Near(x.Position.X,y.Position.X)&&Near(x.Position.Y,y.Position.Y)&&Near(x.SlowTime,y.SlowTime)&&Near(x.NextCharge,y.NextCharge),"enemy differs");}for(int i=0;i<a.Shots.Count;i++){var x=a.Shots[i];var y=b.Shots[i];Check(x.Kind==y.Kind&&x.Target==y.Target&&Near(x.Position.X,y.Position.X)&&Near(x.Position.Y,y.Position.Y),"shot differs");}}
 static void Advance(Battle b,int n){for(int i=0;i<n;i++)b.Step(1.0/60);}
 static void Reject(CombatSaveData d,string reason){bool failed=false;try{Battle.ImportCombat(d);}catch(ArgumentException){failed=true;}Check(failed,reason);}
 static int Main(){try{foreach(int field in new[]{0,1,2})foreach(bool fast in new[]{false,true})foreach(int ticks in new[]{30,120,900,2100}){
  var b=new Battle(123,field);b.SetFastWaves(fast);b.Gold=500;b.Place(1,0);b.Place(3,1);b.Upgrade(0);b.Start();Advance(b,ticks);Check(b.Phase==Phase.Fighting,"fixture fighting");if(b.EmberCD<=0)b.Ember(new Point(192,128));var data=b.ExportCombat();var restored=Battle.ImportCombat(data);Same(b,restored);Check(restored.FastWaves==fast,"mode preserved");if(data.enemies.Length>0){double hp=restored.Enemies[0].HP;data.enemies[0].HP=1;Check(Near(restored.Enemies[0].HP,hp),"loaded state independent");}
  for(int i=0;i<900&&b.Phase==Phase.Fighting;i++){b.Step(1.0/60);restored.Step(1.0/60);if(i%60==0)Same(b,restored);}Same(b,restored);
 }
 var invalid=new Battle();invalid.Place(0,0);invalid.Start();Advance(invalid,120);var bad=invalid.ExportCombat();bad.field=-1;Reject(bad,"bad field rejected");bad=invalid.ExportCombat();bad.enemies[0].HP=double.NaN;Reject(bad,"NaN enemy rejected");bad=invalid.ExportCombat();bad.pending[0].gate=99;Reject(bad,"bad spawn gate rejected");bad=invalid.ExportCombat();bad.units[0].Invested=-1;Reject(bad,"bad unit rejected");bad=invalid.ExportCombat();bad.nextId=1;Reject(bad,"duplicate IDs rejected");bad=invalid.ExportCombat();bad.accumulator=double.NaN;Reject(bad,"bad clock rejected");
 bool stopped=false;try{new Battle().ExportCombat();}catch(InvalidOperationException){stopped=true;}Check(stopped,"preparation is not combat save");Console.WriteLine("PASS "+checks+" combat-save state, deterministic replay and corruption checks");return 0;
 }catch(Exception e){Console.Error.WriteLine("FAIL "+e.Message);return 1;}}
}

