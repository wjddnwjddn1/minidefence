# Claude's independent T1 review checks (Docs/CLAUDE-REVIEW.md 31차), Windows runner.
# Not named Run-*Checks.ps1 on purpose: it is expected to fail until T1 lands.
# Usage: powershell -NoProfile -ExecutionPolicy Bypass -File Tests/Review-T1Checks.ps1 [current|api]
# Exit: 0 all pass, 1 regression ([보존]) or error, 2 only [T1요구] failures / api stage not compiling yet.
param([string]$Stage='all')
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$bin=Join-Path $PSScriptRoot 'bin'
New-Item -ItemType Directory -Path $bin -Force | Out-Null
$sources=@(Get-ChildItem (Join-Path $root 'Assets\Scripts\Core') -Filter *.cs | Sort-Object Name | ForEach-Object FullName)
$sources+=Join-Path $root 'Assets\Scripts\Presentation\ProgressStore.cs'
# Assumption A5: the extracted win-record helper may live anywhere under Assets/Scripts.
Get-ChildItem (Join-Path $root 'Assets\Scripts') -Recurse -Filter *.cs | Where-Object { (Get-Content -Raw -Encoding UTF8 $_.FullName) -match '\bclass\s+VictoryRecords\b' } | ForEach-Object { if($sources -notcontains $_.FullName){$sources+=$_.FullName} }
function Invoke-Stage([string]$name,[string[]]$tests,[string]$main){
 $exe=Join-Path $bin "$name.exe"
 $files=$sources+($tests | ForEach-Object { Join-Path $PSScriptRoot $_ })
 $log=& $compiler /nologo /utf8output /codepage:65001 "/main:$main" "/out:$exe" $files
 if($LASTEXITCODE -ne 0){Write-Host "COMPILE FAIL [$name]"; $log | Select-String ': error ' | Select-Object -First 12 | ForEach-Object { Write-Host ('  '+$_) }; return -1}
 & $exe | Out-Host
 return $LASTEXITCODE
}
$codes=@()
if($Stage -eq 'all' -or $Stage -eq 'current'){Write-Host '== current API: Tests/T1ReviewChecks.cs'; $codes+=Invoke-Stage 'T1ReviewChecks' @('T1ReviewChecks.cs') 'T1ReviewChecks'}
if($Stage -eq 'all' -or $Stage -eq 'api'){Write-Host '== assumed T1 API: Tests/T1ReviewApiChecks.cs'; $c=Invoke-Stage 'T1ReviewApiChecks' @('T1ReviewChecks.cs','T1ReviewApiChecks.cs') 'T1ReviewApiChecks'; if($c -eq -1){$c=2}; $codes+=$c}
if($codes | Where-Object { $_ -ne 0 -and $_ -ne 2 }){exit 1}
if($codes -contains 2){exit 2}
exit 0
