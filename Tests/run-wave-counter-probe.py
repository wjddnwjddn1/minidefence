#!/usr/bin/env python3
"""Claude r27 wave-composition probe (macOS, Unity Mono). Never modifies game code or saves.

Copies Battle.cs to Tests/bin/mac/wave-counter, injects read-only probe switches
(extended-wave gap exponent, challenge counters on every difficulty), compiles with
Tests/GameplayDurationProbe.cs (for the Smart placement helper) and prints one summary
line per field/difficulty: policy:wins/8·median PeakAlive (lower = more slack).

  python3 Tests/run-wave-counter-probe.py                     # current v4 candidate (baseline)
  python3 Tests/run-wave-counter-probe.py --variant KG+S      # r27 recommended set
  python3 Tests/run-wave-counter-probe.py --variant KG+S --marsh 2.0 --fields 3
  python3 Tests/run-wave-counter-probe.py --raw ...           # per-battle CSV rows instead of summaries

Variants: base | G0.5 | K | KG | KG+S | B2.4 | KG+S+B2.3 (see Docs/CLAUDE-DIRECTION.md r27).
Policies: bomb (all bombs), smartB (mixed), bombArcher / bombFrost (one slot swapped), archer (all archers).
"""
import argparse
import collections
import importlib.util
import os
import statistics
import subprocess
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
spec = importlib.util.spec_from_file_location("mac_checks", os.path.join(HERE, "run-checks-mac.py"))
mac = importlib.util.module_from_spec(spec)
argv, sys.argv = sys.argv[1:], sys.argv[:1]
spec.loader.exec_module(mac)
ROOT = mac.ROOT
OUT = os.path.join(mac.BIN, "wave-counter")

VARIANTS = {  # GAP_EXP, EXT_RUNNERS, ALL_COUNTERS, BOMB_INTERVAL
    "base": ("1", "0", "0", None),
    "G0.5": ("0.5", "0", "0", None),
    "K": ("1", "1", "0", None),
    "KG": ("0.5", "1", "0", None),
    "KG+S": ("0.5", "1", "1", None),
    "B2.4": ("1", "0", "0", "2.4"),
    "KG+S+B2.3": ("0.5", "1", "1", "2.3"),
}
COUNTERS = [
    ("FastWaves&&!HardWaves?Balance.Enemies[targets[i].Kind].MeleeTaken:1",
     "(Probe.All||FastWaves&&!HardWaves)?Balance.Enemies[targets[i].Kind].MeleeTaken:1"),
    ("FastWaves&&!HardWaves?Balance.Enemies[e.Kind].BlastTaken:1",
     "(Probe.All||FastWaves&&!HardWaves)?Balance.Enemies[e.Kind].BlastTaken:1"),
    ("FastWaves&&!HardWaves&&s.Kind==1&&Balance.ArcherFavored(target.Kind)?Balance.ArcherFavoredMult:1",
     "(Probe.All||FastWaves&&!HardWaves)&&s.Kind==1&&Balance.ArcherFavored(target.Kind)?Balance.ArcherFavoredMult:1"),
    ("g.Gap/(HardWaves?1.7:FastWaves?1.4:2.5)",
     "g.Gap/Math.Pow((HardWaves?1.7:FastWaves?1.4:2.5),Probe.GapExp)"),
]
PROBE = ('namespace EmberBastion {\n public static class Probe {'
         ' public static bool All=System.Environment.GetEnvironmentVariable("ALL_COUNTERS")=="1";'
         ' public static double GapExp=double.Parse(System.Environment.GetEnvironmentVariable("GAP_EXP")??"1",'
         'System.Globalization.CultureInfo.InvariantCulture); }')


def build():
    os.makedirs(OUT, exist_ok=True)
    with open(os.path.join(ROOT, "Assets/Scripts/Core/Battle.cs"), encoding="utf-8-sig") as f:
        text = f.read()
    for old, new in COUNTERS:
        if text.count(old) != 1:
            sys.exit("Battle.cs changed; update COUNTERS for: " + old)
        text = text.replace(old, new)
    text = text.replace("namespace EmberBastion {", PROBE, 1)
    copy = os.path.join(OUT, "Battle.cs")
    with open(copy, "w", encoding="utf-8") as f:
        f.write(text)
    exe = os.path.join(OUT, "WaveCounterProbe.exe")
    sources = [os.path.join(ROOT, "Assets/Scripts/Core/Balance.cs"), copy, os.path.join(ROOT, "Assets/Scripts/Core/CombatSave.cs"),
               os.path.join(HERE, "GameplayDurationProbe.cs"), os.path.join(HERE, "WaveCounterProbe.cs")]
    r = subprocess.run([mac.MONO, mac.CSC, "-nologo", "-langversion:5", "-main:WaveCounterProbe", "-out:" + exe] + sources, capture_output=True, text=True)
    if r.returncode:
        sys.exit(r.stdout)
    return exe


def main():
    p = argparse.ArgumentParser()
    p.add_argument("--variant", default="base", choices=list(VARIANTS))
    p.add_argument("--rules", default="4")
    p.add_argument("--fields", default="0,1,2,3")
    p.add_argument("--modes", default="0,1,2")
    p.add_argument("--policies", default="bomb,smartB,bombArcher,bombFrost,archer")
    p.add_argument("--seeds", default="29,41,57,73")
    p.add_argument("--marsh", help="marsh (field 3) count multiplier from wave 4, base difficulty only")
    p.add_argument("--raw", action="store_true")
    a = p.parse_args(argv)
    exe = build()
    gap, runners, counters, bomb = VARIANTS[a.variant]
    env = dict(os.environ, GAP_EXP=gap, EXT_RUNNERS=runners, ALL_COUNTERS=counters)
    if bomb:
        env["BOMB_INTERVAL"] = bomb
    if a.marsh:
        env.update(MARSH_SCALE=a.marsh, MARSH_BASE_ONLY="1")
    print("variant %s rules %s (wins/battles·median PeakAlive)" % (a.variant, a.rules))
    for field in a.fields.split(","):
        for mode in a.modes.split(","):
            r = subprocess.run([mac.MONO, exe, a.rules, field, "", a.policies, mode, a.seeds], capture_output=True, text=True, env=env, cwd=OUT)
            if r.returncode:
                sys.exit(r.stderr)
            if a.raw:
                print(r.stdout.rstrip())
                continue
            groups = collections.OrderedDict()
            for line in r.stdout.splitlines():
                row = line.split(",")
                groups.setdefault(row[0], []).append(row)
            cells = ["%s:%d/%d·%.0f" % (k, sum(x[4] == "W" for x in v), len(v), statistics.median(int(x[6]) for x in v)) for k, v in groups.items()]
            print("field %s mode %s  %s" % (field, mode, "  ".join(cells)))


if __name__ == "__main__":
    main()
