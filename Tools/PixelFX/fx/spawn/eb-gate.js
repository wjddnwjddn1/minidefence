// Ember Bastion enemy gate (r45): where a wave comes out. The vendored arcane/portal, unchanged in shape,
// rendered at 12 fps so the looping sheet stays small. BattleView tints it red while a group is about to spawn.
import * as portal from "../../../../ThirdParty/PixelFX/src/fx/arcane/portal.js";

export const meta = {
  ...portal.meta,
  name: "Ember Bastion enemy gate",
  desc: "A dark rift standing on the road where the goblins come out.",
  fps: 12,
};
export const drawBack = portal.drawBack;
export const draw = portal.draw;
