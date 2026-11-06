// Renders the project's own effects (Tools/PixelFX/fx) into the sprite sheets the game loads.
// The vendored library stays untouched: its renderer is reused, only the effect folder differs.
// Usage: node Tools/PixelFX/render-game.mjs [Name ...]   (default: every entry in game-effects.json)
import { readFileSync, mkdirSync } from 'node:fs';
import { resolve, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { loadEffect, renderFrames, spriteSheet, contactSheet, save } from '../../ThirdParty/PixelFX/tools/lib/fxlib.mjs';

const root = resolve(fileURLToPath(new URL('../..', import.meta.url)));
const fxDir = join(root, 'Tools', 'PixelFX', 'fx');
const out = join(root, 'Assets', 'Resources', 'PixelFX');
const preview = join(root, 'Docs', 'pixelfx-game-effects');
const manifest = JSON.parse(readFileSync(join(root, 'Tools', 'PixelFX', 'game-effects.json'), 'utf8'));
const wanted = process.argv.slice(2);
const entries = manifest.effects.filter(e => !wanted.length || wanted.includes(e.name));
if (!entries.length) { console.error('No matching effect in game-effects.json'); process.exit(2); }
mkdirSync(out, { recursive: true });
mkdirSync(preview, { recursive: true });
for (const e of entries) {
  const fx = await loadEffect(e.id, fxDir);
  const r = renderFrames(fx, { actor: false, options: e.opts });
  const s = spriteSheet(fx, r, e.name);
  save(join(out, e.name + '.png'), s.png);
  save(join(out, e.name + '.json'), JSON.stringify(s.json, null, 1));
  save(join(preview, e.name + '-contact.png'), contactSheet(fx, r, { max: 16 }));
  console.log(`${e.name}  ${e.id}  ${r.n} frames ${r.w}x${r.h} @${r.fps}fps -> Assets/Resources/PixelFX/${e.name}.png`);
}
