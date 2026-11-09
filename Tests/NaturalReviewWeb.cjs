const fs=require('fs'),path=require('path'),http=require('http');
const {chromium}=require('C:/Users/snome/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root=path.resolve(__dirname,'bin/TacticalWeb'),out=path.resolve(__dirname,'bin/natural-review-evidence');
const types={'.html':'text/html; charset=utf-8','.js':'application/javascript','.wasm':'application/wasm','.data':'application/octet-stream','.css':'text/css'};
const server=http.createServer((req,res)=>{const relative=decodeURIComponent(new URL(req.url,'http://127.0.0.1').pathname).replace(/^\/+/,''),file=path.resolve(root,relative||'index.html');if(!file.startsWith(root+path.sep)){res.writeHead(403).end();return;}fs.readFile(file,(error,data)=>{if(error){res.writeHead(404).end();return;}res.writeHead(200,{'Content-Type':types[path.extname(file)]||'application/octet-stream','Cache-Control':'no-store'});res.end(data);});});
(async()=>{let browser;try{
 fs.mkdirSync(out,{recursive:true});await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));
 browser=await chromium.launch({executablePath:'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe',headless:true,args:['--use-gl=angle','--use-angle=swiftshader','--enable-webgl','--enable-unsafe-swiftshader']});
 const context=await browser.newContext({viewport:{width:1280,height:720}}),page=await context.newPage(),messages=[],errors=[],actions=[];let state;
 page.on('console',m=>{const text=m.text().trim();messages.push(text);if(m.type()==='error')errors.push(text);if(text.startsWith('REVIEW_STATE|')){const p=text.split('|');state={rules:+p[1],phase:p[2],wave:+p[3],time:+p[4],gold:+p[5],enemies:+p[6],units:p[7]?p[7].split(';').map(v=>{const a=v.split(':').map(Number);return {slot:a[0],kind:a[1],level:a[2],branch:a[3]};}):[]};}});page.on('pageerror',e=>errors.push(e.message));
 await page.goto('http://127.0.0.1:'+server.address().port+'/?fx-smoke=tutorial&fx-smoke=autosave&review-flow=1',{waitUntil:'domcontentloaded'});
 await page.waitForFunction(()=>[...document.querySelectorAll('#unity-build-title button')].some(b=>b.textContent==='Unload'),null,{timeout:120000});
 await page.evaluate(()=>{document.querySelector('#unity-container').style.cssText='position:fixed;left:0;top:0;transform:none;width:1280px;height:720px';document.querySelector('#unity-fullscreen-container').style.cssText='width:1280px;height:720px';document.querySelector('#unity-canvas').style.cssText='width:1280px;height:720px';document.querySelector('#unity-footer').style.display='none';});await page.waitForTimeout(2500);
 if(!state||state.rules!==3||state.gold!==200||state.units.length!==0)throw Error('Expected untouched normal start: '+JSON.stringify(state)+' '+messages.slice(-12).join(' / '));await page.screenshot({path:path.join(out,'first-screen.png')});
 const slots=[[320,218],[480,218],[640,218],[800,218],[960,218],[320,550],[480,550],[640,550],[800,550],[960,550],[278,330],[278,438],[1002,330],[1002,438]],scale=530/768,ox=(1280-1280*scale)/2;
 const point=slot=>[ox+slots[slot][0]*scale,70+slots[slot][1]*scale];
 async function place(kind,slot){await page.keyboard.press(String(kind+1));await page.mouse.click(...point(slot));actions.push({time:state.time,action:'place',kind,slot});await page.waitForTimeout(1100);}
 await page.mouse.click(65,650);await place(0,0);await place(0,10);await page.keyboard.press('Enter');await page.waitForTimeout(1500);
 if(!state||state.phase!=='Fighting'||state.units.length!==2)throw Error('Natural start UI failed '+JSON.stringify(state));
 await page.mouse.click(920,24);await page.mouse.click(920,24);await page.waitForTimeout(500);
 const planned=[[1,1],[2,3],[3,4],[4,2]],cost=[65,85,105,90,90],start=Date.now();let lastWave=0;
 while(Date.now()-start<210000&&state.phase==='Fighting'){
  if(state.wave!==lastWave){lastWave=state.wave;await page.screenshot({path:path.join(out,'wave-'+lastWave+'.png')});console.log('REVIEW wave '+state.wave+' time '+state.time+' gold '+state.gold+' enemies '+state.enemies);}
  const missing=planned.find(([kind,slot])=>!state.units.some(u=>u.slot===slot));
  if(missing&&state.gold>=cost[missing[0]])await place(...missing);
  else {const u=state.units.find(u=>u.level<3&&state.gold>=(u.level===1?50:90));if(u){await page.mouse.click(...point(u.slot));await page.waitForTimeout(180);await page.mouse.click(1150,625);if(u.level===2){await page.waitForTimeout(250);await page.mouse.click(u.kind===0||u.kind===3||u.kind===4?750:530,350);}await page.waitForTimeout(1100);const current=state.units.find(v=>v.slot===u.slot);actions.push({time:state.time,action:'upgrade',slot:u.slot,from:u.level,to:current?.level,applied:current?.level>u.level});}}
  await page.waitForTimeout(800);
 }
 await page.screenshot({path:path.join(out,'last-screen.png')});fs.writeFileSync(path.join(out,'report.json'),JSON.stringify({state,actions,messages,errors,limits:'Scripted mouse/keyboard with natural gold. Not human enjoyment or audio verification.'},null,2));
 if(errors.length)throw Error(errors.join('\n'));if(state.wave<3)throw Error('Did not reach wave3');console.log('PASS natural UI flow: '+JSON.stringify(state));await context.close();
}catch(e){console.error(e);process.exitCode=1;}finally{if(browser)await browser.close();server.close();}})();
