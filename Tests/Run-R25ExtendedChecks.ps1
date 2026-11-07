$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$out=Join-Path $PSScriptRoot 'bin\R25ExtendedChecks.exe'
New-Item -ItemType Directory -Path (Split-Path $out) -Force | Out-Null
& $compiler /nologo /utf8output /codepage:65001 "/out:$out" (Join-Path $root 'Assets\Scripts\Core\Balance.cs') (Join-Path $root 'Assets\Scripts\Core\Battle.cs') (Join-Path $root 'Assets\Scripts\Core\CombatSave.cs') (Join-Path $PSScriptRoot 'R25ExtendedChecks.cs')
if($LASTEXITCODE -ne 0){throw 'Compilation failed'}
& $out
if($LASTEXITCODE -ne 0){throw 'Checks failed'}
