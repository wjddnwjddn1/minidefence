param([string]$OutputPath='', [string]$Factor='2.5')
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$bin=Join-Path $PSScriptRoot 'bin'
New-Item -ItemType Directory -Path $bin -Force | Out-Null
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$exe=Join-Path $bin 'R25CandidateProbe.exe'
& $compiler /nologo /utf8output /codepage:65001 "/out:$exe" (Join-Path $root 'Assets\Scripts\Core\Balance.cs') (Join-Path $root 'Assets\Scripts\Core\Battle.cs') (Join-Path $root 'Assets\Scripts\Core\CombatSave.cs') (Join-Path $PSScriptRoot 'R25CandidateProbe.cs')
if($LASTEXITCODE -ne 0){throw 'Compilation failed'}
$prior=$env:R25_CANDIDATE_FACTOR
try {
 $env:R25_CANDIDATE_FACTOR=$Factor
 $all=@()
 foreach($mode in @(0,1,2)){
  $rows=& $exe $mode 100 on 4
  if($LASTEXITCODE -ne 0 -or $rows.Count -ne 217){throw "Unexpected simulation output for mode $mode"}
  if($mode -eq 0){$all+=$rows}else{$all+=$rows[1..216]}
 }
 if($OutputPath){[IO.File]::WriteAllLines([IO.Path]::GetFullPath($OutputPath),[string[]]$all,[Text.UTF8Encoding]::new($false))}
 else{$all}
} finally {$env:R25_CANDIDATE_FACTOR=$prior}
