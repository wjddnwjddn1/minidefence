import fs from 'node:fs';
import {pathToFileURL,fileURLToPath} from 'node:url';
import {execFileSync} from 'node:child_process';
const root='C:/퍼블리싱/htdocs/static/ai/ember-bastion/v2-campaign/rules/';
const {createFieldEngine}=await import(pathToFileURL(root+'sim.mjs'));const {fieldById}=await import(pathToFileURL(root+'fields.mjs'));const expected=[];
for(let field=0;field<3;field++){const def=fieldById('f'+(field+1)),engine=createFieldEngine(def);for(let branch=0;branch<3;branch++)for(let wave=1;wave<=def.waves.length;wave++){
 const state=engine.createRun(42);state.wave=wave-1;state.gold=5000;function cmd(c){const r=engine.command(state,c);if(!r.ok)throw Error(JSON.stringify({c,r}));}
 for(let slot=0;slot<4;slot++){cmd({type:'place',troop:['sword','smith','bomb','frost'][slot],slot});if(slot===1&&branch){cmd({type:'upgrade',slot});cmd({type:'upgrade',slot,branch:branch===1?'a':'b'});}}cmd({type:'startWave'});
 for(let n=1;n<=7200;n++){engine.step(state,1/60);if(n%600)continue;const e=state.enemies[0];expected.push([field,branch,wave,n,{prep:'Preparation',fight:'Fighting',won:'Won',lost:'Lost'}[state.phase],state.gold,state.hp,state.kills,state.enemies.length,state.queue.length,state.shots.length,e?.traveled??'-',e?.hp??'-']);}
}}
const actual=execFileSync(fileURLToPath(new URL('./bin/CoreChecks.exe',import.meta.url)),['smith-parity'],{encoding:'utf8'}).trim().split(/\r?\n/).map(x=>x.split(',')),differences=[];
if(actual.length!==expected.length)throw Error('checkpoint count mismatch');expected.forEach((row,i)=>{if(row.length!==actual[i].length)throw Error('field count mismatch');row.forEach((v,j)=>{let a=actual[i][j];if(j>=11&&v!=='-'){if(!Number.isFinite(v)||!Number.isFinite(Number(a))||Math.abs(Number(a)-v)>1e-7)differences.push({i,j,a,v});}else if(String(v)!==a)differences.push({i,j,a,v});});});
fs.writeFileSync(new URL('../Docs/web-smith-parity.json',import.meta.url),JSON.stringify({scenario:'three fields, all waves and smith branches; isolated fixture budget 5000',checkpoints:expected.length,differences,expected,actual},null,2));if(differences.length)throw Error(JSON.stringify(differences.slice(0,15)));console.log('PASS smith parity: '+expected.length+' checkpoints');
