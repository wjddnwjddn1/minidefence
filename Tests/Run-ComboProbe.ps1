param([ValidateRange(0,2)][int]$Mode=1,[ValidateRange(50,200)][int]$Density=100,[ValidateSet('on','off')][string]$Bundle='on',[switch]$Actual)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$bin=Join-Path $PSScriptRoot 'bin'
New-Item -ItemType Directory -Path $bin -Force | Out-Null
$source=[System.IO.File]::ReadAllText((Join-Path $root 'Assets\Scripts\Core\Battle.cs'),[System.Text.Encoding]::UTF8)
# Reconstruct the unmodified baseline in a temporary test copy; never write engine files.
if(-not $Actual){
 $source=$source.Replace('Hit(targets[i],damage*(FastWaves&&!HardWaves?Balance.Enemies[targets[i].Kind].MeleeTaken:1),d.Pen,u.Branch==2,1,false,1,0)','Hit(targets[i],damage,d.Pen,u.Branch==2,1,false,1,0)')
 $source=$source.Replace('Hit(target,s.Damage*(FastWaves&&!HardWaves&&s.Kind==1&&Balance.ArcherFavored(target.Kind)?Balance.ArcherFavoredMult:1),s.Pen,false,s.BossMult,false,1,s.Kind)','Hit(target,s.Damage,s.Pen,false,s.BossMult,false,1,s.Kind)')
 $source=$source.Replace('Hit(e,s.Damage*(FastWaves&&!HardWaves?Balance.Enemies[e.Kind].BlastTaken:1),s.Pen,false,1,false,1,2)','Hit(e,s.Damage,s.Pen,false,1,false,1,2)')
}
$replacements=@{
 'Hit(targets[i],damage,d.Pen,u.Branch==2,1,false,1,0)'='Hit(targets[i],damage*Exp.Melee(targets[i].Kind),d.Pen,u.Branch==2,1,false,1,0)'
 'Hit(target,s.Damage,s.Pen,false,s.BossMult,false,1,s.Kind)'='Hit(target,s.Damage*(s.Kind==1?Exp.Archer(target.Kind):1),s.Pen,false,s.BossMult,false,1,s.Kind)'
 'Hit(e,s.Damage,s.Pen,false,1,false,1,2)'='Hit(e,s.Damage*Exp.Blast(e.Kind),s.Pen,false,1,false,1,2)'
}
if(-not $Actual){foreach($key in $replacements.Keys){if(-not $source.Contains($key)){throw "Expected battle expression not found: $key"};$source=$source.Replace($key,$replacements[$key])}}
$copy=Join-Path $bin 'ComboBattle.cs'
[System.IO.File]::WriteAllText($copy,$source,[System.Text.UTF8Encoding]::new($false))
$output=Join-Path $bin 'ComboProbe.exe'
& $compiler /nologo /utf8output /codepage:65001 "/out:$output" (Join-Path $root 'Assets\Scripts\Core\Balance.cs') $copy (Join-Path $root 'Assets\Scripts\Core\CombatSave.cs') (Join-Path $PSScriptRoot 'ComboProbe.cs')
if($LASTEXITCODE -ne 0){throw 'Compilation failed'}
$rows=& $output $Mode $Density $Bundle
if($LASTEXITCODE -ne 0){throw 'Simulation failed'}
if($rows.Count -ne 217){throw "Expected 216 scenarios, received $($rows.Count-1)"}
$dest=Join-Path $root "Docs\combo-probe-$Mode-$Density-$Bundle$(if($Actual){'-actual'}).csv"
[System.IO.File]::WriteAllLines($dest,[string[]]$rows,[System.Text.UTF8Encoding]::new($false))
"PASS execution 216 scenarios; $dest"
