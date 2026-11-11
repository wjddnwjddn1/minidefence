#!/usr/bin/env python3
"""Claude r35: frost candidates against both gates (real Core + experimental frost knobs in a COPY).

1. Slot value: one archer slot -> frost vs keeping the archer (FrostRoleProbe, paired, Lv3 branch 1).
2. Codex r33 dominance gates (WaveCounterProbe smartB / smartBFrost2 / bombFrost2 / frost, unique seeds):
   a) neither frost army beats mixed by more than 5 peak (paired median) in all 12 field x difficulty cells,
   b) bombFrost2 is not best in all 12 cells,
   c) frost-only wins 0 of 32 base-difficulty battles.
  python3 Tests/run-frost-gates.py [--candidates "M L+interval0.8,..."]
"""
import argparse, collections, importlib.util, os, statistics, subprocess, sys
from pathlib import Path
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import probe_runtime as runtime
spec = importlib.util.spec_from_file_location("frost_role", HERE/"run-frost-role-probe.py"); role = importlib.util.module_from_spec(spec); spec.loader.exec_module(role)
OUT = runtime.BIN/'frost-gates'
SEEDS = "29,41,57,73,101,137,181,223"
INTERVAL = ("interval=(u.Kind==1&&u.Branch==1?.75:d.Interval)/(1+haste);",
            "interval=(u.Kind==1&&u.Branch==1?.75:u.Kind==3&&Probe.FrostInterval>0?Probe.FrostInterval:d.Interval)/(1+haste);")
EXTRA = (' public static double FrostInterval=double.Parse(System.Environment.GetEnvironmentVariable("FROST_INTERVAL")??"0",'
         'System.Globalization.CultureInfo.InvariantCulture); }')
CANDIDATES = collections.OrderedDict([
    ("now", {}),
    ("J r33-rejected", {"FROST_SPLASH": "70", "FROST_SPLASH_DAMAGE": "1", "FROST_BRITTLE": "0.35", "FROST_INTERVAL": "0.8"}),
    ("H slow70+brittle35", {"FROST_SPLASH": "70", "FROST_BRITTLE": "0.35"}),
    ("L slow70+brittle50", {"FROST_SPLASH": "70", "FROST_BRITTLE": "0.5"}),
    ("O slow70+brittle35+int0.8", {"FROST_SPLASH": "70", "FROST_BRITTLE": "0.35", "FROST_INTERVAL": "0.8"}),
    ("M slow70+brittle50+int0.8", {"FROST_SPLASH": "70", "FROST_BRITTLE": "0.5", "FROST_INTERVAL": "0.8"}),
    ("N slow90+brittle50", {"FROST_SPLASH": "90", "FROST_BRITTLE": "0.5"}),
    ("P slow90+brittle50+int0.8", {"FROST_SPLASH": "90", "FROST_BRITTLE": "0.5", "FROST_INTERVAL": "0.8"}),
    ("Q slow70+brittle65+int0.8", {"FROST_SPLASH": "70", "FROST_BRITTLE": "0.65", "FROST_INTERVAL": "0.8"}),
])


def build_wave():
    os.makedirs(OUT, exist_ok=True)
    text = (runtime.ROOT/'Assets/Scripts/Core/Battle.cs').read_text(encoding='utf-8-sig')
    for old, new in role.PATCH + [INTERVAL]:
        if text.count(old) != 1:
            sys.exit("Battle.cs changed; update patch for: " + old)
        text = text.replace(old, new)
    text = text.replace("namespace EmberBastion {", role.PROBE[:-2] + EXTRA, 1)
    copy = OUT/'Battle.cs'; copy.write_text(text, encoding='utf-8')
    src = [runtime.ROOT/'Assets/Scripts/Core/Balance.cs', copy, runtime.ROOT/'Assets/Scripts/Core/CombatSave.cs', HERE/'GameplayDurationProbe.cs', HERE/'WaveCounterProbe.cs']
    return runtime.compile_exe(OUT/'WaveCounterFrost.exe', src, 'WaveCounterProbe')


def slot_value(exe, knobs):
    rows = role.measure(exe, 3, "0,1,2,3", "0,1,2", SEEDS, knobs)
    deltas, wins = [], 0
    for field, lines in rows:
        pairs = collections.defaultdict(dict)
        for m, seed, swapped, outcome, peak, gold, slot in lines:
            pairs[(m, seed)][swapped] = (outcome, int(peak))
        deltas += [v['1'][1] - v['0'][1] for v in pairs.values()]; wins += sum(v['1'][0] == 'W' for v in pairs.values())
    return wins, len(deltas), statistics.median(deltas)


def dominance(exe, knobs):
    env = {k: v for k, v in os.environ.items() if not k.startswith(('FROST_', 'SMITH_'))}; env.update(knobs)
    cells, solo = [], 0
    for field in "0123":
        for mode in "012":
            out = runtime.run_exe(exe, ["4", field, "", "smartB,smartBFrost2,bombFrost2,frost", mode, SEEDS, "unique"], env=env, cwd=str(OUT))
            peak = collections.defaultdict(dict); wins = collections.Counter()
            for line in out.splitlines():
                x = line.split(','); peak[x[0]][x[2]] = int(x[6]); wins[x[0]] += x[4] == 'W'
            if mode == "0":
                solo += wins['frost']
            med = {p: statistics.median(peak[p][s] - peak['smartB'][s] for s in peak['smartB']) for p in ('smartBFrost2', 'bombFrost2')}
            best = min(statistics.median(peak[p].values()) for p in ('smartB', 'smartBFrost2', 'bombFrost2', 'frost'))
            cells.append((med['smartBFrost2'], med['bombFrost2'], statistics.median(peak['bombFrost2'].values()) <= best))
    a = not all(c[0] < -5 for c in cells) and not all(c[1] < -5 for c in cells)
    b = not all(c[2] for c in cells)
    return a, b, solo == 0, solo, statistics.median(c[0] for c in cells), statistics.median(c[1] for c in cells)


def main():
    p = argparse.ArgumentParser(); p.add_argument("--candidates", default=",".join(CANDIDATES)); a = p.parse_args()
    role_exe = role.build(False, "HEAD"); wave_exe = build_wave()
    print("candidate                   1 frost: wins·median | mixed+2 / bomb+2 median vs mixed | frost-only base wins | gates a b c", flush=True)
    for name in a.candidates.split(','):
        knobs = CANDIDATES[name]
        w, n, d = slot_value(role_exe, knobs)
        ga, gb, gc, solo, m2, b2 = dominance(wave_exe, knobs)
        print("%-27s %3d/%d · %+4.1f      | %+5.1f / %+5.1f                    | %2d/32                | %s %s %s" % (
            name, w, n, d, m2, b2, solo, *("P" if g else "F" for g in (ga, gb, gc))), flush=True)


if __name__ == '__main__':
    main()
