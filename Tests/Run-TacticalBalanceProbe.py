"""Compare frozen live-source copies. Never modifies game code or user saves."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Tests/bin/tactical-balance'
OUT.mkdir(parents=True, exist_ok=True)
SOURCES = ['Assets/Scripts/Core/Balance.cs', 'Assets/Scripts/Core/Battle.cs',
           'Assets/Scripts/Core/CombatSave.cs', 'Tests/GameplayDurationProbe.cs',
           'Tests/TacticalBalanceProbe.cs']
snapshots = {name: (ROOT / name).read_bytes() for name in SOURCES}
compiler = Path(os.environ['WINDIR']) / 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
profiles = {'R3': None, 'C': None, 'CSoft': (.75, .75, 1.25), 'CStrong': (.5, .5, 1.5),
            'CShieldOnly': (None, .5, None), 'CArcherOnly': (None, None, 1.5),
            'CShieldArcher': (None, .5, 1.5)}
parser = argparse.ArgumentParser()
parser.add_argument('--profiles', nargs='+', choices=list(profiles), default=['R3', 'C', 'CSoft', 'CStrong'])
parser.add_argument('--output', default='Docs/t4-role-comparison.csv')
args = parser.parse_args()
profiles = {name: profiles[name] for name in args.profiles}
all_rows = []
for profile, values in profiles.items():
    folder = OUT / profile
    folder.mkdir(exist_ok=True)
    for name, raw in snapshots.items():
        text = raw.decode('utf-8-sig')
        if name.endswith('/Battle.cs') and values:
            melee, blast, archer = values
            replacements = {
                'FastWaves&&!HardWaves?Balance.Enemies[targets[i].Kind].MeleeTaken:1':
                    f'HasNormalCards?(targets[i].Kind==1?{melee}:1):(FastWaves&&!HardWaves?Balance.Enemies[targets[i].Kind].MeleeTaken:1)',
                'FastWaves&&!HardWaves?Balance.Enemies[e.Kind].BlastTaken:1':
                    f'HasNormalCards?(e.Kind==2?{blast}:1):(FastWaves&&!HardWaves?Balance.Enemies[e.Kind].BlastTaken:1)',
                'FastWaves&&!HardWaves&&s.Kind==1&&Balance.ArcherFavored(target.Kind)?Balance.ArcherFavoredMult:1':
                    f'HasNormalCards?(s.Kind==1&&Balance.ArcherFavored(target.Kind)?{archer}:1):(FastWaves&&!HardWaves&&s.Kind==1&&Balance.ArcherFavored(target.Kind)?Balance.ArcherFavoredMult:1)',
            }
            for (old, new), value in zip(replacements.items(), values):
                if value is None:
                    continue
                assert text.count(old) == 1, old
                text = text.replace(old, new)
        (folder / Path(name).name).write_text(text, encoding='utf-8')
    exe = folder / 'Probe.exe'
    subprocess.run([str(compiler), '/nologo', '/utf8output', '/codepage:65001',
                    '/main:TacticalBalanceProbe', f'/out:{exe}'] +
                   [str(folder / Path(name).name) for name in SOURCES], check=True)
    result = subprocess.run([str(exe), profile], check=True, capture_output=True, encoding='utf-8')
    rows = result.stdout.splitlines()
    assert len(rows) == 1153, (profile, len(rows))
    (folder / 'results.csv').write_text('\n'.join(rows) + '\n', encoding='utf-8')
    all_rows.extend(rows if not all_rows else rows[1:])
    print(f'PASS {profile}: 1152 battles', flush=True)
target = ROOT / args.output
target.write_text('\n'.join(all_rows) + '\n', encoding='utf-8')
manifest = {'base_commit': subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=ROOT, text=True).strip(),
            'source_sha256': {name: hashlib.sha256(raw).hexdigest() for name, raw in snapshots.items()},
            'profiles': profiles, 'rows': len(all_rows)-1,
            'limitations': ['first offered card, no rerolls', 'fixed purchase/upgrade policies',
                            'one-slot swaps differ by 5G base cost', 'not human play or fun evidence']}
target.with_name(target.stem + '-source.json').write_text(
    json.dumps(manifest, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
print(f'PASS {len(all_rows)-1} frozen-source battles: {target}', flush=True)
