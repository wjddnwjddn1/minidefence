// Renders the game's sword attack sheets from the project effect Tools/PixelFX/fx/melee/eb-slash.js (r41, Claude).
// The vendored PixelFX library only supplies the renderer. Usage: node Tools/PixelFX/render-attacks.mjs
import {mkdirSync,writeFileSync} from 'node:fs';
import {fileURLToPath} from 'node:url';
import {loadEffect,renderFrames,spriteSheet,contactSheet} from '../../ThirdParty/PixelFX/tools/lib/fxlib.mjs';
import {entries,fxDir} from './attack-effects.mjs';
mkdirSync('Docs/pixelfx-attack-effects',{recursive:true});
for(const [name,id,options] of entries){const fx=await loadEffect(id,fileURLToPath(fxDir)),r=renderFrames(fx,{actor:false,options}),sheet=spriteSheet(fx,r,name);writeFileSync('Assets/Resources/PixelFX/'+name+'.png',sheet.png);writeFileSync('Assets/Resources/PixelFX/'+name+'.json',JSON.stringify(sheet.json,null,1));writeFileSync('Docs/pixelfx-attack-effects/'+name+'-contact.png',contactSheet(fx,r,{max:12}));console.log(name,r.n,r.w,r.h);}
