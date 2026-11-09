using System;
using EmberBastion;
class NextWaveAdviceChecks {
 static int checks;
 static void Check(bool ok,string name){checks++;if(!ok)throw new Exception(name);}
 static void Main(){
  for(int field=0;field<Balance.Fields.Length;field++){
   var original=Balance.Fields[field].Waves[0];
   try{for(int mode=0;mode<3;mode++)for(int kind=0;kind<5;kind++){
    Balance.Fields[field].Waves[0]=new[]{new Group(kind,1,1)};
    var b=new Battle(29,field);b.SetDifficulty(mode);var before=b.SavePreparation();string hint=b.NextWaveAdvice();var after=b.SavePreparation();
    Check(hint.Length>0,"upcoming threat has an action");
    Check(before.rng==after.rng&&before.gold==after.gold&&before.time==after.time,"advice does not advance battle state");
    if(kind==1||kind==2)Check(hint.Contains("반감")== (mode==1),"resistance advice matches actual difficulty rule");
    if(kind==4)Check(hint.Contains("대장 우선"),"boss hint names existing target control");
    if(kind==2&&mode!=1)Check(hint.Contains("관통")&&hint.Contains("파쇄"),"normal and extreme shield hint uses armor counters");
    b.Wave=b.WaveCount;Check(b.NextWaveAdvice()=="","no future advice after final wave");
   }}finally{Balance.Fields[field].Waves[0]=original;}
  }
  Console.WriteLine("PASS "+checks+" actionable wave advice and rule consistency checks");
 }
}
