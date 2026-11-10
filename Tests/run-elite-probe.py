#!/usr/bin/env python3
"""Claude r27 elite-slot experiment (macOS, Unity Mono). Never modifies game code or saves.

Builds a patched COPY of Battle.cs (the run-wave-counter-probe.py switches plus Battle.EliteSlot and
the elite effects) and compares where the one elite goes. Prints, per field/difficulty:
policy:wins/battles·median PeakAlive·median final gold·median wave the elite was bought (0 = never).

  python3 Tests/run-elite-probe.py                    # KG+S waves (r27 recommendation)
  python3 Tests/run-elite-probe.py --variant base     # current v4 candidate waves
Policies: none, first (first Lv3), sword, archer, bomb, frost, smith.
"""
import argparse
import collections
import importlib.util
import os
import statistics
import subprocess
import sys

argv, sys.argv = sys.argv[1:], sys.argv[:1]
HERE = os.path.dirname(os.path.abspath(__file__))
spec = importlib.util.spec_from_file_location("wave_probe", os.path.join(HERE, "run-wave-counter-probe.py"))
wave = importlib.util.module_from_spec(spec)
spec.loader.exec_module(wave)
mac = wave.mac
OUT = os.path.join(mac.BIN, "elite-slot")

ELITE = [
    ("public sealed partial class Battle {", "public sealed partial class Battle {\n  public int EliteSlot=-1;"),
    ("public bool Burning,Freezing;", "public bool Burning,Freezing,Elite;"),
    ("*(1+bonusDamage);}", "*(1+bonusDamage)*(u.Slot==EliteSlot&&(u.Kind==0||u.Kind==1)?1.35:1);}"),
    ("Pen=d.Pen,Speed=d.Speed,Radius=", "Elite=u.Slot==EliteSlot,Pen=d.Pen+(u.Slot==EliteSlot&&u.Kind==1?8:0),Speed=d.Speed,Radius="),
    ("(u.Kind==2?Math.Pow(1.25,CardCount(ReinforcementDraft.BombRadius)):1)",
     "(u.Kind==2?Math.Pow(1.25,CardCount(ReinforcementDraft.BombRadius)):1)*(u.Slot==EliteSlot&&u.Kind==2?1.2:1)"),
    ("BossMult=u.Kind==1&&u.Branch==2?1.5:1", "BossMult=(u.Kind==1&&u.Branch==2?1.5:1)*(u.Slot==EliteSlot&&u.Kind==3?1.5:1)"),
    ("target.SlowTime=Math.Max(target.SlowTime,2)", "target.SlowTime=Math.Max(target.SlowTime,s.Elite?3:2)"),
    ("Point.Distance(p,Field.Slots[supporter.Slot])>UnitRange(supporter))continue;haste=",
     "Point.Distance(p,Field.Slots[supporter.Slot])>UnitRange(supporter)+(supporter.Slot==EliteSlot?60:0))continue;haste="),
]


def build():
    os.makedirs(OUT, exist_ok=True)
    with open(os.path.join(mac.ROOT, "Assets/Scripts/Core/Battle.cs"), encoding="utf-8-sig") as f:
        text = f.read()
    for old, new in wave.COUNTERS + ELITE:
        expected = 3 if old == "g.Extended&&g.Kind==0?" else 2 if old == "MarshCandidateBoost(g)" else 1
        if text.count(old) != expected:
            sys.exit("Battle.cs changed; update patch for: " + old)
        text = text.replace(old, new)
    old = "HasNormalCards&&FieldIndex==1?1.5:1.7"
    if text.count(old) != 1:
        sys.exit("Battle.cs changed; update forest factor replacement")
    text = text.replace(old, "HasNormalCards&&FieldIndex==1?Probe.ForestHardFactor:1.7")
    text = text.replace("namespace EmberBastion {", wave.PROBE, 1)
    copy = os.path.join(OUT, "Battle.cs")
    with open(copy, "w", encoding="utf-8") as f:
        f.write(text)
    exe = os.path.join(OUT, "EliteSlotProbe.exe")
    sources = [os.path.join(mac.ROOT, "Assets/Scripts/Core/Balance.cs"), copy, os.path.join(mac.ROOT, "Assets/Scripts/Core/CombatSave.cs"),
               os.path.join(HERE, "GameplayDurationProbe.cs"), os.path.join(HERE, "EliteSlotProbe.cs")]
    r = subprocess.run([mac.MONO, mac.CSC, "-nologo", "-langversion:5", "-main:EliteSlotProbe", "-out:" + exe] + sources, capture_output=True, text=True)
    if r.returncode:
        sys.exit(r.stdout)
    return exe


def main():
    p = argparse.ArgumentParser()
    p.add_argument("--variant", default="KG+S", choices=list(wave.VARIANTS))
    p.add_argument("--fields", default="0,1,2,3")
    p.add_argument("--modes", default="0,1,2")
    p.add_argument("--policies", default="none,first,sword,archer,bomb,frost,smith")
    p.add_argument("--seeds", default="29,41,57,73")
    p.add_argument("--raw", action="store_true")
    a = p.parse_args(argv)
    exe = build()
    gap, runners, counters, bomb = wave.VARIANTS[a.variant]
    env = dict(os.environ, GAP_EXP=gap, EXT_RUNNERS=runners, ALL_COUNTERS=counters)
    print("elite probe, waves %s, rules 4 (wins/battles·median peak·median gold·median elite wave)" % a.variant)
    for field in a.fields.split(","):
        for mode in a.modes.split(","):
            r = subprocess.run([mac.MONO, exe, "4", field, a.policies, mode, a.seeds], capture_output=True, text=True, env=env, cwd=OUT)
            if r.returncode:
                sys.exit(r.stderr)
            if a.raw:
                print(r.stdout.rstrip())
                continue
            groups = collections.OrderedDict()
            for line in r.stdout.splitlines():
                row = line.split(",")
                groups.setdefault(row[0], []).append(row)
            med = lambda v, i: statistics.median(int(x[i]) for x in v)
            cells = ["%s:%d/%d·%.0f·%.0fG·w%.0f" % (k, sum(x[4] == "W" for x in v), len(v), med(v, 6), med(v, 8), med(v, 7)) for k, v in groups.items()]
            print("field %s mode %s  %s" % (field, mode, "  ".join(cells)))


if __name__ == "__main__":
    main()
