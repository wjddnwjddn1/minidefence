// Project effect, kept outside the vendored library. The moment a ranged troop lets go: an inked burst that
// points forward, so the player sees who fired and which way. Faces right, origin at the hand.
// Render with `node Tools/PixelFX/render-attacks.mjs`.
import { Rng, fit, clamp, ease, WHITE } from "../../../../ThirdParty/PixelFX/src/core/index.js";

export const meta = {
  name: "Ember Bastion launch",
  desc: "A release flash for ranged troops: an inked star at the hand and inked wedges fanning forward, gone in a quarter second.",
  tags: ["projectile", "launch", "muzzle", "game", "readable", "inked", "one-shot"],
  size: [84, 60],
  origin: [24, 30],
  duration: 0.25,
  loop: false,
  fps: 20,
  bg: "ground",
  // ink, deep, mid, light, core
  options: { palette: ["#1d1012", "#a8321c", "#e8662a", "#ffc46a", "#ffffff"], spread: 0.55, wedges: 3, seed: 2 },
};

export function draw(p, t, o) {
  const C = fit(o.palette, 5), R = new Rng(o.seed), n = Math.max(1, o.wedges | 0);
  // wedges jab forward and snap back
  for (let i = 0; i < n; i++) {
    const a = (n === 1 ? 0 : (i / (n - 1) - 0.5) * 2 * o.spread) + R.range(-0.06, 0.06), reach = (i === (n >> 1) ? 34 : 24) * R.range(0.9, 1.1);
    const s = t < 0.06 ? ease.outCubic(t / 0.06) : 1 - ease.inQuad(clamp((t - 0.1) / 0.14)), L = reach * s;
    if (L < 2) continue;
    const ca = Math.cos(a), sa = Math.sin(a), r0 = 8, hw = 4.2 * (0.6 + 0.4 * s);
    const bx = ca * r0, by = sa * r0, ex = ca * (r0 + L), ey = sa * (r0 + L), nx = -sa * hw, ny = ca * hw;
    p.poly([[bx - ca * 1.6, by - sa * 1.6], [ex + nx * 1.5 + ca * 1.6, ey + ny * 1.5 + sa * 1.6], [ex - nx * 1.5 + ca * 1.6, ey - ny * 1.5 + sa * 1.6]], C[0]);
    p.poly([[bx, by], [ex + nx, ey + ny], [ex - nx, ey - ny]], t < 0.06 ? C[4] : t < 0.14 ? C[3] : C[2]);
  }
  // the star at the hand
  if (t < 0.15) {
    const r = t < 0.05 ? 7 : t < 0.1 ? 8.5 : 5.5;
    p.shade(-r - 3, -r - 3, r + 3, r + 3, (x, y) => { const d = Math.abs(x) + Math.abs(y) * 1.05; return d > r + 1.7 ? undefined : d > r ? C[0] : d < r * 0.45 ? WHITE : t < 0.1 ? C[3] : C[2]; });
  }
  if (t < 0.08) p.glow(6, 0, 18, C[3], 0.3 * (1 - t / 0.08));
}
