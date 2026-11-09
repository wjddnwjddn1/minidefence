#!/usr/bin/env python3
"""Claude's independent T1 review checks (Docs/CLAUDE-REVIEW.md 31차) on macOS with Unity Mono.

  python3 Tests/run-t1-review.py           # both stages
  python3 Tests/run-t1-review.py current   # only Tests/T1ReviewChecks.cs (current API)
  python3 Tests/run-t1-review.py api       # only Tests/T1ReviewApiChecks.cs (assumed T1 API)

Stage "current" compiles against today's code. Stage "api" needs the T1 API; before T1 it is
expected to stop at COMPILE with the missing names listed. Output goes to Tests/bin/mac.
Exit: 0 all pass, 1 regression ([보존] fail) or unexpected error, 2 only [T1요구] failures
or the api stage does not compile yet.
"""
import glob
import importlib.util
import os
import re
import subprocess
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
spec = importlib.util.spec_from_file_location("mac_checks", os.path.join(HERE, "run-checks-mac.py"))
mac = importlib.util.module_from_spec(spec)
sys.argv, argv = sys.argv[:1], sys.argv[1:]  # the runner module reads nothing at import, but keep argv clean
spec.loader.exec_module(mac)
ROOT, BIN = mac.ROOT, mac.BIN


def sources():
    files = sorted(glob.glob(os.path.join(ROOT, "Assets/Scripts/Core/*.cs")))
    files.append(os.path.join(ROOT, "Assets/Scripts/Presentation/ProgressStore.cs"))
    # A5: the extracted win-record helper may live anywhere under Assets/Scripts.
    for path in glob.glob(os.path.join(ROOT, "Assets/Scripts/**/*.cs"), recursive=True):
        with open(path, encoding="utf-8-sig") as f:
            if re.search(r"\bclass\s+VictoryRecords\b", f.read()) and path not in files:
                files.append(path)
    return files


def stage(name, tests, main):
    exe = os.path.join(BIN, name + ".exe")
    cmd = [mac.MONO, mac.CSC, "-nologo", "-langversion:5", "-main:" + main, "-out:" + exe] + sources() + [os.path.join(HERE, t) for t in tests]
    r = subprocess.run(cmd, capture_output=True, text=True)
    if r.returncode != 0:
        errors = [l for l in r.stdout.splitlines() if ": error " in l]
        missing = sorted(set(re.findall(r"'([A-Za-z_][\w.]*)'", "\n".join(e for e in errors if "CS0103" in e or "CS0117" in e or "CS0246" in e or "CS1061" in e))))
        print("COMPILE FAIL [%s] %d errors" % (name, len(errors)))
        for e in errors[:12]:
            print("  " + e.split("Tests/")[-1])
        if missing:
            print("  missing names: " + ", ".join(missing))
        return None
    code, out, err = mac.run_exe(exe)
    print(out.rstrip())
    if err.strip():
        print(err.rstrip())
    return code


def main():
    os.makedirs(BIN, exist_ok=True)
    which = argv[0] if argv else "all"
    results = []
    if which in ("all", "current"):
        print("== current API: Tests/T1ReviewChecks.cs")
        results.append(stage("T1ReviewChecks", ["T1ReviewChecks.cs"], "T1ReviewChecks"))
    if which in ("all", "api"):
        print("== assumed T1 API: Tests/T1ReviewApiChecks.cs")
        code = stage("T1ReviewApiChecks", ["T1ReviewChecks.cs", "T1ReviewApiChecks.cs"], "T1ReviewApiChecks")
        results.append(2 if code is None else code)
    if any(c is None or c == 1 or c not in (0, 1, 2) for c in results):
        sys.exit(1)
    sys.exit(2 if 2 in results else 0)


if __name__ == "__main__":
    main()
