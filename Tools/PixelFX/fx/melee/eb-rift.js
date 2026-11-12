// Project effect, kept outside the vendored library. A sword cut that travels: several inked streaks race along the
// ground away from the swordsman and fan out, each a jagged tapering line with its tail catching up.
// Faces right, origin at the swordsman's feet; the game rotates it towards the target.
// Render with `node Tools/PixelFX/render-attacks.mjs`.
import { Rng, fit, clamp, ease, WHITE } from "../../../../ThirdParty/PixelFX/src/core/index.js";

export const meta = {
  name: "Ember Bastion ground rift",
  desc: "Five inked streaks tear along the ground and fan out from a sword cut; chips kick up where they pass.",
  tags: ["melee", "wave", "ground", "sword", "game", "readable", "inked", "one-shot"],
  size: [156, 200],
  origin: [12, 100],
  duration: 0.35,
  loop: false,
  fps: 20,
  bg: "ground",
  // ink, deep, mid, light (kept near white so the game can tint it per branch)
  options: { palette: ["#141826", "#7f8fb0", "#cfdcf2", "#ffffff"], streaks: 5, spread: 0.3, seed: 12 },
};

export function draw(p, t, o) {
  const C = fit(o.palette, 4), R = new Rng(o.seed), n = Math.max(1, o.streaks | 0);
  const S = Array.from({ length: n }, (_, i) => {
    const a = (n === 1 ? 0 : (i / (n - 1) - 0.5) * 2 * o.spread) + R.range(-0.04, 0.04), mid = 1 - Math.abs(i - (n - 1) / 2) / ((n - 1) / 2 || 1);
    return { ca: Math.cos(a), sa: Math.sin(a), len: 96 + 36 * mid + R.range(-6, 6), t0: R.range(0, 0.04), w: 3.2 + 1.8 * mid, ph: R.range(0, 14), amp: R.range(1.8, 3) };
  });
  const live = S.map((s) => {
    const u = t - s.t0;
    return { s, head: s.len * ease.outCubic(clamp(u / 0.2)), tail: s.len * ease.inQuad(clamp((u - 0.08) / 0.21)) };
  }).filter((x) => x.head - x.tail > 2);
  if (!live.length) return;
  p.shade(-10, -98, 142, 98, (x, y) => {
    let best;
    for (const { s, head, tail } of live) {
      const lx = x * s.ca + y * s.sa;
      if (lx < tail - 2 || lx > head + 2) continue;
      // a zigzag centre line, thick at the racing head and thin at the tail
      const zig = (Math.abs(((lx + s.ph) % 14) - 7) - 3.5) * (s.amp / 3.5), ly = -x * s.sa + y * s.ca - zig;
      const k = clamp((lx - tail) / Math.max(1, head - tail)), w = s.w * (0.25 + 0.75 * k) * (lx < 10 ? lx / 10 : 1), d = Math.abs(ly);
      if (lx < tail || lx > head) { if (d <= w) best = best ?? C[0]; continue; }
      if (d > w + 1.6) continue;
      const c = d > w ? C[0] : k > 0.8 && d < w * 0.6 ? WHITE : d < w * 0.45 ? C[3] : k > 0.35 ? C[2] : C[1];
      if (c !== C[0] || best === undefined) best = c;
    }
    return best;
  });
  // chips kicked up at each head
  for (const { s, head } of live) {
    if (t > 0.24) continue;
    const hx = s.ca * head, hy = s.sa * head, up = 5 + 6 * Math.sin(t * 40 + s.ph);
    for (let dy = -1; dy <= 2; dy++) for (let dx = -1; dx <= 2; dx++) p.dot(hx + 3 + dx, hy - up + dy, C[0]);
    for (let dy = 0; dy < 2; dy++) for (let dx = 0; dx < 2; dx++) p.dot(hx + 3 + dx, hy - up + dy, dx + dy === 0 ? WHITE : C[2]);
  }
}
