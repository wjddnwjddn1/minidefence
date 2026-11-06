$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$bin=Join-Path $PSScriptRoot 'bin'
New-Item -ItemType Directory -Path $bin -Force | Out-Null
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$source=[System.IO.File]::ReadAllText((Join-Path $root 'Assets\Scripts\Core\Battle.cs'),[System.Text.Encoding]::UTF8)
$pattern='rng=seed;RulesVersion=2;FieldIndex=fieldIndex;'
if(-not $source.Contains($pattern)){throw 'Unexpected Battle constructor'}
$hashes=@()
foreach($version in @(1,2)){
 $copy=Join-Path $bin "RuleParityBattle$version.cs"
 $code=if($version -eq 1){$source.Replace($pattern,'rng=seed;RulesVersion=1;FieldIndex=fieldIndex;')}else{$source}
 [System.IO.File]::WriteAllText($copy,$code,[System.Text.UTF8Encoding]::new($false))
 $exe=Join-Path $bin "RuleParity$version.exe"
 & $compiler /nologo /utf8output /codepage:65001 "/out:$exe" (Join-Path $root 'Assets\Scripts\Core\Balance.cs') $copy (Join-Path $root 'Assets\Scripts\Core\CombatSave.cs') (Join-Path $PSScriptRoot 'ComboProbe.cs')
 if($LASTEXITCODE -ne 0){throw 'Compilation failed'}
 $rows=& $exe 1 100 on
 if($LASTEXITCODE -ne 0 -or $rows.Count -ne 217){throw 'Simulation failed'}
 $csv=Join-Path $bin "rule-parity$version.csv"
 [System.IO.File]::WriteAllLines($csv,[string[]]$rows,[System.Text.UTF8Encoding]::new($false))
 $hashes+=(Get-FileHash $csv -Algorithm SHA256).Hash
}
if($hashes[0] -ne $hashes[1] -or $hashes[0] -ne '9FD2B168AB217A4B15D24FDC0E120048F17D612AAF6FB2702EB4021E278A5A11'){throw "Unexpected rule parity: $hashes"}
"PASS 216 scenarios: v1 == v2 == $($hashes[0])"
