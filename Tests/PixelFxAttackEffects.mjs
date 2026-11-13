import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import {fileURLToPath} from 'node:url';
import {loadEffect,renderFrames,spriteSheet} from '../ThirdParty/PixelFX/tools/lib/fxlib.mjs';
import {entries,fxDir} from '../Tools/PixelFX/attack-effects.mjs';
for(const [name,id,options] of entries){const fx=await loadEffect(id,fileURLToPath(fxDir)),r=renderFrames(fx,{actor:false,options}),s=spriteSheet(fx,r,name);assert.deepEqual(readFileSync('Assets/Resources/PixelFX/'+name+'.png'),Buffer.from(s.png));assert.equal(readFileSync('Assets/Resources/PixelFX/'+name+'.json','utf8').replace(/\r\n/g,'\n'),JSON.stringify(s.json,null,1));const opaque=f=>f.clear.reduce((n,v,i)=>n+(i%4===3&&v>8?1:0),0);
 if(name==='RoundBomb'){
  assert(fx.meta.loop&&r.frames.every(frame=>opaque(frame)>200),name+' body must remain visible on every frame');
  assert(new Set(r.frames.map(frame=>Buffer.from(frame.clear).toString('base64'))).size>=3,name+' fuse must flicker');
  console.log('PASS '+name+' deterministic loop and flickering fuse');
  continue;
 }
 // a sword cut must be bold (hundreds of solid pixels at its peak), short enough to play whole in 0.4 s, and end empty
 assert(Math.max(...r.frames.map(opaque))>(name.startsWith('Trail')?90:name.startsWith('Launch')||name.startsWith('SwordRift')?250:600),name+' too thin: '+Math.max(...r.frames.map(opaque)));assert(r.n<=18,name+' too many frames: '+r.n);assert(opaque(r.frames[r.n-1])<12,name+' must end empty');console.log('PASS '+name+' deterministic actor-free PNG+JSON, bold release '+Math.max(...r.frames.map(opaque))+' px, '+r.n+' frames, empty ending');}
