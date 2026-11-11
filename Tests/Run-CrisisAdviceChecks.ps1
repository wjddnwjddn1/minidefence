$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$output=Join-Path $PSScriptRoot 'bin\CrisisAdviceChecks.exe'
New-Item -ItemType Directory -Path (Split-Path $output) -Force | Out-Null
& $compiler /nologo /utf8output /codepage:65001 "/out:$output" (Join-Path $root 'Assets\Scripts\Core\Balance.cs') (Join-Path $root 'Assets\Scripts\Core\Battle.cs') (Join-Path $root 'Assets\Scripts\Core\CombatSave.cs') (Join-Path $root 'Assets\Scripts\Presentation\CrisisAdvice.cs') (Join-Path $PSScriptRoot 'CrisisAdviceChecks.cs')
if($LASTEXITCODE -ne 0){throw 'Crisis advice compilation failed'}
& $output
if($LASTEXITCODE -ne 0){throw 'Crisis advice checks failed'}
