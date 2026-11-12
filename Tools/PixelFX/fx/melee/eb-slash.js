// Project effect, kept outside the vendored library. Sword attacks that read on bright grass and sand:
// a thick inked crescent instead of a thin glowing arc. Three shapes by `mode` for the three sword branches:
//   0 slash  - one crescent cut in front of the swordsman
//   1 sweep  - a flat ring cut all the way round him (multi-target branch)
//   2 smash  - a short overhead chop that lands in an inked ground burst (armour-breaking branch)
// Faces right, origin at the swordsman's centre. Render with `node Tools/PixelFX/render-attacks.mjs`.
import { Rng, fit, clamp, ease, TAU, WHITE } from "../../../../ThirdParty/PixelFX/src/core/index.js";

export const meta = {
  name: "Ember Bastion sword cut",
  desc: "A thick inked crescent sweeps and its tail catches up; chips fly off the leading edge. Modes: slash, sweep, smash.",
  tags: ["melee", "slash", "sword", "game", "readable", "inked", "one-shot"],
  size: [152, 150],
  origin: [64, 86],
  duration: 0.35,
  loop: false,
  fps: 20,
  bg: "ground",
  // ink, deep, mid, light, core
  options: { palette: ["#141826", "#3a5a9c", "#7fb4f0", "#d6ecff", "#ffffff"], mode: 0, seed: 4 },
};

// A crescent on an ellipse: between angles tail..lead, thickest at the leading edge, inked on both rims.
function crescent(p, cx, cy, R, thick, tail, lead, squash, C) {
  const span = lead - tail;
  if (span <= 0.02) return;
  p.shade(cx - R - 4, cy - R * squash - 4, cx + R + 4, cy + R * squash + 4, (x, y) => {
    const dx = x - cx, dy = (y - cy) / squash, r = Math.hypot(dx, dy);
    if (r > R + 2 || r < R - thick - 2) return;
    let a = Math.atan2(dy, dx);
    while (a < tail) a += TAU;
    while (a - TAU >= tail) a -= TAU;
    if (a > lead) return;
    const u = (a - tail) / span, w = thick * (0.18 + 0.82 * Math.pow(u, 0.8)), inner = R - w;
    if (r > R + 1.7 || r < inner - 1.7) return;
    if (r > R || r < inner) return C[0];
    const q = (r - inner) / w;
    return u > 0.82 && q > 0.3 ? C[4] : q > 0.62 ? C[3] : q > 0.28 ? C[2] : C[1];
  });
}

function chips(p, R, x, y, t, t0, n, C, dir) {
  for (let i = 0; i < n; i++) {
    const a = dir + R.range(-0.9, 0.9), v = R.range(60, 130), life = R.range(0.14, 0.24), u = t - t0;
    if (u < 0 || u > life) continue;
    const px = x + Math.cos(a) * v * u, py = y + Math.sin(a) * v * u + 160 * u * u, big = i % 2 === 0 ? 3 : 2, fade = clamp((life - u) / 0.08);
    for (let dy = -1; dy <= big; dy++) for (let dx = -1; dx <= big; dx++) p.dot(px + dx, py + dy, C[0], fade);
    for (let dy = 0; dy < big; dy++) for (let dx = 0; dx < big; dx++) p.dot(px + dx, py + dy, dx + dy === 0 ? WHITE : C[3], fade);
  }
}

export function draw(p, t, o) {
  const C = fit(o.palette, 5), R = new Rng(o.seed), mode = o.mode | 0;
  if (mode === 1) {
    // sweep: a flat ring round the body, drawn back half first so it seems to pass behind and then in front
    const a0 = -Math.PI * 0.75, turn = TAU * 1.08, lead = a0 + turn * ease.outCubic(clamp(t / 0.2)), tail = a0 + turn * ease.inOutQuad(clamp((t - 0.07) / 0.24));
    crescent(p, 0, 4, 58, 15, Math.max(tail, lead - TAU * 0.62), lead, 0.42, C);
    chips(p, R, Math.cos(lead) * 58, 4 + Math.sin(lead) * 58 * 0.42, t, 0.05, 6, C, lead + Math.PI / 2);
    if (t < 0.1) p.glow(0, 4, 40, C[3], 0.25 * (1 - t / 0.1));
    return;
  }
  if (mode === 2) {
    // smash: a fast overhead chop, then the blade lands and the ground bursts
    const a0 = -Math.PI * 0.72, a1 = Math.PI * 0.3, lead = a0 + (a1 - a0) * ease.outQuad(clamp(t / 0.1)), tail = a0 + (a1 - a0) * ease.inQuad(clamp((t - 0.05) / 0.12));
    crescent(p, 4, 2, 50, 20, tail, lead, 1, C);
    const hx = 4 + Math.cos(a1) * 50, hy = 2 + Math.sin(a1) * 50, u = t - 0.09;
    if (u >= 0) {
      const k = clamp(u / 0.26), wedges = 7;
      for (let i = 0; i < wedges; i++) {
        const a = -Math.PI + (i / (wedges - 1)) * Math.PI + R.range(-0.12, 0.12), len = R.range(14, 26) * (u < 0.06 ? ease.outCubic(u / 0.06) : 1 - ease.inQuad(clamp((u - 0.1) / 0.16))), hw = R.range(3, 4.5);
        if (len < 2) continue;
        const ca = Math.cos(a), sa = Math.sin(a) * 0.7, bx = hx + ca * 7, by = hy + sa * 7, ex = hx + ca * (7 + len), ey = hy + sa * (7 + len), nx = -sa * hw, ny = ca * hw;
        p.poly([[bx - ca * 1.6, by - sa * 1.6], [ex + nx * 1.5 + ca * 1.6, ey + ny * 1.5 + sa * 1.6], [ex - nx * 1.5 + ca * 1.6, ey - ny * 1.5 + sa * 1.6]], C[0]);
        p.poly([[bx, by], [ex + nx, ey + ny], [ex - nx, ey - ny]], u < 0.06 ? C[4] : k < 0.5 ? C[3] : C[2]);
      }
      if (u < 0.12) p.shade(hx - 12, hy - 9, hx + 12, hy + 9, (x, y) => { const d = Math.hypot(x - hx, (y - hy) * 1.3), r = u < 0.05 ? 7 : 9 - u * 30; return d > r + 1.6 ? undefined : d > r ? C[0] : d < r * 0.5 ? C[4] : C[3]; });
      chips(p, R, hx, hy, t, 0.1, 7, C, -Math.PI / 2);
    }
    return;
  }
  // slash: one crescent from above the shoulder down across the front
  const a0 = -Math.PI * 0.62, a1 = Math.PI * 0.5, lead = a0 + (a1 - a0) * ease.outCubic(clamp(t / 0.1)), tail = a0 + (a1 - a0) * ease.inOutQuad(clamp((t - 0.04) / 0.16));
  crescent(p, 0, 0, 54, 20, tail, lead, 1, C);
  chips(p, R, Math.cos(lead) * 54, Math.sin(lead) * 54, t, 0.06, 5, C, lead + Math.PI / 2);
  if (t < 0.08) p.glow(30, 0, 30, C[3], 0.3 * (1 - t / 0.08));
}
