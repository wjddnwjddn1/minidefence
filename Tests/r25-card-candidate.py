"""Isolated r25 card-pressure experiment; never changes game sources or saves."""
from pathlib import Path
import os
import subprocess
import sys

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / 'Tests' / 'bin' / 'r25-card-prototype'
OUT.mkdir(parents=True, exist_ok=True)
REV = 'bfcc0b7'

def source(path):
    return subprocess.check_output(['git', 'show', f'{REV}:{path}'], cwd=ROOT).decode('utf-8-sig')

def once(text, old, new):
    if text.count(old) != 1:
        raise RuntimeError(f'Unexpected pinned source: {old}')
    return text.replace(old, new)

battle = source('Assets/Scripts/Core/Battle.cs')
for old, new in [
    ('||!SetDifficulty(1))return false;Reinforcements=new ReinforcementDraft(rng);', ' )return false;Reinforcements=new ReinforcementDraft(rng);'),
    ('return Reinforcements!=null&&g.Kind!=4?(int)Math.Round(count*1.15):count;', 'return count;'),
    ('return Reinforcements!=null&&g.Kind!=4?gap/1.15:gap;', 'return gap;'),
    ('MaxChoices=2;', 'MaxChoices=3;'),
    ('(Wave==5&&Reinforcements.SelectedCount==1))', '(Wave==5&&Reinforcements.SelectedCount==1)||(Wave==8&&Reinforcements.SelectedCount==2))'),
]:
    battle = once(battle, old, new)
probe = source('Tests/R25CandidateProbe.cs')
probe = once(probe, 'b.SetDifficulty(mode);int ticks=0,peak=0;', 'b.SetDifficulty(mode);if(!b.EnableReinforcements())throw new Exception("draft init failed");int ticks=0,peak=0;')
probe = once(probe, 'if(b.Phase==Phase.Preparation&&!b.Start())throw new Exception("start failed");', 'if(b.Phase==Phase.Preparation&&!b.Start())throw new Exception("start failed");if(b.ReinforcementPending&&!b.ChooseReinforcement(b.Reinforcements.Choices()[0]))throw new Exception("choice failed");')
probe = once(probe, 'seconds,level2,level3', 'seconds,level2,level3,cards')
probe = once(probe, '+level2+","+level3);', '+level2+","+level3+","+b.Reinforcements.SelectedCount);')
for name, data in [('Battle.cs', battle), ('Balance.cs', source('Assets/Scripts/Core/Balance.cs')), ('CombatSave.cs', source('Assets/Scripts/Core/CombatSave.cs')), ('Probe.cs', probe)]:
    (OUT / name).write_text(data, encoding='utf-8')
csc = Path(os.environ['WINDIR']) / 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
exe = OUT / 'Probe.exe'
subprocess.run([str(csc), '/nologo', '/utf8output', '/codepage:65001', f'/out:{exe}', *(str(OUT / x) for x in ('Balance.cs','Battle.cs','CombatSave.cs','Probe.cs'))], check=True, cwd=ROOT)
env = os.environ.copy()
env.update(R25_CANDIDATE_FACTOR=sys.argv[1] if len(sys.argv)>1 else '2.5', R25_CANDIDATE_VARIATION='1', R25_CANDIDATE_TUNED='1', R25_CANDIDATE_DISTINCT='1')
all_rows = []
for mode in range(3):
    rows = subprocess.check_output([str(exe), str(mode), '100', 'on', '4'], cwd=ROOT, env=env).decode('utf-8').splitlines()
    if len(rows) != 217:
        raise RuntimeError(f'Unexpected row count: {len(rows)}')
    all_rows.extend(rows if mode == 0 else rows[1:])
output = Path(sys.argv[2]) if len(sys.argv)>2 else OUT / 'results.csv'
output.write_text('\n'.join(all_rows)+'\n', encoding='utf-8')
print(f'PASS 648 isolated candidate battles: {output}')
