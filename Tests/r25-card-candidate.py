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
strategy = sys.argv[3] if len(sys.argv)>3 else 'first'
if strategy not in ('first','last','branches'):
    raise ValueError('strategy must be first, last, or branches')
if strategy == 'last':
    probe = once(probe, 'b.Reinforcements.Choices()[0]', 'b.Reinforcements.Choices()[b.Reinforcements.Choices().Length-1]')
if strategy == 'branches':
    probe = once(probe, 'new uint[]{29,41,57,73}', 'new uint[]{29}')
    probe = once(probe, 'new[]{"sword","archer","bomb","swordB","smart","smartB","bombA","bombB","sniper"}', 'new[]{"smartB"}')
    probe = once(probe, '(idle?new[]{false}:new[]{false,true})', '(idle?new[]{false}:new[]{true})')
    probe = once(probe, 'var b=new Battle(seed,field);', 'string cardSequence="";var b=new Battle(seed,field);')
    probe = once(probe, 'if(b.ReinforcementPending&&!b.ChooseReinforcement(b.Reinforcements.Choices()[0]))throw new Exception("choice failed");', 'if(b.ReinforcementPending){int branch=int.Parse(args[5]);int index=(branch/(int)Math.Pow(3,b.Reinforcements.SelectedCount))%3;int card=b.Reinforcements.Choices()[index];cardSequence+=(cardSequence.Length==0?"":"-")+card;if(!b.ChooseReinforcement(card))throw new Exception("choice failed");}')
probe = once(probe, 'seconds,level2,level3', 'seconds,level2,level3,cards')
probe = once(probe, '+level2+","+level3);', '+level2+","+level3+","+b.Reinforcements.SelectedCount);')
if strategy == 'branches':
    probe = once(probe, 'seconds,level2,level3,cards', 'seconds,level2,level3,cards,sequence')
    probe = once(probe, '+level2+","+level3+","+b.Reinforcements.SelectedCount);', '+level2+","+level3+","+b.Reinforcements.SelectedCount+","+cardSequence);')
for name, data in [('Battle.cs', battle), ('Balance.cs', source('Assets/Scripts/Core/Balance.cs')), ('CombatSave.cs', source('Assets/Scripts/Core/CombatSave.cs')), ('Probe.cs', probe)]:
    (OUT / name).write_text(data, encoding='utf-8')
csc = Path(os.environ['WINDIR']) / 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
exe = OUT / 'Probe.exe'
subprocess.run([str(csc), '/nologo', '/utf8output', '/codepage:65001', f'/out:{exe}', *(str(OUT / x) for x in ('Balance.cs','Battle.cs','CombatSave.cs','Probe.cs'))], check=True, cwd=ROOT)
env = os.environ.copy()
env.update(R25_CANDIDATE_FACTOR=sys.argv[1] if len(sys.argv)>1 else '2.5', R25_CANDIDATE_VARIATION='1', R25_CANDIDATE_TUNED='1', R25_CANDIDATE_DISTINCT='1')
all_rows = []
for mode in range(3):
    branches = range(27) if strategy == 'branches' else (None,)
    if strategy == 'branches':
        env['R25_CANDIDATE_FACTOR'] = '2.5' if mode == 0 else '1.25'
    for branch in branches:
        args = [str(exe), str(mode), '100', 'on', '4']
        if branch is not None:
            args += ['branch', str(branch)]
        rows = subprocess.check_output(args, cwd=ROOT, env=env).decode('utf-8').splitlines()
        expected = 4 if strategy == 'branches' else 217
        if len(rows) != expected:
            raise RuntimeError(f'Unexpected row count: {len(rows)}')
        all_rows.extend(rows if not all_rows else rows[1:])
output = Path(sys.argv[2]) if len(sys.argv)>2 else OUT / 'results.csv'
output.write_text('\n'.join(all_rows)+'\n', encoding='utf-8')
print(f'PASS {len(all_rows)-1} isolated candidate battles: {output}')
