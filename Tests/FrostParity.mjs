import fs from 'node:fs';
import {pathToFileURL,fileURLToPath} from 'node:url';
import {execFileSync} from 'node:child_process';
const root='C:/퍼블리싱/htdocs/static/ai/ember-bastion/v2-campaign/rules/';
const {createFieldEngine}=await import(pathToFileURL(root+'sim.mjs'));
const {fieldById}=await import(pathToFileURL(root+'fields.mjs'));
export function compare({mode,field='f1',period=60,frostOnly=false,strong=false,aggregate=false,report}){
 const engine=createFieldEngine(fieldById(field)),expected=[],waves=fieldById(field).waves.length;
 function command(state,cmd){const result=engine.command(state,cmd);if(!result.ok)throw new Error('Fixture command failed: '+JSON.stringify({cmd,result}));}
 for(let wave=1;wave<=waves;wave++)for(let branch=0;branch<3;branch++){
  const state=engine.createRun(42);state.wave=wave-1;state.gold=5000;
  for(let slot=0;slot<4;slot++){command(state,{type:'place',troop:frostOnly?'frost':['sword','archer','bomb','frost'][slot],slot});if(branch){command(state,{type:'upgrade',slot});command(state,{type:'upgrade',slot,branch:branch===1?'a':'b'});if(!frostOnly&&(slot===1||slot===3))command(state,{type:'priority',slot,mode:strong?'strong':'fast'});}}
  command(state,{type:'startWave'});
  for(let n=1;n<=7200;n++){engine.step(state,1/60);if(n%period)continue;const e=state.enemies[0];const row=[wave,branch,n,{prep:'Preparation',fight:'Fighting',won:'Won',lost:'Lost'}[state.phase],state.gold,state.hp,state.kills,state.enemies.length,state.queue.length,state.shots.length,e?.traveled??'-',e?.hp??'-',e?.slowT??'-',e?.freezeT??'-'];
   if(aggregate)for(const key of ['hp','traveled','slowT','freezeT'])row.push(state.enemies.reduce((sum,enemy)=>sum+enemy[key],0));expected.push(row);
  }
 }
 const exe=fileURLToPath(new URL('./bin/CoreChecks.exe',import.meta.url));
 const actual=execFileSync(exe,[mode],{encoding:'utf8'}).trim().split(/\r?\n/).map(line=>line.split(',')),differences=[];
 if(actual.length!==expected.length)throw new Error('checkpoint count mismatch: '+actual.length+' / '+expected.length);
 expected.forEach((row,i)=>{if(actual[i].length!==row.length)throw new Error('field count mismatch at '+i);row.forEach((value,j)=>{const a=actual[i]?.[j];if(j>=10&&value!=='-'){if(!Number.isFinite(value)||a===undefined||a.trim()===''||!Number.isFinite(Number(a))||Math.abs(Number(a)-value)>1e-7)differences.push({i,j,actual:a,expected:value});}else if(String(value)!==a)differences.push({i,j,actual:a,expected:value});});});
 fs.writeFileSync(new URL('../Docs/'+report,import.meta.url),JSON.stringify({scenario:`Field ${field}; ${waves} waves; isolated test budget 5000; four ${frostOnly?'frost':'mixed'} units; three branches`,checkpoints:expected.length,differences,expected,actual},null,2));
 if(differences.length)throw new Error(JSON.stringify(differences.slice(0,20)));
 console.log('PASS '+mode+' parity: '+expected.length+' checkpoints');
}
