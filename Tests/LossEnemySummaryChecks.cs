using System;
using System.Collections.Generic;
using EmberBastion;
class LossEnemySummaryChecks {
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 static void Main(){var enemies=new List<Enemy>{new Enemy{Kind=2,HP=10},new Enemy{Kind=2,HP=20},new Enemy{Kind=1,HP=1},new Enemy{Kind=1,HP=0}};Check(LossEnemySummary.Describe(enemies,"적 100마리 도달")=="가장 많은 적 · 방패병 2명","largest count");Check(LossEnemySummary.Hint(enemies,"적 100마리 도달")=="방패병에는 관통 공격 검토","shield hint");enemies.Add(new Enemy{Kind=1,HP=10});Check(LossEnemySummary.Describe(enemies,"적 100마리 도달")=="최다 질주·방패 각 2명","tie");Check(LossEnemySummary.Hint(enemies,"적 100마리 도달")=="","no arbitrary hint on tie");enemies.Add(new Enemy{Kind=3,HP=1});enemies.Add(new Enemy{Kind=3,HP=1});Check(LossEnemySummary.Describe(enemies,"적 100마리 도달")=="최다 질주·방패 등 각 2명","three-way tie");Check(LossEnemySummary.Hint(enemies,"전투 포기")=="","no hint on surrender");Check(LossEnemySummary.Describe(enemies,"전투 포기")=="","surrender");Check(LossEnemySummary.Describe(new List<Enemy>(),"적 100마리 도달")=="","empty");string[] hints={"횃불병 다수 · 범위 공격 검토","빠른 적은 서리 둔화 검토","방패병에는 관통 공격 검토","폭약병 처치 시 기절 주의","대장 우선 표적 설정 검토"};for(int i=0;i<hints.Length;i++)Check(LossEnemySummary.Hint(new List<Enemy>{new Enemy{Kind=i,HP=1}},"적 100마리 도달")==hints[i],"enemy hint "+i);Console.WriteLine("PASS loss enemy count, ties, surrender and five hints");}
}
