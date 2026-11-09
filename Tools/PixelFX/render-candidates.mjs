// Renders effect candidates (Tools/PixelFX/candidate-effects.json) that the game does not load yet, so they stay out
// of Assets/Resources and out of the shipped-sheet check. Same renderer as render-game.mjs.
// Usage: node Tools/PixelFX/render-candidates.mjs [Name ...]
import { readFileSync, mkdirSync } from 'node:fs';
import { resolve, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { loadEffect, renderFrames, spriteSheet, contactSheet, save } from '../../ThirdParty/PixelFX/tools/lib/fxlib.mjs';

const root = resolve(fileURLToPath(new URL('../..', import.meta.url)));
const fxDir = join(root, 'Tools', 'PixelFX', 'fx');
const out = join(root, 'Art', 'PixelFX', 'candidates');
const manifest = JSON.parse(readFileSync(join(root, 'Tools', 'PixelFX', 'candidate-effects.json'), 'utf8'));
const wanted = process.argv.slice(2);
const entries = manifest.effects.filter(e => !wanted.length || wanted.includes(e.name));
if (!entries.length) { console.error('No matching effect in candidate-effects.json'); process.exit(2); }
mkdirSync(out, { recursive: true });
for (const e of entries) {
  const fx = await loadEffect(e.id, fxDir);
  const r = renderFrames(fx, { actor: false, options: e.opts });
  const s = spriteSheet(fx, r, e.name);
  save(join(out, e.name + '.png'), s.png);
  save(join(out, e.name + '.json'), JSON.stringify(s.json, null, 1));
  save(join(out, e.name + '-contact.png'), contactSheet(fx, r, { max: 16 }));
  console.log(`${e.name}  ${e.id}  ${r.n} frames ${r.w}x${r.h} @${r.fps}fps -> Art/PixelFX/candidates/${e.name}.png`);
}
