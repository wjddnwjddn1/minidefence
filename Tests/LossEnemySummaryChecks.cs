using System;
using System.Collections.Generic;
using EmberBastion;
class LossEnemySummaryChecks {
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 static void Main(){var enemies=new List<Enemy>{new Enemy{Kind=2,HP=10},new Enemy{Kind=2,HP=20},new Enemy{Kind=1,HP=1},new Enemy{Kind=1,HP=0}};Check(LossEnemySummary.Describe(enemies,"적 100마리 도달")=="가장 많은 적 · 방패병 2명","largest count");enemies.Add(new Enemy{Kind=1,HP=10});Check(LossEnemySummary.Describe(enemies,"적 100마리 도달")=="최다 적 동률 · 각 2명","tie");Check(LossEnemySummary.Describe(enemies,"전투 포기")=="","surrender");Check(LossEnemySummary.Describe(new List<Enemy>(),"적 100마리 도달")=="","empty");Console.WriteLine("PASS loss enemy count, ties and surrender");}
}
