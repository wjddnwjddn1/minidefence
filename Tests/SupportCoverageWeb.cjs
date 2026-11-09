const fs=require('fs'),path=require('path'),http=require('http');
const {chromium}=require('C:/Users/snome/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root=path.resolve(__dirname,'bin/TacticalWeb'),out=path.resolve(__dirname,'bin/support-coverage-evidence');
const types={'.html':'text/html; charset=utf-8','.js':'application/javascript','.wasm':'application/wasm','.data':'application/octet-stream','.css':'text/css'};
const server=http.createServer((req,res)=>{const relative=decodeURIComponent(new URL(req.url,'http://127.0.0.1').pathname).replace(/^\/+/,''),file=path.resolve(root,relative||'index.html');if(!file.startsWith(root+path.sep)){res.writeHead(403).end();return;}fs.readFile(file,(error,data)=>{if(error){res.writeHead(404).end();return;}res.writeHead(200,{'Content-Type':types[path.extname(file)]||'application/octet-stream','Cache-Control':'no-store'});res.end(data);});});
(async()=>{let browser;try{
 fs.mkdirSync(out,{recursive:true});await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));
 browser=await chromium.launch({executablePath:'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe',headless:true,args:['--use-gl=angle','--use-angle=swiftshader','--enable-webgl','--enable-unsafe-swiftshader']});
 for(const [w,h] of [[800,500],[1280,720],[1920,1080]]){
  const context=await browser.newContext({viewport:{width:w,height:h}}),page=await context.newPage(),messages=[],errors=[];
  page.on('console',m=>{messages.push(m.text());if(m.type()==='error')errors.push(m.text());});page.on('pageerror',e=>errors.push(e.message));
  await page.goto('http://127.0.0.1:'+server.address().port+(process.env.UNIT_DETAIL_KIND?'/?fx-smoke=unitdetail&kind='+process.env.UNIT_DETAIL_KIND:'/?fx-smoke=supportcoverage'),{waitUntil:'domcontentloaded'});
  await page.waitForFunction(()=>[...document.querySelectorAll('#unity-build-title button')].some(b=>b.textContent==='Unload'),null,{timeout:120000});
  await page.evaluate(([w,h])=>{document.querySelector('#unity-container').style.cssText=`position:fixed;left:0;top:0;transform:none;width:${w}px;height:${h}px`;document.querySelector('#unity-fullscreen-container').style.cssText=`width:${w}px;height:${h}px`;document.querySelector('#unity-canvas').style.cssText=`width:${w}px;height:${h}px`;document.querySelector('#unity-footer').style.display='none';},[w,h]);
  await page.waitForTimeout(2500);const ready=messages.find(m=>m.includes(process.env.UNIT_DETAIL_KIND?'UNIT_DETAIL_READY ':'SUPPORT_COVERAGE_READY '));if(!ready)throw Error('fixture missing');
  const values=process.env.UNIT_DETAIL_KIND?null:ready.match(/SUPPORT_COVERAGE_READY (\d+) (\d+)/);if(process.env.UNIT_DETAIL_KIND){if(!ready.includes('UNIT_DETAIL_READY '+process.env.UNIT_DETAIL_KIND))throw Error('wrong unit fixture');}else if(!values||Number(values[1])!==2||Number(values[2])!==1)throw Error('expected 2 in range, 1 with additional effect');
  if(errors.length)throw Error(errors.join('\n'));await page.screenshot({path:path.join(out,`support-${w}${process.env.UNIT_DETAIL_KIND?"-unit"+process.env.UNIT_DETAIL_KIND:""}.png`)});
  console.log(`PASS ${process.env.UNIT_DETAIL_KIND?'unit detail '+process.env.UNIT_DETAIL_KIND:'support 2 in range, 1 with additional effect'} fixture ${w}x${h}; screenshot requires visual review`);await context.close();
 }
}catch(e){console.error(e);process.exitCode=1;}finally{if(browser)await browser.close();server.close();}})();
