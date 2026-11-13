// Ember Bastion sword cut (r45): the vendored melee/sword-slash without its actor, played in 0.5 s.
import * as lib from "../../../../ThirdParty/PixelFX/src/fx/melee/sword-slash.js";
import { hurry } from "../_wrap.js";
const fx = hurry(lib, { name: "Ember Bastion sword cut", desc: "A tilted crescent whips round, cools as it trails off and throws sparks.", rate: 1.5, duration: 0.5 });
export const meta = fx.meta, create = fx.create;
