$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$output = Join-Path $PSScriptRoot 'bin\EmberUpgradeChecks.exe'
New-Item -ItemType Directory -Path (Split-Path $output) -Force | Out-Null
& $compiler /nologo /utf8output /codepage:65001 "/out:$output" (Join-Path $projectRoot 'Assets\Scripts\Core\Balance.cs') (Join-Path $projectRoot 'Assets\Scripts\Core\Battle.cs') (Join-Path $projectRoot 'Assets\Scripts\Core\CombatSave.cs') (Join-Path $PSScriptRoot 'EmberUpgradeChecks.cs')
if ($LASTEXITCODE -ne 0) { throw 'C# compilation failed' }
& $output
if ($LASTEXITCODE -ne 0) { throw 'Ember upgrade checks failed' }
