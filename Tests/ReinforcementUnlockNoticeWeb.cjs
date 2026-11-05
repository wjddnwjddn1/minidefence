// First normal victory should announce the unlock only once in an isolated profile.
const fs=require('fs'),path=require('path'),http=require('http');
const {chromium}=require('C:/Users/snome/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root=path.resolve(__dirname,'../Builds/Web');
const types={'.html':'text/html; charset=utf-8','.js':'application/javascript','.wasm':'application/wasm','.data':'application/octet-stream','.css':'text/css'};
const server=http.createServer((req,res)=>{const rel=decodeURIComponent(new URL(req.url,'http://127.0.0.1').pathname).replace(/^\/+/,''),file=path.resolve(root,rel||'index.html');if(!file.startsWith(root+path.sep)&&file!==root){res.writeHead(403).end();return;}fs.readFile(file,(error,data)=>{if(error){res.writeHead(404).end();return;}res.writeHead(200,{'Content-Type':types[path.extname(file)]||'application/octet-stream','Cache-Control':'no-store'});res.end(data);});});
(async()=>{let browser;try{
 await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));
 browser=await chromium.launch({executablePath:'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe',headless:true,args:['--use-gl=angle','--use-angle=swiftshader','--enable-webgl','--enable-unsafe-swiftshader']});
 for(const [width,height] of [[320,300],[1280,720]]){
  const context=await browser.newContext({viewport:{width,height}}),page=await context.newPage(),messages=[],errors=[];
  page.on('pageerror',error=>errors.push(error.message));page.on('console',message=>{messages.push(message.text());if(message.type()==='error')errors.push(message.text());});
  async function win(extra='',target=page){await target.goto('http://127.0.0.1:'+server.address().port+'/?fx-smoke=recordwin'+extra,{waitUntil:'domcontentloaded'});await target.waitForFunction(()=>[...document.querySelectorAll('#unity-build-title button')].some(button=>button.textContent==='Unload'),null,{timeout:120000});await target.evaluate(({width,height})=>{const c=document.querySelector('#unity-container'),v=document.querySelector('#unity-canvas'),f=document.querySelector('#unity-fullscreen-container'),size='width:'+width+'px;height:'+height+'px';c.style.cssText='position:fixed;left:0;top:0;transform:none;'+size;f.style.cssText=size;v.style.cssText=size;document.querySelector('#unity-footer').style.display='none';},{width,height});await target.waitForTimeout(4500);}
  await win();if(!messages.some(m=>m.includes('REINFORCEMENT_UNLOCK_NOTICE True')))throw Error('first victory did not announce unlock');
  messages.length=0;await win();if(!messages.some(m=>m.includes('REINFORCEMENT_UNLOCK_NOTICE False')))throw Error('repeat victory announced unlock again');
  messages.length=0;await win('&field=forest');if(!messages.some(m=>m.includes('REINFORCEMENT_UNLOCK_NOTICE False')))throw Error('another normal field announced unlock again');
  const fresh=await browser.newContext({viewport:{width,height}}),other=await fresh.newPage();other.on('pageerror',error=>errors.push(error.message));other.on('console',message=>{messages.push(message.text());if(message.type()==='error')errors.push(message.text());});
  messages.length=0;await win('&difficulty=challenge',other);if(!messages.some(m=>m.includes('REINFORCEMENT_UNLOCK_NOTICE False')))throw Error('challenge victory incorrectly announced unlock');
  messages.length=0;await win('',other);if(!messages.some(m=>m.includes('REINFORCEMENT_UNLOCK_NOTICE True')))throw Error('normal victory after challenge did not announce unlock');await fresh.close();
  if(errors.length)throw Error(errors.join(' | '));await context.close();console.log('PASS first-only reinforcement unlock notice '+width+'x'+height);
 }
}catch(error){console.error(error);process.exitCode=1;}finally{if(browser)await browser.close();server.close();}})();
