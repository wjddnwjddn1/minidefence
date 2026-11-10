$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$compiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$output=Join-Path $PSScriptRoot 'bin/R30InteractionChecks.exe'
$core=Get-ChildItem (Join-Path $root 'Assets/Scripts/Core') -Filter *.cs | ForEach-Object FullName
& $compiler /nologo /utf8output /codepage:65001 "/out:$output" $core (Join-Path $root 'Assets/Scripts/Presentation/ArrowFlightView.cs') (Join-Path $PSScriptRoot 'R30InteractionChecks.cs')
if($LASTEXITCODE -ne 0){throw 'R30 checks compilation failed'}
& $output
if($LASTEXITCODE -ne 0){throw 'R30 checks failed'}
