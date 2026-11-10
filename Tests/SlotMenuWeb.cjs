const fs=require('fs'),path=require('path'),http=require('http');
const {chromium,devices}=require('C:/Users/snome/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root=path.resolve(__dirname,'bin/TacticalWeb'),out=path.resolve(__dirname,'bin/r30-ui-evidence');
const types={'.html':'text/html; charset=utf-8','.js':'application/javascript','.wasm':'application/wasm','.data':'application/octet-stream','.css':'text/css'};
const server=http.createServer((req,res)=>{const relative=decodeURIComponent(new URL(req.url,'http://127.0.0.1').pathname).replace(/^\/+/,''),file=path.resolve(root,relative||'index.html');if(!file.startsWith(root+path.sep)){res.writeHead(403).end();return;}fs.readFile(file,(error,data)=>{if(error){res.writeHead(404).end();return;}res.writeHead(200,{'Content-Type':types[path.extname(file)]||'application/octet-stream','Cache-Control':'no-store'});res.end(data);});});
const summaries=[];
async function wait(page,predicate,label,timeout=10000){const until=Date.now()+timeout;while(!predicate()){if(Date.now()>until)throw Error('timeout '+label);await page.waitForTimeout(100);}}
(async()=>{let browser;try{
 fs.mkdirSync(out,{recursive:true});await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));
 browser=await chromium.launch({executablePath:'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe',headless:true,args:['--use-gl=angle','--use-angle=swiftshader','--enable-webgl','--enable-unsafe-swiftshader']});
 async function fixture(w,h,query='',touch=false,natural=false){
  const context=await browser.newContext({viewport:{width:w,height:h},hasTouch:touch,isMobile:touch,userAgent:touch?devices['Pixel 7'].userAgent:undefined}),page=await context.newPage(),messages=[],errors=[];const data={context,page,messages,errors,touch,w,h,menu:null,serial:0,state:null,geometry:null,info:null,cards:{}};
  page.on('console',m=>{const t=m.text().trim();messages.push(t);if(m.type()==='error')errors.push(t);if(t.startsWith('CARD_RECT|')){const p=t.split('|');data.cards[+p[1]]=p.slice(2).map(Number);}if(t.startsWith('SLOT_INFO|'))data.info=t.split('|').slice(1).map(Number);if(t.startsWith('SLOT_VIEW|'))data.geometry=t.split('|').slice(1).map(Number);if(t.startsWith('SLOT_MENU|')){const p=t.split('|');data.menu={slot:+p[1],level:+p[2],actions:p[3].split(';').filter(Boolean).map(v=>v.split(',').map(Number))};data.serial++;}if(t.startsWith('REVIEW_STATE|')){const p=t.split('|');data.state={rules:+p[1],phase:p[2],time:+p[4],gold:+p[5],units:p[7]?p[7].split(';').map(v=>v.split(':').map(Number)):[]};}});page.on('pageerror',e=>errors.push(e.message));
  await page.goto('http://127.0.0.1:'+server.address().port+(natural?'/?fx-smoke=tutorial&review-flow=1':'/?fx-smoke=slotmenu&review-flow=1')+query,{waitUntil:'domcontentloaded'});
  await wait(page,()=>data.state&&data.geometry,'Unity first frame',120000);
  await page.evaluate(([w,h])=>{document.querySelector('#unity-container').style.cssText=`position:fixed;left:0;top:0;transform:none;width:${w}px;height:${h}px`;document.querySelector('#unity-fullscreen-container').style.cssText=`width:${w}px;height:${h}px`;document.querySelector('#unity-canvas').style.cssText=`width:${w}px;height:${h}px`;document.querySelector('#unity-footer').style.display='none';},[w,h]);
  await wait(page,()=>data.state&&data.geometry,'fixture');await page.waitForTimeout(500);data.viewport=await page.evaluate(()=>({width:innerWidth,height:innerHeight,scale:visualViewport.scale,canvasWidth:document.querySelector('#unity-canvas').getBoundingClientRect().width,canvasHeight:document.querySelector('#unity-canvas').getBoundingClientRect().height}));if(data.viewport.width!==w||Math.abs(data.viewport.scale-1)>.01||Math.abs(data.viewport.canvasWidth-w)>.1||Math.abs(data.viewport.canvasHeight-h)>.1)throw Error('scaled or mismatched viewport '+JSON.stringify(data.viewport));return data;
 }
 async function tap(d,x,y){if(d.touch)await d.page.touchscreen.tap(x,y);else await d.page.mouse.click(x,y);await d.page.waitForTimeout(150);}
 async function open(d,slot){const points={8:[640,548],9:[704,452]};const [s,x,y]=d.geometry,serial=d.serial;await tap(d,x+points[slot][0]*s,y+points[slot][1]*s);await wait(d.page,()=>d.serial>serial,'menu '+slot);for(const a of d.menu.actions){const [id,ax,ay,aw,ah]=a;if(ax<0||ay<0||ax+aw>d.w+.2||ay+ah>d.h+.2||ah<44){await d.page.screenshot({path:path.join(out,'layout-failure.png')});throw Error('target outside viewport or below 44px '+a);}for(const b of d.menu.actions)if(b[0]>id&&Math.hypot(ax+aw/2-b[1]-b[3]/2,ay+ah/2-b[2]-b[4]/2)<(aw+b[3])/2-.2)throw Error('overlapping circular targets');if(d.info){const [ix,iy,iw,ih]=d.info,cx=ax+aw/2,cy=ay+ah/2;if(ix<0||iy<0||ix+iw>d.w+.2||iy+ih>d.h+.2||Math.hypot(cx-Math.max(ix,Math.min(cx,ix+iw)),cy-Math.max(iy,Math.min(cy,iy+ih)))<aw/2-.2)throw Error('info card obscures action '+id);}}}
 async function action(d,id,confirm=true){const a=d.menu.actions.find(a=>a[0]===id);if(!a)throw Error('missing action '+id);await tap(d,a[1]+a[3]/2,a[2]+a[4]/2);if(d.touch&&confirm){await d.page.waitForTimeout(250);await tap(d,a[1]+a[3]/2,a[2]+a[4]/2);}await d.page.waitForTimeout(1100);}
 async function done(d,name){if(d.errors.length)throw Error(d.errors.join('\n'));fs.writeFileSync(path.join(out,name+'.json'),JSON.stringify({state:d.state,viewport:d.viewport,messages:d.messages,errors:d.errors},null,2));summaries.push({name,state:d.state});console.log('PASS '+name);await d.context.close();}
 if(!process.env.R29_ONLY_DRAG){
 for(const [w,h,touch] of [[320,300,false],[800,500,false],[1280,720,false],[1920,1080,false],[390,844,true],[844,390,true]]){
  const d=await fixture(w,h,'',touch),slot=w===320?8:9;await open(d,slot);if(w===844){const c=d.menu.actions.find(a=>a[0]===15),top=d.menu.actions.find(a=>a[0]===0),cx=c[1]+c[3]/2,cy=c[2]+c[4]/2,[s,ox,oy]=d.geometry,radius=Math.hypot(top[1]+top[3]/2-cx,top[2]+top[4]/2-cy);if(Math.hypot(cx-ox-704*s,cy-oy-452*s)>radius+.2)throw Error('landscape ring detached from its slot');if(d.menu.actions.some(a=>a[2]<72)||d.info[1]<72)throw Error('landscape menu obscures HUD');}await d.page.screenshot({path:path.join(out,`build-${w}.png`)});
  if(touch){await action(d,0,false);if(d.state.units.length)throw Error('first touch purchased');if(!d.messages.some(t=>t==='SLOT_TOUCH_PREVIEW 0'))throw Error('no touch preview');await action(d,0,false);}else await action(d,0);
  if(d.state.gold!==935||d.state.units.length!==1)throw Error('placement failed '+JSON.stringify(d.state));await open(d,slot);await action(d,10);if(d.state.units[0][2]!==2)throw Error('upgrade failed');
  await open(d,slot);await d.page.screenshot({path:path.join(out,`branches-${w}.png`)});const branch=w===800||touch?12:11;await action(d,branch);if(d.state.units[0][2]!==3||d.state.units[0][3]!==branch-10||d.state.gold!==795)throw Error('branch failed '+JSON.stringify(d.state));await d.page.screenshot({path:path.join(out,`selected-${w}.png`)});
  await open(d,slot);if(w===320){await d.page.keyboard.press('Escape');await d.page.waitForTimeout(150);}else await action(d,15,false);await done(d,'flow-'+w);
 }
 for(const kind of [1,2,3,4]){const d=await fixture(1280,720,'&kind='+kind+'&level=2');await open(d,9);await action(d,12);if(d.state.units[0][3]!==2)throw Error('branch2 kind '+kind);await d.page.screenshot({path:path.join(out,'black-kind-'+kind+'.png')});await done(d,'branch-kind-'+kind);}
 for(const touch of [false,true]){const d=await fixture(1280,720,'&kind=0&level=2&slot=8&fighting=1&pause=1',touch);await open(d,8);const gold=d.state.gold;await action(d,13,false);if(d.state.units.length!==1||d.state.gold!==gold)throw Error('sale confirmation bypassed');await action(d,13,false);if(d.state.units.length!==0)throw Error('sale failed');await done(d,'sale-'+touch);}
 {const d=await fixture(1280,720,'&kind=1&level=3&fighting=1');await open(d,9);const time=d.state.time;await d.page.waitForTimeout(1400);if(d.state.time<=time)throw Error('menu paused combat');await action(d,14);if(d.state.units[0][0]!==9)throw Error('unit lost');await done(d,'running-priority');}
 {const d=await fixture(1280,720,'&kind=0&level=2&gold=45');await open(d,9);await action(d,11);if(d.state.gold!==45||d.state.units[0][2]!==2)throw Error('poor branch changed state');await d.page.screenshot({path:path.join(out,'poor-branch.png')});await done(d,'poor-branch');}
 for(const flag of ['stun','reload']){const d=await fixture(1280,720,'&kind=0&level=2&'+flag+'=1');await open(d,9);await d.page.waitForTimeout(1400);if(!d.messages.some(t=>t==='SLOT_CLOSED 9'))throw Error('menu did not close on '+flag);await done(d,'close-'+flag);}
 }
 function world(d,x,y){const [s,ox,oy]=d.geometry;return [ox+x*s,oy+y*s];}
 async function drag(d,from,to,cancel=false){
  if(d.touch){const cdp=await d.context.newCDPSession(d.page);const point=(x,y)=>({x,y,radiusX:2,radiusY:2,force:1,id:1});try{await cdp.send('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[point(...from)]});for(let i=1;i<=12;i++){await cdp.send('Input.dispatchTouchEvent',{type:'touchMove',touchPoints:[point(from[0]+(to[0]-from[0])*i/12,from[1]+(to[1]-from[1])*i/12)]});await d.page.waitForTimeout(30);}await cdp.send('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[]});}finally{await cdp.detach();}}
  else{await d.page.mouse.move(...from);await d.page.mouse.down();await d.page.mouse.move(...to,{steps:12});await d.page.waitForTimeout(180);if(cancel)await d.page.keyboard.press('Escape');await d.page.mouse.up();}
  await d.page.waitForTimeout(1200);
 }
 for(const touch of [false,true]){
  const d=await fixture(touch?390:1280,touch?844:720,'',touch);await open(d,9);const start=d.menu.actions.find(a=>a[0]===15),end=d.menu.actions.find(a=>a[0]===1);await drag(d,[start[1]+start[3]/2,start[2]+start[4]/2],[end[1]+end[3]/2,end[2]+end[4]/2]);if(d.state.units.length!==1||d.state.units[0][1]!==1||d.state.gold!==915)throw Error('menu drag failed '+JSON.stringify(d.state));if(d.messages.some(t=>t==='SLOT_TOUCH_PREVIEW 1'))throw Error('drag required preview');await done(d,'menu-drag-'+touch);
 }
 async function unitGesture(d,id,cancel=false){
  const from=world(d,704,452),serial=d.serial;let cdp;
  if(d.touch){cdp=await d.context.newCDPSession(d.page);await cdp.send('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[{x:from[0],y:from[1],id:1}]});}else{await d.page.mouse.move(...from);await d.page.mouse.down();}
  await wait(d.page,()=>d.serial>serial,'menu opens while held');const a=d.menu.actions.find(a=>a[0]===id),to=cancel?[5,80]:[a[1]+a[3]/2,a[2]+a[4]/2];
  if(d.touch){for(let i=1;i<=10;i++){await cdp.send('Input.dispatchTouchEvent',{type:'touchMove',touchPoints:[{x:from[0]+(to[0]-from[0])*i/10,y:from[1]+(to[1]-from[1])*i/10,id:1}]});await d.page.waitForTimeout(30);}await cdp.send('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[]});await cdp.detach();}
  else{await d.page.mouse.move(...to,{steps:12});await d.page.waitForTimeout(100);await d.page.mouse.up();}
  await d.page.waitForTimeout(1200);
 }
 for(const touch of [false,true])for(const mode of ['upgrade','branch','sale','cancel']){
  const d=await fixture(touch?390:1280,touch?844:720,'&kind=0&level='+(mode==='branch'?2:1),touch),gold=d.state.gold;
  await unitGesture(d,mode==='branch'?12:mode==='sale'?13:10,mode==='cancel');
  if(mode==='upgrade'&&(d.state.units[0][2]!==2||d.state.gold!==gold-50))throw Error('press-drag upgrade failed');
  if(mode==='branch'&&(d.state.units[0][3]!==2||d.state.gold!==gold-90))throw Error('press-drag branch failed');
  if(mode==='sale'&&(d.state.units.length||d.state.gold!==gold+65))throw Error('press-drag sale failed');
  if(mode==='cancel'&&(d.state.units[0][2]!==1||d.state.gold!==gold))throw Error('press-drag cancellation purchased');
  await d.page.screenshot({path:path.join(out,`held-${mode}-${touch}.png`)});await done(d,'unit-hold-'+mode+'-'+touch);
 }
 for(const touch of [false,true]){
  const d=await fixture(touch?390:1280,touch?844:720,'',touch);await d.page.keyboard.press('2');await open(d,9);await d.page.waitForTimeout(1100);if(d.state.units.length||d.state.gold!==1000)throw Error('empty click auto purchased after keyboard preselection');await action(d,15,false);if(d.messages.some(t=>t.startsWith('CARD_RECT|')))throw Error('removed troop cards still rendered');await done(d,'empty-no-purchase-'+touch);
 }
 for(const dragging of [false,true]){
  const d=await fixture(1280,720,'',false,true);if(d.state.rules!==3||d.state.gold!==200||d.state.units.length)throw Error('not fresh tutorial');
  const serial=d.serial;await tap(d,...world(d,320,218));await wait(d.page,()=>d.serial>serial,'tutorial slot menu');await d.page.waitForTimeout(1100);if(d.state.gold!==200||d.state.units.length)throw Error('tutorial empty click purchased');
  if(dragging){const start=d.menu.actions.find(a=>a[0]===15),end=d.menu.actions.find(a=>a[0]===0);await drag(d,[start[1]+start[3]/2,start[2]+start[4]/2],[end[1]+end[3]/2,end[2]+end[4]/2]);}else await action(d,0);
  if(d.state.units.length!==1||d.state.gold!==135||!d.messages.some(t=>t==='TUTORIAL_STAGE 3'))throw Error('tutorial menu purchase failed');await d.page.keyboard.press('Enter');await d.page.waitForTimeout(1100);if(d.state.phase!=='Fighting')throw Error('tutorial start failed');await done(d,'tutorial-map-'+dragging);
 }
 {const d=await fixture(1280,720);await d.page.keyboard.down('Escape');await d.page.keyboard.down('Escape');await d.page.keyboard.down('Escape');await d.page.keyboard.up('Escape');await d.page.waitForTimeout(300);if(d.messages.filter(t=>t==='TOOLS_OPEN').length!==1)throw Error('Esc did not open tools');await d.page.keyboard.press('Escape');await d.page.keyboard.press('Escape');await d.page.waitForTimeout(200);if(d.messages.filter(t=>t==='TOOLS_OPEN').length!==2)throw Error('Esc did not close/reopen tools');await d.page.screenshot({path:path.join(out,'menu-esc.png')});await done(d,'menu-escape');}
 fs.writeFileSync(path.join(out,'summary.json'),JSON.stringify(summaries,null,2));
}catch(e){console.error(e);process.exitCode=1;}finally{if(browser)await browser.close();server.close();}})();
