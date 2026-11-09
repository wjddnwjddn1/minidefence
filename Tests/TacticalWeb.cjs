const fs=require('fs'),path=require('path'),http=require('http');
const {chromium}=require('C:/Users/snome/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root=path.resolve(__dirname,'bin/TacticalWeb'),out=path.resolve(__dirname,'bin/tactical-web-evidence');
const types={'.html':'text/html; charset=utf-8','.js':'application/javascript','.wasm':'application/wasm','.data':'application/octet-stream','.css':'text/css'};
const server=http.createServer((req,res)=>{const relative=decodeURIComponent(new URL(req.url,'http://127.0.0.1').pathname).replace(/^\/+/,''),file=path.resolve(root,relative||'index.html');if(!file.startsWith(root+path.sep)){res.writeHead(403).end();return;}fs.readFile(file,(error,data)=>{if(error){res.writeHead(404).end();return;}res.writeHead(200,{'Content-Type':types[path.extname(file)]||'application/octet-stream','Cache-Control':'no-store'});res.end(data);});});
(async()=>{let browser;try{
 fs.mkdirSync(out,{recursive:true});await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));
 browser=await chromium.launch({executablePath:'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe',headless:true,args:['--use-gl=angle','--use-angle=swiftshader','--enable-webgl','--enable-unsafe-swiftshader']});
 for(const [w,h] of [[320,300],[568,320],[800,500],[1280,720],[1920,1080]]){
  if(process.env.TACTICAL_WIDTH&&w!==Number(process.env.TACTICAL_WIDTH))continue;
  const context=await browser.newContext({viewport:{width:w,height:h}}),page=await context.newPage(),messages=[],errors=[];
  page.on('console',m=>{messages.push(m.text());if(m.type()==='error')errors.push(m.text());});page.on('pageerror',e=>errors.push(e.message));
  await page.goto('http://127.0.0.1:'+server.address().port+'/?fx-smoke=tactical',{waitUntil:'domcontentloaded'});
  await page.waitForFunction(()=>[...document.querySelectorAll('#unity-build-title button')].some(b=>b.textContent==='Unload'),null,{timeout:120000});
  await page.evaluate(([w,h])=>{document.querySelector('#unity-container').style.cssText=`position:fixed;left:0;top:0;transform:none;width:${w}px;height:${h}px`;document.querySelector('#unity-fullscreen-container').style.cssText=`width:${w}px;height:${h}px`;document.querySelector('#unity-canvas').style.cssText=`width:${w}px;height:${h}px`;document.querySelector('#unity-footer').style.display='none';},[w,h]);
  await page.waitForTimeout(2500);if(!messages.some(m=>m.includes('TACTICAL_READY 2 ')))throw Error('fixture absent: '+messages.slice(-5));
  await page.screenshot({path:path.join(out,`choice-${w}.png`)});
  const height=Math.min(h-16,476),y=(h-height)/2,scale=w>=1500?1.4:1,screenY=value=>h/2+(value-h/2)*scale;
  await page.mouse.click(w/2,screenY(y+height-63));await page.waitForTimeout(500);
  if(!messages.some(m=>m.includes('TACTICAL_REROLLED 1 ')))throw Error('reroll click failed '+w+': '+messages.slice(-5));
  await page.screenshot({path:path.join(out,`reroll-${w}.png`)});
  const index=Number(process.env.TACTICAL_CARD_INDEX||0);let scroll=0;
  if(index===2){await page.mouse.move(w/2,screenY(y+122));for(let step=0;step<25;step++){await page.mouse.wheel(0,120);await page.waitForTimeout(30);}await page.waitForTimeout(300);scroll=Math.max(0,276-(height-188));await page.screenshot({path:path.join(out,`scrolled-${w}.png`)});}
  await page.mouse.click(w/2,screenY(y+80+index*92+42-scroll));await page.waitForTimeout(500);
  const offered=messages.find(m=>m.includes('TACTICAL_REROLLED 1 ')).match(/TACTICAL_REROLLED 1 \d+ ([0-6],[0-6],[0-6])/)[1].split(',');
  if(!messages.some(m=>m.includes('TACTICAL_CHOSEN 3 '+offered[index])))throw Error('card click failed '+w+': '+messages.slice(-5));
  if(errors.length)throw Error(errors.join('\n'));
  await page.screenshot({path:path.join(out,`chosen-${w}.png`)});await context.close();console.log(`PASS tactical UI reroll and card ${Number(process.env.TACTICAL_CARD_INDEX||0)+1} click ${w}x${h}`);
 }
}catch(error){console.error(error);process.exitCode=1;}finally{if(browser)await browser.close();server.close();}})();
