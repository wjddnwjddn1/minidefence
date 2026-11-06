param([string]$OutputPath = '')
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$bin=Join-Path $PSScriptRoot 'bin'
New-Item -ItemType Directory -Path $bin -Force | Out-Null
$csc=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$exe=Join-Path $bin 'GameplayDurationProbe.exe'
try {
 & $csc /nologo /utf8output /codepage:65001 "/out:$exe" (Join-Path $root 'Assets\Scripts\Core\Balance.cs') (Join-Path $root 'Assets\Scripts\Core\Battle.cs') (Join-Path $root 'Assets\Scripts\Core\CombatSave.cs') (Join-Path $PSScriptRoot 'GameplayDurationProbe.cs')
 if($LASTEXITCODE -ne 0){throw 'Probe compilation failed'}
 $all=@()
 foreach($mode in 0,1,2){$rows=& $exe $mode 100 on;if($LASTEXITCODE -ne 0 -or $rows.Count -ne 217){throw "Unexpected output for difficulty $mode"};if($mode -eq 0){$all+=$rows}else{$all+=$rows[1..216]}}
 if($OutputPath){[IO.File]::WriteAllLines([IO.Path]::GetFullPath($OutputPath),[string[]]$all,[Text.UTF8Encoding]::new($false))}
 else{$all}
} finally {if(Test-Path -LiteralPath $exe){Remove-Item -LiteralPath $exe}}
