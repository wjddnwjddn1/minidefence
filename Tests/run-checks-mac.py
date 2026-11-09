#!/usr/bin/env python3
"""Run the Windows `Tests/Run-*Checks.ps1` C# checks on macOS with Unity's bundled Mono.

The .ps1 files stay the source of truth: this reads each script's compile line and
source list instead of keeping a second list. Output goes to Tests/bin/mac (ignored).

Usage (from the repository root):
  python3 Tests/run-checks-mac.py                 # every check
  python3 Tests/run-checks-mac.py Core Supply     # names containing these words
  UNITY_MONO=/path/to/MonoBleedingEdge python3 Tests/run-checks-mac.py

Exit code is 0 only when every selected check passes.
"""
import glob
import hashlib
import os
import re
import subprocess
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TESTS = os.path.join(ROOT, "Tests")
BIN = os.path.join(TESTS, "bin", "mac")
CTOR = "rng=seed;RulesVersion=3;FieldIndex=fieldIndex;"
PARITY_HASH = "9FD2B168AB217A4B15D24FDC0E120048F17D612AAF6FB2702EB4021E278A5A11"


def find_mono():
    override = os.environ.get("UNITY_MONO")
    candidates = [override] if override else []
    version = None
    try:
        with open(os.path.join(ROOT, "ProjectSettings", "ProjectVersion.txt"), encoding="utf-8") as f:
            version = re.search(r"m_EditorVersion:\s*(\S+)", f.read()).group(1)
    except (OSError, AttributeError):
        pass
    hub = "/Applications/Unity/Hub/Editor"
    suffix = "Unity.app/Contents/Resources/Scripting/MonoBleedingEdge"
    if version:
        candidates.append(os.path.join(hub, version, suffix))
    candidates += sorted(glob.glob(os.path.join(hub, "*", suffix)), reverse=True)
    for c in candidates:
        if c and os.path.isfile(os.path.join(c, "bin", "mono")) and os.path.isfile(os.path.join(c, "lib", "mono", "4.5", "csc.exe")):
            return c
    sys.exit("Unity MonoBleedingEdge not found. Install Unity via Hub or set UNITY_MONO.")


MONO_ROOT = find_mono()
MONO = os.path.join(MONO_ROOT, "bin", "mono")
CSC = os.path.join(MONO_ROOT, "lib", "mono", "4.5", "csc.exe")


def compile_exe(name, sources):
    exe = os.path.join(BIN, name + ".exe")
    r = subprocess.run([MONO, CSC, "-nologo", "-out:" + exe] + sources, capture_output=True, text=True)
    if r.returncode != 0:
        raise RuntimeError("compile failed: " + (r.stdout.strip().splitlines() or ["?"])[-1])
    return exe


def run_exe(exe, args=(), env=None):
    r = subprocess.run([MONO, exe] + list(args), capture_output=True, text=True, cwd=BIN, env=env, timeout=1800)
    return r.returncode, r.stdout, r.stderr


def battle_copy(version, name):
    with open(os.path.join(ROOT, "Assets/Scripts/Core/Battle.cs"), encoding="utf-8-sig") as f:
        source = f.read()
    if CTOR not in source:
        raise RuntimeError("unexpected Battle constructor")
    path = os.path.join(BIN, name)
    with open(path, "w", encoding="utf-8", newline="") as f:
        f.write(source.replace(CTOR, CTOR.replace("RulesVersion=3", "RulesVersion=%d" % version)))
    return path


def core(path):
    return os.path.join(ROOT, "Assets/Scripts/Core", path)


