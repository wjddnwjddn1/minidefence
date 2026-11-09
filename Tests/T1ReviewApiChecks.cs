// Claude independent review checks for T1 that need the T1 API. Does NOT compile on `6d78d7f`.
// Compile together with Tests/T1ReviewChecks.cs (shares Review helpers and the PlayerPrefs stub).
// Every assumed T1 name is confined to class T1Api below: when the real T1 commit differs,
// adapt T1Api only and keep the assertions unless the contract itself changed.
// C# 5 only. Exit codes as in T1ReviewChecks (0 pass, 1 [보존] fail, 2 only [T1요구] fail).
//
// Assumed API (Docs/TERRA-IMPLEMENTATION-PLAN.md §2, §5; Docs/CLAUDE-REVIEW.md 31차):
//  A1 enum BattleMode { Unspecified=0, Normal=1, Reinforcement=2 } and Battle.Mode (get).
//  A2 Battle.CreateForRules(uint seed,int field,int rulesVersion): isolated non-default creation.
//     Rules 4 normal mode starts with an empty card draft in Battle.Reinforcements.
//  A3 Draft accessors keep today's names: Reinforcements, ReinforcementPending, ChooseReinforcement,
//     ReinforcementOffered, SelectedCount, Count(card), Choices(). GoldIncome stays x1.15 per card.
//  A4 v4 reinforcement = CreateForRules(...,4) + EnableReinforcements(), same meaning as v3.
//  A5 The win-record block of BattleView.cs:64 is extracted to a Unity-free helper
//     VictoryRecords.Apply(Battle) (any return type) using ProgressStore only.
//  A6 T1 does not change v4 wave data or density versus v3 (tuning belongs to T4).
using System;
using System.Collections.Generic;
using EmberBastion;
static class T1Api {
 public static bool IsNormal(Battle b){return b.Mode==BattleMode.Normal;}
 public static bool IsReinforcement(Battle b){return b.Mode==BattleMode.Reinforcement;}
 public static Battle Create(uint seed,int field,int rules){return Battle.CreateForRules(seed,field,rules);}
 public static Battle NormalCards(uint seed,int field,int difficulty){var b=Create(seed,field,4);Review.Check(b.SetDifficulty(difficulty),"v4 normal difficulty "+difficulty);return b;}
 public static Battle Reinforcement(uint seed,int field,int rules){var b=Create(seed,field,rules);Review.Check(b.EnableReinforcements(),"enable reinforcement v"+rules);return b;}
 public static void RecordVictory(Battle b){VictoryRecords.Apply(b);}
}
class T1ReviewApiChecks {
 static void Check(bool ok,string m){Review.Check(ok,m);}
 static readonly string[] Names={"기본","도전","극한"};
 static Battle Won(Battle b,int peak){b.Wave=b.WaveCount;b.Phase=Phase.Won;b.PeakAlive=peak;return b;}
 static void RunToEnd(Battle b,List<int> offers){b.ReinforcementOffered=x=>offers.Add(x.Wave);for(int guard=0;guard<600000&&b.Phase==Phase.Fighting;guard++){if(b.ReinforcementPending){Check(b.ChooseReinforcement(b.Reinforcements.Choices()[0]),"choose");continue;}b.Step(1.0/60);}}
 static int Main(){
  // ---- A. 모드 분리 ----
  for(int d=0;d<3;d++){int diff=d;
   Review.Run("T1요구","A1 v4 일반 카드("+Names[diff]+"): 일반 모드, 드래프트 있음, 밀도는 같은 난도 v3 일반과 동일",()=>{
    for(int f=0;f<Balance.Fields.Length;f++){var b=T1Api.NormalCards(1,f,diff);Check(T1Api.IsNormal(b)&&!T1Api.IsReinforcement(b),"normal mode field "+f);Check(b.Reinforcements!=null&&b.Reinforcements.SelectedCount==0,"empty draft");
     Check(b.FastWaves==(diff>0)&&b.HardWaves==(diff==2),"difficulty flags");var plain=new Battle(1,f);plain.SetDifficulty(diff);Check(b.WaveCount==plain.WaveCount,"wave count");
     foreach(var wave in b.Field.Waves)foreach(var g in wave){Check(b.SpawnCount(g)==plain.SpawnCount(g),"no 1.15 count field "+f);Check(Math.Abs(b.SpawnGap(g)-plain.SpawnGap(g))<1e-9,"no 1.15 gap field "+f);}}
   });}
  Review.Run("T1요구","A2 v4 일반 카드: 준비 단계에서 난도 0·1·2 변경 가능",()=>{var b=T1Api.NormalCards(2,0,0);Check(b.SetDifficulty(2)&&b.HardWaves,"to hard");Check(b.SetDifficulty(1)&&b.FastWaves&&!b.HardWaves,"to challenge");Check(b.SetDifficulty(0)&&!b.FastWaves,"to normal");Check(T1Api.IsNormal(b)&&b.Reinforcements!=null,"still normal cards");});
  Review.Run("T1요구","A3 v4 일반 카드: 준비·전투 저장 왕복에서 모드·난도·드래프트 유지 (극한 포함)",()=>{
   for(int d=0;d<3;d++){var b=T1Api.NormalCards(4,1,d);b.Place(0,0);var prep=Battle.RestorePreparation(b.SavePreparation());Check(T1Api.IsNormal(prep)&&prep.Reinforcements!=null&&prep.HardWaves==b.HardWaves&&prep.FastWaves==b.FastWaves&&prep.RulesVersion==4,"prep d"+d);
    Check(b.Start(),"start");for(int i=0;i<300;i++)b.Step(1.0/60);var back=Battle.ImportCombat(b.ExportCombat());Check(T1Api.IsNormal(back)&&back.Reinforcements!=null&&back.HardWaves==b.HardWaves&&back.RulesVersion==4,"combat d"+d);
    var copy=b.CopyCombat();Check(T1Api.IsNormal(copy)&&copy.Reinforcements!=null,"copy d"+d);}
  });
  Review.Run("보존","A4 v0~3 저장은 드래프트 유무로 원래 모드 복원 (새 필드 기본값에 끌려가지 않음)",()=>{
   var r=new Battle(42,0);r.EnableReinforcements();r.Place(0,0);var rp=Battle.RestorePreparation(r.SavePreparation());Check(T1Api.IsReinforcement(rp)&&rp.RulesVersion==3,"v3 reinforcement prep");
   Check(r.Start(),"start");for(int i=0;i<300;i++)r.Step(1.0/60);var rc=Battle.ImportCombat(r.ExportCombat());Check(T1Api.IsReinforcement(rc),"v3 reinforcement combat");
   for(int v=0;v<=3;v++){var p=T1Api.Create(5,1,v);p.Place(0,0);var pp=Battle.RestorePreparation(p.SavePreparation());Check(T1Api.IsNormal(pp)&&pp.Reinforcements==null&&pp.RulesVersion==v,"v"+v+" plain prep");
    Check(p.Start(),"start v"+v);for(int i=0;i<300;i++)p.Step(1.0/60);var pc=Battle.ImportCombat(p.ExportCombat());Check(T1Api.IsNormal(pc)&&pc.Reinforcements==null&&pc.RulesVersion==v,"v"+v+" plain combat");}
  });
  Review.Run("보존","A5 v4 증원: 증원 모드, 밀도 1.15, 난도 잠금, Wave 2·5에서 최대 2장",()=>{
   var r=T1Api.Reinforcement(42,0,4);Check(T1Api.IsReinforcement(r),"mode");Check(!r.SetDifficulty(0)&&!r.SetDifficulty(2),"locked");
   var plain=new Battle(42,0);plain.SetDifficulty(1);foreach(var wave in r.Field.Waves)foreach(var g in wave)Check(r.SpawnCount(g)==(g.Kind==4?plain.SpawnCount(g):(int)Math.Round(plain.SpawnCount(g)*1.15)),"1.15");
   Review.Army(r);Check(r.Start(),"start");var offers=new List<int>();RunToEnd(r,offers);Check(offers.Count==2&&offers[0]==2&&offers[1]==5,"offers "+string.Join(",",offers.ConvertAll(o=>o.ToString()).ToArray()));Check(r.Reinforcements.SelectedCount==2,"two cards");
  });
  // ---- B. 보상 1회 지급 (v4 일반 3·6·9) ----
  foreach(int f in new[]{0,3})for(int d=0;d<3;d++){int field=f,diff=d;
   Review.Run("T1요구","B v4 일반 "+Names[diff]+" 전장 "+field+": 3·6·9 전 보상 1회 (조기·자동, 대기·저장·복원·선택)",()=>{
    foreach(bool early in new[]{true,false}){var b=T1Api.NormalCards(7+(uint)field,field,diff);Review.Army(b);Check(b.Start(),"start");
     Review.AdvanceTo(b,1);Review.Transition(b,early,false,"wave 2 start");
     Review.AdvanceTo(b,2);Review.Transition(b,early,true,"before 3");
     Review.AdvanceTo(b,5);Review.Transition(b,early,true,"before 6");
     Review.AdvanceTo(b,8);Review.Transition(b,early,true,"before 9");
     Review.AdvanceTo(b,9);Review.Transition(b,early,false,"wave 10 (no offer)");}
   });}
  // ---- C. 카드 3장 저장·복원 ----
  Review.Run("T1요구","C1 세 번째 카드 직후 WaveStarted 체크포인트·저장·복원 예외 없음",()=>{
   var b=T1Api.NormalCards(11,0,0);Review.Army(b);Battle checkpoint=null;b.WaveStarted=x=>{checkpoint=x.CopyCombat();};Check(b.Start(),"start");
   Review.AdvanceTo(b,9);Check(b.Reinforcements.SelectedCount==3,"three cards by wave 9, got "+b.Reinforcements.SelectedCount);
   Check(checkpoint!=null&&checkpoint.Wave==9&&checkpoint.Reinforcements.SelectedCount==3,"checkpoint after third choice");
   var back=Battle.ImportCombat(b.ExportCombat());Check(back.Reinforcements.SelectedCount==3&&T1Api.IsNormal(back),"roundtrip");
   for(int c=0;c<ReinforcementDraft.CardCount;c++)Check(back.Reinforcements.Count(c)==b.Reinforcements.Count(c),"card "+c);
   var cp=Battle.ImportCombat(checkpoint.ExportCombat());Check(cp.Reinforcements.SelectedCount==3,"checkpoint roundtrip");
  });
  Review.Run("T1요구","C2 v4 일반: 끝까지 진행해도 제시는 Wave 2·5·8 세 번뿐",()=>{var b=T1Api.NormalCards(13,1,0);Review.Army(b);Check(b.Start(),"start");var offers=new List<int>();RunToEnd(b,offers);Check(b.Phase==Phase.Won,"won ("+b.EndCause+")");Check(offers.Count==3&&offers[0]==2&&offers[1]==5&&offers[2]==8,"offers "+string.Join(",",offers.ConvertAll(o=>o.ToString()).ToArray()));Check(b.Reinforcements.SelectedCount==3,"three");});
  Review.Run("T1요구","C3 v4 일반 저장 변조 거부: 4장, 일정과 다른 선택 수, 경계 밖 제시",()=>{
   var b=T1Api.NormalCards(11,0,0);Review.Army(b);Check(b.Start(),"start");Review.AdvanceTo(b,9);
   var four=b.ExportCombat();four.reinforcement.owned[ReinforcementDraft.Range]++;Review.Throws<ArgumentException>(()=>Battle.ImportCombat(four),"four cards");
   var two=b.ExportCombat();int k=Array.FindIndex(two.reinforcement.owned,n=>n>0);two.reinforcement.owned[k]--;Review.Throws<ArgumentException>(()=>Battle.ImportCombat(two),"two cards at wave 9");
   var off=b.ExportCombat();off.wave=4;off.pending=new SpawnSave[0];off.reinforcement.owned=new int[ReinforcementDraft.CardCount];off.reinforcement.owned[3]=1;off.reinforcement.offered=new[]{4,5,6};Review.Throws<ArgumentException>(()=>Battle.ImportCombat(off),"offer at wave 4");
  });
  // ---- D. 승리 기록·보급 ----
  for(int d=0;d<3;d++){int diff=d;
   Review.Run("T1요구","D1 v4 일반 카드 "+Names[diff]+" 승리: 전역 완료·규칙4 클리어·최고·배지, 증원 기록 없음, v3 불변",()=>{
    UnityEngine.PlayerPrefs.DeleteAll();bool fast=diff>0,hard=diff==2;var b=Won(T1Api.NormalCards(1,1,diff),40);T1Api.RecordVictory(b);
    Check(ProgressStore.Cleared(1,fast,hard),"global clear (unlock + supply entitlement)");Check(ProgressStore.ClearedForRules(1,fast,hard,4),"rules 4 clear");
    Check(!ProgressStore.ClearedForRules(1,fast,hard,3),"v3 clear untouched");Check(ProgressStore.BestPeak(1,fast,hard,0,4)==40,"rules 4 best");
    Check(!ProgressStore.BestPeak(1,fast,hard,0,3).HasValue,"v3 best untouched");Check(!ProgressStore.ReinforcementBest(1,0,4).HasValue&&!ProgressStore.ReinforcementBest(1,0,3).HasValue,"no reinforcement record");
   });}
  foreach(int rv in new[]{3,4}){int v=rv;
  Review.Run(v==3?"보존":"T1요구","D2 v"+v+" 증원 승리: 증원 최고 기록만, 일반 완료·보급 자격 없음",()=>{
   {UnityEngine.PlayerPrefs.DeleteAll();var r=v==3?new Battle(1,0):T1Api.Create(1,0,4);Check(r.EnableReinforcements(),"enable");T1Api.RecordVictory(Won(r,45));
    Check(ProgressStore.ReinforcementBest(0,0,v)==45,"reinforcement best v"+v);Check(!ProgressStore.Cleared(0)&&!ProgressStore.Cleared(0,true)&&!ProgressStore.ClearedForRules(0,true,false,v),"no normal clear v"+v);}
  });}
  Review.Run("보존","D3 v3 일반 승리는 기존 키 그대로",()=>{UnityEngine.PlayerPrefs.DeleteAll();T1Api.RecordVictory(Won(new Battle(1,2),50));Check(ProgressStore.Cleared(2)&&ProgressStore.ClearedForRules(2,false,false,3)&&ProgressStore.BestPeak(2,false,false,0,3)==50,"v3 records");Check(!ProgressStore.ClearedForRules(2,false,false,4),"no v4 record");});
  Review.Run("T1요구","D4 v4 일반 승리로 보급 자격 증가, 4전장이어도 60G, 같은 전장 재승리는 중복 없음",()=>{
   UnityEngine.PlayerPrefs.DeleteAll();int[] expect={20,40,60,60};
   for(int f=0;f<Balance.Fields.Length;f++){T1Api.RecordVictory(Won(T1Api.NormalCards(1,f,0),40));int completed=0;for(int i=0;i<Balance.Fields.Length;i++)if(ProgressStore.Cleared(i)||ProgressStore.Cleared(i,true)||ProgressStore.Cleared(i,true,true))completed++;
    Check(Math.Min(completed,Balance.MaxSupplyFields)*20==expect[f],"supply after field "+f);}
   T1Api.RecordVictory(Won(T1Api.NormalCards(1,0,1),40));T1Api.RecordVictory(Won(new Battle(1,0),40));int again=0;for(int i=0;i<Balance.Fields.Length;i++)if(ProgressStore.Cleared(i)||ProgressStore.Cleared(i,true)||ProgressStore.Cleared(i,true,true))again++;Check(again==4,"no duplicate entitlement");
   var b=T1Api.NormalCards(2,0,0);Check(b.GrantSupply(60)&&!b.GrantSupply(20),"grant once");
  });
  return Review.Finish();
 }
}
