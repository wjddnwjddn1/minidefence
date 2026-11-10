$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$output=Join-Path $PSScriptRoot 'bin\PerformanceScenarioChecks.exe'
New-Item -ItemType Directory -Path (Split-Path $output) -Force | Out-Null
& $compiler /nologo /utf8output /codepage:65001 /define:DEVELOPMENT_BUILD "/out:$output" (Join-Path $root 'Assets\Scripts\Core\Balance.cs') (Join-Path $root 'Assets\Scripts\Core\Battle.cs') (Join-Path $root 'Assets\Scripts\Core\CombatSave.cs') (Join-Path $root 'Assets\Scripts\Presentation\PerformanceScenario.cs') (Join-Path $PSScriptRoot 'PerformanceScenarioChecks.cs')
if($LASTEXITCODE -ne 0){throw 'Performance scenario compilation failed'}
& $output
if($LASTEXITCODE -ne 0){throw 'Performance scenario checks failed'}
