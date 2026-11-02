$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$bin=Join-Path $PSScriptRoot 'bin'
$exe=Join-Path $bin 'EarlyCallProbe.exe'
& (Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe') /nologo /utf8output /codepage:65001 "/out:$exe" (Join-Path $root 'Assets\Scripts\Core\Balance.cs') (Join-Path $root 'Assets\Scripts\Core\Battle.cs') (Join-Path $root 'Assets\Scripts\Core\CombatSave.cs') (Join-Path $PSScriptRoot 'EarlyCallProbe.cs')
if($LASTEXITCODE -ne 0){throw 'Compilation failed'}
$rows=& $exe
[System.IO.File]::WriteAllLines((Join-Path $root 'Docs\early-call-probe.csv'),[string[]]$rows,[System.Text.UTF8Encoding]::new($false))
"PASS $($rows.Count-1)"
