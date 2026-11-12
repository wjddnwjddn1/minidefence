$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$compiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$core=Get-ChildItem (Join-Path $root 'Assets/Scripts/Core') -Filter *.cs | ForEach-Object FullName
$output=Join-Path $PSScriptRoot 'bin/ExpeditionSessionChecks.exe'
& $compiler /nologo /utf8output /codepage:65001 "/out:$output" $core (Join-Path $PSScriptRoot 'ExpeditionSessionChecks.cs')
if($LASTEXITCODE -ne 0){throw 'Compilation failed'}
& $output
if($LASTEXITCODE -ne 0){throw 'Expedition session checks failed'}
