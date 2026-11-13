// r38: portrait gameplay retired; low landscape fixtures replace portrait cases.
// Regression: the pause overlay's button resumes on mouse/touch (the aimed Ember ability it once guarded is retired).
const fs=require('fs'),path=require('path'),http=require('http');
const {chromium}=require('C:/Users/snome/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root=path.resolve(__dirname,'bin/TacticalWeb'),out=process.env.RESUME_CAPTURE_DIR?path.resolve(process.env.RESUME_CAPTURE_DIR):null,mime={'.html':'text/html; charset=utf-8','.js':'application/javascript','.wasm':'application/wasm','.data':'application/octet-stream','.css':'text/css'};if(out)fs.mkdirSync(out,{recursive:true});
const server=http.createServer((req,res)=>{const rel=decodeURIComponent(new URL(req.url,'http://127.0.0.1').pathname).replace(/^\/+/,''),file=path.resolve(root,rel||'index.html');if(!file.startsWith(root+path.sep)&&file!==root){res.writeHead(403).end();return;}fs.readFile(file,(e,data)=>{if(e){res.writeHead(404).end();return;}res.writeHead(200,{'Content-Type':mime[path.extname(file)]||'application/octet-stream','Cache-Control':'no-store'});res.end(data);});});
(async()=>{let browser;try{await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));browser=await chromium.launch({executablePath:'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe',headless:true,args:['--use-gl=angle','--use-angle=swiftshader','--enable-webgl','--enable-unsafe-swiftshader']});
 for(const [width,height,touch] of [[320,300,true],[740,360,true],[640,360,true],[800,500,false],[1280,720,false],[1920,1080,false]]){
  const context=await browser.newContext({viewport:{width,height},hasTouch:touch}),page=await context.newPage(),messages=[],errors=[];page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{messages.push(m.text().trim());if(m.type()==='error')errors.push(m.text());});
  await page.goto('http://127.0.0.1:'+server.address().port+'/?fx-smoke=earlybutton&fx-smoke=emberbutton&fx-smoke=resume-button',{waitUntil:'domcontentloaded'});await page.waitForFunction(()=>[...document.querySelectorAll('#unity-build-title button')].some(b=>b.textContent==='Unload'),null,{timeout:120000});
  await page.evaluate(({width,height})=>{const c=document.querySelector('#unity-container'),v=document.querySelector('#unity-canvas'),f=document.querySelector('#unity-fullscreen-container'),size='width:'+width+'px;height:'+height+'px';c.style.cssText='position:fixed;left:0;top:0;transform:none;'+size;f.style.cssText=size;v.style.cssText=size;document.querySelector('#unity-footer').style.display='none';v.focus();},{width,height});
  for(let i=0;i<40&&!messages.some(m=>m.includes('EARLY_BUTTON_READY'));i++)await page.waitForTimeout(250);if(!messages.some(m=>m.includes('EARLY_BUTTON_READY')))throw Error('fixture not ready '+width);
  await page.keyboard.press('p');let geometry;for(let i=0;i<30;i++){geometry=messages.find(m=>m.startsWith('PAUSE_RESUME_RECT|'+width+'x'+height+'|'));if(geometry)break;await page.waitForTimeout(200);}if(!geometry||!messages.some(m=>m.includes('PAUSE_STATE True')))throw Error('pause or button missing '+width);
  const fields=geometry.split('|'),x=Number(fields[2]),y=Number(fields[3]),w=Number(fields[4]),h=Number(fields[5]);if(!(w>=100&&h>=40&&x>=0&&y>=0&&x+w<=width+.2&&y+h<=height+.2))throw Error('button bounds invalid '+geometry);
  if(out)await page.screenshot({path:path.join(out,'resume-'+width+'x'+height+'.png')});
  // The manual Ember aim was retired (7653354): E does nothing now, so only the button's own resume is checked (Claude, r47).
  const click=async()=>{if(touch)await page.touchscreen.tap(x+w/2,y+h/2);else await page.mouse.click(x+w/2,y+h/2);await page.waitForTimeout(300);};
  await click();
  if(!messages.some(m=>m.includes('PAUSE_STATE False')))throw Error('resume button did not resume '+width);
  await page.keyboard.press('p');await page.waitForTimeout(300);
  if(messages.filter(m=>m.includes('PAUSE_STATE True')).length!==2)throw Error('second pause missing '+width);
  await click();await page.waitForTimeout(300);
  if(messages.filter(m=>m.includes('PAUSE_STATE False')).length!==2)throw Error('second resume missing '+width);
  if(messages.some(m=>m.includes('EMBER_BUTTON_FIRE')))throw Error('a resume click fired something on the map '+width);

  if(errors.length)throw Error(errors.join(' | '));await context.close();console.log('PASS resume button pauses and resumes twice '+width+'x'+height+(touch?' touch':' mouse'));
 }
}catch(e){console.error(e);process.exitCode=1;}finally{if(browser)await browser.close();server.close();}})();
