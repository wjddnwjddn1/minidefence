"""Validate the committed slot comparison against unchanged historical controls."""
import csv
from pathlib import Path

root = Path(__file__).resolve().parents[1]
def read(name):
    with (root / 'Docs' / name).open(encoding='utf-8-sig') as stream:
        return list(csv.DictReader(stream))

rows = read('r27-slot-comparison.csv')
key_columns = ['seed', 'mode', 'field', 'policy', 'ember']
def key(row):
    return tuple(row[k] for k in key_columns)
def compare(row, old):
    assert {k: v for k, v in row.items() if k != 'profile'} == {
        k: v for k, v in old.items() if k != 'profile'}, (key(row), row, old)

old_rules = {key(r): r for r in read('t4-role-usage.csv') if r['profile'] == 'R3'}
eight_slots = {key(r): r for r in rows if r['profile'] == 'C8'}
counts = {}
legacy = other_fields = 0
for row in rows:
    counts[row['profile']] = counts.get(row['profile'], 0) + 1
    assert row['result'] in ('Won', 'Lost'), row
    if row['profile'] == 'R3':
        compare(row, old_rules[key(row)])
        legacy += 1
    elif row['profile'] == 'C' and row['field'] != '2':
        compare(row, eight_slots[key(row)])
        other_fields += 1
assert counts == {'R3': 1344, 'C8': 1344, 'C': 1344}, counts
assert legacy == 1344 and other_fields == 1008
print(f'PASS {legacy} full historical R3 controls, {other_fields} unchanged other-field controls; 4032 terminal outcomes')
