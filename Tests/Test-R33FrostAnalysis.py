"""Regression tests for the rejection gate, including boundaries and bad evidence."""
import importlib.util
from pathlib import Path
import tempfile
import unittest

spec = importlib.util.spec_from_file_location('gate', Path(__file__).with_name('Analyze-R33Frost.py'))
gate = importlib.util.module_from_spec(spec)
spec.loader.exec_module(gate)

class FrostGateTests(unittest.TestCase):
    def rows(self, peak=None, win=None):
        result = ['variant real rules 4 (wins/battles·median PeakAlive·median final gold)']
        for f in range(4):
            for m in range(3):
                for p in gate.POLICIES:
                    for seed in gate.SEEDS:
                        value = {'smartB':70, 'smartBFrost2':74, 'bombFrost2':76, 'frost':100}[p]
                        if peak:
                            value = peak(f, m, p, value)
                        outcome = 'L' if p=='frost' else 'W'
                        if win:
                            outcome = win(f, m, p, seed, outcome)
                        result.append(f'{p},{m},{seed},0,{outcome},12,{value},,0,100,0')
        return result

    def analyze(self, rows):
        with tempfile.TemporaryDirectory() as directory:
            p = Path(directory)/'raw.txt'
            p.write_text('\n'.join(rows)+'\n', encoding='utf-8')
            return gate.analyze(p)

    def test_supported_mix_without_solo_wins_passes(self):
        result = self.analyze(self.rows())
        self.assertTrue(result['passed'])
        self.assertEqual((result['cases'], len(result['cells'])), (384, 48))

    def test_one_basic_solo_win_rejects(self):
        result = self.analyze(self.rows(win=lambda f,m,p,s,o: 'W' if (f,m,p,s)==(2,0,'frost',29) else o))
        self.assertFalse(result['passed'])
        self.assertEqual(result['frost_basic_wins'], 1)

    def test_solo_wins_on_harder_mode_do_not_trigger_basic_gate(self):
        self.assertTrue(self.analyze(self.rows(win=lambda f,m,p,s,o: 'W' if p=='frost' and m==1 else o))['passed'])

    def test_more_than_five_better_in_all_cells_rejects(self):
        result = self.analyze(self.rows(peak=lambda f,m,p,v:64 if p=='smartBFrost2' else v))
        self.assertEqual(result['dominant_all_12'], ['smartBFrost2'])
        self.assertFalse(result['passed'])

    def test_exactly_five_is_allowed(self):
        self.assertTrue(self.analyze(self.rows(peak=lambda f,m,p,v:65 if p=='smartBFrost2' else v))['passed'])

    def test_only_eleven_dominant_cells_are_allowed(self):
        self.assertTrue(self.analyze(self.rows(peak=lambda f,m,p,v:64 if p=='smartBFrost2' and (f,m)!=(3,2) else v))['passed'])

    def test_bomb_frost_best_in_all_cells_rejects_even_without_five_point_gap(self):
        result = self.analyze(self.rows(peak=lambda f,m,p,v:69 if p=='bombFrost2' else v))
        self.assertTrue(result['bomb_frost_best_all_12'])
        self.assertFalse(result['passed'])

    def test_missing_rows_duplicate_seed_stall_and_ember_duplicate_reject(self):
        good = self.rows()
        for bad in [good[:-1], good[:1]+[good[1]]+good[1:],
                    good[:2]+[good[2].replace(',41,', ',29,')]+good[3:],
                    [good[0], good[1].replace(',W,', ',S,')]+good[2:],
                    [good[0], good[1].replace(',0,W,', ',1,W,')]+good[2:]]:
            with self.subTest(row_count=len(bad)):
                with self.assertRaises(AssertionError):
                    self.analyze(bad)

if __name__ == '__main__':
    unittest.main()
