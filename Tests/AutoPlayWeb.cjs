// Automated Web match in a fresh browser profile; no user saves or records are used.
const fs=require('fs'),path=require('path'),http=require('http');
const {chromium}=require('C:/Users/snome/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root=path.resolve(__dirname,'../Builds/Web'),out=path.resolve(__dirname,'../Docs/web-smoke');
const types={'.html':'text/html; charset=utf-8','.js':'application/javascript','.wasm':'application/wasm','.data':'application/octet-stream','.css':'text/css','.png':'image/png','.ico':'image/x-icon'};
const server=http.createServer((req,res)=>{const rel=decodeURIComponent(new URL(req.url,'http://127.0.0.1').pathname).replace(/^\/+/,''),f=path.resolve(root,rel||'index.html');if(!f.startsWith(root+path.sep)&&f!==root){res.writeHead(403).end();return;}fs.readFile(f,(err,data)=>{if(err){res.writeHead(404).end();return;}res.writeHead(200,{'Content-Type':types[path.extname(f)]||'application/octet-stream','Cache-Control':'no-store'});res.end(data);});});
(async()=>{let browser;try{
 await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));
 browser=await chromium.launch({executablePath:'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe',headless:true,args:['--use-gl=angle','--use-angle=swiftshader','--enable-webgl','--enable-unsafe-swiftshader']});
 const context=await browser.newContext({viewport:{width:1200,height:850}}),page=await context.newPage(),errors=[];page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(m.type()==='error')errors.push(m.text());});
 const outcome=page.waitForEvent('console',{predicate:m=>/AUTOPLAY_(WIN|LOSS|STUCK)/.test(m.text()),timeout:300000});
 await page.goto('http://127.0.0.1:'+server.address().port+'/?fx-smoke=autoplay',{waitUntil:'domcontentloaded'});
 const message=(await outcome).text();await page.waitForTimeout(500);await page.screenshot({path:path.join(out,'autoplay-complete.png')});
 if(message.includes('AUTOPLAY_WIN')){const box=await page.locator('#unity-canvas').boundingBox(),w=(box.width-24)/6;await page.mouse.click(box.x+12+3.5*w,box.y+box.height-90);await page.waitForTimeout(400);await page.screenshot({path:path.join(out,'autoplay-next.png')});await page.mouse.click(box.x+box.width-78,box.y+24);await page.waitForTimeout(400);await page.screenshot({path:path.join(out,'autoplay-record-check.png')});}
 const report={outcome:message,errors};fs.writeFileSync(path.join(out,'report-autoplay.json'),JSON.stringify(report,null,2));console.log(JSON.stringify(report));
 if(!message.includes('AUTOPLAY_WIN')||errors.length)process.exitCode=1;
 await context.close();
 }catch(error){console.error(error);process.exitCode=1;}finally{if(browser)await browser.close();server.close();}})();
