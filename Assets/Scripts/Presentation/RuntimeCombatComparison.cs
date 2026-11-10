#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Reflection;
namespace EmberBastion {
 // Diagnostics only: exact discrete state, bounded absolute JSON round-trip noise in doubles.
 public static class RuntimeCombatComparison {
  public const double Tolerance=1e-9;
  public static bool Equivalent(CombatSaveData a,CombatSaveData b,out double maximumError){maximumError=0;return Same(a,b,ref maximumError);}
  static bool Same(object a,object b,ref double error){
   if(a==null||b==null)return a==null&&b==null;
   var type=a.GetType();if(type!=b.GetType())return false;
   if(type==typeof(double)){double x=(double)a,y=(double)b;if(double.IsNaN(x)||double.IsInfinity(x)||double.IsNaN(y)||double.IsInfinity(y))return false;error=Math.Max(error,Math.Abs(x-y));return Math.Abs(x-y)<=Tolerance;}
   if(type.IsPrimitive||type.IsEnum||type==typeof(string))return a.Equals(b);
   if(type.IsArray){var x=(Array)a;var y=(Array)b;if(x.Length!=y.Length)return false;for(int i=0;i<x.Length;i++)if(!Same(x.GetValue(i),y.GetValue(i),ref error))return false;return true;}
   foreach(var field in type.GetFields(BindingFlags.Public|BindingFlags.Instance))if(!Same(field.GetValue(a),field.GetValue(b),ref error))return false;
   return true;
  }
 }
}
#endif
