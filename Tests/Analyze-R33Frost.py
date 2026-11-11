"""Check the r33 dominance gate using eight distinct seeds, not Ember duplicates."""
import argparse
import json
from pathlib import Path
import statistics

SEEDS = [29, 41, 57, 73, 101, 137, 181, 223]
POLICIES = ['smartB', 'smartBFrost2', 'bombFrost2', 'frost']

def analyze(path):
    lines = path.read_text(encoding='utf-8-sig').splitlines()
    assert lines[0].startswith('variant real rules 4 '), 'requires unpatched rules 4'
    rows = [line.split(',') for line in lines[1:]]
    assert len(rows) == 384, 'four fields × three difficulties × four policies × eight seeds'
    groups = {}
    # The existing raw format has no field column. Verify the complete producer order
    # rather than accidentally treating each repeated field/seed as an extra sample.
    position = 0
    for field in range(4):
        for mode in range(3):
            for policy in POLICIES:
                group = []
                for seed in SEEDS:
                    row = rows[position]
                    position += 1
                    assert len(row) == 11 and row[:4] == [policy, str(mode), str(seed), '0']
                    assert row[4] in ('W', 'L'), 'stalled or invalid outcome'
                    group.append(dict(seed=seed, won=row[4]=='W', peak=int(row[6]), gold=int(row[9])))
                groups[(field, mode, policy)] = group
    cells = []
    for field in range(4):
        for mode in range(3):
            base = groups[(field, mode, 'smartB')]
            for policy in POLICIES:
                group = groups[(field, mode, policy)]
                cells.append(dict(field=field, mode=mode, policy=policy,
                                  wins=sum(r['won'] for r in group), battles=8,
                                  median_peak=statistics.median(r['peak'] for r in group),
                                  paired_peak_delta=statistics.median(r['peak']-b['peak'] for r, b in zip(group, base)),
                                  median_gold=statistics.median(r['gold'] for r in group)))
    by_key = {(c['field'], c['mode'], c['policy']): c for c in cells}
    # A positive threshold is strict: -5 is allowed, below -5 in all 12 cells is not.
    dominant = [policy for policy in POLICIES[1:] if all(
        by_key[(f, m, policy)]['paired_peak_delta'] < -5 for f in range(4) for m in range(3))]
    bomb_best_all = all(by_key[(f, m, 'bombFrost2')]['median_peak'] <= min(
        by_key[(f, m, p)]['median_peak'] for p in POLICIES if p != 'bombFrost2')
        for f in range(4) for m in range(3))
    frost_basic = sum(by_key[(f, 0, 'frost')]['wins'] for f in range(4))
    return dict(cases=384, seeds=SEEDS, cells=cells, dominant_all_12=dominant,
                bomb_frost_best_all_12=bomb_best_all, frost_basic_wins=frost_basic,
                frost_basic_battles=32, passed=not dominant and not bomb_best_all and frost_basic==0)

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('raw', type=Path)
    parser.add_argument('--require-pass', action='store_true')
    args = parser.parse_args()
    result = analyze(args.raw)
    args.raw.with_suffix('.analysis.json').write_text(json.dumps(result, indent=2)+'\n', encoding='utf-8')
    print(('PASS' if result['passed'] else 'FAIL')+' r33 gate: '+
          f"frost-only basic wins {result['frost_basic_wins']}/32; "+
          f"all-12 dominant {result['dominant_all_12']}; bomb+frost best all12 {result['bomb_frost_best_all_12']}")
    if args.require_pass and not result['passed']:
        raise SystemExit(1)

if __name__ == '__main__':
    main()
