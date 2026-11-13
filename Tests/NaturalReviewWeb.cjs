// Natural UI flow on the gate with natural gold: place troops from the slot ring menu, start, keep buying and
// upgrading through the ring while waves come, and reach wave 3. Scripted mouse/keyboard, not human play.
// r47 (Claude): rewritten for the current UI. Rules 4 is the default start; an empty slot opens the troop ring
// (SLOT_MENU) instead of taking a number-key troop; the board transform is read from SLOT_VIEW; the smith is not
// offered on the gate, so the plan uses no smith.
// Run: node Tests/NaturalReviewWeb.cjs     (serves bin/TacticalWeb, writes evidence to bin/natural-review-evidence)
const fs = require('fs'), path = require('path'), http = require('http');
const {chromium} = require('C:/Users/snome/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root = path.resolve(__dirname, 'bin/TacticalWeb'), out = path.resolve(__dirname, 'bin/natural-review-evidence');
const types = {'.html': 'text/html; charset=utf-8', '.js': 'application/javascript', '.wasm': 'application/wasm', '.data': 'application/octet-stream', '.css': 'text/css'};
const server = http.createServer((req, res) => {
  const relative = decodeURIComponent(new URL(req.url, 'http://127.0.0.1').pathname).replace(/^\/+/, ''), file = path.resolve(root, relative || 'index.html');
  if (!file.startsWith(root + path.sep)) { res.writeHead(403).end(); return; }
  fs.readFile(file, (error, data) => { if (error) { res.writeHead(404).end(); return; } res.writeHead(200, {'Content-Type': types[path.extname(file)] || 'application/octet-stream', 'Cache-Control': 'no-store'}); res.end(data); });
});
(async () => {
  let browser;
  try {
    fs.mkdirSync(out, {recursive: true});
    await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
    browser = await chromium.launch({executablePath: 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe', headless: true, args: ['--use-gl=angle', '--use-angle=swiftshader', '--enable-webgl', '--enable-unsafe-swiftshader']});
    const context = await browser.newContext({viewport: {width: 1280, height: 720}}), page = await context.newPage(), messages = [], errors = [], actions = [];
    let state, view, menu;
    page.on('console', m => {
      const text = m.text().trim(); messages.push(text); if (m.type() === 'error') errors.push(text);
      if (text.startsWith('REVIEW_STATE|')) { const p = text.split('|'); state = {rules: +p[1], phase: p[2], wave: +p[3], time: +p[4], gold: +p[5], enemies: +p[6], units: p[7] ? p[7].split(';').filter(Boolean).map(v => { const a = v.split(':').map(Number); return {slot: a[0], kind: a[1], level: a[2], branch: a[3]}; }) : []}; }
      if (text.startsWith('SLOT_VIEW|')) view = text.split('|').slice(1).map(Number);
      if (text.startsWith('SLOT_MENU|')) { const p = text.split('|'); menu = {slot: +p[1], actions: p[3].split(';').filter(Boolean).map(v => v.split(',').map(Number))}; }
    });
    page.on('pageerror', e => errors.push(e.message));
    await page.goto('http://127.0.0.1:' + server.address().port + '/?fx-smoke=autosave&review-flow=1', {waitUntil: 'domcontentloaded'});
    await page.waitForFunction(() => [...document.querySelectorAll('#unity-build-title button')].some(b => b.textContent === 'Unload'), null, {timeout: 120000});
    await page.evaluate(() => { const c = document.querySelector('#unity-container'), v = document.querySelector('#unity-canvas'), f = document.querySelector('#unity-fullscreen-container'), s = 'width:1280px;height:720px'; c.style.cssText = 'position:fixed;left:0;top:0;transform:none;' + s; f.style.cssText = s; v.style.cssText = s; document.querySelector('#unity-footer').style.display = 'none'; });
    for (let i = 0; i < 60 && !(state && view); i++) await page.waitForTimeout(250);
    if (!state || state.rules !== 4 || state.gold !== 200 || state.units.length !== 0) throw Error('Expected untouched normal start: ' + JSON.stringify(state));
    await page.screenshot({path: path.join(out, 'first-screen.png')});

    const slots = [[320,218],[480,218],[640,218],[800,218],[960,218],[320,550],[480,550],[640,550],[800,550],[960,550],[278,330],[278,438],[1002,330],[1002,438]];
    const point = slot => [view[1] + slots[slot][0] * view[0], view[2] + slots[slot][1] * view[0]];
    // open the ring on a slot and press one of its actions (0-4 troops, 10 level 2, 11/12 branches)
    async function ring(slot, id) {
      menu = null; await page.mouse.click(...point(slot));
      for (let i = 0; i < 20 && !(menu && menu.slot === slot); i++) await page.waitForTimeout(100);
      const a = menu && menu.slot === slot ? menu.actions.find(v => v[0] === id) : null;
      if (!a) { await page.keyboard.press('Escape'); return false; }
      await page.mouse.click(a[1] + a[3] / 2, a[2] + a[4] / 2); await page.waitForTimeout(700); return true;
    }
    async function place(kind, slot) { const ok = await ring(slot, kind); actions.push({time: state.time, action: 'place', kind, slot, ok, placed: state.units.some(u => u.slot === slot)}); }
    await page.focus('#unity-canvas');
    await place(0, 0); await place(0, 10);
    if (state.units.length !== 2) throw Error('Natural placement failed ' + JSON.stringify(state) + ' ' + JSON.stringify(actions));
    await page.keyboard.press('Enter'); await page.waitForTimeout(1500);
    if (!state || state.phase !== 'Fighting' || state.units.length !== 2) throw Error('Natural start UI failed ' + JSON.stringify(state));

    const planned = [[1,1],[2,3],[3,4],[1,2]], cost = [65,85,105,90,90], start = Date.now();
    let lastWave = 0;
    while (Date.now() - start < 240000 && state.phase === 'Fighting' && state.wave < 3) {
      if (state.wave !== lastWave) { lastWave = state.wave; await page.screenshot({path: path.join(out, 'wave-' + lastWave + '.png')}); console.log('REVIEW wave ' + state.wave + ' time ' + state.time + ' gold ' + state.gold + ' enemies ' + state.enemies); }
      const missing = planned.find(([kind, slot]) => !state.units.some(u => u.slot === slot));
      if (missing && state.gold >= cost[missing[0]]) await place(...missing);
      else {
        const u = state.units.find(u => u.level < 3 && state.gold >= (u.level === 1 ? 50 : 90));
        if (u) { const ok = await ring(u.slot, u.level === 1 ? 10 : 11); const now = state.units.find(v => v.slot === u.slot); actions.push({time: state.time, action: 'upgrade', slot: u.slot, from: u.level, to: now && now.level, applied: !!(ok && now && now.level > u.level)}); }
      }
      await page.waitForTimeout(800);
    }
    await page.screenshot({path: path.join(out, 'last-screen.png')});
    fs.writeFileSync(path.join(out, 'report.json'), JSON.stringify({state, actions, errors, limits: 'Scripted mouse/keyboard with natural gold. Not human enjoyment or audio verification.'}, null, 2));
    if (errors.length) throw Error(errors.join('\n'));
    if (state.wave < 3) throw Error('Did not reach wave 3: ' + JSON.stringify(state));
    if (!actions.some(a => a.action === 'upgrade' && a.applied)) throw Error('no upgrade went through the ring: ' + JSON.stringify(actions.slice(-6)));
    console.log('PASS natural UI flow: ' + actions.filter(a => a.action === 'place' && a.placed).length + ' placed, ' + actions.filter(a => a.applied).length + ' upgrades, wave ' + state.wave);
    await context.close();
  } catch (e) { console.error(e.message || e); process.exitCode = 1; }
  finally { if (browser) await browser.close(); server.close(); }
})();
