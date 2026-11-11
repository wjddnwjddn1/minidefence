$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$executable=Join-Path $PSScriptRoot 'bin/RulesMigrationQA/EmberBastion.exe'
foreach($classic in @($false,$true)){
 $started=[DateTime]::UtcNow
 $arguments=@('--rules-migration-qa','-screen-fullscreen','0','-logFile',(Join-Path $PSScriptRoot ('bin/r32-migration-'+$classic+'.log')))
 if($classic){$arguments+='--classic-rules'}
 $process=Start-Process -FilePath $executable -ArgumentList $arguments -WindowStyle Hidden -PassThru
 if(-not $process.WaitForExit(120000)){if(-not $process.HasExited){Stop-Process -Id $process.Id -Force};throw 'Own migration diagnostic timed out'}
 $process.Refresh();if($process.ExitCode -ne 0){throw ('Migration failed '+$classic+' exit '+$process.ExitCode)}
 $latest=Get-ChildItem (Join-Path $PSScriptRoot 'bin/RulesMigrationQA/Evidence') -Directory | Where-Object {$_.LastWriteTimeUtc -ge $started.AddSeconds(-2)} | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
 if(-not $latest -or -not (Test-Path -LiteralPath (Join-Path $latest.FullName 'PASS.txt'))){throw 'Missing migration evidence'}
 Write-Output ('MIGRATION_OUTPUT '+$latest.FullName)
 Get-Content -Encoding UTF8 -LiteralPath (Join-Path $latest.FullName 'result.json')
}
