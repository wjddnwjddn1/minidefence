using System;
using System.Collections.Generic;
using EmberBastion;
// r37 (Claude): the roguelite run. Checks that other modes are untouched, the boon and threat math, warmth loss by laps,
// save/restore and corruption refusal for expedition battles, and the run's state machine from start to both endings.
class ExpeditionChecks {
 static int checks;
 static void Check(bool ok,string what){checks++;if(!ok)throw new Exception(what);}
 static bool Throws(Action a){try{a();}catch(ArgumentException){return true;}catch(InvalidOperationException){return true;}return false;}
 static int[] NoBoons(){return new int[Boon.BattleCount];}
 static void Arm(Battle b){b.Gold=20000;for(int s=0;s<b.Field.Slots.Length;s++)Check(b.Place(s%3==2?3:s%2,s),"place "+s);foreach(var u in b.Units.ToArray())for(int k=0;k<2;k++)b.Upgrade(u.Slot);}
 // Plays to the end, always taking the first offered card. Returns a line that must be identical for identical battles.
 static string Finish(Battle b,List<string> trace=null){
  if(b.Phase==Phase.Preparation)Check(b.Start(),"start");int ticks=0;
  while(b.Phase==Phase.Fighting&&ticks<600000){if(b.ReinforcementPending)Check(b.ChooseReinforcement(b.Reinforcements.Choices()[0]),"card");b.Step(Balance.Tick);ticks++;if(trace!=null&&ticks%300==0)trace.Add(b.Wave+" "+b.Kills+" "+b.Gold+" "+b.PeakAlive+" "+b.Enemies.Count);}
  return b.Phase+" wave "+b.Wave+" kills "+b.Kills+" gold "+b.Gold+" peak "+b.PeakAlive+" warmth "+b.HP;
 }
 static double Damage(Battle b,int slot){double d,i;b.AttackStats(b.At(slot),out d,out i);return d;}
 static double Interval(Battle b,int slot){double d,i;b.AttackStats(b.At(slot),out d,out i);return i;}
 static bool Near(double a,double b){return Math.Abs(a-b)<1e-9;}

