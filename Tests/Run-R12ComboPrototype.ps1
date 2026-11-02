param([ValidateSet(0,1,2)][int]$Mode=1,[ValidateSet(100,125)][int]$Density=125)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$bin=Join-Path $PSScriptRoot 'bin'
New-Item -ItemType Directory -Path $bin -Force | Out-Null
$source=[System.IO.File]::ReadAllText((Join-Path $PSScriptRoot 'ComboProbe.cs'),[System.Text.Encoding]::UTF8)
$old='foreach(var x in baseN){x.Key.Count='
if(-not $source.Contains($old)){throw 'Unexpected ComboProbe source'}
$source=$source.Replace($old,'foreach(var x in baseN){if(x.Key.Kind==4)continue;x.Key.Count=')
$copy=Join-Path $bin 'ComboProbeR12.cs'
[System.IO.File]::WriteAllText($copy,$source,[System.Text.UTF8Encoding]::new($false))
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$exe=Join-Path $bin 'ComboProbeR12.exe'
& $compiler /nologo /utf8output /codepage:65001 "/out:$exe" (Join-Path $root 'Assets\Scripts\Core\Balance.cs') (Join-Path $root 'Assets\Scripts\Core\Battle.cs') (Join-Path $root 'Assets\Scripts\Core\CombatSave.cs') $copy
if($LASTEXITCODE -ne 0){throw 'Compilation failed'}
$rows=& $exe $Mode $Density on
if($LASTEXITCODE -ne 0 -or $rows.Count -ne 217){throw 'Probe failed'}
$dest=Join-Path $root "Docs\r12-probe-$Mode-$Density.csv"
[System.IO.File]::WriteAllLines($dest,[string[]]$rows,[System.Text.UTF8Encoding]::new($false))
"PASS 216 isolated runs; boss groups unchanged; $dest"
