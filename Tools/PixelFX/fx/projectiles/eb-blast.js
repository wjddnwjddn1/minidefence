// Ember Bastion skill effect (r45): the vendored explosions/small-explosion without its actor, hurried to fit one skill.
import * as lib from "../../../../ThirdParty/PixelFX/src/fx/explosions/small-explosion.js";
import { hurry } from "../_wrap.js";
const fx = hurry(lib, { name: "Ember Bastion big blast", desc: "A white flash and a shock ring, fire that cools into rolling smoke.", rate: 2.3, duration: 0.7, fade: 0.25 });
export const meta = fx.meta, create = fx.create;