 static void Main(){
  // --- other modes are untouched -------------------------------------------------------------------------------
  for(int field=0;field<Balance.Fields.Length;field++){
   var normal=Battle.CreateForRules(41,field,4);Arm(normal);
   Check(normal.Mode==BattleMode.Normal&&normal.Threat==0&&normal.BoonCount(Boon.Spyglass)==0,"normal has no expedition state");
   Check(ExpeditionBattleData.IsAbsent(normal.SavePreparation().expedition),"normal preparation carries no expedition data");
   // an expedition battle with no boons and no threat fights exactly like the normal battle; only warmth can differ
   var plain=Battle.CreateExpedition(41,field,100,NoBoons(),0,0);Arm(plain);
   var a=new List<string>();var b=new List<string>();string endNormal=Finish(normal,a),endPlain=Finish(plain,b);
   Check(normal.HP==100&&normal.Leaks==0,"normal battles never lose warmth (field "+field+")");
   int same=Math.Min(a.Count,b.Count);for(int i=0;i<same;i++)Check(a[i]==b[i],"field "+field+" trace diverged at "+i+": "+a[i]+" | "+b[i]);
   if(plain.Phase==Phase.Won)Check(endPlain.Replace(" warmth "+plain.HP,"")==endNormal.Replace(" warmth 100",""),"field "+field+" same ending: "+endPlain+" | "+endNormal);
   Check(plain.Leaks==100-plain.HP||plain.HP==0,"leaks account for the lost warmth");
  }
  // legacy rules cannot be expedition battles
  foreach(int rules in new[]{0,1,2,3}){var prep=Battle.CreateForRules(7,0,rules).SavePreparation();prep.expedition=new ExpeditionBattleData{boons=NoBoons(),threat=0};Check(Throws(()=>Battle.RestorePreparation(prep)),"rules "+rules+" refuses expedition data");}

  // --- boon and threat math ------------------------------------------------------------------------------------
  {
   var none=Battle.CreateExpedition(5,0,100,NoBoons(),0,0);none.Gold=5000;for(int k=0;k<5;k++)none.Place(k,k);
   for(int boon=0;boon<3;boon++)for(int n=1;n<=Boon.MaxStacks(boon);n++){var list=NoBoons();list[boon]=n;var b=Battle.CreateExpedition(5,0,100,list,0,0);b.Gold=5000;for(int k=0;k<5;k++)b.Place(k,k);
    for(int k=0;k<4;k++)Check(Near(Damage(b,k),Damage(none,k)*(k==boon?Math.Pow(Boon.ClassDamage,n):1)),"class damage boon "+boon+" x"+n+" on kind "+k);}
   {var list=NoBoons();list[Boon.WarDrum]=2;var b=Battle.CreateExpedition(5,0,100,list,0,0);b.Gold=5000;for(int k=0;k<5;k++)b.Place(k,k);for(int k=0;k<4;k++){Check(Near(Interval(b,k),Interval(none,k)/Math.Pow(Boon.Haste,2)),"war drum interval kind "+k);Check(Near(Damage(b,k),Damage(none,k)),"war drum leaves damage");}}
   {var list=NoBoons();list[Boon.Spyglass]=2;var b=Battle.CreateExpedition(5,0,100,list,0,0);b.Gold=5000;for(int k=0;k<5;k++)b.Place(k,k);for(int k=0;k<5;k++){double factor=k>=1&&k<=3?Math.Pow(Boon.Range,2):1;Check(Near(b.AttackRange(b.At(k)),none.AttackRange(none.At(k))*factor),"spyglass range kind "+k);Check(Near(b.PlacementRange(k),none.PlacementRange(k)*factor),"spyglass placement range kind "+k);}}
   // threat: enemy health at spawn
   for(int threat=0;threat<=Boon.MaxThreat;threat+=3){var b=Battle.CreateExpedition(5,0,100,NoBoons(),threat,0);b.Place(0,0);b.Start();while(b.Enemies.Count==0)b.Step(Balance.Tick);var baseBattle=Battle.CreateExpedition(5,0,100,NoBoons(),0,0);baseBattle.Place(0,0);baseBattle.Start();while(baseBattle.Enemies.Count==0)baseBattle.Step(Balance.Tick);Check(Near(b.Enemies[0].MaxHP,baseBattle.Enemies[0].MaxHP*(1+Boon.ThreatHealth*threat)),"threat "+threat+" health");}
   // spoils: wave and boss rewards only; a 3 G kill stays 3 G
   {var list=NoBoons();list[Boon.Spoils]=1;var rich=Battle.CreateExpedition(9,0,100,list,0,0);var poor=Battle.CreateExpedition(9,0,100,NoBoons(),0,0);foreach(var b in new[]{rich,poor}){b.Gold=5000;for(int k=0;k<5;k++)b.Place(k%2,k);b.Start();}
    int richKill=-1,poorKill=-1;for(int t=0;t<60*31&&(richKill<0||poorKill<0);t++){int g1=rich.Gold,k1=rich.Kills,g2=poor.Gold,k2=poor.Kills;rich.Step(Balance.Tick);poor.Step(Balance.Tick);if(richKill<0&&rich.Kills>k1&&rich.Wave==1)richKill=(rich.Gold-g1)/(rich.Kills-k1);if(poorKill<0&&poor.Kills>k2&&poor.Wave==1)poorKill=(poor.Gold-g2)/(poor.Kills-k2);}
    Check(richKill==3&&poorKill==3,"kill reward unchanged by spoils: "+richKill+" "+poorKill);
    int richStart=rich.Gold,poorStart=poor.Gold;Check(rich.CanCallEarly()==poor.CanCallEarly(),"same early-call state");
    // the wave reward is paid when the next wave begins
    while(rich.Wave==1&&rich.Phase==Phase.Fighting){rich.Step(Balance.Tick);poor.Step(Balance.Tick);}
    Check(rich.Wave==2&&poor.Wave==2,"both reached wave 2");Check(rich.Gold-poor.Gold>=(int)Math.Round(45*Boon.Reward)-45-1&&rich.Gold>poor.Gold,"spoils raises the wave reward: "+rich.Gold+" vs "+poor.Gold);}
   // frost core: longer slow
   {var list=NoBoons();list[Boon.FrostCore]=2;foreach(int cores in new[]{0,2}){var b=Battle.CreateExpedition(3,0,100,cores==0?NoBoons():list,0,0);b.Gold=5000;b.Place(3,0);b.Start();double longest=0;for(int t=0;t<60*30;t++){b.Step(Balance.Tick);foreach(var e in b.Enemies)longest=Math.Max(longest,e.SlowTime);}Check(Near(longest,2+Boon.FrostSeconds*cores),"frost core slow "+cores+": "+longest);}}
  }
  Check(Throws(()=>Battle.CreateExpedition(1,0,0,NoBoons(),0,0))&&Throws(()=>Battle.CreateExpedition(1,0,101,NoBoons(),0,0))&&Throws(()=>Battle.CreateExpedition(1,0,100,new int[3],0,0))&&Throws(()=>Battle.CreateExpedition(1,0,100,NoBoons(),Boon.MaxThreat+1,0))&&Throws(()=>Battle.CreateExpedition(1,0,100,NoBoons(),0,-1)),"invalid expedition battles refused");
  {var over=NoBoons();over[Boon.SwordDrill]=Boon.MaxStacks(Boon.SwordDrill)+1;Check(Throws(()=>Battle.CreateExpedition(1,0,100,over,0,0)),"boon above its cap refused");}

  // --- warmth: goblins that finish a lap steal it; a cold ember loses the battle -------------------------------
  {
   var weak=Battle.CreateExpedition(13,0,30,NoBoons(),0,0);Check(weak.Place(0,0),"one sword");Check(!weak.SetDifficulty(1)&&!weak.EnableReinforcements()&&!weak.GrantSupply(20),"expedition keeps its own pacing and economy");
   weak.Start();int before=weak.HP;bool leakCue=false;while(weak.Phase==Phase.Fighting){if(weak.ReinforcementPending)weak.ChooseReinforcement(weak.Reinforcements.Choices()[0]);weak.Step(Balance.Tick);foreach(var c in weak.Cues)if(c.Kind=="leak")leakCue=true;Check(weak.HP<=before,"warmth never rises in battle");before=weak.HP;}
   Check(weak.Phase==Phase.Lost&&weak.HP==0&&weak.EndCause=="잔불이 꺼졌습니다"&&leakCue,"a cold ember ends the battle: "+weak.Phase+" "+weak.HP+" "+weak.EndCause);
   Check(Boon.LapPoints(4)==Boon.PointsPerWarmth&&Boon.LapPoints(0)==1,"a boss lap costs a whole point of warmth, others a fraction");
   // lap points below one warmth survive a combat save
   {var slow=Battle.CreateExpedition(13,0,100,NoBoons(),0,0);slow.Place(0,0);slow.Start();while(slow.Phase==Phase.Fighting&&slow.ExportCombat().expedition.lapPoints==0)slow.Step(Balance.Tick);var save=slow.ExportCombat();Check(save.expedition.lapPoints>0&&save.expedition.lapPoints<Boon.PointsPerWarmth,"lap points saved");var copy=Battle.ImportCombat(save);Check(copy.ExportCombat().expedition.lapPoints==save.expedition.lapPoints,"lap points restored");save.expedition.lapPoints=Boon.PointsPerWarmth;Check(Throws(()=>Battle.ImportCombat(save)),"lap points above the limit refused");}
  }

  // --- save and restore ----------------------------------------------------------------------------------------
  {
   var boons=NoBoons();boons[Boon.StrongBow]=2;boons[Boon.Spyglass]=1;boons[Boon.FrostCore]=1;
   var fresh=Battle.CreateExpedition(77,2,64,boons,2,100);
   Check(fresh.Gold==fresh.Field.StartGold+100&&fresh.HP==64&&fresh.Field.Slots.Length==Battle.FieldFor(2,4,BattleMode.Normal).Slots.Length,"expedition battle setup");
   var prep=fresh.SavePreparation();var back=Battle.RestorePreparation(prep);
   Check(back.Mode==BattleMode.Expedition&&back.Threat==2&&back.HP==64&&back.Gold==fresh.Gold&&back.BoonCount(Boon.StrongBow)==2&&back.BoonCount(Boon.Spyglass)==1&&back.BoonCount(Boon.FrostCore)==1&&back.BoonCount(Boon.SwordDrill)==0,"preparation round trip");
   // corrupt preparation saves
   Func<PreparationData> P=()=>fresh.SavePreparation();
   {var d=P();d.expedition=null;Check(Throws(()=>Battle.RestorePreparation(d)),"mode 3 without expedition data");}
   {var d=P();d.expedition.boons=new int[Boon.BattleCount-1];Check(Throws(()=>Battle.RestorePreparation(d)),"short boon list");}
   {var d=P();d.expedition.boons[Boon.FrostCore]=Boon.MaxStacks(Boon.FrostCore)+1;Check(Throws(()=>Battle.RestorePreparation(d)),"boon above cap");}
   {var d=P();d.expedition.boons[0]=-1;Check(Throws(()=>Battle.RestorePreparation(d)),"negative boon");}
   {var d=P();d.expedition.threat=Boon.MaxThreat+1;Check(Throws(()=>Battle.RestorePreparation(d)),"threat above cap");}
   {var d=P();d.fastWaves=true;Check(Throws(()=>Battle.RestorePreparation(d)),"expedition with challenge pacing");}
   {var d=P();d.supplyBonus=20;Check(Throws(()=>Battle.RestorePreparation(d)),"expedition with supply");}
   {var d=P();d.mode=1;Check(Throws(()=>Battle.RestorePreparation(d)),"normal mode with expedition data");}
   {var d=P();d.mode=4;Check(Throws(()=>Battle.RestorePreparation(d)),"unknown mode");}
   {var d=Battle.CreateForRules(77,2,4).SavePreparation();d.mode=3;Check(Throws(()=>Battle.RestorePreparation(d)),"mode 3 forged onto a normal save");}
   // a combat save in the middle resumes to exactly the same end
   var live=Battle.CreateExpedition(77,2,64,boons,2,100);Arm(live);live.Start();string resumed=null;
   while(live.Phase==Phase.Fighting){if(live.ReinforcementPending)live.ChooseReinforcement(live.Reinforcements.Choices()[0]);live.Step(Balance.Tick);if(resumed==null&&live.Wave==5&&live.Phase==Phase.Fighting&&!live.ReinforcementPending){var save=live.ExportCombat();var copy=Battle.ImportCombat(save);Check(copy.Mode==BattleMode.Expedition&&copy.Threat==2&&copy.HP==live.HP&&copy.BoonCount(Boon.StrongBow)==2,"combat round trip state");resumed=Finish(copy);
     {var d=live.ExportCombat();d.expedition=null;Check(Throws(()=>Battle.ImportCombat(d)),"combat save without expedition data");}
     {var d=live.ExportCombat();d.expedition.threat=-1;Check(Throws(()=>Battle.ImportCombat(d)),"combat save with bad threat");}
     {var d=live.ExportCombat();d.mode=1;Check(Throws(()=>Battle.ImportCombat(d)),"combat save forged to normal");}
     var retry=live.CopyCombat();Check(retry.Mode==BattleMode.Expedition&&retry.Threat==2&&retry.BoonCount(Boon.Spyglass)==1&&retry.HP==live.HP,"in-memory copy keeps the expedition state");}}
   string end="";end=live.Phase+" wave "+live.Wave+" kills "+live.Kills+" gold "+live.Gold+" peak "+live.PeakAlive+" warmth "+live.HP;
   Check(resumed!=null&&resumed==end,"combat save resumes to the same end: "+resumed+" | "+end);
   Check(CombatSaveData.IsAbsent(new CombatSaveData())&&!CombatSaveData.IsAbsent(new CombatSaveData{expedition=new ExpeditionBattleData{boons=NoBoons()}}),"empty checkpoint detection sees expedition data");
  }

  // --- the run -------------------------------------------------------------------------------------------------
  {
   var run=Expedition.Start(2026);
   Check(run.State==Expedition.Status.Battle&&run.Stage==0&&run.Warmth==100&&run.Stages==4&&run.Field==0&&run.Offers().Length==0,"fresh run");
   Check(!run.Choose(Boon.SwordDrill),"no camp before the first battle");
   var seeds=new HashSet<uint>();for(int s=0;s<run.Stages;s++){Check(run.StageSeed(s)!=0&&seeds.Add(run.StageSeed(s)),"stage seeds differ");}
   Check(Expedition.Start(2027).StageSeed(0)!=run.StageSeed(0),"runs fight on different seeds");
   var b=run.CreateBattle();Check(run.Matches(b)&&b.Mode==BattleMode.Expedition&&b.FieldIndex==0&&b.HP==100&&b.Threat==0,"first battle");
   Check(!run.Complete(b),"an unfinished battle cannot be reported");
   Check(!run.Complete(Battle.CreateForRules(1,0,4)),"a normal battle is not part of the run");
   b.Phase=Phase.Won;b.HP=83;b.Kills=120;b.PeakAlive=44;Check(run.Complete(b)&&!run.Complete(b),"report a win once");
   Check(run.State==Expedition.Status.Camp&&run.Warmth==83&&run.Kills==120&&run.Peak(0)==44&&run.Stage==0,"camp after the first win");
   var offers=run.Offers();Check(offers.Length==3&&offers[0]!=offers[1]&&offers[1]!=offers[2]&&offers[0]!=offers[2],"three different offers");
   Check(Throws(()=>run.CreateBattle()),"no battle while camping");
   // same seed, same history, same offers
   var twin=Expedition.Start(2026);var tb=twin.CreateBattle();tb.Phase=Phase.Won;tb.HP=83;twin.Complete(tb);var twinOffers=twin.Offers();Check(offers[0]==twinOffers[0]&&offers[1]==twinOffers[1]&&offers[2]==twinOffers[2],"offers are deterministic");
   // restore in camp keeps the offer; choosing something not offered fails
   var camp=Expedition.Restore(run.Export());var campOffers=camp.Offers();Check(camp.State==Expedition.Status.Camp&&campOffers[0]==offers[0]&&campOffers[2]==offers[2]&&camp.Warmth==83,"camp restore");
   int notOffered=-1;for(int i=0;i<Boon.Count;i++)if(Array.IndexOf(offers,i)<0){notOffered=i;break;}Check(!run.Choose(notOffered)&&!run.Choose(-1)&&!run.Choose(Boon.Count),"only offered boons can be taken");
   Check(run.Choose(offers[0])&&!run.Choose(offers[1]),"one boon per camp");
   Check(run.State==Expedition.Status.Battle&&run.Stage==1&&run.Field==1&&run.Threat==1&&run.Count(offers[0])==1,"second stage");
   var second=run.CreateBattle();Check(second.FieldIndex==1&&second.Threat==1&&run.Matches(second)&&!run.Matches(b),"second battle carries the run");
   if(offers[0]<Boon.BattleCount)Check(second.BoonCount(offers[0])==1,"battle boon carried");
   Check(second.HP==run.Warmth&&second.Gold==second.Field.StartGold+run.PendingGold,"warmth and gold carried");
   // losing ends the run
   var lostRun=Expedition.Restore(run.Export());var lb=lostRun.CreateBattle();lb.Phase=Phase.Lost;lb.HP=0;Check(lostRun.Complete(lb)&&lostRun.State==Expedition.Status.Lost&&lostRun.Warmth==0&&!lostRun.Choose(0)&&Throws(()=>lostRun.CreateBattle()),"a lost battle ends the run");
   Check(Expedition.Restore(lostRun.Export()).State==Expedition.Status.Lost,"lost run restores");
   // crowd loss keeps warmth but still ends the run
   var crowdRun=Expedition.Restore(run.Export());var cb=crowdRun.CreateBattle();cb.Phase=Phase.Lost;cb.HP=40;Check(crowdRun.Complete(cb)&&crowdRun.State==Expedition.Status.Lost&&crowdRun.Warmth==40&&Expedition.Restore(crowdRun.Export()).Warmth==40,"a crowd loss ends the run with warmth left");
  }
  // instant boons and offer rules, over many seeds
  {
   int rekindleSeen=0,chestSeen=0;
   for(uint seed=1;seed<=400;seed++){
    var run=Expedition.Start(seed);
    for(int stage=0;stage<run.Stages;stage++){
     var b=run.CreateBattle();int startGold=b.Gold;Check(b.Gold==b.Field.StartGold+run.PendingGold,"start gold");
     b.Phase=Phase.Won;b.HP=Math.Max(1,run.Warmth-(int)(seed%4==0?0:(seed*7+(uint)stage*13)%45));Check(run.Complete(b),"win stage "+stage);
     Check(run.PendingGold==0,"war chest is spent by the battle it paid for");
     if(stage==run.Stages-1){Check(run.State==Expedition.Status.Won&&run.Offers().Length==0,"run won after the last stage");Check(Expedition.Restore(run.Export()).State==Expedition.Status.Won,"won run restores");break;}
     var offers=run.Offers();foreach(int o in offers){Check(run.Count(o)<Boon.MaxStacks(o),"maxed boons are not offered");if(o==Boon.Rekindle)Check(run.Warmth<100,"rekindle is not offered to a full ember");}
     int pick=offers[(int)((seed+(uint)stage)%3)];int warmth=run.Warmth;
     var again=Expedition.Restore(run.Export());Check(again.Choose(pick),"restored camp can choose");
     Check(run.Choose(pick),"choose");
     if(pick==Boon.Rekindle){rekindleSeen++;Check(run.Warmth==Math.Min(100,warmth+Boon.RekindleWarmth),"rekindle warms the ember");}else Check(run.Warmth==warmth,"other boons leave warmth");
     if(pick==Boon.WarChest){chestSeen++;Check(run.PendingGold==Boon.WarChestGold,"war chest pays the next battle");}
     Check(Expedition.Restore(run.Export()).Stage==stage+1,"battle state restores");
    }
   }
   Check(rekindleSeen>20&&chestSeen>20,"instant boons appear: "+rekindleSeen+" "+chestSeen);
  }
  // corrupt run data
  {
   var run=Expedition.Start(55);var b=run.CreateBattle();b.Phase=Phase.Won;run.Complete(b);
   Func<ExpeditionData> D=()=>run.Export();
   {var d=D();d.version=2;Check(Throws(()=>Expedition.Restore(d)),"unknown version");}
   {var d=D();d.offered=new int[0];Check(Throws(()=>Expedition.Restore(d)),"camp without an offer");}
   {var d=D();d.offered[1]=d.offered[0];Check(Throws(()=>Expedition.Restore(d)),"duplicate offer");}
   {var d=D();d.offered[0]=Boon.Count;Check(Throws(()=>Expedition.Restore(d)),"unknown boon offered");}
   {var d=D();d.boons[Boon.SwordDrill]=1;Check(Throws(()=>Expedition.Restore(d)),"a boon nobody chose");}
   {var d=D();d.warmth=0;Check(Throws(()=>Expedition.Restore(d)),"camping on a cold ember");}
   {var d=D();d.warmth=101;Check(Throws(()=>Expedition.Restore(d)),"too much warmth");}
   {var d=D();d.stage=4;Check(Throws(()=>Expedition.Restore(d)),"stage past the route");}
   {var d=D();d.state=2;Check(Throws(()=>Expedition.Restore(d)),"won before the last stage");}
   {var d=D();d.pendingGold=100;Check(Throws(()=>Expedition.Restore(d)),"gold nobody earned");}
   {var d=D();d.peaks[2]=10;Check(Throws(()=>Expedition.Restore(d)),"a record from the future");}
   {var d=D();d.peaks=new int[2];Check(Throws(()=>Expedition.Restore(d)),"short record");}
   Check(Throws(()=>Expedition.Restore(null)),"null run");
  }

  // --- a whole run on the real engine, twice -------------------------------------------------------------------
  {
   string[] lines=new string[2];
   for(int pass=0;pass<2;pass++){
    var run=Expedition.Start(31337);var log=new List<string>();
    while(run.State==Expedition.Status.Battle){var b=run.CreateBattle();Arm(b);string end=Finish(b);Check(run.Complete(b),"report stage");log.Add(run.Stage+":"+end);if(run.State==Expedition.Status.Camp){var saved=Expedition.Restore(run.Export());Check(saved.Offers()[0]==run.Offers()[0],"camp survives a save");Check(run.Choose(run.Offers()[0]),"take the first boon");}}
    lines[pass]=run.State+" stage "+run.Stage+" warmth "+run.Warmth+" kills "+run.Kills+" | "+string.Join(" | ",log.ToArray());
   }
   Check(lines[0]==lines[1],"a run is deterministic:\n"+lines[0]+"\n"+lines[1]);
   Check(lines[0].StartsWith("Won stage 3")||lines[0].StartsWith("Lost stage"),"the run ends one way or the other: "+lines[0]);
   Console.WriteLine(lines[0]);
  }
  Console.WriteLine("PASS "+checks+" expedition checks: neutral mode, boons, threat, warmth, saves and the run");
 }
}
