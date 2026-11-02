$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$bin=Join-Path $PSScriptRoot 'bin'
New-Item -ItemType Directory -Path $bin -Force | Out-Null
$source=[System.IO.File]::ReadAllText((Join-Path $root 'Assets\Scripts\Core\Battle.cs'),[System.Text.Encoding]::UTF8)
$hit='double initial=raw*(e.Kind==4?bossMult:1);'
$frost='if(s.Kind==3)Cues.Add(new Cue("frost",target.Position));'
if(-not $source.Contains($hit) -or -not $source.Contains($frost)){throw 'Expected experimental hook not found'}
$source=$source.Replace($hit,'double initial=raw*(e.Kind==4?bossMult:1)*(e.SlowTime>0||e.FreezeTime>0?Exp.SlowAmp:1);')
$source=$source.Replace($frost,$frost+'if(s.Kind==3&&Exp.SlowRadius>0)foreach(var other in Enemies)if(other.HP>0&&Point.Distance(other.Position,target.Position)<=Exp.SlowRadius){if(s.SlowPct>=other.SlowPct||other.SlowTime<=0)other.SlowPct=s.SlowPct;other.SlowTime=Math.Max(other.SlowTime,2);}')
$copy=Join-Path $bin 'FrostBalanceNextBattle.cs'
[System.IO.File]::WriteAllText($copy,$source,[System.Text.UTF8Encoding]::new($false))
$output=Join-Path $bin 'FrostBalanceNextProbe.exe'
& $compiler /nologo /utf8output /codepage:65001 "/out:$output" (Join-Path $root 'Assets\Scripts\Core\Balance.cs') $copy (Join-Path $root 'Assets\Scripts\Core\CombatSave.cs') (Join-Path $PSScriptRoot 'FrostBalanceNextProbe.cs')
if($LASTEXITCODE -ne 0){throw 'Compilation failed'}
$rows=& $output
if($LASTEXITCODE -ne 0){throw 'Simulation failed'}
if($rows.Count -ne 1152){throw "Expected 1152 rows; got $($rows.Count)"}
[System.IO.File]::WriteAllLines((Join-Path $root 'Docs\frost-next-probe.csv'),[string[]](@('seed,variant,density,mode,field,pattern,skill,result,wave,peak,kills')+$rows),[System.Text.UTF8Encoding]::new($false))
'PASS 1152 isolated frost scenarios; game rules unchanged'
