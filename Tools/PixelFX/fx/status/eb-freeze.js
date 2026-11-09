// Project effect, kept outside the vendored library. A frozen status for small enemies on bright ground.
// The library's status/frozen wraps the stock actor's mask, which our Tiny Swords enemies don't have, so this one
// stays at the feet: inked ice crystals encase the ankles and a frosted patch sits under them. Nothing is drawn
// over the head or hands. Render with `node Tools/PixelFX/render-candidates.mjs`.
import { cycle, fit, TAU } from "../../../../ThirdParty/PixelFX/src/core/index.js";

export const meta = {
  name: "Ember Bastion freeze",
  desc: "Frozen status that never blinks out: inked ice crystals stand around the feet over a frosted patch, glints run up their edges.",
  tags: ["status", "ice", "freeze", "frozen", "game", "readable", "inked", "loop"],
  size: [56, 36],
  origin: [28, 30],
  duration: 0.6,
  loop: true,
  fps: 20,
  bg: "ground",
  // bright to dark, then the ink
  options: { palette: ["#ffffff", "#dcfbff", "#a6ecff", "#62c8f4", "#3a8bd8", "#2c58aa", "#1b2f66", "#0b0f24"], seed: 4 },
};

// a crystal: a wedge standing on y = 0, tip leaning by `lean` px; lit on its left face, shaded on its right
function crystal(p, x0, h, w, lean, F, ink) {
  for (let y = 0; y <= h; y++) {
    const u = y / h, half = w * (1 - u), cx = x0 + lean * u;
    for (let x = Math.floor(cx - half) - (ink ? 1 : 0); x <= Math.ceil(cx + half) + (ink ? 1 : 0); x++) {
      if (ink) { p.px(x, -y, F[7]); if (y === h) p.px(x, -y - 1, F[7]); continue; }
      const side = x < cx - 0.5 ? 2 : x > cx + 0.5 ? 4 : 3;
      p.px(x, -y, u > 0.8 ? F[1] : F[side]);
    }
  }
}

export function draw(p, t, o) {
  const F = fit(o.palette, 8), D = meta.duration, S = o.seed, phase = t / D;
  // frosted patch under the feet: an inked flat ellipse
  for (let y = -3; y <= 3; y++) for (let x = -20; x <= 20; x++) {
    const d = (x / 19) ** 2 + (y / 3.2) ** 2;
    if (d <= 1.25) p.px(x, y, d > 0.85 ? F[7] : d > 0.55 ? F[4] : F[2]);
  }
  // crystals: outer ones tall and leaning out, the ones over the ankles short so the legs still read
  const set = [[-16, 13, 3.4, -4], [-11, 7, 2.4, -2], [-6, 10, 2.6, -1], [-1, 4, 1.8, 0], [5, 6, 2, 1], [10, 11, 2.8, 2], [16, 8, 2.6, 4]];
  const grow = (i) => Math.round(Math.sin((phase + i * 0.17) * TAU) * 0.8);
  for (const ink of [true, false])
    set.forEach(([x, h, w, lean], i) => crystal(p, x, h + grow(i), w, lean, F, ink));
  // glints: a 2 px white spark climbs a crystal edge, a few at a time
  cycle(t, D, 3, S + 30, 0.3, (age, life, g) => {
    const [x, h, , lean] = set[Math.floor(g.next() * set.length)], u = age / life;
    const gx = Math.round(x + lean * u - 1), gy = -Math.round(h * u);
    p.px(gx, gy, F[0]); p.px(gx + 1, gy, F[1]); p.px(gx, gy - 1, F[1]);
  });
}
