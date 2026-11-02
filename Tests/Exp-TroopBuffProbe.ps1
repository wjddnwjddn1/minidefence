# Isolated frost/engineer candidates on a temp copy of Battle.cs; the product is not changed.
param([double]$Amp=1,[double]$Radius=0,[double]$Eng=1,[string]$Label='x',[int]$Brittle=0,[double]$Weight=1,[int]$ChillGold=0)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$bin=Join-Path $PSScriptRoot 'bin'
$s=[System.IO.File]::ReadAllText((Join-Path $root 'Assets\Scripts\Core\Battle.cs'),[System.Text.Encoding]::UTF8)
function Patch($old,$new){if(-not $script:s.Contains($old)){throw "Missing: $old"};$script:s=$script:s.Replace($old,$new)}
Patch 'double initial=raw*(e.Kind==4?bossMult:1);' 'double initial=raw*(e.Kind==4?bossMult:1)*(e.SlowTime>0||e.FreezeTime>0?Exp.SlowAmp:1);'
Patch 'if(s.Kind==3)Cues.Add(new Cue("frost",target.Position));' 'if(s.Kind==3)Cues.Add(new Cue("frost",target.Position));if(s.Kind==3&&Exp.SlowRadius>0)foreach(var other in Enemies)if(other.HP>0&&Point.Distance(other.Position,target.Position)<=Exp.SlowRadius){if(s.SlowPct>=other.SlowPct||other.SlowTime<=0)other.SlowPct=s.SlowPct;other.SlowTime=Math.Max(other.SlowTime,target.SlowTime);}'
Patch 'interval=(u.Kind==1&&u.Branch==1?.75:d.Interval)/(1+haste);' 'haste*=Exp.Eng;bonusDamage*=Exp.Eng;interval=(u.Kind==1&&u.Branch==1?.75:d.Interval)/(1+haste);'
Patch 'Hit(e,s.Damage*(FastWaves&&!HardWaves?Balance.Enemies[e.Kind].BlastTaken:1)' 'Hit(e,s.Damage*(FastWaves&&!HardWaves&&!(Exp.Brittle&&(e.SlowTime>0||e.FreezeTime>0))?Balance.Enemies[e.Kind].BlastTaken:1)'
Patch 'damage*(FastWaves&&!HardWaves?Balance.Enemies[targets[i].Kind].MeleeTaken:1)' 'damage*(FastWaves&&!HardWaves&&!(Exp.Brittle&&(targets[i].SlowTime>0||targets[i].FreezeTime>0))?Balance.Enemies[targets[i].Kind].MeleeTaken:1)'
Patch 'PeakAlive=Math.Max(PeakAlive,Enemies.Count);' 'PeakAlive=Math.Max(PeakAlive,Exp.Alive(Enemies));'
$s=$s.Replace('if(Enemies.Count>=Balance.MaxAlive)','if(Exp.Alive(Enemies)>=Balance.MaxAlive)')
Patch 'int reward=e.Kind==4?40:3;' 'int reward=(e.Kind==4?40:3)+(e.SlowTime>0||e.FreezeTime>0?Exp.ChillGold:0);'
$copy=Join-Path $bin 'BattleBuff.cs'
[System.IO.File]::WriteAllText($copy,$s,[System.Text.UTF8Encoding]::new($false))
$probe=[System.IO.File]::ReadAllText((Join-Path $PSScriptRoot 'TroopValueProbe.cs'),[System.Text.Encoding]::UTF8)
$probe=$probe.Replace('static void Main(){','static void Main(){Exp.SlowAmp='+$Amp+';Exp.SlowRadius='+$Radius+';Exp.Eng='+$Eng+';Exp.ChillWeight='+$Weight+';Exp.ChillGold='+$ChillGold+';Exp.Brittle='+$(if($Brittle){'true'}else{'false'})+';')
$probe=$probe.Replace('using EmberBastion;','using EmberBastion;namespace EmberBastion{public static class Exp{public static double SlowAmp=1,SlowRadius=0,Eng=1;public static bool Brittle=false;public static double ChillWeight=1;public static int ChillGold=0;public static int Alive(System.Collections.Generic.List<Enemy> l){if(ChillWeight>=1)return l.Count;double n=0;foreach(var e in l)n+=e.SlowTime>0||e.FreezeTime>0?ChillWeight:1;return (int)Math.Ceiling(n);}}}')
$pc=Join-Path $bin 'TroopBuffProbe.cs'
[System.IO.File]::WriteAllText($pc,$probe,[System.Text.UTF8Encoding]::new($false))
$exe=Join-Path $bin 'TroopBuffProbe.exe'
& (Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe') /nologo /utf8output /codepage:65001 "/out:$exe" (Join-Path $root 'Assets\Scripts\Core\Balance.cs') $copy (Join-Path $root 'Assets\Scripts\Core\CombatSave.cs') $pc
if($LASTEXITCODE -ne 0){throw 'Compilation failed'}
$rows=& $exe
[System.IO.File]::WriteAllLines((Join-Path $root "Docs\troop-buff-$Label.csv"),[string[]]$rows,[System.Text.UTF8Encoding]::new($false))
"PASS $Label"
