"""Summarize native evidence without turning hidden/no-GUI samples into FPS claims."""
import argparse
import csv
import json
import math
from pathlib import Path
import statistics

def percentile(values, quantile):
    values = sorted(values)
    return values[math.ceil((len(values)-1)*quantile)] if values else None

def distribution(values):
    return {'samples': len(values), 'median': statistics.median(values) if values else None,
            'p95': percentile(values, .95), 'p99': percentile(values, .99),
            'maximum': max(values) if values else None}

def read_csv(path):
    with path.open(encoding='utf-8-sig') as stream:
        return list(csv.DictReader(stream))

def analyze(folder):
    result = {'scope': 'Native Unity development diagnostic on this PC; not user fun or device-to-display latency',
              'frame_budget_ms': 1000/60, 'rendering': {}, 'memory_budget': 'NR: no project budget specified'}
    if (folder/'logic.csv').exists():
        rows = read_csv(folder/'logic.csv')
        assert len(rows) == 10000
        assert all(80 <= int(r['before_enemies']) <= 99 and 80 <= int(r['after_enemies']) <= 99 for r in rows)
        result['fixed_step_ms'] = distribution([float(r['step_ms']) for r in rows])
    for path in sorted(folder.glob('*-frames.csv')):
        label = path.name.removesuffix('-frames.csv')
        metadata = json.loads((folder/(label+'-metadata.json')).read_text(encoding='utf-8'))
        rows = read_csv(path)
        timings = read_csv(folder/(label+'-timings.csv'))
        eligible = all(80 <= int(r['enemies']) <= 99 for r in rows)
        gui = rows and all(int(r['gui_calls']) > 0 for r in rows)
        gpu = [float(r['gpu_ms']) for r in timings if float(r['gpu_ms']) > 0]
        stamps = {int(r['timestamp']) for r in timings if float(r['gpu_ms']) > 0 and int(r['timestamp']) > 0}
        valid = bool(metadata['windowVisible'] and metadata.get('timingEnabled', False) and gui and eligible and len(rows) >= 900 and len(stamps) >= 100)
        result['rendering'][label] = {'valid': valid,
            'reason': 'eligible visible GUI/GPU samples' if valid else 'NR: hidden window, missing GUI/GPU data, or insufficient eligible frames',
            'metadata': metadata, 'frame_ms': distribution([float(r['delta_ms']) for r in rows]) if valid else None,
            'update_ms': distribution([float(r['update_ms']) for r in rows]) if valid else None,
            'gui_ms': distribution([float(r['gui_ms']) for r in rows]) if valid else None,
            'core_ms': distribution([float(r['core_ms']) for r in rows]) if valid else None,
            'gpu_ms': distribution(gpu) if valid else None,
            'allocated_bytes': distribution([int(r['gc_allocated_bytes']) for r in rows if int(r['gc_allocated_bytes']) >= 0]) if valid else None,
            'p95_within_60fps_budget': percentile([float(r['delta_ms']) for r in rows], .95) <= 1000/60 if valid else None}
    for name in ['storage', 'input']:
        path = folder/(name+'.json')
        if path.exists():result[name] = json.loads(path.read_text(encoding='utf-8'))
    if 'input' in result:
        point = result['input']
        point['valid'] = bool(point['windowFound'] and point['postSucceeded'] and point['received'] and point['pauseChanged'] and 0 <= point['queueToEventMs'] <= point['queueToFrameEndMs'])
    result['rendering_complete'] = len(result['rendering']) == 4 and all(v['valid'] for v in result['rendering'].values())
    result['frame_budget_pass'] = all(v['p95_within_60fps_budget'] for v in result['rendering'].values()) if result['rendering_complete'] else None
    return result

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('folder', type=Path)
    args = parser.parse_args()
    result = analyze(args.folder)
    (args.folder/'analysis.json').write_text(json.dumps(result, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
    print(json.dumps(result, ensure_ascii=False, indent=2))
    if result['frame_budget_pass'] is False:
        raise SystemExit('FAIL: valid frame p95 exceeds the 16.67ms target')
