// r38: portrait gameplay retired; low landscape fixtures replace portrait cases.
// Validate title bounds without writing screenshots or using the player's browser profile.
const fs=require('fs'),path=require('path'),http=require('http');
const {chromium}=require('C:/Users/snome/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root=path.resolve(__dirname,'bin/TacticalWeb');
const captureDir=process.env.R19_CAPTURE_DIR;if(captureDir)fs.mkdirSync(captureDir,{recursive:true});
const types={'.html':'text/html; charset=utf-8','.js':'application/javascript','.wasm':'application/wasm','.data':'application/octet-stream','.css':'text/css'};
const server=http.createServer((req,res)=>{const rel=decodeURIComponent(new URL(req.url,'http://127.0.0.1').pathname).replace(/^\/+/,''),file=path.resolve(root,rel||'index.html');if(!file.startsWith(root+path.sep)&&file!==root){res.writeHead(403).end();return;}fs.readFile(file,(error,data)=>{if(error){res.writeHead(404).end();return;}res.writeHead(200,{'Content-Type':types[path.extname(file)]||'application/octet-stream','Cache-Control':'no-store'});res.end(data);});});
(async()=>{let browser;try{
 await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));
 browser=await chromium.launch({executablePath:'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe',headless:true,args:['--use-gl=angle','--use-angle=swiftshader','--enable-webgl','--enable-unsafe-swiftshader']});
 for(const [width,height] of [[320,300],[740,360],[640,360],[800,500],[1280,720],[1920,1080]]){
  const context=await browser.newContext({viewport:{width,height}}),page=await context.newPage(),messages=[],errors=[];
  page.on('pageerror',error=>errors.push(error.message));page.on('console',message=>{messages.push(message.text());if(message.type()==='error')errors.push(message.text());});
  for(const [query,expected,shot] of [['fx-smoke=ribbon-layout','','header'],['fx-smoke=ribbon-layout-menu','메뉴','menu'],['fx-smoke=ribbon-layout&fx-smoke=sortie','출정 준비','sortie'],['fx-smoke=ribbon-layout&fx-smoke=reinforcement','증원 선택','reinforcement']]){
   messages.length=0;await page.goto('http://127.0.0.1:'+server.address().port+'/?'+query,{waitUntil:'domcontentloaded'});
   await page.waitForFunction(()=>[...document.querySelectorAll('#unity-build-title button')].some(button=>button.textContent==='Unload'),null,{timeout:120000});
   await page.evaluate(({width,height})=>{const c=document.querySelector('#unity-container'),v=document.querySelector('#unity-canvas'),f=document.querySelector('#unity-fullscreen-container'),size='width:'+width+'px;height:'+height+'px';c.style.cssText='position:fixed;left:0;top:0;transform:none;'+size;f.style.cssText=size;v.style.cssText=size;document.querySelector('#unity-footer').style.display='none';},{width,height});
   const viewport=width+'x'+height;for(let attempt=0;attempt<40;attempt++){if(messages.some(m=>m.includes('RIBBON_HEADER|'+viewport+'|'))&&(!expected||messages.some(m=>m.includes('UI_WINDOW_TITLE|'+expected+'|'+viewport))))break;await page.waitForTimeout(300);}await page.waitForTimeout(2200);
   // r38 compact landscape (height under 480) shows a one-line stats header without the title ribbon, so there is no ribbon to measure there (Claude, r47)
if(height>=480){const headers=messages.filter(m=>m.includes('RIBBON_HEADER|'+viewport+'|'));if(!headers.length)throw Error('header geometry missing '+viewport);
   const header=headers.at(-1).split('|'),ribbonWidth=Number(header[2]),rightLimit=Number(header[3]);if(6+ribbonWidth>rightLimit+.1)throw Error('header overlaps controls '+viewport);const ui=messages.filter(m=>m.includes('UI_HEADER_TITLE|'+viewport+'|')).at(-1);if(!ui)throw Error('new HUD title geometry missing '+viewport);const parts=ui.split('|'),left=Number(parts[2]),right=Number(parts[3]),textWidth=Number(parts[4]),fontSize=Number(parts[5]);if(textWidth>right-left+.2||fontSize<12)throw Error('HUD title does not fit or is too small '+viewport+' '+ui);}
   const titles=messages.filter(m=>m.includes('RIBBON_TITLE|')&&m.includes('|'+viewport+'|'));if(expected&&!messages.some(m=>m.includes('UI_WINDOW_TITLE|'+expected+'|'+viewport)))throw Error('window title missing '+expected+' '+viewport);
   for(const line of titles){const fields=line.split('|'),x=Number(fields[3]),w=Number(fields[4]),left=Number(fields[5]),right=Number(fields[6]),textWidth=Number(fields[7]);if(left<x+29.9||right>x+w-29.9||textWidth>right-left+.2)throw Error('title outside ribbon '+viewport+' '+line);}
   if(errors.length)throw Error(errors.join(' | '));if(captureDir){const target=path.join(captureDir,shot+'-'+viewport+'.png');if(fs.existsSync(target))throw Error('capture already exists '+target);await page.screenshot({path:target});}
  }
  await context.close();console.log('PASS ribbon title and control bounds '+width+'x'+height);
 }
}catch(error){console.error(error);process.exitCode=1;}finally{if(browser)await browser.close();server.close();}})();
