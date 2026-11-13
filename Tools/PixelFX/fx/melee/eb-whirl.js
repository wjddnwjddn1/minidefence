// Ember Bastion sweeping cut (r45): the vendored melee/spin-slash without its actor, played in 0.5 s.
import * as lib from "../../../../ThirdParty/PixelFX/src/fx/melee/spin-slash.js";
import { hurry } from "../_wrap.js";
const fx = hurry(lib, { name: "Ember Bastion sweeping cut", desc: "The blade whirls once round the waist; the closed ring flares and dissolves.", rate: 1.6, duration: 0.5 });
export const meta = fx.meta, create = fx.create;
