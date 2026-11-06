// Checks that the game's own hit sheets in Assets/Resources/PixelFX are exactly what
// Tools/PixelFX/render-game.mjs makes from Tools/PixelFX/fx and game-effects.json, and that each
// effect ends empty. Run: node Tests/PixelFxGameEffects.mjs
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { loadEffect, renderFrames, spriteSheet } from '../ThirdParty/PixelFX/tools/lib/fxlib.mjs';

const root = resolve(fileURLToPath(new URL('..', import.meta.url)));
const manifest = JSON.parse(readFileSync(join(root, 'Tools/PixelFX/game-effects.json'), 'utf8'));
assert.deepEqual(manifest.effects.map(e => e.name), ['HitSteel', 'HitArrow', 'HitFrost']);
for (const e of manifest.effects) {
  const fx = await loadEffect(e.id, join(root, 'Tools/PixelFX/fx'));
  const r = renderFrames(fx, { actor: false, options: e.opts });
  // BattleView plays 10 frames of 72x52 at 20 fps, three to a row
  assert.equal(r.n, 10, e.name + ' frame count');
  assert.deepEqual([r.w, r.h, r.fps], [72, 52, 20], e.name + ' frame size');
  const last = r.frames[r.n - 1].clear, lit = last.reduce((n, v, i) => n + (i % 4 === 3 && v > 8 ? 1 : 0), 0);
  assert.ok(lit < 12, e.name + ' should be nearly empty on its last frame, has ' + lit + ' pixels');
  const s = spriteSheet(fx, r, e.name);
  assert.deepEqual(readFileSync(join(root, 'Assets/Resources/PixelFX', e.name + '.png')), Buffer.from(s.png), e.name + '.png is stale: run node Tools/PixelFX/render-game.mjs');
  // git may check JSON out with CRLF line endings, so compare the text, not the line endings
  assert.equal(readFileSync(join(root, 'Assets/Resources/PixelFX', e.name + '.json'), 'utf8').replace(/\r\n/g, '\n'), JSON.stringify(s.json, null, 1), e.name + '.json is stale');
}
console.log('PASS game PixelFX sheets match Tools/PixelFX/fx (' + manifest.effects.length + ' effects)');
