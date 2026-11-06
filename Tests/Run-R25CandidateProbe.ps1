param([string]$OutputPath='', [string]$Factor='2.5', [switch]$Varied, [switch]$Idle, [switch]$Tuned)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$bin=Join-Path $PSScriptRoot 'bin'
New-Item -ItemType Directory -Path $bin -Force | Out-Null
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$exe=Join-Path $bin 'R25CandidateProbe.exe'
& $compiler /nologo /utf8output /codepage:65001 "/out:$exe" (Join-Path $root 'Assets\Scripts\Core\Balance.cs') (Join-Path $root 'Assets\Scripts\Core\Battle.cs') (Join-Path $root 'Assets\Scripts\Core\CombatSave.cs') (Join-Path $PSScriptRoot 'R25CandidateProbe.cs')
if($LASTEXITCODE -ne 0){throw 'Compilation failed'}
$prior=$env:R25_CANDIDATE_FACTOR
$priorVariation=$env:R25_CANDIDATE_VARIATION
$priorTuned=$env:R25_CANDIDATE_TUNED
try {
 $env:R25_CANDIDATE_FACTOR=$Factor
 $env:R25_CANDIDATE_VARIATION=if($Varied){'1'}else{'0'}
 $env:R25_CANDIDATE_TUNED=if($Tuned){'1'}else{'0'}
 $all=@()
 foreach($mode in @(0,1,2)){
  $rows=if($Idle){@(& $exe $mode 100 on 4 idle)}else{@(& $exe $mode 100 on 4)}
  $count=if($Idle){13}else{217}
  if($LASTEXITCODE -ne 0 -or $rows.Count -ne $count){throw "Unexpected simulation output for mode $mode"}
  if($mode -eq 0){$all+=$rows}else{$all+=$rows[1..($count-1)]}
 }
 if($OutputPath){[IO.File]::WriteAllLines([IO.Path]::GetFullPath($OutputPath),[string[]]$all,[Text.UTF8Encoding]::new($false))}
 else{$all}
} finally {$env:R25_CANDIDATE_FACTOR=$prior; $env:R25_CANDIDATE_VARIATION=$priorVariation; $env:R25_CANDIDATE_TUNED=$priorTuned}
