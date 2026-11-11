#!/usr/bin/env python3
"""Claude r33 frost/smith role probe (macOS, Unity Mono). Never modifies game code or saves.

Patched COPY of Battle.cs adds two knobs (default off = shipped behaviour):
  FROST_SPLASH=r   frost slow also applies to enemies within r of the target
  FROST_BRITTLE=x  slowed enemies take (1+x) damage from every source
Numbers (FROST_DAMAGE, FROST_COST, SMITH_COST) are set in the probe.
Paired: one archer slot swapped for frost (or smith) vs keeping the archer, same seed/difficulty.
Prints per variant: swapped wins / archer wins, median paired peak delta (swap - archer), per-field breakdown.

  python3 Tests/run-frost-role-probe.py
"""
import argparse, collections, importlib.util, os, statistics, subprocess, sys
HERE = os.path.dirname(os.path.abspath(__file__))
spec = importlib.util.spec_from_file_location("mac_checks", os.path.join(HERE, "run-checks-mac.py"))
mac = importlib.util.module_from_spec(spec)
argv, sys.argv = sys.argv[1:], sys.argv[:1]
spec.loader.exec_module(mac)
OUT = os.path.join(mac.BIN, "frost-role")
PATCH = [
    ("target.SlowTime=Math.Max(target.SlowTime,2);}",
     "target.SlowTime=Math.Max(target.SlowTime,2);if(s.Kind==3&&Probe.Splash>0)foreach(var o in Enemies.ToArray())if(o!=target&&o.HP>0&&Point.Distance(o.Position,target.Position)<=Probe.Splash){if(Probe.SplashDamage)Hit(o,s.Damage,s.Pen,false,1,true,1,3);if(o.HP>0){if(s.SlowPct>=o.SlowPct||o.SlowTime<=0)o.SlowPct=s.SlowPct;o.SlowTime=Math.Max(o.SlowTime,2);}}}"),
    ("if(e.Shield>0)amount*=.5;", "if(e.Shield>0)amount*=.5;if(e.SlowTime>0)amount*=1+Probe.Brittle;"),
]
PROBE = ('namespace EmberBastion {\n public static class Probe {'
         ' public static double Splash=double.Parse(System.Environment.GetEnvironmentVariable("FROST_SPLASH")??"0",System.Globalization.CultureInfo.InvariantCulture);'
         ' public static bool SplashDamage=System.Environment.GetEnvironmentVariable("FROST_SPLASH_DAMAGE")=="1";'
         ' public static double Brittle=double.Parse(System.Environment.GetEnvironmentVariable("FROST_BRITTLE")??"0",System.Globalization.CultureInfo.InvariantCulture); }')
VARIANTS = collections.OrderedDict([
    ("frost-now", (3, {})),
    ("A splash55", (3, {"FROST_SPLASH": "55"})),
    ("B splash55+dmg10", (3, {"FROST_SPLASH": "55", "FROST_DAMAGE": "10"})),
    ("C brittle20", (3, {"FROST_BRITTLE": "0.2"})),
    ("D splash55+brittle20", (3, {"FROST_SPLASH": "55", "FROST_BRITTLE": "0.2"})),
    ("E cost70", (3, {"FROST_COST": "70"})),
    ("F splash70dmg", (3, {"FROST_SPLASH": "70", "FROST_SPLASH_DAMAGE": "1"})),
    ("G D+interval0.8", (3, {"FROST_SPLASH": "55", "FROST_BRITTLE": "0.2", "FROST_INTERVAL": "0.8"})),
    ("H splash70+brittle35", (3, {"FROST_SPLASH": "70", "FROST_BRITTLE": "0.35"})),
    ("I H+interval0.8", (3, {"FROST_SPLASH": "70", "FROST_BRITTLE": "0.35", "FROST_INTERVAL": "0.8"})),
    ("J F+brittle35+int0.8", (3, {"FROST_SPLASH": "70", "FROST_SPLASH_DAMAGE": "1", "FROST_BRITTLE": "0.35", "FROST_INTERVAL": "0.8"})),
    ("K J+dmg10", (3, {"FROST_SPLASH": "70", "FROST_SPLASH_DAMAGE": "1", "FROST_BRITTLE": "0.35", "FROST_INTERVAL": "0.8", "FROST_DAMAGE": "10"})),
    ("smith-now", (4, {})),
    ("smith cost60", (4, {"SMITH_COST": "60"})),
])


def build():
    os.makedirs(OUT, exist_ok=True)
    with open(os.path.join(mac.ROOT, "Assets/Scripts/Core/Battle.cs"), encoding="utf-8-sig") as f:
        text = f.read()
    for old, new in PATCH:
        if text.count(old) != 1:
            sys.exit("Battle.cs changed; update patch for: " + old)
        text = text.replace(old, new)
    text = text.replace("namespace EmberBastion {", PROBE, 1)
    copy = os.path.join(OUT, "Battle.cs")
    open(copy, "w", encoding="utf-8").write(text)
    exe = os.path.join(OUT, "FrostRoleProbe.exe")
    src = [os.path.join(mac.ROOT, "Assets/Scripts/Core/Balance.cs"), copy, os.path.join(mac.ROOT, "Assets/Scripts/Core/CombatSave.cs"),
           os.path.join(HERE, "GameplayDurationProbe.cs"), os.path.join(HERE, "FrostRoleProbe.cs")]
    r = subprocess.run([mac.MONO, mac.CSC, "-nologo", "-langversion:5", "-main:FrostRoleProbe", "-out:" + exe] + src, capture_output=True, text=True)
    if r.returncode:
        sys.exit(r.stdout)
    return exe


def main():
    p = argparse.ArgumentParser()
    p.add_argument("--variants", default=",".join(VARIANTS))
    p.add_argument("--fields", default="0,1,2,3")
    p.add_argument("--modes", default="0,1,2")
    p.add_argument("--seeds", default="29,41,57,73,101,137,181,223")
    a = p.parse_args(argv)
    exe = build()
    print("frost/smith role probe, real engine rules 4 + knobs (swap wins / archer wins · median paired peak delta · per field)")
    for name in a.variants.split(","):
        swap, knobs = VARIANTS[name]
        env = {k: v for k, v in os.environ.items() if not k.startswith(("FROST_", "SMITH_"))}
        env.update(knobs)
        deltas, sw, aw, per = [], 0, 0, []
        for field in a.fields.split(","):
            r = subprocess.run([mac.MONO, exe, field, str(swap), a.modes, a.seeds], capture_output=True, text=True, env=env, cwd=OUT)
            if r.returncode:
                sys.exit(r.stderr)
            rows = [l.split(",") for l in r.stdout.splitlines()]
            pairs = collections.defaultdict(dict)
            for m, s, sp, res, peak, gold, slot in rows:
                pairs[(m, s)][sp] = (res, int(peak))
            fd = [v["1"][1] - v["0"][1] for v in pairs.values()]
            fw = sum(v["1"][0] == "W" for v in pairs.values())
            deltas += fd; sw += fw; aw += sum(v["0"][0] == "W" for v in pairs.values())
            per.append("f%s %d/%d %+.0f" % (field, fw, len(fd), statistics.median(fd)))
        print("%-22s %3d/%d vs %3d · %+5.1f   %s" % (name, sw, len(deltas), aw, statistics.median(deltas), "  ".join(per)))


if __name__ == "__main__":
    main()
