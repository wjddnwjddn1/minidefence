// r38: portrait gameplay retired; low landscape fixtures replace portrait cases.
// r37 (Claude): the roguelite run on the debug Web build, with real mouse input.
//   entry button -> run battle, status window, abandon; camp -> choose a boon -> next field (wide and narrow layouts);
//   finished runs -> new run or back to free battles. Fixtures only set up the state; every transition is clicked.
// Run: node Tests/ExpeditionWeb.cjs     (serves bin/TacticalWeb, writes screenshots to bin/r37-38-expedition-evidence)
const fs = require('fs'), path = require('path'), http = require('http');
const {chromium} = require('C:/Users/snome/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root = path.resolve(__dirname, 'bin/TacticalWeb'), out = path.resolve(__dirname, 'bin/r37-38-expedition-evidence');
const mime = {'.html': 'text/html; charset=utf-8', '.js': 'application/javascript', '.wasm': 'application/wasm', '.data': 'application/octet-stream', '.css': 'text/css'};
const server = http.createServer((req, res) => {
  const file = path.resolve(root, decodeURIComponent(new URL(req.url, 'http://localhost').pathname).replace(/^\/+/, '') || 'index.html');
  if (!file.startsWith(root + path.sep) && file !== root) return res.writeHead(403).end();
  fs.readFile(file, (e, d) => { if (e) return res.writeHead(404).end(); res.writeHead(200, {'Content-Type': mime[path.extname(file)] || 'application/octet-stream', 'Cache-Control': 'no-store'}); res.end(d); });
});
let checks = 0;
const check = (ok, what) => { checks++; if (!ok) throw new Error(what); };
// EXPEDITION|state|stage|warmth|offers|mode|field|hp|gold|boons
const parse = line => { const p = line.trim().split('|'); return {state: p[1], stage: +p[2], warmth: +p[3], offers: p[4] ? p[4].split(',').map(Number) : [], mode: p[5], field: +p[6], hp: +p[7], gold: +p[8], boons: p[9] || '', phase:p[10], time:+p[11], wave:+p[12], paused:p[13]==='True'}; };

(async () => {
  let browser;
  try {
    await new Promise(r => server.listen(0, '127.0.0.1', r));
    browser = await chromium.launch({executablePath: 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe', headless: true, args: ['--use-gl=angle', '--use-angle=swiftshader', '--enable-webgl', '--enable-unsafe-swiftshader']});
    const open = async (query, w, h) => {
      const context = await browser.newContext({viewport: {width: w, height: h}}), page = await context.newPage(), lines = [], errors = [], rects = {}, traces=[];
      page.on('console', m => { const t = m.text();if(traces.length<500)traces.push(t.trim()); if (t.startsWith('EXPEDITION|')) lines.push(parse(t)); if(t.startsWith('RUN_RECT|')){const a=t.trim().split('|');rects[a[1]]=a[2].split(',').map(Number);}if (m.type() === 'error') errors.push(t); });
      page.on('pageerror', e => errors.push(e.message));
      await page.goto('http://127.0.0.1:' + server.address().port + '/' + (query ? '?' + query : ''), {waitUntil: 'domcontentloaded'});
      await page.waitForFunction(() => [...document.querySelectorAll('#unity-build-title button')].some(b => b.textContent === 'Unload'), null, {timeout: 120000});
      await page.evaluate(({w, h}) => { const c = document.querySelector('#unity-container'), v = document.querySelector('#unity-canvas'), f = document.querySelector('#unity-fullscreen-container'), s = 'width:' + w + 'px;height:' + h + 'px'; c.style.cssText = 'position:fixed;left:0;top:0;transform:none;' + s; f.style.cssText = s; v.style.cssText = s; document.querySelector('#unity-footer').style.display = 'none'; }, {w, h});
      const last = () => lines[lines.length - 1];
      const until = async (test, what) => { const t0 = Date.now(); while (!(lines.length && test(last()))) { if (Date.now() - t0 > 30000) throw new Error(what + ' (last: ' + JSON.stringify(last()) + ')'); await page.waitForTimeout(100); } return last(); };
      return {context, page, lines, errors, rects, traces, last, until};
    };
    // the camp window: same geometry as BattleView.ExpeditionPanel
    const campCard = (w, h, i) => {
      const wide = w >= 640 && h >= 420, width = Math.min(w - 24, wide ? 700 : 480), height = Math.min(h - 16, wide ? 324 : 340), x = (w - width) / 2, y = (h - height) / 2, left = x + 18, inner = width - 36, top = y + 94, areaH = height - 94 - 14;
      return wide ? [left + i * (inner + 10) / 3 + (inner - 20) / 6, top + areaH / 2] : [left + inner / 2, top + i * (areaH + 6) / 3 + (areaH - 12) / 6];
    };
    const panelButton = (w, h, right) => { const wide = w >= 640 && h >= 420, width = Math.min(w - 24, wide ? 700 : 480), height = Math.min(h - 16, 250), x = (w - width) / 2, y = (h - height) / 2, inner = width - 36, half = (inner - 8) / 2; return [x + 18 + (right ? half + 8 + half / 2 : half / 2), y + height - 44 + 16]; };

    const regionCheck=t=>{const r=t.rects.panel;check(r&&r[0]>=0&&r[1]>=0&&r[0]+r[2]<=t.page.viewportSize().width+.1&&r[1]+r[3]<=t.page.viewportSize().height+.1,'panel within viewport');for(const [name,b] of Object.entries(t.rects))if(name.startsWith('card')||name==='left'||name==='right'){check(b[0]>=r[0]&&b[1]>=r[1]&&b[0]+b[2]<=r[0]+r[2]+.1&&b[1]+b[3]<=r[1]+r[3]+.1,'control inside panel '+name);}};
    if(!process.env.RUN_RESUME_ONLY){
    // 1. camp, wide: three offers, click the second card, the next battle carries that boon
    for (const [w, h, pick] of [[1280, 720, 1], [844, 390, 2], [740, 360, 0]]) {
      const t = await open('fx-smoke=expedition-camp&run-review=1', w, h);
      const camp = await t.until(l => l.state === 'Camp', 'camp did not open ' + w);
      check(camp.stage === 0 && camp.warmth === 91 && camp.offers.length === 3 && new Set(camp.offers).size === 3, 'camp state ' + w + ': ' + JSON.stringify(camp));
      await t.page.waitForTimeout(1200);
      if (w === 1280) await t.page.screenshot({path: path.join(out, 'expedition-camp-1280.png')});
      if (w === 390) await t.page.screenshot({path: path.join(out, 'expedition-camp-390.png')});
      regionCheck(t);const [cx, cy] = campCard(w, h, pick);
      await t.page.mouse.click(cx, cy);
      const next = await t.until(l => l.state === 'Battle' && l.stage === 1, 'boon click did not start the next stage ' + w);
      const boon = camp.offers[pick];
      check(next.boons[boon] === '1' && [...next.boons].filter(c => c !== '0').length === 1, 'the clicked boon was taken ' + w + ': offers ' + camp.offers + ' boons ' + next.boons);
      check(next.mode === 'Expedition' && next.field === 1 && next.hp === (boon === 7 ? 100 : 91) && next.gold === 200 + (boon === 8 ? 100 : 0), 'next battle carries the run ' + w + ': ' + JSON.stringify(next));
      check(t.errors.length === 0, 'browser errors ' + w + ': ' + t.errors.join(' | '));
      await t.context.close();
    }
    // 2. a won run: result window, then a new run starts from the gate with a full ember
    for(const [w,h] of [[1280,720],[844,390],[740,360]]){
      const t = await open('fx-smoke=expedition-won&run-review=1', w, h);
      const won = await t.until(l => l.state === 'Won', 'won run did not show');
      check(won.stage === 3 && won.warmth > 0 && [...won.boons].reduce((n, c) => n + (+c), 0) === 3, 'won state: ' + JSON.stringify(won));
      await t.page.waitForTimeout(1200);
      await t.page.screenshot({path: path.join(out, 'expedition-won-'+w+'.png')});
      regionCheck(t);await t.page.mouse.click(...panelButton(w, h, false));
      const fresh = await t.until(l => l.state === 'Battle' && l.stage === 0 && l.warmth === 100, 'new run did not start');
      check(fresh.field === 0 && fresh.boons === '000000000' && fresh.gold === 200, 'fresh run: ' + JSON.stringify(fresh));
      check(t.errors.length === 0, 'browser errors won: ' + t.errors.join(' | '));
      await t.context.close();
    }
    // 3. a lost run: result window, then back to free battles
    for(const [w,h] of [[1280,720],[844,390],[740,360]]){
      const t = await open('fx-smoke=expedition-lost&run-review=1', w, h);
      const lost = await t.until(l => l.state === 'Lost', 'lost run did not show');
      check(lost.stage === 0 && lost.warmth === 0, 'lost state: ' + JSON.stringify(lost));
      await t.page.waitForTimeout(1200);
      await t.page.screenshot({path: path.join(out, 'expedition-lost-'+w+'.png')});
      regionCheck(t);await t.page.mouse.click(...panelButton(w, h, true));
      const free = await t.until(l => l.state === 'Lost' && l.mode==='Normal', 'did not return to free battles');
      check(free.mode === 'Normal' && free.field === 0, 'free battle: ' + JSON.stringify(free));
      check(t.errors.length === 0, 'browser errors lost: ' + t.errors.join(' | '));
      await t.context.close();
    }
    // 4. entry from an ordinary preparation, status window, abandon with a second click
    for(const [w,h] of [[1280,720],[844,390],[740,360]]){
      const t = await open('run-review=1', w, h);
      await t.page.waitForTimeout(4000);
      check(!t.lines.some(l=>l.state!=='None'), 'no run before the button is pressed');
      const entry=t.rects.entry;check(entry,'entry geometry');await t.page.mouse.click(entry[0]+entry[2]/2,entry[1]+entry[3]/2);
      const started = await t.until(l => l.state === 'Battle', 'entry button did not start a run');
      check(started.mode === 'Expedition' && started.stage === 0 && started.warmth === 100 && started.field === 0, 'started: ' + JSON.stringify(started));
      await t.page.waitForTimeout(600);
      await t.page.mouse.click(entry[0]+entry[2]/2,entry[1]+entry[3]/2);              // the same spot now opens the status window
      await t.page.waitForTimeout(600);
      await t.page.screenshot({path: path.join(out, 'expedition-status-'+w+'.png')});
      await t.page.mouse.click(...panelButton(w, h, false));   // abandon: first click only arms it
      await t.page.waitForTimeout(400);
      check(t.last().state === 'Battle', 'one click must not abandon the run');
      await t.page.mouse.click(...panelButton(w, h, false));
      const gone = await t.until(l => l.state === 'Lost' && l.mode==='Normal', 'second click did not record the loss');
      check(gone.mode === 'Normal', 'back to a normal battle: ' + JSON.stringify(gone));
      check(t.errors.length === 0, 'browser errors entry: ' + t.errors.join(' | '));
      await t.context.close();
    }
    }
    for(const [w,h] of [[1280,720],[844,390],[740,360]]){
      const t=await open('fx-smoke=expedition-play&fx-smoke=remember&run-review=1',w,h);
      await t.page.waitForTimeout(2500);await t.page.keyboard.press('F7');
      const saved=await t.until(l=>l.phase==='Fighting'&&l.paused&&l.hp===85,'checkpoint capture');await t.page.waitForTimeout(1500);
      t.lines.length=0;for(const k of Object.keys(t.rects))delete t.rects[k];
      await t.page.goto('http://127.0.0.1:'+server.address().port+'/?fx-smoke=remember&run-review=1&review-flow=1');
      await t.page.waitForFunction(()=>[...document.querySelectorAll('#unity-build-title button')].some(b=>b.textContent==='Unload'),null,{timeout:120000});
      await t.page.evaluate(({w,h})=>{document.querySelector('#unity-container').style.cssText=`position:fixed;left:0;top:0;transform:none;width:${w}px;height:${h}px`;document.querySelector('#unity-fullscreen-container').style.cssText=`width:${w}px;height:${h}px`;document.querySelector('#unity-canvas').style.cssText=`width:${w}px;height:${h}px`;document.querySelector('#unity-footer').style.display='none';},{w,h});
      await t.page.waitForTimeout(1000);let ready=Date.now()+30000;while(!t.rects.entry&&Date.now()<ready)await t.page.waitForTimeout(100);const r=t.rects.entry;if(!r)fs.writeFileSync(path.join(out,'resume-failure.json'),JSON.stringify({lines:t.lines,errors:t.errors,rects:t.rects,traces:t.traces},null,2));check(r&&r[2]>=64&&r[3]>=36,'entry exists at '+w+' '+JSON.stringify(t.rects));await t.page.mouse.click(r[0]+r[2]/2,r[1]+r[3]/2);
      const resumed=await t.until(l=>l.phase==='Fighting'&&l.paused,'real reload must resume combat paused');
      check(resumed.hp===saved.hp&&resumed.gold===saved.gold&&Math.abs(resumed.time-saved.time)<.0002&&resumed.wave===saved.wave,'checkpoint did not reset preparation '+w);
      check(t.errors.length===0,'resume errors '+t.errors.join('|'));await t.page.screenshot({path:path.join(out,'expedition-resume-'+w+'.png')});fs.writeFileSync(path.join(out,'resume-'+w+'.json'),JSON.stringify({saved,resumed,rects:t.rects},null,2));await t.context.close();
    }
    console.log('PASS ' + checks + ' expedition Web checks: camp choice at 1280/844/740, won and lost results, entry, status and abandon');
  } catch (e) { console.error(e); process.exitCode = 1; } finally { if (browser) await browser.close(); server.close(); }
})();
