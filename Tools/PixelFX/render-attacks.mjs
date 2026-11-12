// Pinned PixelFX 0.3.0; actor-free attack sheets, MIT. No gallery fonts are exported.
import {mkdirSync,writeFileSync} from 'node:fs';
import {loadEffect,renderFrames,spriteSheet,contactSheet} from '../../ThirdParty/PixelFX/tools/lib/fxlib.mjs';
const entries=[['SwordSlash','melee/sword-slash',{}],['SwordSweep','melee/spin-slash',{palette:['#261333','#482458','#82439c','#b776d0','#e0a9ef','#f5ddff','#ffffff']}],['SwordBreaker','melee/heavy-cleave',{}]];
mkdirSync('Docs/pixelfx-attack-effects',{recursive:true});
for(const [name,id,options] of entries){const fx=await loadEffect(id),r=renderFrames(fx,{actor:false,options}),sheet=spriteSheet(fx,r,name);writeFileSync('Assets/Resources/PixelFX/'+name+'.png',sheet.png);writeFileSync('Assets/Resources/PixelFX/'+name+'.json',JSON.stringify(sheet.json,null,1));writeFileSync('Docs/pixelfx-attack-effects/'+name+'-contact.png',contactSheet(fx,r,{max:12}));console.log(name,r.n,r.w,r.h);}
