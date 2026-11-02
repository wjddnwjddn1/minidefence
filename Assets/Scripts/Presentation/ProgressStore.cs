using System;
using UnityEngine;
namespace EmberBastion {
 public static class ProgressStore {
  const string Prefix="snomet.ember-bastion.unity-comparison.clear-count.v2.";
  const string FastPrefix="snomet.ember-bastion.unity-comparison.clear-fast.v1.";
  const string HardPrefix="snomet.ember-bastion.unity-comparison.clear-hard.v1.";
  public static bool Cleared(int field,bool fast=false,bool hard=false){return field>=0&&field<Balance.Fields.Length&&PlayerPrefs.GetInt((hard?HardPrefix:fast?FastPrefix:Prefix)+field,0)==1;}
  static bool ValidSupply(int supply){return supply>=0&&supply<=60&&supply%20==0;}
  static string ModeKey(bool fast,bool hard){return hard?"hard.":fast?"fast.":"normal.";}
  static int? ReadBest(string key){int value=PlayerPrefs.GetInt(key,0);return value>=2&&value<=100?(int?)(value-1):null;}
  static string BestKey(int field,bool fast,bool hard,int supply){return "snomet.ember-bastion.unity-comparison.best-peak."+(hard?"v2.":fast?"v4.":"v3.")+ModeKey(fast,hard)+field+"."+supply;}
  public static int? BestPeak(int field,bool fast=false,bool hard=false,int supply=0){if(field<0||field>=Balance.Fields.Length||hard&&!fast||!ValidSupply(supply))return null;return ReadBest(BestKey(field,fast,hard,supply));}
  public static int? PreviousRulesBestPeak(int field,bool fast,bool hard,int supply=0){if(field<0||field>=Balance.Fields.Length||hard||!ValidSupply(supply))return null;return ReadBest("snomet.ember-bastion.unity-comparison.best-peak."+(fast?"v3.fast.":"v2.normal.")+field+"."+supply);}
  public static int? PreviousFastBestPeak(int field,int supply=0){if(field<0||field>=Balance.Fields.Length||!ValidSupply(supply))return null;return ReadBest("snomet.ember-bastion.unity-comparison.best-peak.v2.fast."+field+"."+supply);}
  public static int? LegacyBestPeak(int field,bool fast=false,bool hard=false){if(field<0||field>=Balance.Fields.Length||hard&&!fast)return null;return ReadBest("snomet.ember-bastion.unity-comparison.best-peak.v1."+ModeKey(fast,hard)+field);}
  public static bool RecordBest(int field,bool fast,bool hard,int peak,int supply=0){if(field<0||field>=Balance.Fields.Length||hard&&!fast||peak<1||peak>=Balance.MaxAlive||!ValidSupply(supply))throw new ArgumentException("Invalid peak record");int? best=BestPeak(field,fast,hard,supply);if(best.HasValue&&best.Value<=peak)return false;PlayerPrefs.SetInt(BestKey(field,fast,hard,supply),peak+1);PlayerPrefs.Save();return true;}
  static string BadgeKey(int field,bool fast,bool hard,int badge){return "snomet.ember-bastion.unity-comparison.badge."+(hard?"v1.":"v2.")+badge+"."+ModeKey(fast,hard)+field;}
  public static bool PreviousRulesBadge(int field,bool fast,bool hard,int badge){if(field<0||field>=Balance.Fields.Length||hard||(badge!=2&&badge!=4))return false;return PlayerPrefs.GetInt("snomet.ember-bastion.unity-comparison.badge.v1."+badge+"."+ModeKey(fast,hard)+field,0)==1;}
  public static bool HasBadge(int field,bool fast,bool hard,int badge){if(field<0||field>=Balance.Fields.Length||hard&&!fast||(badge!=2&&badge!=4))return false;return PlayerPrefs.GetInt(BadgeKey(field,fast,hard,badge),0)==1;}
  public static void RecordBadges(int field,bool fast,bool hard,int mask){if(field<0||field>=Balance.Fields.Length||hard&&!fast||(mask&~7)!=0||mask<0)throw new ArgumentException("Invalid badges");foreach(int badge in new[]{2,4})if((mask&badge)!=0)PlayerPrefs.SetInt(BadgeKey(field,fast,hard,badge),1);PlayerPrefs.Save();}
  public static bool Record(int field,bool fast=false,bool hard=false){if(field<0||field>=Balance.Fields.Length||hard&&!fast)throw new ArgumentException("Invalid field or difficulty");if(Cleared(field,fast,hard))return false;PlayerPrefs.SetInt((hard?HardPrefix:fast?FastPrefix:Prefix)+field,1);PlayerPrefs.Save();return true;}
 }
}
