#!/usr/bin/env python3
"""Claude r31 decision 3 probe (macOS, Unity Mono): frost-link cards on the real engine, rules 4.

Prints per field/difficulty: policy:wins/battles·median PeakAlive·median final gold·share of runs that took a frost card.
  python3 Tests/run-frost-link-probe.py
  python3 Tests/run-frost-link-probe.py --fields 0,1,2,3 --modes 0,1,2
"""
import argparse, collections, importlib.util, os, statistics, subprocess, sys
HERE = os.path.dirname(os.path.abspath(__file__))
spec = importlib.util.spec_from_file_location("mac_checks", os.path.join(HERE, "run-checks-mac.py"))
mac = importlib.util.module_from_spec(spec)
argv, sys.argv = sys.argv[1:], sys.argv[:1]
spec.loader.exec_module(mac)


def build():
    out = os.path.join(mac.BIN, "frost-link")
    os.makedirs(out, exist_ok=True)
    exe = os.path.join(out, "FrostLinkProbe.exe")
    core = os.path.join(mac.ROOT, "Assets/Scripts/Core")
    sources = [os.path.join(core, n) for n in ("Balance.cs", "Battle.cs", "CombatSave.cs")] + [
        os.path.join(HERE, "GameplayDurationProbe.cs"), os.path.join(HERE, "FrostLinkProbe.cs")]
    r = subprocess.run([mac.MONO, mac.CSC, "-nologo", "-langversion:5", "-main:FrostLinkProbe", "-out:" + exe] + sources, capture_output=True, text=True)
    if r.returncode:
        sys.exit(r.stdout)
    return exe, out


def main():
    p = argparse.ArgumentParser()
    p.add_argument("--fields", default="0,1")
    p.add_argument("--modes", default="0,1")
    p.add_argument("--policies", default="bomb,mixed,archerFrost,archerFrostNo,swordFrost,swordFrostNo")
    p.add_argument("--seeds", default="29,41,57,73,89,101,113,127")
    p.add_argument("--raw", action="store_true")
    a = p.parse_args(argv)
    exe, out = build()
    print("frost-link probe, real engine rules 4 (wins/battles·median peak·median gold·took frost card)")
    for field in a.fields.split(","):
        for mode in a.modes.split(","):
            r = subprocess.run([mac.MONO, exe, field, a.policies, mode, a.seeds], capture_output=True, text=True, cwd=out)
            if r.returncode:
                sys.exit(r.stderr)
            if a.raw:
                print(r.stdout.rstrip())
                continue
            groups = collections.OrderedDict()
            for line in r.stdout.splitlines():
                row = line.split(",")
                groups.setdefault(row[0], []).append(row)
            cells = ["%s:%d/%d·%.0f·%.0fG·%d%%" % (k, sum(x[3] == "W" for x in v), len(v), statistics.median(int(x[5]) for x in v),
                     statistics.median(int(x[6]) for x in v), 100 * sum(("7" in x[7] or "8" in x[7]) for x in v) // len(v)) for k, v in groups.items()]
            print("field %s mode %s  %s" % (field, mode, "  ".join(cells)))


if __name__ == "__main__":
    main()
