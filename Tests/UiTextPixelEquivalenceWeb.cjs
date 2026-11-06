// Compare the bright text pixels of the sortie dialog across allocation modes.
const fs=require('fs'),path=require('path'),http=require('http');
const {PNG}=require('C:/Users/snome/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/pngjs');
const {chromium}=require('C:/Users/snome/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root=path.resolve(__dirname,'../Builds/Web'),mime={'.html':'text/html; charset=utf-8','.js':'application/javascript','.wasm':'application/wasm','.data':'application/octet-stream','.css':'text/css'};
const server=http.createServer((req,res)=>{const rel=decodeURIComponent(new URL(req.url,'http://127.0.0.1').pathname).replace(/^\/+/,''),file=path.resolve(root,rel||'index.html');if(!file.startsWith(root+path.sep)&&file!==root){res.writeHead(403).end();return;}fs.readFile(file,(e,data)=>{if(e){res.writeHead(404).end();return;}res.writeHead(200,{'Content-Type':mime[path.extname(file)]||'application/octet-stream','Cache-Control':'no-store'});res.end(data);});});
(async()=>{let browser;try{
 await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));browser=await chromium.launch({executablePath:'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe',headless:true,args:['--use-gl=angle','--use-angle=swiftshader','--enable-webgl','--enable-unsafe-swiftshader']});
 for(const [width,height] of [[320,300],[1280,720]]){
  const buffers=[];
  for(const baseline of [true,false]){
   const context=await browser.newContext({viewport:{width,height}}),page=await context.newPage(),messages=[],errors=[];page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{messages.push(m.text());if(m.type()==='error')errors.push(m.text());});
   await page.goto('http://127.0.0.1:'+server.address().port+'/?fx-smoke=ribbon-layout&fx-smoke=sortie'+(baseline?'&fx-smoke=alloc-baseline':''),{waitUntil:'domcontentloaded'});await page.waitForFunction(()=>[...document.querySelectorAll('#unity-build-title button')].some(b=>b.textContent==='Unload'),null,{timeout:120000});
   await page.evaluate(({width,height})=>{const c=document.querySelector('#unity-container'),v=document.querySelector('#unity-canvas'),f=document.querySelector('#unity-fullscreen-container'),size='width:'+width+'px;height:'+height+'px';c.style.cssText='position:fixed;left:0;top:0;transform:none;'+size;f.style.cssText=size;v.style.cssText=size;document.querySelector('#unity-footer').style.display='none';},{width,height});
   for(let i=0;i<50&&!messages.some(m=>m.includes('UI_WINDOW_TITLE|출정 준비|'+width+'x'+height));i++)await page.waitForTimeout(300);await page.waitForTimeout(1500);
   if(!messages.some(m=>m.includes('UI_WINDOW_TITLE|출정 준비|'+width+'x'+height))||errors.length)throw Error('dialog not ready '+errors.join(' | '));
   const wide=width>=700&&height>=420,w=Math.min(width-24,wide?760:480),h=wide?Math.min(height-32,350):Math.min(height-16,284),x=Math.round((width-w)/2),y=Math.round((height-h)/2);
   buffers.push(PNG.sync.read(await page.screenshot({clip:{x,y,width:w,height:h}})));await context.close();
  }
  const [a,b]=buffers;if(a.width!==b.width||a.height!==b.height)throw Error('dialog size changed');let active=0,diff=0;
  for(let i=0;i<a.data.length;i+=4){const bright=p=>p[i]>175&&p[i+1]>160&&p[i+2]>130;const x=bright(a.data),y=bright(b.data);if(x||y)active++;if(x!==y)diff++;}
  if(active<100||diff/active>.005)throw Error('dialog text pixels differ '+width+'x'+height+' '+diff+'/'+active);
  console.log('PASS sortie text pixels '+width+'x'+height+' differing='+diff+' bright='+active);
 }
}catch(e){console.error(e);process.exitCode=1;}finally{if(browser)await browser.close();server.close();}})();
