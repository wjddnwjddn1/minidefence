// Isolated failure injection: after selecting a card, verify its summary and save warning do not overlap.
const fs=require('fs'),path=require('path'),http=require('http');
const {chromium}=require('C:/Users/snome/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root=path.resolve(__dirname,'../Builds/Web');
const types={'.html':'text/html; charset=utf-8','.js':'application/javascript','.wasm':'application/wasm','.data':'application/octet-stream','.css':'text/css'};
const server=http.createServer((req,res)=>{const rel=decodeURIComponent(new URL(req.url,'http://127.0.0.1').pathname).replace(/^\/+/,''),file=path.resolve(root,rel||'index.html');if(!file.startsWith(root+path.sep)&&file!==root){res.writeHead(403).end();return;}fs.readFile(file,(error,data)=>{if(error){res.writeHead(404).end();return;}res.writeHead(200,{'Content-Type':types[path.extname(file)]||'application/octet-stream','Cache-Control':'no-store'});res.end(data);});});
(async()=>{let browser;try{
 await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));
 browser=await chromium.launch({executablePath:'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe',headless:true,args:['--use-gl=angle','--use-angle=swiftshader','--enable-webgl','--enable-unsafe-swiftshader']});
 for(const [width,height] of [[320,300],[640,360],[800,500],[1280,720]]){
  const context=await browser.newContext({viewport:{width,height}}),page=await context.newPage(),messages=[],errors=[];
  page.on('pageerror',error=>errors.push(error.message));page.on('console',message=>{messages.push(message.text());if(message.type()==='error')errors.push(message.text());});
  await page.goto('http://127.0.0.1:'+server.address().port+'/?fx-smoke=reinforcement&fx-smoke=warning-geometry',{waitUntil:'domcontentloaded'});
  await page.waitForFunction(()=>[...document.querySelectorAll('#unity-build-title button')].some(button=>button.textContent==='Unload'),null,{timeout:120000});
  await page.evaluate(({width,height})=>{const c=document.querySelector('#unity-container'),v=document.querySelector('#unity-canvas'),f=document.querySelector('#unity-fullscreen-container'),size='width:'+width+'px;height:'+height+'px';c.style.cssText='position:fixed;left:0;top:0;transform:none;'+size;f.style.cssText=size;v.style.cssText=size;document.querySelector('#unity-footer').style.display='none';},{width,height});
  await page.waitForTimeout(3500);if(!messages.some(m=>m.includes('REINFORCEMENT_READY')))throw Error('card fixture missing '+messages.slice(-12).join(' | '));
  await page.mouse.click(width/2,(height-260)/2+73);await page.waitForTimeout(1200);
  if(!messages.some(m=>m.includes('REINFORCEMENT_CHOSEN')))throw Error('card selection missing');
  const geometry=width<480?'SAVE_SUMMARY_HEADER ':'SAVE_SUMMARY_NONOVERLAP ';if(!messages.some(m=>m.includes(geometry+width)))throw Error('warning and summary were not checked');
  if(errors.length)throw Error(errors.join(' | '));await context.close();console.log('PASS warning and reinforcement summary do not overlap '+width+'x'+height);
 }
}catch(error){console.error(error);process.exitCode=1;}finally{if(browser)await browser.close();server.close();}})();
