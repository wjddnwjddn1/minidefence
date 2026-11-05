using System.Collections.Generic;
namespace EmberBastion {
 public static class LossEnemySummary {
  // A tied maximum is not attributed to an arbitrary enemy type.
  static int Dominant(IList<Enemy> enemies,string endCause,out int best){
   best=0;if(endCause!="적 100마리 도달"||enemies==null||enemies.Count==0)return -1;
   int[] counts=new int[Balance.Enemies.Length];
   foreach(var enemy in enemies)if(enemy!=null&&enemy.HP>0&&enemy.Kind>=0&&enemy.Kind<counts.Length)counts[enemy.Kind]++;
   int kind=-1,ties=0;
   for(int i=0;i<counts.Length;i++){if(counts[i]>best){best=counts[i];kind=i;ties=1;}else if(counts[i]>0&&counts[i]==best)ties++;}
   return ties>1?-2:kind;
  }
  public static string Describe(IList<Enemy> enemies,string endCause){
   int count;int kind=Dominant(enemies,endCause,out count);
   if(kind==-2){int shown=0,tied=0;string names="";for(int i=0;i<Balance.Enemies.Length;i++){int n=0;foreach(var enemy in enemies)if(enemy!=null&&enemy.HP>0&&enemy.Kind==i)n++;if(n!=count)continue;tied++;if(shown<2){if(shown>0)names+="·";names+=i==4?"대장":Balance.Enemies[i].Name.Replace("병","");shown++;}}return "최다 "+names+(tied>shown?" 등":"")+" 각 "+count+"명";}
   return kind<0?"":"가장 많은 적 · "+Balance.Enemies[kind].Name+" "+count+"명";
  }
  public static string Hint(IList<Enemy> enemies,string endCause){
   int count;switch(Dominant(enemies,endCause,out count)){
    case 0:return "횃불병 다수 · 범위 공격 검토";
    case 1:return "빠른 적은 서리 둔화 검토";
    case 2:return "방패병에는 관통 공격 검토";
    case 3:return "폭약병 처치 시 기절 주의";
    case 4:return "대장 우선 표적 설정 검토";
    default:return "";
   }
  }
 }
}
