// Claude independent review checks for T1 (mode split) — compiles against the CURRENT API.
// Review 31 in Docs/CLAUDE-REVIEW.md. C# 5 only (Windows .NET Framework csc).
//
// [보존]   must pass now AND after T1 (legacy v3 reinforcement, v0-3 saves/records, supply cap).
// [T1요구] T1 requirements expressible with today's API; expected to FAIL until T1 lands.
// Exit: 0 all pass, 1 any [보존] failure (regression), 2 only [T1요구] failures.
// Runs on Mac: python3 Tests/run-t1-review.py   Windows: Tests/Review-T1Checks.ps1
using System;
using System.Collections.Generic;
using EmberBastion;
namespace UnityEngine {
 public static class PlayerPrefs {
  static readonly Dictionary<string,int> data=new Dictionary<string,int>();
  public static int GetInt(string key,int fallback){int v;return data.TryGetValue(key,out v)?v:fallback;}
  public static int GetInt(string key){return GetInt(key,0);}
  public static void SetInt(string key,int value){data[key]=value;}
  public static bool HasKey(string key){return data.ContainsKey(key);}
  public static void DeleteKey(string key){data.Remove(key);}
  public static void Save(){}
  public static void DeleteAll(){data.Clear();}
 }
}
static class Review {
 public static int Pass,FailKeep,FailT1;
 public static void Run(string group,string name,Action body){
  try{body();Pass++;Console.WriteLine("PASS ["+group+"] "+name);}
  catch(Exception ex){if(group=="보존")FailKeep++;else FailT1++;Console.WriteLine("FAIL ["+group+"] "+name+" :: "+ex.GetType().Name+": "+ex.Message);}
 }
 public static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 public static void Throws<T>(Action body,string message) where T:Exception{try{body();}catch(T){return;}throw new Exception("expected "+typeof(T).Name+": "+message);}
 public static int Finish(){
  Console.WriteLine("SUMMARY pass "+Pass+", 보존 fail "+FailKeep+", T1요구 fail "+FailT1);
  return FailKeep>0?1:FailT1>0?2:0;
 }
 // Mirrors Battle.Reward (private): round(amount * 1.15^GoldIncome cards).
 public static int Reward(Battle b,int amount){int cards=b.Reinforcements==null?0:b.Reinforcements.Count(ReinforcementDraft.GoldIncome);return (int)Math.Round(amount*Math.Pow(1.15,cards));}
 public static void Army(Battle b){b.Gold=50000;for(int i=0;i<b.Field.Slots.Length;i++){Check(b.Place(i%3,i),"place "+i);Check(b.Upgrade(i),"upgrade "+i);Check(b.UpgradeBranch(i,1),"branch "+i);}}
 // Advance until Wave==wave and early call is possible (the state right before the wave+1 start),
 // choosing the first offered card at any boundary on the way.
 public static void AdvanceTo(Battle b,int wave){
  for(int guard=0;guard<400000;guard++){
   if(b.Phase!=Phase.Fighting)throw new Exception("battle ended at wave "+b.Wave+" ("+b.EndCause+")");
   if(b.ReinforcementPending){Check(b.ChooseReinforcement(b.Reinforcements.Choices()[0]),"choose on the way");continue;}
   // Flow checks only: drop a stacked crowd so repeated early calls cannot end the battle (r27 marsh base doubled).
   if(b.Enemies.Count>80)b.Enemies.Clear();
   if(b.CanCallEarly()){if(b.Wave==wave)return;if(b.Wave<wave){Check(b.CallEarly(),"call early");continue;}}
   if(b.Wave>wave)throw new Exception("passed wave "+wave);
   b.Step(1.0/60);
  }
  throw new Exception("did not reach wave "+wave);
 }
 // One transition from `b` (state of AdvanceTo). early=CallEarly path, else automatic timer path.
 // Asserts the wave reward is paid exactly once across offer, pause, save/restore and choice.
 public static void Transition(Battle b,bool early,bool expectOffer,string label){
  var x=b.CopyCombat();int w=x.Wave;int expected;if(expectOffer)PrepareInputs(x,label);
  if(early){expected=x.Gold+Reward(x,40+5*w)+x.EarlyBonus();Check(x.CallEarly(),label+": call early");}
  else{x.Enemies.Clear();x.Shots.Clear();expected=x.Gold+Reward(x,40+5*w);x.WaveTime=Math.Max(x.WaveTime,x.WaveInterval);x.Step(Balance.Tick);}
  if(!expectOffer){Check(!x.ReinforcementPending&&x.Wave==w+1,label+": plain wave start");Check(x.Gold==expected,label+": reward "+(x.Gold-(expected-Reward(x,40+5*w)))+" gold delta, expected once");return;}
  Check(x.ReinforcementPending&&x.Wave==w,label+": offer before wave "+(w+1)+" (pending="+x.ReinforcementPending+", wave="+x.Wave+")");
  Check(x.Gold==expected,label+": reward once before offer (gold "+x.Gold+" expected "+expected+")");
  double t=x.Time;x.Step(1.0);x.Step(.25);Check(x.Gold==expected&&x.Time==t&&!x.CanCallEarly()&&!x.CallEarly(),label+": paused while choosing");
  Frozen(x,label);InputsBlocked(x,label);Check(x.Gold==expected,label+": blocked inputs changed gold");
  var restored=Battle.ImportCombat(x.ExportCombat());Check(restored.ReinforcementPending&&restored.Gold==expected,label+": pending restored");
  int card=x.Reinforcements.Choices()[0];Check(SameInts(x.Reinforcements.Choices(),restored.Reinforcements.Choices()),label+": same offer after restore");
  Check(x.ChooseReinforcement(card)&&restored.ChooseReinforcement(card),label+": choose");
  Check(!x.ChooseReinforcement(card),label+": second choose rejected");
  Check(x.Wave==w+1&&restored.Wave==w+1&&x.Gold==expected&&restored.Gold==expected,label+": choice starts one wave, no extra reward");
  for(int i=0;i<600&&x.Phase==Phase.Fighting;i++){x.Step(1.0/60);restored.Step(1.0/60);}
  Check(x.Gold==restored.Gold&&x.Kills==restored.Kills&&x.Time==restored.Time&&x.Enemies.Count==restored.Enemies.Count&&x.Wave==restored.Wave,label+": restored run identical");
 }
 // Leaves one empty slot, one Lv1 and one Lv2 unit so that every input below would succeed if not blocked.
 public static void PrepareInputs(Battle x,string label){int n=x.Field.Slots.Length;Check(n>=6,label+": slots");
  for(int s=n-3;s<n;s++)if(x.At(s)!=null)Check(x.Sell(s),label+": sell fixture");
  Check(x.Place(0,n-1),label+": lv1 fixture");Check(x.Place(0,n-2)&&x.Upgrade(n-2),label+": lv2 fixture");x.Gold=Math.Max(x.Gold,5000);}
 public static void InputsBlocked(Battle x,string label){int n=x.Field.Slots.Length,units=x.Units.Count,gold=x.Gold;var archer=x.Units.Find(u=>u.Kind==1);
  Check(!x.Place(1,n-3),label+": place blocked");Check(!x.Upgrade(n-1),label+": upgrade blocked");Check(!x.UpgradeBranch(n-2,1),label+": branch blocked");
  Check(!x.Sell(0),label+": sell blocked");Check(archer==null||!x.SetPriority(archer.Slot,(archer.Priority+1)%4),label+": priority blocked");
  Check(!x.UpgradeEmber(1),label+": ember upgrade blocked");Check(!x.Ember(new Point(640,384)),label+": ember blocked");Check(!x.CallEarly(),label+": early call blocked");
  Check(x.Units.Count==units&&x.Gold==gold&&x.EmberBranch==0&&x.At(n-1).Level==1&&x.At(n-2).Level==2,label+": state unchanged by blocked inputs");}
 // Enemies, projectiles, unit timers, ember cooldown and wave timer must not move while choosing.
 public static void Frozen(Battle x,string label){double cd=x.EmberCD;x.EmberCD=Math.Max(cd,10);var before=Snapshot(x);x.Step(.25);x.Step(1.0);bool same=before==Snapshot(x);x.EmberCD=cd;Check(same,label+": world moved while choosing (enemies, shots, unit timers, ember cooldown or wave timer)");}
 static string Snapshot(Battle x){var sb=new System.Text.StringBuilder();sb.Append(x.Time).Append('|').Append(x.WaveTime).Append('|').Append(x.EmberCD).Append('|').Append(x.Pending).Append('|').Append(x.Kills).Append('|');
  foreach(var e in x.Enemies)sb.Append(e.Id).Append(',').Append(e.HP).Append(',').Append(e.Travel).Append(',').Append(e.Shield).Append(',').Append(e.Charge).Append(',').Append(e.NextCharge).Append(',').Append(e.Burn).Append(',').Append(e.SlowTime).Append(',').Append(e.FreezeTime).Append(';');
  foreach(var sh in x.Shots)sb.Append(sh.Position.X).Append(',').Append(sh.Position.Y).Append(';');foreach(var u in x.Units)sb.Append(u.Slot).Append(',').Append(u.Cooldown).Append(',').Append(u.Stun).Append(';');return sb.ToString();}
 // Wave-start checkpoints (as BattleView.WaveStarted takes them) must carry the cards chosen for that wave.
 public static Dictionary<int,Battle> HookCheckpoints(Battle b){var map=new Dictionary<int,Battle>();b.WaveStarted=x=>{map[x.Wave]=x.CopyCombat();};return map;}
 public static void CheckpointCards(Dictionary<int,Battle> cps,int[] boundaries,string label){
  foreach(int start in boundaries){Check(cps.ContainsKey(start)&&cps.ContainsKey(start-1),label+": checkpoints around wave "+start);
   int before=cps[start-1].Reinforcements.SelectedCount,after=cps[start].Reinforcements.SelectedCount;Check(after==before+1,label+": wave "+start+" checkpoint is post-choice ("+before+"→"+after+")");
   var back=Battle.ImportCombat(cps[start].ExportCombat());Check(back.Reinforcements.SelectedCount==after&&back.Gold==cps[start].Gold&&back.Wave==start,label+": wave "+start+" checkpoint restores cards and gold");
   var old=Battle.ImportCombat(cps[start-1].ExportCombat());Check(old.Reinforcements.SelectedCount==before,label+": older checkpoint keeps older cards");}}
 public static bool SameInts(int[] a,int[] b){if(a.Length!=b.Length)return false;for(int i=0;i<a.Length;i++)if(a[i]!=b[i])return false;return true;}
}
class T1ReviewChecks {
 static void Check(bool ok,string m){Review.Check(ok,m);}
 static int Main(){
  // ---- 보존: v3 reinforcement mode meaning ----
  Review.Run("보존","v3 증원 밀도 1.15·간격 ÷1.15 (대장 제외), 네 전장",()=>{
   for(int f=0;f<Balance.Fields.Length;f++){var plain=new Battle(1,f);Check(plain.SetDifficulty(1),"plain challenge");var r=new Battle(1,f);Check(r.EnableReinforcements(),"enable");
    foreach(var wave in r.Field.Waves)foreach(var g in wave){int c=plain.SpawnCount(g);Check(r.SpawnCount(g)==(g.Kind==4?c:(int)Math.Round(c*1.15)),"count field "+f);double gap=plain.SpawnGap(g);Check(Math.Abs(r.SpawnGap(g)-(g.Kind==4?gap:gap/1.15))<1e-9,"gap field "+f);}}
  });
  Review.Run("보존","v3 증원은 난도 잠금, 도전 고정",()=>{var r=new Battle(3,0);Check(r.EnableReinforcements()&&r.FastWaves&&!r.HardWaves,"enable");Check(!r.SetDifficulty(0)&&!r.SetDifficulty(2),"locked");});
  Review.Run("보존","v3 증원 카드는 Wave 2·5에서만 제시, 최대 2장",()=>{
   var r=new Battle(42,0);r.EnableReinforcements();Review.Army(r);Check(r.Start(),"start");var offers=new List<int>();r.ReinforcementOffered=x=>offers.Add(x.Wave);
   for(int guard=0;guard<400000&&r.Phase==Phase.Fighting;guard++){if(r.ReinforcementPending){r.ChooseReinforcement(r.Reinforcements.Choices()[0]);continue;}if(r.CanCallEarly()){r.CallEarly();continue;}r.Step(1.0/60);}
   Check(offers.Count==2&&offers[0]==2&&offers[1]==5,"offers at "+string.Join(",",offers.ConvertAll(o=>o.ToString()).ToArray()));Check(r.Reinforcements.SelectedCount==2,"two cards");
  });
  Review.Run("보존","증원 드래프트 3장 데이터는 거부 (구 프로필 최대 2)",()=>{
   var d=new ReinforcementDraft(5);d.Offer(true,true,true);d.Choose(d.Choices()[0]);var data=d.Export();data.owned[ReinforcementDraft.Range]+=2;
   Review.Throws<ArgumentException>(()=>ReinforcementDraft.Restore(data),"three owned cards");
  });
  Review.Run("보존","v3 증원 전투 저장: 3장·잘못된 시점 제시는 거부",()=>{
   var r=new Battle(42,0);r.EnableReinforcements();Review.Army(r);Check(r.Start(),"start");Review.AdvanceTo(r,6);
   var ok=r.ExportCombat();Check(Battle.ImportCombat(ok).Reinforcements.SelectedCount==2,"valid save");
   var three=r.ExportCombat();three.reinforcement.owned[ReinforcementDraft.Range]++;Review.Throws<ArgumentException>(()=>Battle.ImportCombat(three),"three cards");
   var late=r.ExportCombat();late.reinforcement.owned=new int[ReinforcementDraft.CardCount];late.reinforcement.owned[0]=1;late.reinforcement.offered=new[]{3,4,5};late.wave=4;late.pending=new SpawnSave[0];
   Review.Throws<ArgumentException>(()=>Battle.ImportCombat(late),"offer at wave 4 (not a boundary)");
  });
  Review.Run("보존","Unity 빈 드래프트 객체는 일반 전투로 복원",()=>{
   var p=new Battle(17,0);p.Place(0,0);Check(p.Start(),"start");var data=p.ExportCombat();data.reinforcement=new ReinforcementDraftData{randomState=0,owned=new int[0],offered=new int[0]};
   var back=Battle.ImportCombat(data);Check(back.Reinforcements==null&&!back.FastWaves,"plain");
   var prep=new Battle(17,1).SavePreparation();prep.reinforcement=new ReinforcementDraftData{randomState=0,owned=new int[0],offered=new int[0]};Check(Battle.RestorePreparation(prep).Reinforcements==null,"plain preparation");
  });
  // ---- 보존: reward exactly once at v3 reinforcement boundaries ----
  for(int f=0;f<Balance.Fields.Length;f++){int field=f;
   Review.Run("보존","v3 증원 3·6웨이브 전 보상 1회 (조기·자동, 대기·저장·복원·선택) 전장 "+field,()=>{
    foreach(bool early in new[]{true,false}){var r=new Battle(42+(uint)field,field);r.EnableReinforcements();Review.Army(r);Check(r.Start(),"start");
     Review.AdvanceTo(r,1);Review.Transition(r,early,false,"wave 2 start");
     Review.AdvanceTo(r,2);Review.Transition(r,early,true,"before 3");
     Review.AdvanceTo(r,5);Review.Transition(r,early,true,"before 6");
     Review.AdvanceTo(r,8);Review.Transition(r,early,false,"wave 9 (no v3 offer)");}
   });}
  Review.Run("보존","v3 증원 체크포인트: 3·6웨이브 시작 체크포인트는 선택 후 상태, 이전 체크포인트는 이전 카드",()=>{
   var r=new Battle(21,2);r.EnableReinforcements();Review.Army(r);var cps=Review.HookCheckpoints(r);Check(r.Start(),"start");Review.AdvanceTo(r,7);Review.CheckpointCards(cps,new[]{3,6},"v3");
  });
  Review.Run("보존","v3 증원 준비 저장은 wave 0만 허용 (준비 단계 제시로 막히는 상태 차단)",()=>{
   var r=new Battle(3,0);r.EnableReinforcements();r.Place(0,0);var data=r.SavePreparation();foreach(int w in new[]{2,5}){data.wave=w;Review.Throws<ArgumentException>(()=>Battle.RestorePreparation(data),"prep wave "+w);}
  });
  Review.Run("보존","v3 일반 전투는 카드 없이 보상 1회",()=>{
   foreach(bool early in new[]{true,false}){var p=new Battle(9,1);Review.Army(p);Check(p.Start(),"start");for(int w=1;w<=9;w++){Review.AdvanceTo(p,w);Review.Transition(p,early,false,"plain wave "+(w+1));}}
  });
  // ---- 보존: records and supply ----
  Review.Run("보존","v0~3 규칙별 클리어 키 분리와 v4 이상 거부(현재 계약)",()=>{
   UnityEngine.PlayerPrefs.DeleteAll();
   Check(ProgressStore.RecordClearForRules(0,false,false,3)&&ProgressStore.ClearedForRules(0,false,false,3),"v3 clear");
   Check(!ProgressStore.ClearedForRules(0,false,false,2)&&!ProgressStore.ClearedForRules(0,true,false,3),"v2 and challenge separate");
   Check(ProgressStore.Record(0)&&ProgressStore.Cleared(0),"global unlock");
  });
  Review.Run("보존","보급: 4전장 완료해도 60G, 전투당 1회",()=>{
   UnityEngine.PlayerPrefs.DeleteAll();for(int f=0;f<Balance.Fields.Length;f++)ProgressStore.Record(f,f%2==1,false);
   int completed=0;for(int i=0;i<Balance.Fields.Length;i++)if(ProgressStore.Cleared(i)||ProgressStore.Cleared(i,true)||ProgressStore.Cleared(i,true,true))completed++;
   int amount=Math.Min(completed,Balance.MaxSupplyFields)*20;Check(amount==60,"amount "+amount);
   var b=new Battle(1,3);Check(b.GrantSupply(amount)&&b.Gold==b.Field.StartGold+60,"grant");Check(!b.GrantSupply(20),"second grant");
   var r=new Battle(1,0);r.EnableReinforcements();Check(r.GrantSupply(60),"reinforcement grant");
  });
  // ---- T1요구: ProgressStore must accept rules 4 without touching v3 ----
  Review.Run("T1요구","ProgressStore 규칙 4 클리어 기록, v3와 분리",()=>{
   UnityEngine.PlayerPrefs.DeleteAll();
   Check(ProgressStore.RecordClearForRules(1,false,false,4),"first v4 clear");Check(ProgressStore.ClearedForRules(1,false,false,4),"v4 cleared");
   Check(!ProgressStore.ClearedForRules(1,false,false,3),"v3 untouched");Check(!ProgressStore.RecordClearForRules(1,false,false,4),"repeat");
  });
  Review.Run("T1요구","ProgressStore 규칙 4 최고 기록·배지, v3와 분리",()=>{
   UnityEngine.PlayerPrefs.DeleteAll();
   Check(ProgressStore.RecordBest(2,true,true,70,20,4)&&ProgressStore.BestPeak(2,true,true,20,4)==70,"v4 best");
   Check(!ProgressStore.BestPeak(2,true,true,20,3).HasValue,"v3 best untouched");
   ProgressStore.RecordBadges(2,true,true,6,4);Check(ProgressStore.HasBadge(2,true,true,2,4)&&ProgressStore.HasBadge(2,true,true,4,4),"v4 badges");
   Check(!ProgressStore.HasBadge(2,true,true,2,3),"v3 badge untouched");
  });
  Review.Run("T1요구","ProgressStore 규칙 4 증원 최고 기록, v3와 분리",()=>{
   UnityEngine.PlayerPrefs.DeleteAll();
   Check(ProgressStore.RecordReinforcementBest(0,55,0,4)&&ProgressStore.ReinforcementBest(0,0,4)==55,"v4 reinforcement best");
   Check(!ProgressStore.ReinforcementBest(0,0,3).HasValue,"v3 untouched");
  });
  return Review.Finish();
 }
}
