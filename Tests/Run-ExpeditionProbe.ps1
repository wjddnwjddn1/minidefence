param([ValidateSet('laps','run','doctrine','build','order')][string]$Command='run',[ValidateRange(1,64)][int]$Seeds=8,[string]$Out='')
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$exe=Join-Path $PSScriptRoot 'bin\ExpeditionProbe.exe'
New-Item -ItemType Directory -Path (Split-Path $exe) -Force | Out-Null
& $compiler /nologo /optimize+ /utf8output /codepage:65001 "/out:$exe" (Join-Path $root 'Assets\Scripts\Core\Balance.cs') (Join-Path $root 'Assets\Scripts\Core\Battle.cs') (Join-Path $root 'Assets\Scripts\Core\CombatSave.cs') (Join-Path $root 'Assets\Scripts\Core\Expedition.cs') (Join-Path $PSScriptRoot 'ExpeditionProbe.cs')
if($LASTEXITCODE -ne 0){throw 'Compilation failed'}
$rows=& $exe $Command $Seeds
if($LASTEXITCODE -ne 0){throw 'Probe failed'}
if($Out){[IO.File]::WriteAllLines($Out,[string[]]$rows,[Text.UTF8Encoding]::new($false));"wrote $($rows.Count) rows to $Out"}else{$rows}
