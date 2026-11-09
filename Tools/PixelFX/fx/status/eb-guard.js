// Project effect, kept outside the vendored library. The boss's "protective roar" status (Enemy.Shield > 0, damage
// halved) for small enemies on bright ground: an inked amber warding ring turns at the feet and two inked shield
// marks bob beside the body. Nothing is drawn over the head or hands. Render with `node Tools/PixelFX/render-candidates.mjs`.
import { fit, TAU } from "../../../../ThirdParty/PixelFX/src/core/index.js";

export const meta = {
  name: "Ember Bastion guard",
  desc: "Protected status that never blinks out: a dashed amber warding ring turns at the feet, two inked shield marks bob beside the body.",
  tags: ["status", "shield", "guard", "buff", "protect", "game", "readable", "inked", "loop"],
  size: [72, 42],
  origin: [36, 30],
  duration: 0.8,
  loop: true,
  fps: 20,
  bg: "ground",
  // bright to dark, then the ink
  options: { palette: ["#fff7d6", "#ffe48a", "#ffc94a", "#f5a32a", "#d97a1a", "#a8501a", "#6a2e12", "#1e0c06"], seed: 2 },
};

// a heater shield, 9 wide and 10 tall, centred on (cx, cy), with an ink rim, a lit left half and a centre ridge
const SHIELD = ["#########", "#########", "#########", "#########", ".#######.", ".#######.", "..#####..", "...###...", "....#....", "........."];
const SW = 9, MID = 4;
function shield(p, cx, cy, F) {
  const at = (x, y) => y >= 0 && y < SHIELD.length && x >= 0 && x < SW && SHIELD[y][x] === "#";
  for (let y = -1; y <= SHIELD.length; y++) for (let x = -1; x <= SW; x++) {
    if (at(x, y)) p.px(cx - MID + x, cy - 5 + y, x === MID ? F[0] : y === 0 ? F[1] : x < MID ? F[2] : F[4]);
    else if (at(x - 1, y) || at(x + 1, y) || at(x, y - 1) || at(x, y + 1)) p.px(cx - MID + x, cy - 5 + y, F[7]);
  }
}

export function draw(p, t, o) {
  const F = fit(o.palette, 8), phase = t / meta.duration;
  // the warding ring: an ellipse split into 8 dashes that turn a full dash per loop, so it wraps seamlessly
  // stamped along the curve, ink first, so every dash is a solid 2 px band with a 1 px rim
  const RX = 25, RY = 7, DASH = 8, N = 240;
  const dashes = [];
  for (let i = 0; i < N; i++) {
    const a = i / N;
    if ((((a + phase / DASH) % 1) * DASH) % 1 >= 0.6) continue;
    dashes.push([Math.round(Math.cos(a * TAU) * RX), Math.round(Math.sin(a * TAU) * RY)]);
  }
  for (const [x, y] of dashes) for (let dy = -1; dy <= 2; dy++) for (let dx = -1; dx <= 2; dx++) p.px(x + dx, y + dy, F[7]);
  for (const [x, y] of dashes) for (let dy = 0; dy <= 1; dy++) for (let dx = 0; dx <= 1; dx++) p.px(x + dx, y + dy, y < 0 ? F[2 + dy] : F[3 + dy]);
  // two shield marks beside the body, bobbing out of step
  const bob = (k) => Math.round(Math.sin((phase + k) * TAU) * 1.5);
  // low and wide, outside the body's outline, so they never sit on a hand or a weapon
  shield(p, -25, -11 + bob(0), F);
  shield(p, 25, -11 + bob(0.5), F);
}
