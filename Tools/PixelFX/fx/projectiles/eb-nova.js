// Ember Bastion skill effect (r45): the vendored ice/frost-nova without its actor, hurried to fit one skill.
import * as lib from "../../../../ThirdParty/PixelFX/src/fx/ice/frost-nova.js";
import { hurry } from "../_wrap.js";
const fx = hurry(lib, { name: "Ember Bastion frost nova", desc: "A white flash, then rings of ice crystals bloom over the floor and fade.", skip: 0.4, rate: 2.5, duration: 0.8, fade: 0.25 });
export const meta = fx.meta, create = fx.create;
