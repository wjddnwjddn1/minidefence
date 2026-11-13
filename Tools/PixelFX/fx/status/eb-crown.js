// Ember Bastion goblin king's crown (r47): the vendored holy/halo in ember colours, at 15 fps so the looping sheet
// stays small. BattleView draws only the ring's rows of each frame (the head light below it needs an actor).
import * as halo from "../../../../ThirdParty/PixelFX/src/fx/holy/halo.js";

export const meta = {
  ...halo.meta,
  name: "Ember Bastion king's crown",
  desc: "A ring of ember light floating over the goblin king's head, a glint running round it.",
  fps: 15,
  actor: undefined,
  options: { ...halo.meta.options, palette: ["#2a0a08", "#5a1410", "#a8301a", "#e0561e", "#ff8a2a", "#ffc46a", "#ffe8b0", "#ffffff"] },
};
export const draw = halo.draw;
