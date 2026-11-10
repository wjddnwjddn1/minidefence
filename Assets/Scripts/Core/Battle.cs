using System;
using System.Collections.Generic;
namespace EmberBastion {
 public enum BattleMode { Unspecified=0,Normal=1,Reinforcement=2 }
 public enum Phase { Preparation,Fighting,Won,Lost }
 [Serializable] public sealed class Unit {public int Slot,Kind,Level=1,Invested,PrepSpent,Priority,Branch,HitCount;public double Cooldown,Stun;internal Unit Copy(){return (Unit)MemberwiseClone();}}
 [Serializable] public sealed class Enemy {public int Id,Kind,Gate,Laps;public double HP,MaxHP,Speed,Travel,Shield,Charge,NextCharge=10,Burn,SlowTime,SlowPct,FreezeTime;public bool Roared;public Point Position;internal Enemy Copy(){return (Enemy)MemberwiseClone();}}
 [Serializable] public sealed class Shot {public int Kind,Target;public Point Position,Destination;public double Damage,Pen,Speed,Radius,BossMult=1;public bool Burning,Freezing;public double SlowPct;internal Shot Copy(){return (Shot)MemberwiseClone();}}
 public sealed class Cue {public string Kind,Text;public Point Position;public double Radius;public int Slot=-1,Facing=1,Source=-1;public Cue(string kind,Point p,string text="",double radius=0){Kind=kind;Position=p;Text=text;Radius=radius;}}
 [Serializable] public sealed class PreparationUnit {public int slot,kind,level,invested,prepSpent,priority,branch,hitCount;public double cooldown,stun;}
 [Serializable] public sealed class PreparationData {public int version=1,field,wave,gold,hp,kills,supplyBonus,emberBranch,killKinds,rulesVersion,mode;public bool fastWaves,hardWaves;public uint rng;public double time,emberCD;public PreparationUnit[] units;public ReinforcementDraftData reinforcement;}
 sealed class Spawn {public int Kind,Gate,Order;public double At,Multiplier;internal Spawn Copy(){return (Spawn)MemberwiseClone();}}
 // No Unity, display, input, persistence, or wall-clock dependencies.
 public sealed partial class Battle {
  public Phase Phase=Phase.Preparation;
  public BattleMode Mode {get;private set;}
  public readonly FieldDef Field;public readonly int FieldIndex;
  public int Gold=Balance.StartGold,HP=100,Wave,Kills,PeakAlive;public int KillKinds {get;private set;}public int SupplyBonus {get;private set;}public int EmberBranch {get;private set;}
  public double Time,WaveTime,EmberCD;public int RulesVersion {get;private set;}
  public ReinforcementDraft Reinforcements {get;private set;}public Action<Battle> ReinforcementOffered;public bool ReinforcementPending {get{return Reinforcements!=null&&Reinforcements.HasOffer;}}
  public string EndCause="";public bool FastWaves;public bool HardWaves {get;private set;}public Action<Battle> WaveStarted;
  public readonly List<Unit> Units=new List<Unit>();
  public readonly List<Enemy> Enemies=new List<Enemy>();
  public readonly List<Shot> Shots=new List<Shot>();
  public readonly List<Cue> Cues=new List<Cue>();
  readonly List<Spawn> queue=new List<Spawn>();uint rng;int nextId=1;double accumulator;Point? ember;
  public Battle(uint seed=1,int fieldIndex=0){if(fieldIndex<0||fieldIndex>=Balance.Fields.Length)throw new ArgumentOutOfRangeException("fieldIndex");rng=seed;RulesVersion=3;FieldIndex=fieldIndex;Mode=BattleMode.Normal;Field=Balance.Fields[fieldIndex];Gold=Field.StartGold;}
  public static Battle CreateForRules(uint seed,int field,int rulesVersion){if(rulesVersion<0||rulesVersion>4||WaveCountFor(field,rulesVersion)==0)throw new ArgumentException("Invalid rules");var b=new Battle(seed,field);b.RulesVersion=rulesVersion;if(b.HasNormalCards)b.Reinforcements=new ReinforcementDraft(seed,3);return b;}
  public bool HasNormalCards {get{return RulesVersion==4&&Mode==BattleMode.Normal;}}
  int DraftLimit {get{return HasNormalCards?3:2;}}
  public PreparationData SavePreparation(){if(Phase!=Phase.Preparation)throw new InvalidOperationException("Only preparation can be saved");var list=new PreparationUnit[Units.Count];for(int i=0;i<list.Length;i++){var u=Units[i];list[i]=new PreparationUnit{slot=u.Slot,kind=u.Kind,level=u.Level,invested=u.Invested,prepSpent=u.PrepSpent,priority=u.Priority,branch=u.Branch,hitCount=u.HitCount,cooldown=u.Cooldown,stun=u.Stun};}return new PreparationData{mode=(int)Mode,rulesVersion=RulesVersion,field=FieldIndex,wave=Wave,gold=Gold,hp=HP,kills=Kills,killKinds=KillKinds,supplyBonus=SupplyBonus,emberBranch=EmberBranch,fastWaves=FastWaves,hardWaves=HardWaves,rng=rng,time=Time,emberCD=EmberCD,units=list,reinforcement=Reinforcements==null?null:Reinforcements.Export()};}
  static bool FiniteNonnegative(double n){return !double.IsNaN(n)&&!double.IsInfinity(n)&&n>=0;}
  public static Battle RestorePreparation(PreparationData data){return RestorePreparationCore(data,false);}
  static Battle RestorePreparationCore(PreparationData data,bool combat){if(data==null||data.version!=1||data.field<0||data.field>=Balance.Fields.Length||data.units==null)throw new ArgumentException("Invalid preparation");var b=new Battle(data.rng,data.field);if(data.rulesVersion<0||data.rulesVersion>4||data.wave<0||data.wave>=WaveCountFor(data.field,data.rulesVersion)||data.gold<0||data.supplyBonus<0||data.supplyBonus>60||data.supplyBonus%20!=0||data.emberBranch<0||data.emberBranch>2||(data.hardWaves&&!data.fastWaves)||data.hp<1||data.hp>100||data.kills<0||data.killKinds<0||(data.killKinds & ~15)!=0||!FiniteNonnegative(data.time)||!FiniteNonnegative(data.emberCD)||data.emberCD>Balance.EmberCooldown||data.units.Length>b.Field.Slots.Length)throw new ArgumentException("Invalid preparation values");b.RulesVersion=data.rulesVersion;if(data.rulesVersion==4){if(data.mode<1||data.mode>2)throw new ArgumentException("Missing battle mode");b.Mode=(BattleMode)data.mode;}b.FastWaves=data.fastWaves;b.HardWaves=data.hardWaves;b.SupplyBonus=data.supplyBonus;b.EmberBranch=data.emberBranch;b.Wave=data.wave;b.Gold=data.gold;b.HP=data.hp;b.Kills=data.kills;b.KillKinds=data.killKinds;b.Time=data.time;b.EmberCD=data.emberCD;foreach(var v in data.units){if(v==null||v.slot<0||v.slot>=b.Field.Slots.Length||v.kind<0||v.kind>=Balance.Troops.Length||v.level<1||v.level>3||b.At(v.slot)!=null||v.invested!=Balance.Troops[v.kind].Cost+(v.level>=2?50:0)+(v.level==3?90:0)||v.prepSpent<0||v.prepSpent>v.invested||v.priority<0||v.priority>3||(v.kind!=1&&v.kind!=3&&v.priority!=0)||(v.level==3?(v.branch<1||v.branch>2):v.branch!=0)||v.hitCount<0||!FiniteNonnegative(v.cooldown)||v.cooldown>Balance.Troops[v.kind].Interval||!FiniteNonnegative(v.stun)||v.stun>1.5)throw new ArgumentException("Invalid unit");b.Units.Add(new Unit{Slot=v.slot,Kind=v.kind,Level=v.level,Invested=v.invested,PrepSpent=v.prepSpent,Priority=v.priority,Branch=v.branch,HitCount=v.hitCount,Cooldown=v.cooldown,Stun=v.stun});}if(!combat){bool present=!ReinforcementDraft.IsAbsent(data.reinforcement);if(data.rulesVersion==4){if(!present||data.wave!=0)throw new ArgumentException("Missing preparation draft");if(b.Mode==BattleMode.Reinforcement&&(!data.fastWaves||data.hardWaves))throw new ArgumentException("Invalid reinforcement mode");}else if(present){if(!data.fastWaves||data.hardWaves||data.wave!=0)throw new ArgumentException("Invalid reinforcement preparation");b.Mode=BattleMode.Reinforcement;}if(present){b.Reinforcements=ReinforcementDraft.Restore(data.reinforcement,b.DraftLimit);if(b.Reinforcements.SelectedCount!=0||b.Reinforcements.HasOffer)throw new ArgumentException("Invalid preparation draft state");}}return b;}
  // In-memory retry copy: keep the exact RNG, spawn queue, projectiles and timers.
  // Player progress and user storage are not touched.
  public Battle CopyCombat(){if(Phase!=Phase.Fighting)throw new InvalidOperationException("Only combat can be copied");var copy=new Battle(rng,FieldIndex);copy.Phase=Phase;copy.Mode=Mode;copy.RulesVersion=RulesVersion;copy.Gold=Gold;copy.SupplyBonus=SupplyBonus;copy.EmberBranch=EmberBranch;copy.HP=HP;copy.Wave=Wave;copy.Kills=Kills;copy.KillKinds=KillKinds;copy.PeakAlive=PeakAlive;copy.Time=Time;copy.WaveTime=WaveTime;copy.EmberCD=EmberCD;copy.EndCause=EndCause;copy.FastWaves=FastWaves;copy.HardWaves=HardWaves;copy.nextId=nextId;copy.accumulator=accumulator;copy.ember=ember;copy.Reinforcements=Reinforcements==null?null:ReinforcementDraft.Restore(Reinforcements.Export(),DraftLimit);foreach(var u in Units)copy.Units.Add(u.Copy());foreach(var e in Enemies)copy.Enemies.Add(e.Copy());foreach(var shot in Shots)copy.Shots.Add(shot.Copy());foreach(var pending in queue)copy.queue.Add(pending.Copy());return copy;}
  Point Path(double distance){return FieldIndex==0?Balance.Path(distance):Field.Path(distance);}
  // Freeze legacy campaign length before adding newer waves to the shared field data.
  public static int WaveCountFor(int fieldIndex,int rulesVersion){if(fieldIndex<0||fieldIndex>=Balance.Fields.Length)throw new ArgumentOutOfRangeException("fieldIndex");int count=Balance.Fields[fieldIndex].Waves.Length;if(rulesVersion<=1&&fieldIndex>=Balance.LegacyFieldCount)return 0;if(rulesVersion<=2)return Math.Min(count,fieldIndex==0?10:fieldIndex<Balance.LegacyFieldCount?8:12);return count;}
  static readonly Group[][][] candidateWaves=new Group[Balance.Fields.Length][][];
  // Rule 4 normal waves are isolated from legacy and reinforcement data.
  public Group[] WaveGroups(int index){
   if(index<0||index>=WaveCount)throw new ArgumentOutOfRangeException("index");
   if(!HasNormalCards)return Field.Waves[index];
   if(candidateWaves[FieldIndex]==null){var source=Field.Waves;var copy=new Group[source.Length][];for(int w=0;w<source.Length;w++){copy[w]=new Group[source[w].Length];for(int i=0;i<source[w].Length;i++){var g=source[w][i];copy[w][i]=new Group(g.Kind,g.Count,g.Gap,g.Delay,g.Gate,g.Multiplier){Extended=g.Extended};}}candidateWaves[FieldIndex]=copy;}
   return candidateWaves[FieldIndex][index];
  }
  public int WaveCount {get{return WaveCountFor(FieldIndex,RulesVersion);}}
  public int Pending {get{return queue.Count;}}
  public bool Decided {get{return Phase==Phase.Fighting&&Wave==WaveCount&&queue.Count==0;}}
  public bool UpcomingBoss {get{if(Wave<0||Wave>=WaveCount)return false;foreach(var group in WaveGroups(Wave))if(group.Kind==4)return true;return false;}}
  public string NextWaveAdvice(){
   if(Wave<0||Wave>=WaveCount)return "";bool runner=false,shield=false,bomb=false,boss=false;
   foreach(var g in WaveGroups(Wave)){runner|=g.Kind==1;shield|=g.Kind==2;bomb|=g.Kind==3;boss|=g.Kind==4;}
   if(boss)return "대장: 궁수·서리 대장 우선 표적";
   if(shield)return FastWaves&&!HardWaves?"방패: 폭발 반감·궁수 추가 피해":"방패: 궁수 관통·검사 파쇄 검토";
   if(runner)return FastWaves&&!HardWaves?"질주: 근접 반감·궁수 추가 피해":"질주: 서리 둔화·궁수 빠른 적 표적";
   if(bomb)return "폭약: 기절 주의·병력 밀집 피하기";
   return "무리: 검사 다수 타격·폭탄 범위";
  }
  public double WaveInterval {get{return HardWaves?Balance.HardWaveInterval:FastWaves?Balance.FastWaveInterval:Balance.AutoWaveInterval;}}
  public double NextWaveIn {get{return Phase==Phase.Fighting&&Wave<WaveCount?Math.Max(0,WaveInterval-WaveTime):0;}}
  // The caller determines entitlement from distinct completed fields. One grant per fresh run.
  public bool GrantSupply(int amount){if(Phase!=Phase.Preparation||Wave!=0||Units.Count!=0||Gold!=Field.StartGold||SupplyBonus!=0||amount<20||amount>60||amount%20!=0)return false;SupplyBonus=amount;Gold+=amount;return true;}
  public bool SetDifficulty(int mode){if(Mode==BattleMode.Reinforcement||Phase!=Phase.Preparation||Wave!=0||mode<0||mode>2)return false;FastWaves=mode>0;HardWaves=mode==2;return true;}
  public bool EnableReinforcements(){if(Mode==BattleMode.Reinforcement||Phase!=Phase.Preparation||Wave!=0||!SetDifficulty(1))return false;Reinforcements=new ReinforcementDraft(rng);Mode=BattleMode.Reinforcement;return true;}
  public bool DisableReinforcements(){if(Mode!=BattleMode.Reinforcement||Phase!=Phase.Preparation||Wave!=0||Units.Count!=0)return false;Mode=BattleMode.Normal;Reinforcements=HasNormalCards?new ReinforcementDraft(rng,3):null;return true;}
  public bool ChooseReinforcement(int card){if(Phase!=Phase.Fighting||!ReinforcementPending||!Reinforcements.Choose(card))return false;BeginWave();return true;}
  public int RerollCost {get{return 60+30*(Reinforcements==null?0:Reinforcements.Rerolls);}}
  public bool CanRerollReinforcement {get{return HasNormalCards&&ReinforcementPending&&Gold>=RerollCost&&Reinforcements.CanReroll(Units.Exists(u=>u.Kind==0),Units.Exists(u=>u.Kind==1),Units.Exists(u=>u.Kind==2));}}
  public bool RerollReinforcement(){if(!CanRerollReinforcement)return false;int cost=RerollCost;if(!Reinforcements.Reroll(Units.Exists(u=>u.Kind==0),Units.Exists(u=>u.Kind==1),Units.Exists(u=>u.Kind==2)))return false;Gold-=cost;if(ReinforcementOffered!=null)ReinforcementOffered(this);return true;}
  public bool SetFastWaves(bool fast){return SetDifficulty(fast?1:0);}
  double Random(){unchecked{uint t=(rng+=0x6D2B79F5);t=(t^(t>>15))*(t|1);t^=t+((t^(t>>7))*(t|61));return (t^(t>>14))/4294967296.0;}}
  public Unit At(int slot){return Units.Find(u=>u.Slot==slot);}
  public bool SetPriority(int slot,int priority){var u=At(slot);if(ReinforcementPending||(Phase!=Phase.Preparation&&Phase!=Phase.Fighting)||u==null||(u.Kind!=1&&u.Kind!=3)||priority<0||priority>3)return false;u.Priority=priority;return true;}
  static double EffectiveSpeed(Enemy e){return e.FreezeTime>0?0:e.Speed*(e.Charge>0?2.2:1)*(e.SlowTime>0?1-e.SlowPct:1);} static double TargetKey(Enemy e,int priority){if(priority==1)return e.HP;if(priority==2)return EffectiveSpeed(e);if(priority==3&&e.Kind==4)return double.PositiveInfinity;return e.Travel;} static int CompareTargets(Enemy a,Enemy b,int priority){double ak=TargetKey(a,priority),bk=TargetKey(b,priority);if(ak!=bk)return bk.CompareTo(ak);if(priority==2&&a.Travel!=b.Travel)return b.Travel.CompareTo(a.Travel);return a.Id.CompareTo(b.Id);}
  public bool Place(int kind,int slot){if(ReinforcementPending||(Phase!=Phase.Preparation&&Phase!=Phase.Fighting)||kind<0||kind>=Balance.Troops.Length||slot<0||slot>=Field.Slots.Length||At(slot)!=null)return false;var d=Balance.Troops[kind];if(Gold<d.Cost)return false;Gold-=d.Cost;Units.Add(new Unit{Kind=kind,Slot=slot,Invested=d.Cost,PrepSpent=Phase==Phase.Preparation?d.Cost:0});return true;}
  public bool Upgrade(int slot){var u=At(slot);if(ReinforcementPending||(Phase!=Phase.Preparation&&Phase!=Phase.Fighting)||u==null||u.Level!=1||Gold<50)return false;Gold-=50;u.Level=2;u.Invested+=50;if(Phase==Phase.Preparation)u.PrepSpent+=50;return true;}
  public bool UpgradeBranch(int slot,int branch){var u=At(slot);if(ReinforcementPending||(Phase!=Phase.Preparation&&Phase!=Phase.Fighting)||u==null||u.Level!=2||branch<1||branch>2||Gold<90)return false;Gold-=90;u.Level=3;u.Branch=branch;u.Invested+=90;if(Phase==Phase.Preparation)u.PrepSpent+=90;return true;}
  public static double UnitRange(Unit u){return u.Kind==1&&u.Branch==2?320:u.Kind==4&&u.Branch==1?220:Balance.Troops[u.Kind].Range;}
  int CardCount(int card){return Reinforcements==null?0:Reinforcements.Count(card);}
  public double AttackRange(Unit u){return UnitRange(u)*Math.Pow(1.07,HasNormalCards&&(u.Kind==0||u.Kind==4)?0:CardCount(ReinforcementDraft.Range));}
  public double PlacementRange(int kind){if(kind<0||kind>=Balance.Troops.Length)throw new ArgumentOutOfRangeException("kind");return Balance.Troops[kind].Range*(kind==4||(HasNormalCards&&kind==0)?1:Math.Pow(1.07,CardCount(ReinforcementDraft.Range)));}
  int Reward(int amount){return (int)Math.Round(amount*Math.Pow(1.15,CardCount(ReinforcementDraft.GoldIncome)));}
  public void SupportBonuses(Unit u,out double haste,out double bonusDamage){SupportBonuses(u,null,out haste,out bonusDamage);}
  void SupportBonuses(Unit u,Unit excluded,out double haste,out double bonusDamage){haste=0;bonusDamage=0;if(u==null||u.Kind==4)return;Point p=Field.Slots[u.Slot];foreach(var supporter in Units){if(supporter==excluded||supporter.Kind!=4||supporter.Stun>0||Point.Distance(p,Field.Slots[supporter.Slot])>UnitRange(supporter))continue;haste=Math.Max(haste,supporter.Level>=2?.35:.25);if(supporter.Branch==2)bonusDamage=Math.Max(bonusDamage,.2);}}
  public void SupportCoverage(Unit supporter,out int covered,out int additional){covered=0;additional=0;if(supporter==null||supporter.Kind!=4||!Units.Contains(supporter))return;foreach(var ally in Units){if(ally.Kind==4||Point.Distance(Field.Slots[supporter.Slot],Field.Slots[ally.Slot])>UnitRange(supporter))continue;covered++;double allHaste,allDamage,otherHaste,otherDamage;SupportBonuses(ally,out allHaste,out allDamage);SupportBonuses(ally,supporter,out otherHaste,out otherDamage);if(allHaste>otherHaste||allDamage>otherDamage)additional++;}}
  public void AttackStats(Unit u,out double damage,out double interval){if(u==null||u.Kind==4){damage=0;interval=0;return;}double haste,bonusDamage;SupportBonuses(u,out haste,out bonusDamage);var d=Balance.Troops[u.Kind];interval=(u.Kind==1&&u.Branch==1?.75:d.Interval)/(1+haste);if(u.Kind==1)interval/=Math.Pow(1.2,CardCount(ReinforcementDraft.ArcherHaste));damage=(u.Kind==0?Math.Pow(HasNormalCards?1.08:1.15,CardCount(ReinforcementDraft.SwordDamage)):1)*d.Damage*(u.Level>=2?1.4:1)*(u.Branch==2&&u.Kind==0?1.15:u.Branch==2&&u.Kind==1?1.2:1)*(1+bonusDamage);}
  public static int SaleRefund(Unit u){return u==null?0:(int)Math.Floor(u.PrepSpent+(u.Invested-u.PrepSpent)*.6);}
  public bool Sell(int slot){var u=At(slot);if(ReinforcementPending||(Phase!=Phase.Preparation&&Phase!=Phase.Fighting)||u==null)return false;Gold+=SaleRefund(u);Units.Remove(u);return true;}
  public static int Stars(int hp){return hp<=0?0:hp>=80?3:hp>=40?2:1;}
  public bool Repair(){if(Phase!=Phase.Preparation||HP>=100||Gold<60)return false;Gold-=60;HP=Math.Min(100,HP+15);return true;}
  public bool Surrender(){if(Phase!=Phase.Fighting)return false;EndCause="전투 포기";Phase=Phase.Lost;return true;}
  public bool Start(){if(Phase!=Phase.Preparation||!Units.Exists(u=>u.Kind!=4)||Wave>=WaveCount)return false;BeginWave();return true;}
  public int EarlyBonus(){return Math.Min(20,10+Enemies.FindAll(e=>e.HP>0).Count);}
  public bool CanCallEarly(){return Phase==Phase.Fighting&&!ReinforcementPending&&Wave<WaveCount&&queue.Count==0;}
  public bool CallEarly(){if(!CanCallEarly())return false;Gold+=Reward(40+5*Wave)+EarlyBonus();BeginWave();return true;}
  public int SpawnCount(Group g){int raw=g.Extended&&RulesVersion>=3&&g.Kind!=4?(int)Math.Round(g.Count*(HardWaves?1.7:FastWaves?1.4:2.5)):g.Count;int count=RulesVersion>0&&!HardWaves&&g.Kind!=4?(int)Math.Round(raw*1.25):raw;return Mode==BattleMode.Reinforcement&&g.Kind!=4?(int)Math.Round(count*1.15):count;}
  public double SpawnGap(Group g){double raw=g.Extended&&RulesVersion>=3&&g.Kind!=4?g.Gap/(HardWaves?1.7:FastWaves?1.4:2.5):g.Gap;double gap=RulesVersion>0&&!HardWaves&&g.Kind!=4?raw*100.0/125:raw;return Mode==BattleMode.Reinforcement&&g.Kind!=4?gap/1.15:gap;}
  void BeginWave(){if(Reinforcements!=null&&((Wave==2&&Reinforcements.SelectedCount==0)||(Wave==5&&Reinforcements.SelectedCount==1)||(HasNormalCards&&Wave==8&&Reinforcements.SelectedCount==2))){if(!ReinforcementPending){Reinforcements.Offer(Units.Exists(u=>u.Kind==0),Units.Exists(u=>u.Kind==1),Units.Exists(u=>u.Kind==2));if(ReinforcementOffered!=null)ReinforcementOffered(this);}return;}Wave++;WaveTime=0;Phase=Phase.Fighting;int order=0;foreach(var u in Units)u.PrepSpent=0;foreach(var g in WaveGroups(Wave-1)){int count=SpawnCount(g);double gap=SpawnGap(g);for(int i=0;i<count;i++)queue.Add(new Spawn{Kind=g.Kind,Gate=g.Gate,At=g.Delay+i*gap,Multiplier=g.Multiplier*Math.Pow(1.09,Wave-1),Order=order++});}queue.Sort((a,b)=>a.At==b.At?a.Order.CompareTo(b.Order):a.At.CompareTo(b.At));if(WaveStarted!=null)WaveStarted(this);}
  public double EmberRadius {get{return EmberBranch==2?130:Balance.EmberRadius;}}
  public double EmberDamage {get{return (60+5*Wave)*(EmberBranch==1?1.5:1);}}
  public bool UpgradeEmber(int branch){if(ReinforcementPending||(Phase!=Phase.Preparation&&Phase!=Phase.Fighting)||EmberBranch!=0||branch<1||branch>2||Gold<100||ember.HasValue)return false;Gold-=100;EmberBranch=branch;return true;}
  public bool Ember(Point p){if(ReinforcementPending||Phase!=Phase.Fighting||EmberCD>0||double.IsNaN(p.X)||double.IsInfinity(p.X)||double.IsNaN(p.Y)||double.IsInfinity(p.Y)||p.X<0||p.X>1280||p.Y<0||p.Y>768)return false;EmberCD=Balance.EmberCooldown*Math.Pow(.7,CardCount(ReinforcementDraft.EmberCooldown));ember=p;return true;}
  void Hit(Enemy e,double raw,double pen,bool pierce=false,double bossMult=1,bool quiet=false,double damageScale=1,int source=-1){if(e.HP<=0)return;double armor=pierce?0:Math.Max(0,Balance.Enemies[e.Kind].Armor-pen);double initial=raw*(e.Kind==4?bossMult:1);double amount=armor>0?Math.Max(damageScale,initial-armor*damageScale):initial;if(e.Shield>0)amount*=.5;e.HP-=amount;if(!quiet)Cues.Add(new Cue("hit",e.Position,Math.Round(amount).ToString()){Source=source});if(e.Kind==4&&!e.Roared&&e.HP>0&&e.HP<=e.MaxHP*.5){e.Roared=true;foreach(var other in Enemies)if(other.HP>0&&Point.Distance(other.Position,e.Position)<=160)other.Shield=3;Cues.Add(new Cue("roar",e.Position,"보호 함성",160));}if(e.HP<=0){Kills++;if(source>=0&&source<4)KillKinds|=1<<source;int reward=e.Kind==4?40:3;reward=Reward(reward);Gold+=reward;Cues.Add(new Cue("death",e.Position,"+"+reward+"G"));if(e.Kind==3)foreach(var u in Units)if(Point.Distance(Field.Slots[u.Slot],e.Position)<=110)u.Stun=Math.Max(u.Stun,1.5);}}
  public void Step(double dt){Cues.Clear();if(Phase!=Phase.Fighting||ReinforcementPending||dt<=0||double.IsNaN(dt)||double.IsInfinity(dt))return;accumulator+=Math.Min(dt,.25);while(accumulator>=Balance.Tick-1e-9&&Phase==Phase.Fighting&&!ReinforcementPending){accumulator-=Balance.Tick;Tick();}if(Phase!=Phase.Fighting)accumulator=0;}
  void Tick(){PeakAlive=Math.Max(PeakAlive,Enemies.Count);if(Enemies.Count>=Balance.MaxAlive){Phase=Phase.Lost;EndCause="적 100마리 도달";return;}double dt=Balance.Tick;Time+=dt;WaveTime+=dt;EmberCD=Math.Max(0,EmberCD-dt);
   // An explicitly queued skill resolves before the next spawn, so a paused player
   // can clear space before the living-enemy limit is evaluated.
   if(ember.HasValue){Point p=ember.Value;ember=null;Cues.Add(new Cue("ember",p,"잔불",EmberRadius));foreach(var e in Enemies)if(Point.Distance(e.Position,p)<=EmberRadius)Hit(e,EmberDamage,0);Enemies.RemoveAll(e=>e.HP<=0);}
   while(queue.Count>0&&queue[0].At<=WaveTime){var q=queue[0];queue.RemoveAt(0);var d=Balance.Enemies[q.Kind];var e=new Enemy{Id=nextId++,Kind=q.Kind,Gate=q.Gate,HP=d.HP*q.Multiplier*(HardWaves?Balance.HardEnemyHealth:FastWaves?Balance.FastEnemyHealth:1),MaxHP=d.HP*q.Multiplier*(HardWaves?Balance.HardEnemyHealth:FastWaves?Balance.FastEnemyHealth:1),Speed=d.Speed*(.95+.1*Random()),Position=Path(Field.Gates[q.Gate])};Enemies.Add(e);PeakAlive=Math.Max(PeakAlive,Enemies.Count);if(Enemies.Count>=Balance.MaxAlive){Phase=Phase.Lost;EndCause="적 100마리 도달";return;}}
   foreach(var e in Enemies){if(e.HP<=0)continue;e.Shield=Math.Max(0,e.Shield-dt);e.Charge=Math.Max(0,e.Charge-dt);e.SlowTime=Math.Max(0,e.SlowTime-dt);e.FreezeTime=Math.Max(0,e.FreezeTime-dt);if(e.Burn>0){double burnTime=Math.Min(dt,e.Burn);e.Burn=Math.Max(0,e.Burn-dt);Hit(e,8*burnTime,0,false,1,true,burnTime,2);if(e.HP<=0)continue;}if(e.Kind==4){double before=e.NextCharge;e.NextCharge-=dt;if(before>1.5&&e.NextCharge<=1.5)Cues.Add(new Cue("warn",e.Position,"돌진 예고",55));if(e.NextCharge<=0){e.Charge=2;e.NextCharge+=10;}}e.Travel+=EffectiveSpeed(e)*dt;e.Position=Path(Field.Gates[e.Gate]+e.Travel);int laps=(int)(e.Travel/Field.Perimeter);if(laps>e.Laps)e.Laps=laps;}
   foreach(var u in Units){u.Cooldown=Math.Max(0,u.Cooldown-dt);if(u.Stun>0){u.Stun=Math.Max(0,u.Stun-dt);continue;}if(u.Cooldown>0||u.Kind==4)continue;double damage,interval;AttackStats(u,out damage,out interval);var d=Balance.Troops[u.Kind];Point p=Field.Slots[u.Slot];var targets=Enemies.FindAll(e=>e.HP>0&&Point.Distance(p,e.Position)<=AttackRange(u)&&Point.Distance(p,e.Position)>=d.MinRange);targets.Sort((a,b)=>CompareTargets(a,b,u.Priority));if(targets.Count==0)continue;u.Cooldown=interval;Cues.Add(new Cue("attack",p){Slot=u.Slot,Facing=targets[0].Position.X>=p.X?1:-1});if(u.Kind==0){for(int i=0;i<Math.Min((u.Branch==1?5:d.Targets)+CardCount(ReinforcementDraft.SwordTargets),targets.Count);i++)Hit(targets[i],damage*(FastWaves&&!HardWaves?Balance.Enemies[targets[i].Kind].MeleeTaken:1),d.Pen,u.Branch==2,1,false,1,0);}else{if(u.Kind==1||u.Kind==3)u.HitCount++;Shots.Add(new Shot{Kind=u.Kind,Target=targets[0].Id,Position=new Point(p.X,p.Y-12),Destination=targets[0].Position,Damage=damage,Pen=d.Pen,Speed=d.Speed,Radius=(u.Kind==2&&u.Branch==1?75:d.Radius)*(u.Kind==2?Math.Pow(1.25,CardCount(ReinforcementDraft.BombRadius)):1),BossMult=u.Kind==1&&u.Branch==2?1.5:1,Burning=u.Kind==2&&u.Branch==2,SlowPct=u.Kind==3?(u.Branch==1?.5:.35):0,Freezing=u.Kind==3&&u.Branch==2&&u.HitCount%5==0});}}
   for(int i=0;i<Shots.Count;){var s=Shots[i];var target=Enemies.Find(e=>e.Id==s.Target&&e.HP>0);if((s.Kind==1||s.Kind==3)&&target==null){Shots.RemoveAt(i);continue;}Point p=s.Kind==1||s.Kind==3?target.Position:s.Destination;double distance=Point.Distance(s.Position,p);if(distance<=s.Speed*dt){if(s.Kind==1||s.Kind==3){Hit(target,s.Damage*(FastWaves&&!HardWaves&&s.Kind==1&&Balance.ArcherFavored(target.Kind)?Balance.ArcherFavoredMult:1),s.Pen,false,s.BossMult,false,1,s.Kind);if(s.Kind==3)Cues.Add(new Cue("frost",target.Position));if(target.HP>0&&s.SlowPct>0){if(s.SlowPct>=target.SlowPct||target.SlowTime<=0)target.SlowPct=s.SlowPct;target.SlowTime=Math.Max(target.SlowTime,2);}if(target.HP>0&&s.Freezing)target.FreezeTime=1;}else{Cues.Add(new Cue("blast",p,"",s.Radius));foreach(var e in Enemies)if(e.HP>0&&Point.Distance(e.Position,p)<=s.Radius){Hit(e,s.Damage*(FastWaves&&!HardWaves?Balance.Enemies[e.Kind].BlastTaken:1),s.Pen,false,1,false,1,2);if(e.HP>0&&s.Burning)e.Burn=3;}}Shots.RemoveAt(i);}else {s.Position=new Point(s.Position.X+(p.X-s.Position.X)/distance*s.Speed*dt,s.Position.Y+(p.Y-s.Position.Y)/distance*s.Speed*dt);i++;}}
   Enemies.RemoveAll(e=>e.HP<=0);if(Enemies.Count>=Balance.MaxAlive){Phase=Phase.Lost;EndCause="적 100마리 도달";}else if(Wave==WaveCount&&queue.Count==0&&Enemies.Count==0){Shots.Clear();Gold+=Reward(40+5*Wave);Phase=Phase.Won;}else if(Wave<WaveCount&&queue.Count==0&&WaveTime>=WaveInterval){Gold+=Reward(40+5*Wave);BeginWave();}
  }
 }
 // This state is deliberately separate from Battle's spawn and combat RNG.
 [Serializable] public sealed class ReinforcementDraftData {
  public uint randomState;
  public int[] owned,offered;public int rerolls,offerRerolls;
 }
 public sealed class ReinforcementDraft {
  public const int CardCount=7,ChoicesPerOffer=3,MaxChoices=2;
  public const int ArcherHaste=0,BombRadius=1,SwordTargets=2,EmberCooldown=3,GoldIncome=4,Range=5,SwordDamage=6;
  uint randomState;readonly int[] owned;int[] offered;readonly int limit;
  public int Rerolls {get;private set;}public int OfferRerolls {get;private set;}
  public ReinforcementDraft(uint seed,int maxChoices=MaxChoices){if(maxChoices!=2&&maxChoices!=3)throw new ArgumentException("Invalid draft limit");limit=maxChoices;randomState=seed^0xA511E9B3u;owned=new int[CardCount];offered=new int[0];}
  ReinforcementDraft(uint state,int[] cards,int[] choices,int maxChoices){limit=maxChoices;randomState=state;owned=cards;offered=choices;}
  uint Next(){unchecked{uint t=(randomState+=0x6D2B79F5u);t=(t^(t>>15))*(t|1);t^=t+((t^(t>>7))*(t|61));return t^(t>>14);}}
  public int SelectedCount {get{int total=0;foreach(int count in owned)total+=count;return total;}}
  public bool HasOffer {get{return offered.Length!=0;}}
  public int Count(int card){if(card<0||card>=CardCount)throw new ArgumentOutOfRangeException("card");return owned[card];}
  public int[] Choices(){return (int[])offered.Clone();}
  public bool Offer(bool sword,bool archer,bool bomb){
   if(HasOffer||SelectedCount>=limit)return false;
   var pool=Pool(sword,archer,bomb);
   offered=new int[ChoicesPerOffer];for(int i=0;i<offered.Length;i++){int pick=(int)(Next()%(uint)pool.Count);offered[i]=pool[pick];pool.RemoveAt(pick);}return true;
  }
  List<int> Pool(bool sword,bool archer,bool bomb){
   var pool=new List<int>{EmberCooldown,GoldIncome,Range};
   if(archer)pool.Add(ArcherHaste);if(bomb)pool.Add(BombRadius);if(sword){pool.Add(SwordTargets);pool.Add(SwordDamage);}
   for(int card=0;card<CardCount;card++)if(owned[card]>0&&pool.Count>ChoicesPerOffer)pool.Remove(card);
   return pool;
  }
  List<int[]> Alternatives(bool sword,bool archer,bool bomb){var pool=Pool(sword,archer,bomb);pool.Sort();var options=new List<int[]>();for(int a=0;a<pool.Count-2;a++)for(int b=a+1;b<pool.Count-1;b++)for(int c=b+1;c<pool.Count;c++){int[] choice={pool[a],pool[b],pool[c]};if(Array.IndexOf(offered,choice[0])>=0&&Array.IndexOf(offered,choice[1])>=0&&Array.IndexOf(offered,choice[2])>=0)continue;options.Add(choice);}return options;}
  public bool CanReroll(bool sword,bool archer,bool bomb){return limit==3&&HasOffer&&OfferRerolls<2&&Alternatives(sword,archer,bomb).Count>0;}
  public bool Reroll(bool sword,bool archer,bool bomb){if(!CanReroll(sword,archer,bomb))return false;var options=Alternatives(sword,archer,bomb);offered=options[(int)(Next()%(uint)options.Count)];Rerolls++;OfferRerolls++;return true;}
  public bool Choose(int card){if(Array.IndexOf(offered,card)<0)return false;owned[card]++;offered=new int[0];OfferRerolls=0;return true;}
  public ReinforcementDraftData Export(){return new ReinforcementDraftData{rerolls=Rerolls,offerRerolls=OfferRerolls,randomState=randomState,owned=(int[])owned.Clone(),offered=Choices()};}
  public static bool IsAbsent(ReinforcementDraftData data){return data==null||(data.randomState==0&&data.rerolls==0&&data.offerRerolls==0&&(data.owned==null||data.owned.Length==0)&&(data.offered==null||data.offered.Length==0));}
  public static ReinforcementDraft Restore(ReinforcementDraftData data,int maxChoices=MaxChoices){if(maxChoices!=2&&maxChoices!=3)throw new ArgumentException("Invalid draft limit");
   if(data==null||data.owned==null||data.owned.Length!=CardCount||data.offered==null||(data.offered.Length!=0&&data.offered.Length!=ChoicesPerOffer))throw new ArgumentException("Invalid reinforcement draft");
   int total=0;foreach(int count in data.owned){if(count<0||count>maxChoices)throw new ArgumentException("Invalid reinforcement cards");total+=count;}if(total>maxChoices||(total==maxChoices&&data.offered.Length!=0))throw new ArgumentException("Invalid reinforcement count");
   var seen=new HashSet<int>();foreach(int card in data.offered)if(card<0||card>=CardCount||!seen.Add(card))throw new ArgumentException("Invalid reinforcement offer");
   if(data.offerRerolls<0||data.offerRerolls>2||data.rerolls<data.offerRerolls||data.rerolls>2*total+data.offerRerolls||data.rerolls>6||(data.offered.Length==0&&data.offerRerolls!=0)||(maxChoices==2&&(data.rerolls!=0||data.offerRerolls!=0)))throw new ArgumentException("Invalid reroll state");
   return new ReinforcementDraft(data.randomState,(int[])data.owned.Clone(),(int[])data.offered.Clone(),maxChoices){Rerolls=data.rerolls,OfferRerolls=data.offerRerolls};
  }
 }
}
