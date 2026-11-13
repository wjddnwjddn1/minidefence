// Checks that the game's own effect sheets in Assets/Resources/PixelFX are exactly what
// Tools/PixelFX/render-game.mjs makes from Tools/PixelFX/fx and game-effects.json, that the one-shot hits
// end empty and that the burning loop never blinks out. Run: node Tests/PixelFxGameEffects.mjs
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { loadEffect, renderFrames, spriteSheet } from '../ThirdParty/PixelFX/tools/lib/fxlib.mjs';

const root = resolve(fileURLToPath(new URL('..', import.meta.url)));
const manifest = JSON.parse(readFileSync(join(root, 'Tools/PixelFX/game-effects.json'), 'utf8'));
// what BattleView expects of each sheet: frame count, frame size, fps, loop
const expected = {
  HitSteel: [10, 72, 52, 20, false],
  HitArrow: [10, 72, 52, 20, false],
  HitFrost: [10, 72, 52, 20, false],
  Burn: [12, 48, 56, 20, true],
  GatePortal: [29, 152, 116, 12, true],
  SwordWind: [15, 120, 52, 30, true],
  PierceShot: [18, 200, 40, 30, true],
  KingCrown: [30, 56, 80, 15, true],
};
assert.deepEqual(manifest.effects.map(e => e.name), Object.keys(expected));
const opaque = rgba => rgba.reduce((n, v, i) => n + (i % 4 === 3 && v > 8 ? 1 : 0), 0);
for (const e of manifest.effects) {
  const fx = await loadEffect(e.id, join(root, 'Tools/PixelFX/fx'));
  const r = renderFrames(fx, { actor: false, options: e.opts });
  const [n, w, h, fps, loop] = expected[e.name];
  assert.deepEqual([r.n, r.w, r.h, r.fps, !!fx.meta.loop], [n, w, h, fps, loop], e.name + ' frames, size, fps and loop');
  if (loop) {
    // a status shows for as long as it lasts, so no frame may be close to empty
    const least = Math.min(...r.frames.map(f => opaque(f.clear)));
    assert.ok(least > 60, e.name + ' should never blink out, its emptiest frame has ' + least + ' pixels');
  } else {
    const lit = opaque(r.frames[r.n - 1].clear);
    assert.ok(lit < 12, e.name + ' should be nearly empty on its last frame, has ' + lit + ' pixels');
  }
  const s = spriteSheet(fx, r, e.name);
  assert.deepEqual(readFileSync(join(root, 'Assets/Resources/PixelFX', e.name + '.png')), Buffer.from(s.png), e.name + '.png is stale: run node Tools/PixelFX/render-game.mjs');
  // git may check JSON out with CRLF line endings, so compare the text, not the line endings
  assert.equal(readFileSync(join(root, 'Assets/Resources/PixelFX', e.name + '.json'), 'utf8').replace(/\r\n/g, '\n'), JSON.stringify(s.json, null, 1), e.name + '.json is stale');
}
console.log('PASS game PixelFX sheets match Tools/PixelFX/fx (' + manifest.effects.length + ' effects)');
