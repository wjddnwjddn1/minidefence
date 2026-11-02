$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$output=Join-Path $PSScriptRoot 'bin\BombStrategy.exe'
New-Item -ItemType Directory -Path (Split-Path $output) -Force | Out-Null
& $compiler /nologo /utf8output /codepage:65001 "/out:$output" (Join-Path $root 'Assets\Scripts\Core\Balance.cs') (Join-Path $root 'Assets\Scripts\Core\Battle.cs') (Join-Path $root 'Assets\Scripts\Core\CombatSave.cs') (Join-Path $PSScriptRoot 'BombStrategy.cs')
if($LASTEXITCODE -ne 0){throw 'Compilation failed'}
$results=& $output
if($LASTEXITCODE -ne 0){throw 'Simulation failed'}
$results | Set-Content -Encoding utf8 (Join-Path $root 'Docs\bomb-balance.csv')
'PASS 54 bomb-branch strategy runs'