def rule_parity():
    hashes = []
    for v in (1, 2):
        exe = compile_exe("RuleParity%d" % v, [core("Balance.cs"), battle_copy(v, "RuleParityBattle%d.cs" % v), core("CombatSave.cs"), os.path.join(TESTS, "ComboProbe.cs")])
        code, out, err = run_exe(exe, ["1", "100", "on"])
        rows = out.splitlines()
        if code != 0 or len(rows) != 217:
            raise RuntimeError("simulation failed")
        # PowerShell WriteAllLines writes CRLF; the baseline hash was taken from that file.
        hashes.append(hashlib.sha256(("\r\n".join(rows) + "\r\n").encode("utf-8")).hexdigest().upper())
    if hashes[0] != hashes[1] or hashes[0] != PARITY_HASH:
        raise RuntimeError("unexpected rule parity: " + " ".join(hashes))
    return "PASS 216 scenarios: v1 == v2 == " + hashes[0]


def r25_legacy():
    exe = compile_exe("R25LegacyCandidate", [core("Balance.cs"), battle_copy(1, "R25LegacyCandidateBattle.cs"), core("CombatSave.cs"), os.path.join(TESTS, "R25CandidateProbe.cs")])
    env = dict(os.environ, R25_CANDIDATE_FACTOR="2.5", R25_CANDIDATE_VARIATION="0", R25_CANDIDATE_TUNED="0", R25_CANDIDATE_DISTINCT="0")
    with open(os.path.join(ROOT, "Docs", "r24-gameplay-baseline.csv"), encoding="utf-8-sig") as f:
        baseline = f.read().splitlines()
    for mode in (0, 1, 2):
        code, out, err = run_exe(exe, [str(mode), "100", "on", "4"], env)
        actual = out.splitlines()
        if code != 0 or len(actual) != 217:
            raise RuntimeError("unexpected candidate output in mode %d" % mode)
        expected = [baseline[0]] + [row for row in baseline[1:] if row.split(",")[1] == str(mode)]
        if expected != actual:
            raise RuntimeError("legacy result mismatch in mode %d" % mode)
    return "PASS 648 legacy battles across all difficulties match baseline with four extra candidate waves"


SPECIAL = {"RuleParityChecks": rule_parity, "R25LegacyCandidateChecks": r25_legacy}


def generic(script, name):
    with open(script, encoding="utf-8-sig") as f:
        text = f.read()
    lines = [l for l in text.splitlines() if l.strip().startswith("& $compiler")]
    if len(lines) != 1 or "ReadAllText" in text:
        raise RuntimeError("unsupported script shape; add a SPECIAL handler")
    sources = []
    for base, rel in re.findall(r"Join-Path \$(\w+) '([^']+\.cs)'", lines[0]):
        rel = rel.replace("\\", "/")
        sources.append(os.path.join(ROOT if base in ("root", "projectRoot") else TESTS, rel))
    exe = compile_exe(name, sources)
    run = re.search(r"^\s*& \$(?:output|out|exe)\b(.*)$", text, re.M)
    args = run.group(1).split() if run else []
    code, out, err = run_exe(exe, args)
    tail = (out + err).strip().splitlines()
    if code != 0:
        raise RuntimeError(next((l.strip() for l in tail if re.match(r"\s*System\.\w*Exception:", l)), tail[-1] if tail else "exit %d" % code))
    return tail[-1] if tail else "exit 0"


def main():
    os.makedirs(BIN, exist_ok=True)
    words = sys.argv[1:]
    scripts = sorted(glob.glob(os.path.join(TESTS, "Run-*Checks.ps1")))
    selected = [s for s in scripts if not words or any(w.lower() in os.path.basename(s).lower() for w in words)]
    if not selected:
        sys.exit("no check matches: " + " ".join(words))
    print("Mono:", MONO_ROOT)
    failed = 0
    for script in selected:
        name = os.path.basename(script)[4:-4]
        try:
            result = SPECIAL[name]() if name in SPECIAL else generic(script, name)
            print("PASS  %-32s %s" % (name, result[:150]))
        except Exception as ex:  # report and continue with the next check
            failed += 1
            print("FAIL  %-32s %s" % (name, str(ex)[:200]))
    print("%d/%d passed" % (len(selected) - failed, len(selected)))
    sys.exit(1 if failed else 0)


if __name__ == "__main__":
    main()
