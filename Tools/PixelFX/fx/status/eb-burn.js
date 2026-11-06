// Project effect, kept outside the vendored library. A burning status for small enemies on bright ground. The flame
// tongues come from PixelFX status/_lib.js (adapted from Sprite Fusion's open-source fire, MIT); an ink layer outside
// them is what reads on sand. Render with `node Tools/PixelFX/render-game.mjs`.
import { cycle, fit } from "../../../../ThirdParty/PixelFX/src/core/index.js";
import { tongue, tnoise, LAYERS } from "../../../../ThirdParty/PixelFX/src/fx/status/_lib.js";

export const meta = {
  name: "Ember Bastion burn",
  desc: "Burning status that never blinks out: three inked flame tongues sway and flicker over the body, inked embers rise off them.",
  tags: ["status", "fire", "burn", "dot", "game", "readable", "inked", "loop"],
  size: [48, 56],
  origin: [24, 50],
  duration: 0.6,
  loop: true,
  fps: 20,
  bg: "ground",
  // bright to dark, then the ink
  options: { palette: ["#fff6c8", "#ffe066", "#ffc23d", "#ff8a2a", "#f2561d", "#c9281f", "#8a1f17", "#2a0c08"], seed: 5 },
};

// the library's layers plus an ink rim one step wider and taller than the darkest flame
const INKED = [[1.32, 1.1, 7], ...LAYERS];

export function draw(p, t, o) {
  const F = fit(o.palette, 8), D = meta.duration, S = o.seed;
  // three tongues on the upper body: a tall middle one and two shorter ones, each swaying and flickering on its own
  const tongues = [
    { x: 0, h: 24, w: 5.2, n: S },
    { x: -7, h: 15, w: 3.8, n: S + 11 },
    { x: 7, h: 17, w: 4, n: S + 23 },
  ];
  const sway = tnoise(t, D, 3, S + 40) * 3;
  // the back two first, so the middle one's ink overlaps them
  for (const g of [tongues[1], tongues[2], tongues[0]]) {
    const h = g.h * (0.82 + 0.18 * tnoise(t, D, 5, g.n));
    tongue(p, g.x, -6, h, g.w, sway + g.x * 0.15, F, t, D, g.n, INKED);
  }
  // embers: inked 2 px sparks that rise and drift off, a few at a time
  cycle(t, D, 4, S + 70, 0.5, (age, life, g) => {
    const u = age / life, x = Math.round(-8 + g.next() * 16 + sway * u * 2), y = Math.round(-20 - age * 46);
    const c = u < 0.5 ? F[1] : F[3];
    for (let dy = -1; dy <= 2; dy++) for (let dx = -1; dx <= 2; dx++) p.px(x + dx, y + dy, F[7]);
    for (let dy = 0; dy < 2; dy++) for (let dx = 0; dx < 2; dx++) p.px(x + dx, y + dy, dx + dy === 0 ? F[0] : c);
  });
}
