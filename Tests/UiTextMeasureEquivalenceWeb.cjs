// Compare rendered title measurements using the same development build in both allocation modes.
const fs=require('fs'),path=require('path'),http=require('http');
const {chromium}=require('C:/Users/snome/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root=path.resolve(__dirname,'../Builds/Web');
const mime={'.html':'text/html; charset=utf-8','.js':'application/javascript','.wasm':'application/wasm','.data':'application/octet-stream','.css':'text/css'};
const server=http.createServer((req,res)=>{const rel=decodeURIComponent(new URL(req.url,'http://127.0.0.1').pathname).replace(/^\/+/,''),file=path.resolve(root,rel||'index.html');if(!file.startsWith(root+path.sep)&&file!==root){res.writeHead(403).end();return;}fs.readFile(file,(e,data)=>{if(e){res.writeHead(404).end();return;}res.writeHead(200,{'Content-Type':mime[path.extname(file)]||'application/octet-stream','Cache-Control':'no-store'});res.end(data);});});
(async()=>{let browser;try{
 await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));
 browser=await chromium.launch({executablePath:'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe',headless:true,args:['--use-gl=angle','--use-angle=swiftshader','--enable-webgl','--enable-unsafe-swiftshader']});
 for(const [width,height] of [[320,300],[1280,720],[1920,1080]]){
  for(const [query,expected] of [['fx-smoke=ribbon-layout',''],['fx-smoke=ribbon-layout-menu','메뉴'],['fx-smoke=ribbon-layout&fx-smoke=sortie','출정 준비'],['fx-smoke=ribbon-layout&fx-smoke=reinforcement','증원 선택']]){
   const results=[];
   for(const baseline of [true,false]){
    const context=await browser.newContext({viewport:{width,height}}),page=await context.newPage(),messages=[],errors=[];
    page.on('pageerror',error=>errors.push(error.message));page.on('console',m=>{messages.push(m.text().trim());if(m.type()==='error')errors.push(m.text());});
    await page.goto('http://127.0.0.1:'+server.address().port+'/?'+query+(baseline?'&fx-smoke=alloc-baseline':''),{waitUntil:'domcontentloaded'});
    await page.waitForFunction(()=>[...document.querySelectorAll('#unity-build-title button')].some(b=>b.textContent==='Unload'),null,{timeout:120000});
    await page.evaluate(({width,height})=>{const c=document.querySelector('#unity-container'),v=document.querySelector('#unity-canvas'),f=document.querySelector('#unity-fullscreen-container'),size='width:'+width+'px;height:'+height+'px';c.style.cssText='position:fixed;left:0;top:0;transform:none;'+size;f.style.cssText=size;v.style.cssText=size;document.querySelector('#unity-footer').style.display='none';},{width,height});
    const viewport=width+'x'+height;
    for(let i=0;i<50;i++){if(messages.some(m=>m.includes('UI_HEADER_TITLE|'+viewport+'|'))&&(!expected||messages.some(m=>m.includes('UI_WINDOW_TITLE|'+expected+'|'+viewport))))break;await page.waitForTimeout(300);}
    if(!messages.some(m=>m.includes('UI_HEADER_TITLE|'+viewport+'|'))||expected&&!messages.some(m=>m.includes('UI_WINDOW_TITLE|'+expected+'|'+viewport)))throw Error('missing title geometry '+viewport+' '+query);
    if(errors.length)throw Error(errors.join(' | '));
    const pairs=[];let geometry=null;
    for(const line of messages){if(line.startsWith('UI_HEADER_TITLE|'+viewport+'|'))geometry=line;else if(line.startsWith('UI_WINDOW_TITLE|')&&line.includes('|'+viewport)&&geometry){pairs.push(line+' / '+geometry);geometry=null;}}
    results.push([...new Set(pairs)].sort());await context.close();
   }
   if(JSON.stringify(results[0])!==JSON.stringify(results[1]))throw Error('title measurements differ '+width+'x'+height+' '+query+'\n'+JSON.stringify(results));
  }
  console.log('PASS baseline and reused title measurements '+width+'x'+height);
 }
}catch(e){console.error(e);process.exitCode=1;}finally{if(browser)await browser.close();server.close();}})();
