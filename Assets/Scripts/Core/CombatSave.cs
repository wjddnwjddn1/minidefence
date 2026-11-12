using System;
using System.Collections.Generic;
namespace EmberBastion {
 [Serializable] public sealed class SpawnSave {public int kind,gate,order;public double at,multiplier;}
 [Serializable] public sealed class CombatSaveData {
  public int version=1,field,wave,gold,hp,kills,peakAlive,nextId,supplyBonus,emberBranch,killKinds,rulesVersion,mode;
  public bool fastWaves,hardWaves,hasEmber;
  public uint rng;
  public double time,waveTime,emberCD,accumulator;
  public Point ember;
  public Unit[] units;public Enemy[] enemies;public Shot[] shots;public SpawnSave[] pending;public ReinforcementDraftData reinforcement;public ExpeditionBattleData expedition;
  // JsonUtility expands a null inline checkpoint into a default-valued object.
  // Only that exact empty shape is absent; any populated/corrupt checkpoint is validated normally.
  public static bool IsAbsent(CombatSaveData d){return d==null||((d.version==0||d.version==1)&&d.field==0&&d.wave==0&&d.gold==0&&d.hp==0&&d.kills==0&&d.peakAlive==0&&d.nextId==0&&d.supplyBonus==0&&d.emberBranch==0&&d.killKinds==0&&d.rulesVersion==0&&d.mode==0&&!d.fastWaves&&!d.hardWaves&&!d.hasEmber&&d.rng==0&&d.time==0&&d.waveTime==0&&d.emberCD==0&&d.accumulator==0&&d.ember.X==0&&d.ember.Y==0&&(d.units==null||d.units.Length==0)&&(d.enemies==null||d.enemies.Length==0)&&(d.shots==null||d.shots.Length==0)&&(d.pending==null||d.pending.Length==0)&&ReinforcementDraft.IsAbsent(d.reinforcement)&&ExpeditionBattleData.IsAbsent(d.expedition));}
 }
 public sealed partial class Battle {
  public CombatSaveData ExportCombat(){
   var source=CopyCombat();var data=new CombatSaveData{mode=(int)source.Mode,rulesVersion=source.RulesVersion,field=source.FieldIndex,wave=source.Wave,gold=source.Gold,hp=source.HP,kills=source.Kills,killKinds=source.KillKinds,supplyBonus=source.SupplyBonus,emberBranch=source.EmberBranch,peakAlive=source.PeakAlive,nextId=source.nextId,fastWaves=source.FastWaves,hardWaves=source.HardWaves,rng=source.rng,time=source.Time,waveTime=source.WaveTime,emberCD=source.EmberCD,accumulator=source.accumulator,hasEmber=source.ember.HasValue,ember=source.ember.GetValueOrDefault(),reinforcement=source.Reinforcements==null?null:source.Reinforcements.Export(),expedition=source.ExportExpedition(),units=source.Units.ToArray(),enemies=source.Enemies.ToArray(),shots=source.Shots.ToArray(),pending=new SpawnSave[source.queue.Count]};
   for(int i=0;i<source.queue.Count;i++){var q=source.queue[i];data.pending[i]=new SpawnSave{kind=q.Kind,gate=q.Gate,order=q.Order,at=q.At,multiplier=q.Multiplier};}return data;
  }
  static bool Finite(double n){return !double.IsNaN(n)&&!double.IsInfinity(n);}
  static bool WorldPoint(Point p){return Finite(p.X)&&Finite(p.Y)&&p.X>=0&&p.X<=1280&&p.Y>=0&&p.Y<=768;}
  public static Battle ImportCombat(CombatSaveData data){
   if(data==null||data.version!=1||data.field<0||data.field>=Balance.Fields.Length||data.units==null||data.enemies==null||data.shots==null||data.pending==null)throw new ArgumentException("Invalid combat save");
   var field=FieldFor(data.field,data.rulesVersion,(BattleMode)data.mode);
   if(data.wave<1||data.wave>WaveCountFor(data.field,data.rulesVersion)||data.gold<0||data.hp<1||data.hp>100||data.kills<0||data.enemies.Length>=Balance.MaxAlive||data.peakAlive<data.enemies.Length||data.peakAlive>=Balance.MaxAlive||data.nextId<1||data.units.Length>field.Slots.Length||data.shots.Length>512||data.pending.Length>512||!FiniteNonnegative(data.time)||!FiniteNonnegative(data.waveTime)||!FiniteNonnegative(data.emberCD)||data.emberCD>Balance.EmberCooldown||!Finite(data.accumulator)||data.accumulator<-.00000001||data.accumulator>.25||(data.hasEmber&&!WorldPoint(data.ember)))throw new ArgumentException("Invalid combat values");
   var unitData=new PreparationUnit[data.units.Length];
   for(int i=0;i<data.units.Length;i++){var u=data.units[i];if(u==null)throw new ArgumentException("Invalid unit");unitData[i]=new PreparationUnit{slot=u.Slot,kind=u.Kind,level=u.Level,invested=u.Invested,prepSpent=u.PrepSpent,priority=u.Priority,branch=u.Branch,hitCount=u.HitCount,cooldown=u.Cooldown,stun=u.Stun};}
   var result=RestorePreparationCore(new PreparationData{mode=data.mode,rulesVersion=data.rulesVersion,field=data.field,wave=data.wave-1,gold=data.gold,hp=data.hp,kills=data.kills,killKinds=data.killKinds,supplyBonus=data.supplyBonus,emberBranch=data.emberBranch,fastWaves=data.fastWaves,hardWaves=data.hardWaves,rng=data.rng,time=data.time,emberCD=data.emberCD,units=unitData,expedition=data.expedition},true);
   bool present=!ReinforcementDraft.IsAbsent(data.reinforcement);if(data.rulesVersion==4&&!present)throw new ArgumentException("Missing combat draft");if(present){if(data.rulesVersion<4)result.Mode=BattleMode.Reinforcement;if(result.Mode==BattleMode.Reinforcement&&(!data.fastWaves||data.hardWaves))throw new ArgumentException("Invalid reinforcement mode");var draft=ReinforcementDraft.Restore(data.reinforcement,result.DraftLimit,result.RulesVersion>=4);int expected=data.wave<3?0:data.wave<6?1:result.HasNormalCards&&data.wave>=9?3:2;if(draft.SelectedCount!=expected||(draft.HasOffer&&data.wave!=2&&data.wave!=5&&!(result.HasNormalCards&&data.wave==8))||(draft.HasOffer&&data.pending.Length!=0))throw new ArgumentException("Invalid reinforcement combat state");result.Reinforcements=draft;}result.Phase=Phase.Fighting;result.Wave=data.wave;result.WaveTime=data.waveTime;result.PeakAlive=data.peakAlive;result.nextId=data.nextId;result.accumulator=data.accumulator;result.ember=data.hasEmber?(Point?)data.ember:null;
   var ids=new HashSet<int>();foreach(var e in data.enemies){if(e==null||e.Id<1||e.Id>=data.nextId||!ids.Add(e.Id)||e.Kind<0||e.Kind>=Balance.Enemies.Length||e.Gate<0||e.Gate>=field.Gates.Length||e.Laps<0||!Finite(e.HP)||!Finite(e.MaxHP)||e.HP<=0||e.MaxHP<=0||e.HP>e.MaxHP||!FiniteNonnegative(e.Speed)||!FiniteNonnegative(e.Travel)||!FiniteNonnegative(e.Shield)||!FiniteNonnegative(e.Charge)||!FiniteNonnegative(e.NextCharge)||!FiniteNonnegative(e.Burn)||!FiniteNonnegative(e.SlowTime)||!FiniteNonnegative(e.SlowPct)||e.SlowPct>1||!FiniteNonnegative(e.FreezeTime)||!WorldPoint(e.Position))throw new ArgumentException("Invalid enemy");result.Enemies.Add(e.Copy());}
   foreach(var shot in data.shots){if(shot==null||shot.Kind<1||shot.Kind>3||shot.Target<1||!WorldPoint(shot.Position)||!WorldPoint(shot.Destination)||!FiniteNonnegative(shot.Damage)||!FiniteNonnegative(shot.Pen)||!Finite(shot.Speed)||shot.Speed<=0||!FiniteNonnegative(shot.Radius)||!FiniteNonnegative(shot.BossMult)||!FiniteNonnegative(shot.SlowPct)||shot.SlowPct>1)throw new ArgumentException("Invalid projectile");result.Shots.Add(shot.Copy());}
   double last=-1;foreach(var q in data.pending){if(q==null||q.kind<0||q.kind>=Balance.Enemies.Length||q.gate<0||q.gate>=field.Gates.Length||q.order<0||!FiniteNonnegative(q.at)||q.at<last||!Finite(q.multiplier)||q.multiplier<=0)throw new ArgumentException("Invalid spawn queue");last=q.at;result.queue.Add(new Spawn{Kind=q.kind,Gate=q.gate,Order=q.order,At=q.at,Multiplier=q.multiplier});}
   return result;
  }
 }
}
