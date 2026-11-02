namespace EmberBastion {
 // Pure victory evaluation; unknown kills in legacy saves are never inferred.
 public static class BadgeRules {
  static readonly int[,] Limits={{41,48,37},{66,67,65},{75,85,75}};static readonly int[,] OldLimits={{35,40,30},{51,50,47},{75,85,75}};
  public static int Limit(int field,bool fast,bool hard,bool legacy=false){if(field<0||field>=3||hard&&!fast)return 0;return (legacy?OldLimits:Limits)[hard?2:fast?1:0,field];}
  public static int Evaluate(Battle b){if(b==null||b.Phase!=Phase.Won)return 0;int result=1;if(b.PeakAlive>0&&b.PeakAlive<=Limit(b.FieldIndex,b.FastWaves,b.HardWaves,b.RulesVersion==0))result|=2;int n=0;for(int i=0;i<4;i++)if((b.KillKinds&(1<<i))!=0)n++;if(n>=3)result|=4;return result;}
 }
}
