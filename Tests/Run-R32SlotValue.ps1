$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$compiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$output=Join-Path $PSScriptRoot 'bin/R32SlotValueProbe.exe'
$core=Get-ChildItem (Join-Path $root 'Assets/Scripts/Core') -Filter *.cs | ForEach-Object FullName
& $compiler /nologo /utf8output /codepage:65001 /main:R32SlotValueProbe "/out:$output" $core (Join-Path $PSScriptRoot 'GameplayDurationProbe.cs') (Join-Path $PSScriptRoot 'R32SlotValueProbe.cs')
if($LASTEXITCODE -ne 0){throw 'Slot value compilation failed'}
$processes=@()
foreach($field in 0..3){
 $csv=Join-Path $PSScriptRoot ('bin/r32-slot-value-'+$field+'.csv')
 $log=Join-Path $PSScriptRoot ('bin/r32-slot-value-'+$field+'.log')
 $errorLog=Join-Path $PSScriptRoot ('bin/r32-slot-value-'+$field+'.error.log')
 $process=Start-Process -FilePath $output -ArgumentList @([string]$field,$csv) -WindowStyle Hidden -RedirectStandardOutput $log -RedirectStandardError $errorLog -PassThru
 $null=$process.Handle # Retain the exit handle even if a fast child exits before WaitForExit.
 $processes+=$process
}
foreach($p in $processes){$p.WaitForExit();$p.Refresh();if($p.ExitCode -ne 0){throw ('Slot value process failed '+$p.Id)}}
