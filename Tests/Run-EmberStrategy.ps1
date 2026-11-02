$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$output = Join-Path $PSScriptRoot 'bin\EmberStrategy.exe'
$report = Join-Path $projectRoot 'Docs\ember-balance.csv'
New-Item -ItemType Directory -Path (Split-Path $output) -Force | Out-Null
& $compiler /nologo /utf8output /codepage:65001 "/out:$output" (Join-Path $projectRoot 'Assets\Scripts\Core\Balance.cs') (Join-Path $projectRoot 'Assets\Scripts\Core\Battle.cs') (Join-Path $PSScriptRoot 'EmberStrategy.cs')
if ($LASTEXITCODE -ne 0) { throw 'C# compilation failed' }
$lines = & $output
if ($LASTEXITCODE -ne 0) { throw 'Ember strategy simulation failed' }
[System.IO.File]::WriteAllLines($report, [string[]]$lines, (New-Object System.Text.UTF8Encoding $false))
Write-Output ("PASS " + ($lines.Count-1) + " strategy runs; report " + $report)
