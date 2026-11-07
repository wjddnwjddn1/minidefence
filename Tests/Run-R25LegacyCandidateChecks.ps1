$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$bin=Join-Path $PSScriptRoot 'bin'
New-Item -ItemType Directory -Path $bin -Force | Out-Null
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$source=[IO.File]::ReadAllText((Join-Path $root 'Assets\Scripts\Core\Battle.cs'),[Text.Encoding]::UTF8)
$pattern='rng=seed;RulesVersion=3;FieldIndex=fieldIndex;'
if(-not $source.Contains($pattern)){throw 'Unexpected battle constructor'}
$copy=Join-Path $bin 'R25LegacyCandidateBattle.cs'
[IO.File]::WriteAllText($copy,$source.Replace($pattern,'rng=seed;RulesVersion=1;FieldIndex=fieldIndex;'),[Text.UTF8Encoding]::new($false))
$exe=Join-Path $bin 'R25LegacyCandidate.exe'
& $compiler /nologo /utf8output /codepage:65001 "/out:$exe" (Join-Path $root 'Assets\Scripts\Core\Balance.cs') $copy (Join-Path $root 'Assets\Scripts\Core\CombatSave.cs') (Join-Path $PSScriptRoot 'R25CandidateProbe.cs')
if($LASTEXITCODE -ne 0){throw 'Compilation failed'}
$prior=$env:R25_CANDIDATE_FACTOR
$priorVariation=$env:R25_CANDIDATE_VARIATION
$priorTuned=$env:R25_CANDIDATE_TUNED
$priorDistinct=$env:R25_CANDIDATE_DISTINCT
try {
 $env:R25_CANDIDATE_FACTOR='2.5'
 $env:R25_CANDIDATE_VARIATION='0'
 $env:R25_CANDIDATE_TUNED='0'
 $env:R25_CANDIDATE_DISTINCT='0'
 $baseline=@(Get-Content -Encoding utf8 (Join-Path $root 'Docs\r24-gameplay-baseline.csv'))
 foreach($mode in @(0,1,2)){
  $actual=@(& $exe $mode 100 on 4)
  if($LASTEXITCODE -ne 0 -or $actual.Count -ne 217){throw "Unexpected candidate output in mode $mode"}
  $expected=@($baseline[0])+@($baseline | Select-Object -Skip 1 | Where-Object {($_ -split ',')[1] -eq [string]$mode})
  if($expected.Count -ne $actual.Count){throw 'Baseline row count mismatch'}
  for($i=0;$i -lt $actual.Count;$i++){if($actual[$i] -cne $expected[$i]){throw "Legacy result mismatch at mode $mode row $i"}}
 }
 'PASS 648 legacy battles across all difficulties match baseline with four extra candidate waves' 
} finally {$env:R25_CANDIDATE_FACTOR=$prior; $env:R25_CANDIDATE_VARIATION=$priorVariation; $env:R25_CANDIDATE_TUNED=$priorTuned; $env:R25_CANDIDATE_DISTINCT=$priorDistinct}
