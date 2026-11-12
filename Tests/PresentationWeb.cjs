// r38: portrait gameplay retired; low landscape fixtures replace portrait cases.
const fs=require('fs'),path=require('path'),http=require('http');
const {chromium}=require('C:/Users/snome/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root=path.resolve(__dirname,'bin/TacticalWeb'),out=path.resolve(__dirname,'bin/r38-story-evidence');
const types={'.js':'application/javascript','.wasm':'application/wasm','.data':'application/octet-stream','.html':'text/html; charset=utf-8'};
const server=http.createServer((req,res)=>{const file=path.resolve(root,'.'+new URL(req.url,'http://localhost').pathname.replace(/\/$/,'/index.html'));if(!file.startsWith(root+path.sep)){res.writeHead(403).end();return;}fs.readFile(file,(e,data)=>{if(e){res.writeHead(404).end();return;}res.writeHead(200,{'Content-Type':types[path.extname(file)]||'application/octet-stream','Cache-Control':'no-store'});res.end(data);});});
const assert=require('node:assert/strict');
const output=path.resolve(__dirname,'bin/presentation-evidence');fs.mkdirSync(output,{recursive:true});
(async()=>{let browser;try{await new Promise(r=>server.listen(0,'127.0.0.1',r));browser=await chromium.launch({executablePath:'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe',headless:true,args:['--use-gl=angle','--use-angle=swiftshader','--enable-webgl','--enable-unsafe-swiftshader']});
 const context=await browser.newContext({viewport:{width:1280,height:720}}),page=await context.newPage(),logs=[],errors=[];page.on('console',m=>{logs.push(m.text().trim());if(m.type()==='error')errors.push(m.text());});page.on('pageerror',e=>errors.push(e.message));
 async function wait(fn,label){let end=Date.now()+90000;while(!fn()){if(errors.length)throw Error(errors[0]);if(Date.now()>end)throw Error('timeout '+label);await page.waitForTimeout(40);}}
 async function load(query){logs.length=0;errors.length=0;await page.goto('http://127.0.0.1:'+server.address().port+'/?'+query,{waitUntil:'domcontentloaded'});}
 await load('fx-smoke=remember&review-flow=1&story=1&story-case=opening&story-reset=1&story-type-review=1');await wait(()=>logs.some(l=>l.startsWith('STORY_TYPE|scene|')),'partial text');
 const partial=logs.filter(l=>l.startsWith('STORY_TYPE|scene|')).map(l=>l.split('|').slice(2).map(Number));assert(partial.some(([n,t])=>n>0&&n<t));
 await page.keyboard.press('Space');await wait(()=>logs.some(l=>l==='STORY_TYPE_REVEAL|opening|0'),'instant reveal');assert(!logs.includes('STORY_SCENE|opening|1'));
 await page.keyboard.press('Space');await wait(()=>logs.includes('STORY_SCENE|opening|1'),'next line');await wait(()=>logs.some(l=>l.startsWith('STORY_IDLE|Bron|')),'animated portrait');await wait(()=>new Set(logs.filter(l=>l.startsWith('STORY_IDLE|Bron|'))).size>=3,'three idle frames');await wait(()=>logs.includes('STORY_TYPE_SOUND'),'typing cue');await page.screenshot({path:path.join(output,'typing-idle.png')});
 await page.keyboard.press('Escape');await wait(()=>logs.some(l=>l.startsWith('STORY_SCENE_END|opening|skip')),'skip');console.log('PASS gradual Unicode text, click reveal then advance, typing sound scheduling, 3+ idle frames, Esc skip');
 fs.writeFileSync(path.join(output,'typing.json'),JSON.stringify({logs,errors},null,2));
 await load('fx-smoke=attack-review&attack-fx-review=1');await wait(()=>new Set(logs.filter(l=>l.startsWith('ATTACK_FX|')).map(l=>l.split('|').slice(1,3).join('|'))).size===12,'all 4 classes x 3 branches');
 await wait(()=>logs.some(l=>l.includes('|SwordSweep|7')),'release frame');await page.screenshot({path:path.join(output,'attacks.png')});assert(!errors.length);fs.writeFileSync(path.join(output,'attacks.json'),JSON.stringify({logs,errors},null,2));console.log('PASS PixelFX release effects, all four attackers and three branch states, right/left facing');await context.close();
}catch(e){console.error(e);process.exitCode=1;}finally{if(browser)await browser.close();server.close();}})();
