"""Compare frozen live-source copies. Never modifies game code or user saves."""
import csv
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
parser.add_argument('--policy-set', choices=['baseline','roles'], default='baseline')
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
    result = subprocess.run([str(exe), profile, args.policy_set], check=True, capture_output=True, encoding='utf-8')
    rows = result.stdout.splitlines()
    expected = 1344 if args.policy_set == 'roles' else 1152
    assert len(rows) == expected + 1, (profile, len(rows))
    (folder / 'results.csv').write_text('\n'.join(rows) + '\n', encoding='utf-8')
    all_rows.extend(rows if not all_rows else rows[1:])
    print(f'PASS {profile}: {expected} battles', flush=True)
if args.policy_set == 'roles':
    baseline = []
    for name in ['t4-role-comparison.csv', 't4-role-ablation.csv']:
        with (ROOT / 'Docs' / name).open(encoding='utf-8-sig') as stream:
            baseline.extend(csv.DictReader(stream))
    key = lambda row: tuple(row[k] for k in ['profile','seed','mode','field','policy','ember'])
    known = {key(row): row for row in baseline if row['policy'] == 'smartB'}
    controls = [row for row in csv.DictReader(all_rows) if row['policy'] == 'smartB']
    for row in controls:
        old = known[key(row)]
        for column in ['result','wave','peak','seconds','gold','invested','level3','cards','card_ids','wave_peaks','wave_gold']:
            assert row[column] == old[column], (key(row), column, row[column], old[column])
    print(f'PASS {len(controls)} unchanged smartB controls against frozen baseline', flush=True)
target = ROOT / args.output
target.write_text('\n'.join(all_rows) + '\n', encoding='utf-8')
manifest = {'base_commit': subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=ROOT, text=True).strip(),
            'source_sha256': {name: hashlib.sha256(raw).hexdigest() for name, raw in snapshots.items()},
            'profiles': profiles, 'policy_set': args.policy_set, 'rows': len(all_rows)-1,
            'limitations': ['first offered card, no rerolls', 'fixed purchase/upgrade policies',
                            'one-slot swaps differ by 5G base cost', 'not human play or fun evidence']}
target.with_name(target.stem + '-source.json').write_text(
    json.dumps(manifest, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
print(f'PASS {len(all_rows)-1} frozen-source battles: {target}', flush=True)
