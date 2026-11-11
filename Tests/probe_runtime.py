"""Small cross-platform runner for the existing C# balance probes."""
import importlib.util
import os
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
PROCESS_OPTIONS = {'creationflags': subprocess.CREATE_NO_WINDOW} if os.name == 'nt' else {}
if os.name == 'nt':
    COMPILER = [str(Path(os.environ['WINDIR'])/'Microsoft.NET/Framework64/v4.0.30319/csc.exe')]
    EXECUTOR = []
    BIN = ROOT/'Tests/bin'
else:
    spec = importlib.util.spec_from_file_location('mac_checks', ROOT/'Tests/run-checks-mac.py')
    mac = importlib.util.module_from_spec(spec)
    saved = sys.argv
    try:
        sys.argv = sys.argv[:1]
        spec.loader.exec_module(mac)
    finally:
        sys.argv = saved
    COMPILER, EXECUTOR, BIN = [mac.MONO, mac.CSC], [mac.MONO], Path(mac.BIN)

def compile_exe(exe, sources, main):
    result = subprocess.run(COMPILER+['-nologo', '-utf8output', '-codepage:65001', '-langversion:5',
                                     '-main:'+main, '-out:'+str(exe)]+[str(p) for p in sources],
                            capture_output=True, text=True, encoding='utf-8', **PROCESS_OPTIONS)
    if result.returncode:
        raise RuntimeError(result.stdout+result.stderr)
    return exe

def run_exe(exe, arguments, env=None, cwd=None):
    result = subprocess.run(EXECUTOR+[str(exe)]+list(arguments), env=env, cwd=cwd,
                            capture_output=True, text=True, encoding='utf-8', timeout=600, **PROCESS_OPTIONS)
    if result.returncode:
        raise RuntimeError(result.stdout+result.stderr)
    return result.stdout
