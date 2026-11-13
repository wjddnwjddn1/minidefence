// r38: portrait gameplay retired; low landscape fixtures replace portrait cases.
const fs=require('fs'),path=require('path'),http=require('http');
const {chromium}=require('C:/Users/snome/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root=path.resolve(__dirname,'bin/TacticalWeb'),out=path.resolve(__dirname,'bin/candidate-slots-evidence');
const types={'.html':'text/html; charset=utf-8','.js':'application/javascript','.wasm':'application/wasm','.data':'application/octet-stream','.css':'text/css'};
const server=http.createServer((req,res)=>{const relative=decodeURIComponent(new URL(req.url,'http://127.0.0.1').pathname).replace(/^\/+/,''),file=path.resolve(root,relative||'index.html');if(!file.startsWith(root+path.sep)){res.writeHead(403).end();return;}fs.readFile(file,(error,data)=>{if(error){res.writeHead(404).end();return;}res.writeHead(200,{'Content-Type':types[path.extname(file)]||'application/octet-stream','Cache-Control':'no-store'});res.end(data);});});
(async()=>{let browser;try{
 fs.mkdirSync(out,{recursive:true});await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));
 browser=await chromium.launch({executablePath:'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe',headless:true,args:['--use-gl=angle','--use-angle=swiftshader','--enable-webgl','--enable-unsafe-swiftshader']});
 for(const [w,h,extra,count] of [[320,300,'',10],[800,500,'',10],[1280,720,'',10],[1920,1080,'',10],[740,360,'',10],[1280,720,'&legacy=1',8],[1280,720,'&reinforcement=1',8]]){
  const context=await browser.newContext({viewport:{width:w,height:h}}),page=await context.newPage(),messages=[],errors=[];let state;
  page.on('console',m=>{const t=m.text().trim();messages.push(t);if(m.type()==='error')errors.push(t);if(t.startsWith('REVIEW_STATE|'))state=t.split('|');});page.on('pageerror',e=>errors.push(e.message));
  await page.goto('http://127.0.0.1:'+server.address().port+'/?fx-smoke=candidateslots&review-flow=1'+extra,{waitUntil:'domcontentloaded'});
  await page.waitForFunction(()=>[...document.querySelectorAll('#unity-build-title button')].some(b=>b.textContent==='Unload'),null,{timeout:120000});
  await page.evaluate(([w,h])=>{document.querySelector('#unity-container').style.cssText=`position:fixed;left:0;top:0;transform:none;width:${w}px;height:${h}px`;document.querySelector('#unity-fullscreen-container').style.cssText=`width:${w}px;height:${h}px`;document.querySelector('#unity-canvas').style.cssText=`width:${w}px;height:${h}px`;document.querySelector('#unity-footer').style.display='none';},[w,h]);
  await page.waitForTimeout(2500);const ready=messages.find(m=>m.startsWith('CANDIDATE_SLOTS_READY '));if(!ready||!ready.endsWith(' '+count))throw Error('wrong layout '+ready);
  const name=`slots-${w}x${h}${extra?'-'+extra.slice(1).replace('=1',''):''}`;
  await page.screenshot({path:path.join(out,name+'.png')});
  if(w===1280&&!extra){/* r38 compact landscape moved the board; read where it is drawn (Claude, r47) */const view=messages.filter(m=>m.startsWith('SLOT_VIEW|')).at(-1).split('|').map(Number),scale=view[1],ox=view[2],oy=view[3];await page.focus('#unity-canvas');await page.keyboard.press('1');await page.waitForTimeout(300);for(const [x,y,slot] of [[640,548,8],[704,452,9]]){await page.mouse.click(ox+x*scale,oy+y*scale);await page.waitForTimeout(600);/* an empty slot opens the troop ring now; take the sword from it (Claude, r47) */{const menu=messages.filter(m=>m.startsWith('SLOT_MENU|'+slot+'|')).at(-1);if(!menu)throw Error('slot menu did not open '+slot);const sword=menu.split('|')[3].split(';').filter(Boolean).map(v=>v.split(',').map(Number)).find(v=>v[0]===0);if(!sword)throw Error('no sword in the ring '+slot);await page.mouse.click(sword[1]+sword[3]/2,sword[2]+sword[4]/2);}await page.waitForTimeout(1200);if(!state||!state[7].split(';').some(u=>u.startsWith(slot+':0:1:')))throw Error('extra slot click failed '+slot+' '+state);}
   await page.screenshot({path:path.join(out,name+'-placed.png')});if(+state[5]!==210)throw Error('placement cost mismatch '+state);}
  if(errors.length)throw Error(errors.join('\n'));fs.writeFileSync(path.join(out,name+'.json'),JSON.stringify({ready,state,errors},null,2));console.log('PASS '+name+' '+ready);await context.close();
 }
}catch(e){console.error(e);process.exitCode=1;}finally{if(browser)await browser.close();server.close();}})();
