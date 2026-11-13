// Ember Bastion release glint (r45): the vendored pickups/collect-burst, played in 0.45 s. Origin at the centre of the burst.
import * as lib from "../../../../ThirdParty/PixelFX/src/fx/pickups/collect-burst.js";
import { hurry } from "../_wrap.js";
const fx = hurry(lib, { name: "Ember Bastion release glint", desc: "A white flash and a cross of light, a thin ring snapping out, twinkles thrown wide.", rate: 1.8, duration: 0.45, fps: 30, fade: 0.15, origin: [56, 64] });
export const meta = fx.meta, create = fx.create;
