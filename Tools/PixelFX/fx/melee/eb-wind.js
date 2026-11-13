// Ember Bastion sword wind (r45): the blade of air a sword cut throws along the ground. The vendored
// projectiles/energy-disc, unchanged; BattleView moves it from the swordsman towards the target and tints it by branch.
import * as disc from "../../../../ThirdParty/PixelFX/src/fx/projectiles/energy-disc.js";
export const meta = { ...disc.meta, name: "Ember Bastion sword wind", desc: "A spinning blade of air flying right, speed lines peeling off it.", bg: "ground" };
export const draw = disc.draw;
