// Project effect, kept outside the vendored library. A short, readable hit for bright pixel-art
// battlefields, built on the inked-wedge idea of PixelFX impacts/blunt-hit (MIT): solid inked shapes
// carry it, glow is only a garnish. Render with `node Tools/PixelFX/render-game.mjs`.
import { Rng, fit, clamp, ease, TAU, WHITE, mix } from "../../../../ThirdParty/PixelFX/src/core/index.js";

export const meta = {
  name: "Ember Bastion hit",
  desc: "A quick hit that reads on bright grass and sand: an inked white star pops, inked wedges jab out and snap back, chunky inked chips arc off and bounce.",
  tags: ["impact", "hit", "game", "readable", "inked", "one-shot"],
  size: [72, 52],
  origin: [36, 44],
  duration: 0.45,
  loop: false,
  fps: 20,
  bg: "ground",
  // steel by default: cool whites and a navy ink stand out on both yellow sand and green grass
  options: { palette: ["#141826", "#2a3048", "#4f5d80", "#8fa3c8", "#cfdcf2", "#eef4ff", "#ffffff"], height: 14, size: 1, chips: 4, seed: 7 },
};

// a lumpy star outline: radius at angle a
const lump = (r, a, ph) => r * (1 + 0.22 * Math.max(0, Math.sin(4 * a + ph)) + 0.1 * Math.sin(7 * a + ph * 1.7));

export function draw(p, t, o) {
  const C = fit(o.palette, 7), R = new Rng(o.seed), S = o.size, X = 0, Y = -o.height * S, PH = R.range(0, TAU);
  // the same random draws every frame, so the effect is a pure function of t
  const wedges = Array.from({ length: 6 }, (_, i) => ({ a: (i / 6) * TAU + R.range(-0.25, 0.25), len: R.range(7, 12) * S, w: R.range(3.4, 4.6) * S, t0: R.range(0, 0.02), out: R.range(0.04, 0.055), hold: 0.05, back: R.range(0.1, 0.13) }));
  const chips = Array.from({ length: o.chips }, () => {
    const a = -Math.PI / 2 + R.range(-1.25, 1.25);
    return { vx: Math.cos(a) * R.range(28, 55) * S, vy: Math.sin(a) * R.range(55, 95) * S, t0: R.range(0.02, 0.05), life: R.range(0.3, 0.4), big: R.chance(0.45) };
  });
  const puffs = Array.from({ length: 6 }, (_, i) => ({ a: (i / 6) * TAU + R.range(-0.3, 0.3), v: R.range(14, 26) * S, r: R.range(1.8, 3.2) * S, life: R.range(0.16, 0.3) }));

  // thin puffs first, so everything solid sits on top of them
  for (const q of puffs) {
    if (t > q.life) continue;
    const k = t / q.life, d = 6 * S + q.v * ease.outCubic(k) * 0.3;
    const x = X + Math.cos(q.a) * d, y = Y + Math.sin(q.a) * d * 0.6, r = q.r * (0.8 + 0.6 * k);
    p.disc(x, y, r, mix(C[3], C[5], 0.4), (dd) => 0.5 * (1 - k) * (1 - dd * dd * 0.7));
  }

  // chips: little two-tone squares with an inked corner, arcing off, bouncing once on the floor, dithering out
  for (const c of chips) {
    const u = t - c.t0;
    if (u < 0 || u > c.life) continue;
    let x = X + c.vx * u, y = Y + c.vy * u + 0.5 * 420 * u * u;
    if (y > -1) { // bounce on the floor at y = 0
      const tHit = (-c.vy - Math.sqrt(Math.max(0, c.vy * c.vy - 2 * 420 * (Y + 1)))) / 420;
      const v2 = -(c.vy + 420 * tHit) * 0.3, u2 = u - tHit;
      x = X + c.vx * tHit + c.vx * 0.5 * u2;
      y = Math.min(-1, -1 + v2 * u2 + 0.5 * 420 * u2 * u2);
    }
    const fade = clamp((c.life - u) / 0.12);
    const n = c.big ? 3 : 2;
    // an inked square behind, then the two-tone chip on it: the ink is what reads on sand
    for (let dy = -1; dy <= n; dy++)
      for (let dx = -1; dx <= n; dx++) p.dot(x + dx, y + dy, C[0], fade);
    for (let dy = 0; dy < n; dy++)
      for (let dx = 0; dx < n; dx++) p.dot(x + dx, y + dy, dx + dy === 0 ? WHITE : dx + dy < n ? C[5] : C[4], fade);
  }

  // inked wedges jab out from the star and snap back
  for (const w of wedges) {
    const u = t - w.t0;
    if (u < 0 || u > w.out + w.hold + w.back) continue;
    const s = u < w.out ? ease.outCubic(u / w.out) : u < w.out + w.hold ? 1 : 1 - ease.inQuad((u - w.out - w.hold) / w.back), L = w.len * s;
    if (L < 1.5) continue;
    const ca = Math.cos(w.a), sa = Math.sin(w.a), r0 = 6.5 * S + 1.5 * s, hw = (w.w * (0.6 + 0.4 * s)) / 2;
    const tip = [X + ca * r0, Y + sa * r0], end = [X + ca * (r0 + L), Y + sa * (r0 + L)], n = [-sa * hw, ca * hw];
    const wedge = (g) => [[tip[0] - ca * g, tip[1] - sa * g], [end[0] + n[0] * (1 + g * 0.5) + ca * g, end[1] + n[1] * (1 + g * 0.5) + sa * g], [end[0] - n[0] * (1 + g * 0.5) + ca * g, end[1] - n[1] * (1 + g * 0.5) + sa * g]];
    p.poly(wedge(1.6), C[0]);
    p.poly(wedge(0), u < w.out ? WHITE : u < w.out + w.hold ? C[5] : C[4]);
  }

  // the star: white-hot, then cream and bigger, then shrinking into its rim colour, inked all round
  if (t < 0.15) {
    const f = t < 0.05 ? 0 : t < 0.1 ? 1 : 2, R0 = [6, 7.5, 5][f] * S;
    p.shade(X - R0 * 1.5 - 2, Y - R0 * 1.5 - 2, X + R0 * 1.5 + 2, Y + R0 * 1.5 + 2, (x, y) => {
      const dx = x - X, dy = (y - Y) * 1.1, d = Math.hypot(dx, dy), ro = lump(R0, Math.atan2(dy, dx), PH + f * 0.7);
      if (d > ro + 1.6) return;
      if (d > ro) return C[0];
      const q = d / ro;
      return f === 0 ? (q < 0.6 ? WHITE : C[5]) : f === 1 ? (q < 0.35 ? WHITE : q < 0.75 ? C[5] : C[4]) : q < 0.5 ? C[5] : C[3];
    });
  }

  // a small warm pop of light: a garnish over dark ground, not what carries the hit
  const pop = Math.exp(-t / 0.05);
  if (pop > 0.03) p.glow(X, Y, 16 * S, C[4], 0.3 * pop);
}
