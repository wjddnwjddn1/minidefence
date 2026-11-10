"""Integration checks for evidence validity, including fast but non-rendering runs."""
import csv
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

spec = importlib.util.spec_from_file_location('native_analysis', Path(__file__).with_name('Analyze-NativePerformance.py'))
analysis = importlib.util.module_from_spec(spec)
spec.loader.exec_module(analysis)

class EvidenceChecks(unittest.TestCase):
    def write_case(self, root, label, visible=True, gui=2, gpu=1, repeat_timestamp=False, enemies=85):
        (root/(label+'-metadata.json')).write_text(json.dumps({'windowVisible': visible, 'timingEnabled': True}), encoding='utf-8')
        rows = [{'frame': i, 'enemies': enemies, 'delta_ms': 10, 'update_ms': 1,
                 'gui_ms': 2, 'core_ms': .1, 'gui_calls': gui, 'gc_allocated_bytes': 1024} for i in range(900)]
        for suffix, data in [('frames', rows), ('timings', [dict(timestamp=1 if repeat_timestamp else i+1, gpu_ms=gpu) for i in range(900)])]:
            with (root/(label+'-'+suffix+'.csv')).open('w', encoding='utf-8', newline='') as stream:
                writer = csv.DictWriter(stream, fieldnames=data[0].keys())
                writer.writeheader()
                writer.writerows(data)

    def test_hidden_fast_frames_are_not_reported_as_fps(self):
        with tempfile.TemporaryDirectory() as tmp:
            root=Path(tmp); self.write_case(root, 'hidden', visible=False, gui=0, gpu=0)
            result=analysis.analyze(root)
            self.assertFalse(result['rendering']['hidden']['valid'])
            self.assertIsNone(result['rendering']['hidden']['frame_ms'])

    def test_duplicate_gpu_data_is_not_independent_evidence(self):
        with tempfile.TemporaryDirectory() as tmp:
            root=Path(tmp); self.write_case(root, 'duplicate', repeat_timestamp=True)
            self.assertFalse(analysis.analyze(root)['rendering']['duplicate']['valid'])

    def test_four_visible_groups_are_complete(self):
        with tempfile.TemporaryDirectory() as tmp:
            root=Path(tmp)
            for label in ['1280-battle','1280-selected','1920-battle','1920-selected']: self.write_case(root,label)
            result=analysis.analyze(root)
            self.assertTrue(result['rendering_complete'])
            self.assertEqual(result['rendering']['1280-battle']['frame_ms']['p95'],10)

    def test_wrong_density_or_missing_gpu_is_rejected(self):
        with tempfile.TemporaryDirectory() as tmp:
            root=Path(tmp); self.write_case(root,'wrong',enemies=100); self.write_case(root,'no_gpu',gpu=0)
            self.assertTrue(all(not v['valid'] for v in analysis.analyze(root)['rendering'].values()))

    def test_visible_frame_budget_failure_is_reported(self):
        with tempfile.TemporaryDirectory() as tmp:
            root=Path(tmp)
            for label in ['1280-battle','1280-selected','1920-battle','1920-selected']:
                self.write_case(root,label)
                path=root/(label+'-frames.csv')
                path.write_text(path.read_text(encoding='utf-8').replace(',10,',',25,'),encoding='utf-8')
            result=analysis.analyze(root)
            self.assertTrue(result['rendering_complete'])
            self.assertFalse(result['frame_budget_pass'])

    def test_input_series_requires_all_thirty_high_density_samples(self):
        with tempfile.TemporaryDirectory() as tmp:
            root=Path(tmp)
            sample=dict(windowFound=True,postSucceeded=True,received=True,pauseChanged=True,enemiesAtSend=85,queueToEventMs=1,queueToFrameEndMs=2)
            series=[dict(sample) for _ in range(30)]
            path=root/'input-series.json'
            path.write_text(json.dumps({'samples':series}),encoding='utf-8')
            self.assertTrue(analysis.analyze(root)['input_series']['complete'])
            series[0]['enemiesAtSend']=79
            path.write_text(json.dumps({'samples':series}),encoding='utf-8')
            result=analysis.analyze(root)['input_series']
            self.assertFalse(result['complete'])
            self.assertEqual(result['valid_samples'],29)

    def test_failed_input_delivery_is_not_a_latency_sample(self):
        with tempfile.TemporaryDirectory() as tmp:
            root=Path(tmp); (root/'input.json').write_text(json.dumps(dict(windowFound=True, postSucceeded=False, received=True,pauseChanged=True,queueToEventMs=1,queueToFrameEndMs=2)),encoding='utf-8')
            self.assertFalse(analysis.analyze(root)['input']['valid'])

if __name__=='__main__': unittest.main()
