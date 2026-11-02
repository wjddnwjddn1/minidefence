param([int]$Density=100,[string]$Mode='base')
# Isolated supply-card experiment. Patches a temp copy of Battle.cs; the product is not changed.
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$bin=Join-Path $PSScriptRoot 'bin'
New-Item -ItemType Directory -Path $bin -Force | Out-Null
$s=[System.IO.File]::ReadAllText((Join-Path $root 'Assets\Scripts\Core\Battle.cs'),[System.Text.Encoding]::UTF8)
function Patch($old,$new){if(-not $script:s.Contains($old)){throw "Missing: $old"};$script:s=$script:s.Replace($old,$new)}
Patch 'damage=d.Damage*(u.Level>=2?1.4:1)' 'if(u.Kind==1)interval/=Card.ArcherHaste;damage=(u.Kind==0?Card.SwordDamage:1)*d.Damage*(u.Level>=2?1.4:1)'
Patch 'u.Branch==1?5:d.Targets' '(u.Branch==1?5:d.Targets)+Card.SwordTargets'
Patch 'Radius=u.Kind==2&&u.Branch==1?75:d.Radius' 'Radius=(u.Kind==2&&u.Branch==1?75:d.Radius)*(u.Kind==2?Card.BombRadius:1)'
Patch 'EmberCD=Balance.EmberCooldown' 'EmberCD=Balance.EmberCooldown*Card.EmberCool'
Patch 'Gold+=reward;' 'Gold+=(int)Math.Round(reward*Card.GoldMult);'
Patch 'Gold+=40+5*Wave;BeginWave();' 'Gold+=(int)Math.Round((40+5*Wave)*Card.GoldMult);BeginWave();'
Patch '<=UnitRange(u)' '<=UnitRange(u)*Card.RangeMult'
$copy=Join-Path $bin 'BattleCard.cs'
[System.IO.File]::WriteAllText($copy,$s,[System.Text.UTF8Encoding]::new($false))
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$exe=Join-Path $bin 'SupplyCardProbe.exe'
& $compiler /nologo /utf8output /codepage:65001 "/out:$exe" (Join-Path $root 'Assets\Scripts\Core\Balance.cs') $copy (Join-Path $root 'Assets\Scripts\Core\CombatSave.cs') (Join-Path $PSScriptRoot 'SupplyCardProbe.cs')
if($LASTEXITCODE -ne 0){throw 'Compilation failed'}
$rows=& $exe $Density $Mode
if($LASTEXITCODE -ne 0){throw 'Probe failed'}
[System.IO.File]::WriteAllLines((Join-Path $root "Docs\supply-card-probe-$Density-$Mode.csv"),[string[]]$rows,[System.Text.UTF8Encoding]::new($false))
"PASS $($rows.Count-1) isolated runs"
