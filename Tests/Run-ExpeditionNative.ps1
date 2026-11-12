$ErrorActionPreference='Stop'
$executable=Join-Path $PSScriptRoot 'bin/ExpeditionQA/EmberBastion.exe'
if(-not(Test-Path -LiteralPath $executable)){throw 'Build ComparisonBuild.ExpeditionQA first'}
foreach($phase in @('prepare','resume')){
 $log=Join-Path $PSScriptRoot ('bin/r37-native-'+$phase+'.log')
 $arguments=@('--expedition-review','-logFile',('"'+$log+'"'))
 if($phase -eq 'prepare'){$arguments+='--prepare'}
 $process=Start-Process -FilePath $executable -ArgumentList $arguments -WindowStyle Hidden -PassThru
 $null=$process.Handle
 if(-not $process.WaitForExit(180000)){Stop-Process -Id $process.Id -Force;throw ('Own native '+$phase+' run timed out')}
 $process.Refresh()
 if($process.ExitCode -ne 0){Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'bin/ExpeditionQA/Evidence/failure.txt');throw ('Native '+$phase+' failed')}
}
Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'bin/ExpeditionQA/Evidence/PASS.txt')
