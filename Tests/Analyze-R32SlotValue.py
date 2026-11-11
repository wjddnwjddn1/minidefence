"""Summarize paired, unmodified rules-4 slot experiments; no third-party modules."""
import csv
import hashlib
import json
import statistics
import sys
from collections import defaultdict
from pathlib import Path

SEEDS = {29, 41, 57, 73, 101, 137, 181, 223}
root = Path(__file__).resolve().parents[1]
folder = Path(sys.argv[1]) if len(sys.argv) > 1 else root / 'Tests/bin'
rows = []
for field in range(4):
    with (folder / f'r32-slot-value-{field}.csv').open(encoding='utf-8', newline='') as stream:
        rows.extend(csv.DictReader(stream))
assert len(rows) == 2304
pairs, simulations = defaultdict(dict), {}
for row in rows:
    assert row['outcome'] in ('Won', 'Lost'), row
    key = tuple(int(row[k]) for k in ('field', 'mode', 'replacement_kind', 'target_level', 'seed'))
    assert row['role'] not in pairs[key]
    pairs[key][row['role']] = row
    signature = tuple(row[k] for k in ('outcome', 'wave', 'peak', 'gold', 'kills', 'time', 'placed_at', 'actual_target_level', 'units', 'cards'))
    sk = row['simulation_key']
    if sk in simulations:
        assert simulations[sk] == signature
    simulations[sk] = signature
assert len(pairs) == 1152 and len(simulations) == 1800
groups, aggregates = defaultdict(list), defaultdict(list)
for key, pair in pairs.items():
    assert set(pair) == {'baseline', 'replacement'}
    a, b = pair['baseline'], pair['replacement']
    assert a['target_slot'] == b['target_slot']
    groups[key[:4]].append((a, b))
    aggregates[key[2:4]].append((a, b))

def summarize(values):
    a, b = zip(*values)
    won = [(x, y) for x, y in values if x['outcome'] == y['outcome'] == 'Won']
    reached = [(x, y) for x, y in values if x['actual_target_level'] == y['actual_target_level'] == y['target_level']]
    delta = lambda k, values: statistics.median(int(y[k]) - int(x[k]) for x, y in values) if values else None
    return dict(pairs=len(values), baseline_wins=sum(x['outcome'] == 'Won' for x in a),
                replacement_wins=sum(y['outcome'] == 'Won' for y in b),
                baseline_peak_median=statistics.median(int(x['peak']) for x in a),
                replacement_peak_median=statistics.median(int(y['peak']) for y in b),
                paired_peak_delta_median=delta('peak', values),
                paired_gold_delta_both_won=delta('gold', won), both_won=len(won),
                both_lost=sum(x['outcome'] == y['outcome'] == 'Lost' for x, y in values),
                target_bought=sum(float(y['placed_at']) >= 0 for y in b),
                target_cap_reached_both=sum(x['actual_target_level'] == y['actual_target_level'] == y['target_level'] for x, y in values),
                paired_peak_delta_cap_reached=delta('peak', reached),
                replacement_wins_cap_reached=sum(y['outcome'] == 'Won' for _, y in reached),
                same_cards=sum(x['cards'] == y['cards'] for x, y in values))

details = []
for (field, mode, kind, level), values in sorted(groups.items()):
    assert len(values) == 8 and {int(a['seed']) for a, _ in values} == SEEDS
    assert len({a['target_slot'] for a, _ in values}) == 1
    details.append(dict(field=field, mode=mode, kind=kind, level=level,
                        slot=int(values[0][0]['target_slot']), **summarize(values)))
with (folder / 'r32-slot-summary.csv').open('w', encoding='utf-8', newline='') as stream:
    writer = csv.DictWriter(stream, fieldnames=list(details[0]))
    writer.writeheader()
    writer.writerows(details)
report = dict(rules=4, fields=4, difficulties=3, seeds=sorted(SEEDS), target_levels=[1, 2, 3],
              replacement_kinds=[0, 2, 3, 4], paired_cases=len(pairs), csv_rows=len(rows),
              unique_simulations=len(simulations), cached_control_rows=sum(r['cache_hit'] == '1' for r in rows),
              stalled=0, core_unchanged_since='cb24a2b',
              aggregate=[dict(kind=k, level=l, **summarize(v)) for (k, l), v in sorted(aggregates.items())],
              source_sha256={str(p.relative_to(root)).replace('\\', '/'): hashlib.sha256(p.read_bytes()).hexdigest()
                             for p in sorted((root / 'Assets/Scripts/Core').glob('*.cs'))})
report['tool_sha256'] = {str(p.relative_to(root)).replace('\\', '/'): hashlib.sha256(p.read_bytes()).hexdigest()
                        for p in [Path(__file__).resolve(), root/'Tests/R32SlotValueProbe.cs',
                                  root/'Tests/Run-R32SlotValue.ps1', root/'Tests/GameplayDurationProbe.cs']}
report['csv_sha256'] = {f'r32-slot-value-{field}.csv': hashlib.sha256((folder/f'r32-slot-value-{field}.csv').read_bytes()).hexdigest()
                        for field in range(4)}
(folder / 'r32-slot-analysis.json').write_text(json.dumps(report, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
print('PASS 1152 matched pairs / 1800 unique simulations / 144 groups; no stalls')
