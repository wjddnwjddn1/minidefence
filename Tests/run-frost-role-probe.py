#!/usr/bin/env python3
"""Claude r33 frost/smith role probe (Windows .NET / macOS Unity Mono). Never modifies game code or saves.

Patched COPY of Battle.cs adds two knobs (default off = shipped behaviour):
  FROST_SPLASH=r   frost slow also applies to enemies within r of the target
  FROST_BRITTLE=x  slowed enemies take (1+x) damage from every source
Numbers (FROST_DAMAGE, FROST_COST, SMITH_COST) are set in the probe.
Paired: one archer slot swapped for frost (or smith) vs keeping the archer, same seed/difficulty.
Prints per variant: swapped wins / archer wins, median paired peak delta (swap - archer), per-field breakdown.

  python3 Tests/run-frost-role-probe.py
"""
import argparse, collections, json, os, statistics, subprocess, sys
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path
import probe_runtime as runtime
HERE = str(Path(__file__).resolve().parent)
OUT = str(runtime.BIN/'frost-role')

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


def build(real=False, revision="eeaf901"):
    os.makedirs(OUT, exist_ok=True)
    if real:
        battle = runtime.ROOT/'Assets/Scripts/Core/Battle.cs'
    else:
        # Pin the pre-rework source: do not accidentally double-apply the experimental patch.
        text = subprocess.check_output(["git", "show", revision+":Assets/Scripts/Core/Battle.cs"],
                                       cwd=runtime.ROOT).decode("utf-8-sig")
        for old, new in PATCH:
            if text.count(old) != 1:
                raise RuntimeError("Reference changed; update patch for: "+old)
            text = text.replace(old, new)
        text = text.replace("namespace EmberBastion {", PROBE, 1)
        battle = Path(OUT)/'ReferenceBattle.cs'
        battle.write_text(text, encoding='utf-8')
    exe = Path(OUT)/('FrostRoleReal.exe' if real else 'FrostRoleReference.exe')
    sources = [runtime.ROOT/'Assets/Scripts/Core/Balance.cs', battle,
               runtime.ROOT/'Assets/Scripts/Core/CombatSave.cs',
               Path(HERE)/'GameplayDurationProbe.cs', Path(HERE)/'FrostRoleProbe.cs']
    return runtime.compile_exe(exe, sources, 'FrostRoleProbe')


def measure(exe, swap, fields, modes, seeds, knobs):
    env = {k: v for k, v in os.environ.items() if not k.startswith(('FROST_', 'SMITH_'))}
    env.update(knobs)
    def field_rows(field):
        output = runtime.run_exe(exe, [field, str(swap), modes, seeds], env=env, cwd=OUT)
        return field, [line.split(',') for line in output.splitlines()]
    with ThreadPoolExecutor(max_workers=4) as pool:
        return list(pool.map(field_rows, fields.split(',')))


def describe(name, results):
    deltas, sw, aw, per = [], 0, 0, []
    for field, rows in results:
        pairs = collections.defaultdict(dict)
        for m, seed, swapped, outcome, peak, gold, slot in rows:
            pairs[(m, seed)][swapped] = (outcome, int(peak))
        assert all(set(v)=={'0','1'} for v in pairs.values())
        fd = [v['1'][1]-v['0'][1] for v in pairs.values()]
        fw = sum(v['1'][0]=='W' for v in pairs.values())
        deltas += fd; sw += fw; aw += sum(v['0'][0]=='W' for v in pairs.values())
        per.append('f%s %d/%d %+.0f' % (field, fw, len(fd), statistics.median(fd)))
    print('%-22s %3d/%d vs %3d · %+5.1f   %s' % (name, sw, len(deltas), aw, statistics.median(deltas), '  '.join(per)), flush=True)


def main():
    p = argparse.ArgumentParser()
    p.add_argument("--variants", help="defaults to frost-now for --real, all historical variants otherwise")
    p.add_argument("--fields", default="0,1,2,3")
    p.add_argument("--modes", default="0,1,2")
    p.add_argument("--seeds", default="29,41,57,73,101,137,181,223")
    p.add_argument('--real', action='store_true', help='measure current unpatched Core with all experimental switches off')
    p.add_argument('--compare-j', action='store_true', help='assert full per-battle parity against the pinned J experiment')
    p.add_argument('--reference-revision', default='eeaf901')
    a = p.parse_args()
    a.variants = a.variants or ("frost-now" if a.real else ",".join(VARIANTS))
    print('frost role: paired, independent seeds; current Core' if a.real else 'frost role: pinned experimental reference', flush=True)
    exe = build(a.real, a.reference_revision)
    if a.compare_j:
        if not a.real:
            p.error('--compare-j requires --real')
        reference = build(False, a.reference_revision)
        expected = measure(reference, 3, a.fields, a.modes, a.seeds, VARIANTS['J F+brittle35+int0.8'][1])
        actual = measure(exe, 3, a.fields, a.modes, a.seeds, {})
        Path(OUT, 'j-reference.json').write_text(json.dumps(expected, indent=2)+'\n', encoding='utf-8')
        Path(OUT, 'j-real.json').write_text(json.dumps(actual, indent=2)+'\n', encoding='utf-8')
        describe('J reference', expected); describe('current real', actual)
        if actual != expected:
            raise RuntimeError('J per-battle parity failed (outcome/peak/gold/slot)')
        print('PASS exact J parity: all paired rows (no game-source patch)', flush=True)
    else:
        raw = {}
        for name in a.variants.split(','):
            swap, knobs = VARIANTS[name]
            if a.real and knobs:
                p.error('--real accepts only variants without experimental switches')
            raw[name] = measure(exe, swap, a.fields, a.modes, a.seeds, knobs)
            describe(name, raw[name])
        Path(OUT, 'real-rows.json' if a.real else 'reference-rows.json').write_text(json.dumps(raw, indent=2)+'\n', encoding='utf-8')


if __name__ == '__main__':
    main()
