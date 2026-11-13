// Ember Bastion piercing arrow (r45): the vendored projectiles/bullet-tracer, unchanged. BattleView flies it down the
// whole line the arrow goes through and tints it gold.
import * as tracer from "../../../../ThirdParty/PixelFX/src/fx/projectiles/bullet-tracer.js";
export const meta = { ...tracer.meta, name: "Ember Bastion piercing arrow", desc: "A white-hot streak tearing along, its tracer burning behind it.", bg: "ground" };
export const draw = tracer.draw;
