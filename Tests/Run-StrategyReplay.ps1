$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$bin = Join-Path $PSScriptRoot 'bin'
New-Item -ItemType Directory -Path $bin -Force | Out-Null
$core = @((Join-Path $projectRoot 'Assets\Scripts\Core\Balance.cs'), (Join-Path $projectRoot 'Assets\Scripts\Core\Battle.cs'))
$probe = Join-Path $bin 'StrategyProbe.exe'
$replay = Join-Path $bin 'ReplayStrategy.exe'
$log = Join-Path $projectRoot 'Docs\strategy-probe.txt'
& $compiler /nologo /utf8output /codepage:65001 "/out:$probe" $core (Join-Path $PSScriptRoot 'StrategyProbe.cs')
if ($LASTEXITCODE -ne 0) { throw 'Strategy probe compilation failed' }
$lines = & $probe
if ($LASTEXITCODE -ne 0) { throw 'Strategy probe failed' }
[System.IO.File]::WriteAllLines($log, [string[]]$lines, (New-Object System.Text.UTF8Encoding $false))
& $compiler /nologo /utf8output /codepage:65001 "/out:$replay" $core (Join-Path $PSScriptRoot 'ReplayStrategy.cs')
if ($LASTEXITCODE -ne 0) { throw 'Strategy replay compilation failed' }
& $replay $log
if ($LASTEXITCODE -ne 0) { throw 'Strategy replay failed' }
