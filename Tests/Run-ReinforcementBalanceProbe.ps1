$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$bin=Join-Path $PSScriptRoot 'bin'
$source=[IO.File]::ReadAllText((Join-Path $root 'Assets\Scripts\Core\Battle.cs'),[Text.Encoding]::UTF8)
$old='int CardCount(int card){return Reinforcements==null?0:Reinforcements.Count(card);}'
$new='int CardCount(int card){return Bench.NoCards||Reinforcements==null?0:Reinforcements.Count(card);}'
if(-not $source.Contains($old)){throw 'Expected card effect expression missing'}
$copy=Join-Path $bin 'BattleReinforcementBench.cs'
[IO.File]::WriteAllText($copy,$source.Replace($old,$new),[Text.UTF8Encoding]::new($false))
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$exe=Join-Path $bin 'ReinforcementBalanceProbe.exe'
& $compiler /nologo /utf8output /codepage:65001 "/out:$exe" (Join-Path $root 'Assets\Scripts\Core\Balance.cs') $copy (Join-Path $PSScriptRoot 'ReinforcementBalanceProbe.cs')
if($LASTEXITCODE -ne 0){throw 'Compilation failed'}
$rows=& $exe
if($LASTEXITCODE -ne 0 -or $rows.Count -ne 1321){throw "Unexpected results: $($rows.Count)"}
[IO.File]::WriteAllLines((Join-Path $bin 'reinforcement-balance.csv'),[string[]]$rows,[Text.UTF8Encoding]::new($false))
"PASS 1320 reinforcement balance runs in isolated test copy"
