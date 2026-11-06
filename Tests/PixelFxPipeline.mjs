import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { readFileSync } from 'node:fs';
import { resolve, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { allIds, loadEffect } from '../ThirdParty/PixelFX/tools/lib/fxlib.mjs';
const root=resolve(fileURLToPath(new URL('..',import.meta.url)));
const run=(script,args)=>spawnSync(process.execPath,[script,...args],{cwd:root,encoding:'utf8'});
const ids=allIds(); assert.equal(ids.length,244);
for(const id of ids){const {meta}=await loadEffect(id);assert.match(id,/^[a-z0-9-]+\/[a-z0-9-]+$/);assert.equal(meta.size.length,2);assert.equal(meta.origin.length,2);assert.ok(meta.origin.every((v,i)=>v>=0&&v<=meta.size[i]),id);}
const wrapper=join(root,'Tools/PixelFX/render.mjs');
assert.equal(run(wrapper,['moves/no-such-effect']).status,2);
assert.equal(run(wrapper,['impacts/hit-spark']).status,2);
const out=join(root,'Tests/bin/pixelfx-matrix');
for(const [id,loop,actor] of [['fire/torch',true,false],['moves/arrow-shot',false,true]]){
 const result=run(join(root,'ThirdParty/PixelFX/tools/render.mjs'),[id,'--sheet',...(actor?[]:['--no-actor']),'--fps','10','--out',out]);
 assert.equal(result.status,0,result.stderr);const name=id.split('/')[1],base=join(out,id,name);
 const png=readFileSync(base+'.png'),sheet=JSON.parse(readFileSync(base+'.json','utf8'));
 assert.equal(png.subarray(0,8).toString('hex'),'89504e470d0a1a0a');
 assert.equal(png.readUInt32BE(16),sheet.meta.size.w);assert.equal(png.readUInt32BE(20),sheet.meta.size.h);
 assert.equal(sheet.meta.pixelfx.id,id);assert.equal(sheet.meta.pixelfx.loop,loop);assert.equal(sheet.meta.pixelfx.fps,10);
 assert.ok(sheet.frames.length>1);assert.equal(sheet.frames.length,sheet.meta.pixelfx.frames);
 for(const {frame,duration} of sheet.frames){assert.ok(duration>0&&frame.x>=0&&frame.y>=0&&frame.x+frame.w<=sheet.meta.size.w&&frame.y+frame.h<=sheet.meta.size.h,id);}
}
for(const ext of ['png','json'])assert.deepEqual(readFileSync(join(root,'Assets/Resources/PixelFX/HitSpark.'+ext)),readFileSync(join(root,'Assets/PixelFX/Generated/impacts/hit-spark/hit-spark.'+ext)));
console.log(`PASS ${ids.length} catalog effects; loop and actor sheets, invalid IDs, overwrite protection, game copies`);
