param([switch]$Visible,[switch]$Story,[ValidateSet('d3d11','d3d12')][string]$Graphics='d3d11')
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$executable=Join-Path $PSScriptRoot 'bin\PerformanceQA\EmberBastion.exe'
if(-not (Test-Path -LiteralPath $executable)){throw 'Build ComparisonBuild.PerformanceQA first'}
$taskStarted=[DateTime]::UtcNow
$log=Join-Path $PSScriptRoot ('bin\native-performance-'+$taskStarted.ToString('yyyyMMdd-HHmmss')+'.log')
$arguments=@('--performance-smoke',('-force-'+$Graphics),'-screen-width','1280','-screen-height','720','-screen-fullscreen','0','-logFile',('"'+$log+'"'))
# Explicit -Visible is an operator request to show this diagnostic window.
# Hidden mode cannot measure rendering, so it runs only CPU/save diagnostics.
if(-not $Visible){$arguments+='--storage-only'}
if($Story){$arguments+='--story-performance'}
$style=if($Visible){'Normal'}else{'Hidden'}
$process=Start-Process -FilePath $executable -ArgumentList $arguments -WindowStyle $style -PassThru
if(-not $process.WaitForExit(600000)){if(-not $process.HasExited){Stop-Process -Id $process.Id -Force};throw 'Own diagnostic exceeded 10 minute timeout'}
$process.Refresh()
$evidenceRoot=Join-Path $PSScriptRoot 'bin\PerformanceQA\PerformanceEvidence'
$latest=Get-ChildItem -LiteralPath $evidenceRoot -Directory | Where-Object {$_.LastWriteTimeUtc -ge $taskStarted.AddSeconds(-2)} | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
if($latest){Write-Output ('PERFORMANCE_OUTPUT '+$latest.FullName)}
if($process.ExitCode -ne 0){if($latest -and (Test-Path -LiteralPath (Join-Path $latest.FullName 'failure.txt'))){Get-Content -Encoding UTF8 -LiteralPath (Join-Path $latest.FullName 'failure.txt')};throw "Native diagnostic failed: $($process.ExitCode)"}
if(-not $latest -or -not (Test-Path -LiteralPath (Join-Path $latest.FullName 'result.txt'))){throw 'Missing native result evidence'}
Get-Content -Encoding UTF8 -LiteralPath (Join-Path $latest.FullName 'result.txt')
