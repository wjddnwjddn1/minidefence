$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$compiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$output=Join-Path $PSScriptRoot 'bin/SpawnForecastChecks.exe'
$core=Get-ChildItem (Join-Path $root 'Assets/Scripts/Core') -Filter *.cs | ForEach-Object FullName
& $compiler /nologo /utf8output /codepage:65001 "/out:$output" $core (Join-Path $root 'Assets/Scripts/Presentation/SpawnForecast.cs') (Join-Path $PSScriptRoot 'SpawnForecastChecks.cs')
if($LASTEXITCODE -ne 0){throw 'Spawn forecast compilation failed'}
& $output
if($LASTEXITCODE -ne 0){throw 'Spawn forecast checks failed'}
