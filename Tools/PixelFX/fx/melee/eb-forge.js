// Ember Bastion skill effect (r45): the vendored melee/hammer-smash without its actor, hurried to fit one skill.
import * as lib from "../../../../ThirdParty/PixelFX/src/fx/melee/hammer-smash.js";
import { hurry } from "../_wrap.js";
const fx = hurry(lib, { name: "Ember Bastion hammer signal", desc: "A hammer of light drops: a white slam with rays, a dome of force, sparks.", skip: 0.1, rate: 1.6, duration: 0.6, fade: 0.2 });
export const meta = fx.meta, create = fx.create;
