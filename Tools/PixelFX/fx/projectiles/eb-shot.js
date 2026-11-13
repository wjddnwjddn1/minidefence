// Ember Bastion powder shot (r45): the vendored projectiles/muzzle-pistol, its first three quarters of a second played in 0.5 s and faded out. Origin at the muzzle, faces right.
import * as lib from "../../../../ThirdParty/PixelFX/src/fx/projectiles/muzzle-pistol.js";
import { hurry } from "../_wrap.js";
const fx = hurry(lib, { name: "Ember Bastion powder shot", desc: "A spiky white-cored flash, sparks and a curl of smoke.", rate: 1.5, duration: 0.5, fade: 0.2, origin: [44, 40] });
export const meta = fx.meta, create = fx.create;
