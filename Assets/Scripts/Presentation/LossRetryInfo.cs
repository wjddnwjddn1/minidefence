using System;
namespace EmberBastion {
 public struct LossRetryInfo {
  public int Gold,Empty,Enemies;
  public bool Restores,CanUpgrade,HasWave;
  public static LossRetryInfo For(Battle current,Battle wave,PreparationData prep){
   var result=new LossRetryInfo();
   if(wave!=null){result.Restores=true;result.HasWave=true;result.Enemies=wave.Enemies.Count;result.Gold=wave.Gold;result.Empty=Math.Max(0,wave.Field.Slots.Length-wave.Units.Count);foreach(var u in wave.Units)if((u.Level==1&&result.Gold>=50)||(u.Level==2&&result.Gold>=90))result.CanUpgrade=true;}
   else if(prep!=null&&prep.units!=null){result.Restores=true;result.Gold=prep.gold;result.Empty=Math.Max(0,current.Field.Slots.Length-prep.units.Length);foreach(var u in prep.units)if(u!=null&&((u.level==1&&result.Gold>=50)||(u.level==2&&result.Gold>=90)))result.CanUpgrade=true;}
   else {result.Gold=current.Gold;result.Empty=Math.Max(0,current.Field.Slots.Length-current.Units.Count);foreach(var u in current.Units)if((u.Level==1&&result.Gold>=50)||(u.Level==2&&result.Gold>=90))result.CanUpgrade=true;}
   return result;
  }
  public string Advice(){if(!Restores)return "새 전투에서 편성을 바꿔 보세요";if(HasWave&&Enemies>=70)return Gold>=200?"복원 위험 · 보강 또는 재도전":"복원 시점도 위험 · 재도전 고려";if(Empty>0&&Gold>=65)return "빈 자리에 병력 추가";if(CanUpgrade)return "병력을 강화해 보세요";return "병과와 배치를 바꿔 보세요";}
 }
}
