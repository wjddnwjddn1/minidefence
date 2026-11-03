using System;
using EmberBastion;
class ReinforcementEffectsChecks {
 static int checks;static void Check(bool ok,string label){checks++;if(!ok)throw new Exception(label);}
 static Battle WithCard(int card){for(uint seed=0;seed<1000;seed++){var b=new Battle(seed);b.EnableReinforcements();b.Gold=10000;b.Place(0,0);b.Place(1,1);b.Place(2,2);b.Reinforcements.Offer(true,true,true);if(b.Reinforcements.Choose(card))return b;}throw new Exception("card unavailable");}
 static int Main(){try{
  var bow=WithCard(ReinforcementDraft.ArcherHaste);double damage,interval;bow.AttackStats(bow.At(1),out damage,out interval);Check(Math.Abs(interval-1.0/1.2)<1e-9,"archer attack speed");
  var sword=WithCard(ReinforcementDraft.SwordDamage);sword.AttackStats(sword.At(0),out damage,out interval);Check(Math.Abs(damage-23)<1e-9,"sword damage");
  var range=WithCard(ReinforcementDraft.Range);Check(Math.Abs(range.AttackRange(range.At(1))-224.7)<1e-9,"range");
  var ember=WithCard(ReinforcementDraft.EmberCooldown);Check(ember.Start()&&ember.Ember(new Point(500,300)),"ember cast");Check(Math.Abs(ember.EmberCD-17.5)<1e-9,"ember cooldown");
  var density=new Battle(1);density.EnableReinforcements();var g=density.Field.Waves[0][0];Check(density.SpawnCount(g)==(int)Math.Round(Math.Round(g.Count*1.25)*1.15),"nonboss density");Check(Math.Abs(density.SpawnGap(g)-g.Gap*.8/1.15)<1e-9,"nonboss gap");var boss=density.Field.Waves[4][1];Check(boss.Kind==4&&density.SpawnCount(boss)==boss.Count&&density.SpawnGap(boss)==boss.Gap,"boss unchanged");
  var targets=WithCard(ReinforcementDraft.SwordTargets);targets.Units.RemoveAll(u=>u.Kind!=0);targets.At(0).Slot=10;Check(targets.Start(),"sword target start");for(int i=0;i<4;i++)targets.Enemies.Add(new Enemy{Id=1000+i,Kind=0,HP=100,MaxHP=100,Speed=0,Travel=2614,Position=new Point(192,330)});targets.Step(1.0/60);for(int i=0;i<4;i++)Check(targets.Enemies.Find(e=>e.Id==1000+i).HP==80,"extra sword target");
  var bomb=WithCard(ReinforcementDraft.BombRadius);bomb.Units.RemoveAll(u=>u.Kind!=2);Check(bomb.Start(),"bomb start");Shot shot=null;for(int i=0;i<1200&&shot==null;i++){bomb.Step(1.0/60);shot=bomb.Shots.Find(x=>x.Kind==2);}Check(shot!=null&&Math.Abs(shot.Radius-68.75)<1e-9,"bomb radius");
  var gold=WithCard(ReinforcementDraft.GoldIncome);var reward=typeof(Battle).GetMethod("Reward",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);Check((int)reward.Invoke(gold,new object[]{40})==46,"gold income");
  Console.WriteLine("PASS "+checks+" reinforcement effect and spawn checks");return 0;}catch(Exception ex){Console.Error.WriteLine(ex.Message);return 1;}
 }
}
