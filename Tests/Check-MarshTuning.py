"""Windows: compare the actual marsh candidate with the pre-tuning r28 commit."""
import csv
import hashlib
import io
import json
import os
from pathlib import Path
import statistics
import subprocess

root = Path(__file__).resolve().parents[1]
out = root / 'Tests/bin/r33-marsh'
out.mkdir(parents=True, exist_ok=True)
baseline = '0924dc4'
names = ['Assets/Scripts/Core/Balance.cs', 'Assets/Scripts/Core/Battle.cs',
         'Assets/Scripts/Core/CombatSave.cs', 'Tests/GameplayDurationProbe.cs',
         'Tests/WaveCounterProbe.cs']
snapshots = {name: (root / name).read_bytes() for name in names}
compiler = Path(os.environ['WINDIR']) / 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
columns = ['policy', 'mode', 'seed', 'ember', 'result', 'wave', 'peak',
           'target_peaks', 'over80_seconds', 'gold', 'wave_peaks']
results = {}
for variant in ['before', 'after']:
    folder = out / variant
    folder.mkdir(exist_ok=True)
    for name, raw in snapshots.items():
        if variant == 'before' and name.endswith('/Battle.cs'):
            raw = subprocess.check_output(['git', 'show', baseline + ':' + name], cwd=root)
        (folder / Path(name).name).write_bytes(raw)
    exe = folder / 'MarshReview.exe'
    subprocess.run([str(compiler), '/nologo', '/utf8output', '/codepage:65001',
                    '/main:WaveCounterProbe', '/out:' + str(exe)] +
                   [str(folder / Path(name).name) for name in names], check=True)
    run = subprocess.run([str(exe), '4', '3', '', 'bomb,smartB,archer', '0,1,2',
                          '29,41,57,73'], check=True, capture_output=True,
                         text=True, encoding='utf-8')
    rows = list(csv.DictReader(io.StringIO(run.stdout), fieldnames=columns))
    assert len(rows) == 72 and all(r['result'] in ('W', 'L') for r in rows)
    results[variant] = rows
key = lambda r: tuple(r[k] for k in ['policy', 'mode', 'seed', 'ember'])
known = {key(r): r for r in results['before']}
controls = [r for r in results['after'] if r['mode'] != '0']
assert len(controls) == 48
for row in controls:
    assert row == known[key(row)], (key(row), row, known[key(row)])
assert all(r['result'] == 'W' for r in results['after'] if r['mode'] == '0')
with (root / 'Docs/r33-marsh-real.csv').open('w', encoding='utf-8', newline='') as stream:
    writer = csv.DictWriter(stream, fieldnames=columns)
    writer.writeheader()
    writer.writerows(results['after'])
manifest = {'baseline_commit': baseline, 'rows_per_variant': 72,
            'unchanged_challenge_extreme_rows': 48,
            'source_sha256': {n: hashlib.sha256(b).hexdigest() for n, b in snapshots.items()},
            'limits': ['actual unpatched engine', 'fixed policies; first card; no reroll',
                       'four seeds, ember on/off; not human fun evidence']}
(root / 'Docs/r33-marsh-real-source.json').write_text(
    json.dumps(manifest, indent=2) + '\n', encoding='utf-8')
for policy in ['bomb', 'smartB', 'archer']:
    values = []
    for variant in ['before', 'after']:
        rows = [r for r in results[variant] if r['mode'] == '0' and r['policy'] == policy]
        values.append(f"{sum(r['result'] == 'W' for r in rows)}/8 peak " +
                      str(statistics.median(int(r['peak']) for r in rows)))
    print(policy + ': ' + ' -> '.join(values))
print('PASS 72 actual battles per variant, 48 unchanged challenge/extreme rows, all basic policies win')
