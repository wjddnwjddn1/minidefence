$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$bin = Join-Path $PSScriptRoot 'bin\PressureProbe.exe'
New-Item -ItemType Directory -Path (Split-Path $bin) -Force | Out-Null
& $compiler /nologo /utf8output /codepage:65001 "/out:$bin" (Join-Path $root 'Assets\Scripts\Core\Balance.cs') (Join-Path $root 'Assets\Scripts\Core\Battle.cs') (Join-Path $root 'Assets\Scripts\Core\CombatSave.cs') (Join-Path $PSScriptRoot 'PressureProbe.cs')
if ($LASTEXITCODE -ne 0) { throw 'Pressure probe compilation failed' }
$rows = & $bin
if ($LASTEXITCODE -ne 0) { throw 'Pressure probe failed' }
if ($rows.Count -ne 721) { throw "Expected header and 720 runs; got $($rows.Count) lines" }
[System.IO.File]::WriteAllLines((Join-Path $root 'Docs\pressure-probe.csv'), [string[]]$rows, [System.Text.UTF8Encoding]::new($false))
'PASS 720 isolated pressure scenarios; actual game balance unchanged'
