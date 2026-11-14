// r47 (Claude): mastery through the slot ring in an expedition battle, and that it survives a page reload.
// Sword on slot 0: branch (ring action 11) -> the ring offers mastery (16) -> buy it -> the ring no longer offers it
// -> reload -> resume the run -> still level 3 and the ring still has no mastery to buy.
// Run: node Tests/MasteryWeb.cjs     (serves bin/TacticalWeb, writes screenshots to bin/mastery-evidence)
const fs = require('fs'), path = require('path'), http = require('http');
const {chromium} = require('C:/Users/snome/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root = path.resolve(__dirname, 'bin/TacticalWeb'), out = path.resolve(__dirname, 'bin/mastery-evidence');
const types = {'.html': 'text/html; charset=utf-8', '.js': 'application/javascript', '.wasm': 'application/wasm', '.data': 'application/octet-stream', '.css': 'text/css'};
const server = http.createServer((req, res) => {
  const rel = decodeURIComponent(new URL(req.url, 'http://127.0.0.1').pathname).replace(/^\/+/, ''), f = path.resolve(root, rel || 'index.html');
  if (!f.startsWith(root + path.sep) && f !== root) { res.writeHead(403).end(); return; }
  fs.readFile(f, (e, data) => { if (e) { res.writeHead(404).end(); return; } res.writeHead(200, {'Content-Type': types[path.extname(f)] || 'application/octet-stream', 'Cache-Control': 'no-store'}); res.end(data); });
});
let checks = 0;
const check = (ok, what) => { checks++; if (!ok) throw new Error('FAIL ' + what); };
(async () => {
  let browser;
  try {
    fs.mkdirSync(out, {recursive: true});
    await new Promise(r => server.listen(0, '127.0.0.1', r));
    browser = await chromium.launch({executablePath: 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe', headless: true, args: ['--use-gl=angle', '--use-angle=swiftshader', '--enable-webgl', '--enable-unsafe-swiftshader']});
    const context = await browser.newContext({viewport: {width: 1280, height: 720}}), page = await context.newPage(), errors = [], rects = {};
    let state, view, menu;
    page.on('console', m => {
      const t = m.text().trim(); if (m.type() === 'error') errors.push(t);
      if (t.startsWith('REVIEW_STATE|')) { const p = t.split('|'); state = {phase: p[2], gold: +p[5], units: p[7] ? p[7].split(';').filter(Boolean).map(v => v.split(':').map(Number)) : []}; }
      if (t.startsWith('SLOT_VIEW|')) view = t.split('|').slice(1).map(Number);
      if (t.startsWith('SLOT_MENU|')) { const p = t.split('|'); menu = {slot: +p[1], actions: p[3].split(';').filter(Boolean).map(v => v.split(',').map(Number))}; }
      if (t.startsWith('RUN_RECT|')) { const a = t.split('|'); rects[a[1]] = a[2].split(',').map(Number); }
    });
    page.on('pageerror', e => errors.push(e.message));
    const load = async query => {
      await page.goto('http://127.0.0.1:' + server.address().port + '/?' + query, {waitUntil: 'domcontentloaded'});
      await page.waitForFunction(() => [...document.querySelectorAll('#unity-build-title button')].some(b => b.textContent === 'Unload'), null, {timeout: 120000});
      await page.evaluate(() => { const c = document.querySelector('#unity-container'), v = document.querySelector('#unity-canvas'), f = document.querySelector('#unity-fullscreen-container'), s = 'width:1280px;height:720px'; c.style.cssText = 'position:fixed;left:0;top:0;transform:none;' + s; f.style.cssText = s; v.style.cssText = s; document.querySelector('#unity-footer').style.display = 'none'; });
    };
    const slot0 = () => [view[1] + 320 * view[0], view[2] + 218 * view[0]];
    const sword = () => state && state.units.find(u => u[0] === 0);
    async function openRing() { menu = null; await page.mouse.click(...slot0()); for (let i = 0; i < 20 && !(menu && menu.slot === 0); i++) await page.waitForTimeout(100); return menu && menu.slot === 0 ? menu.actions.map(a => a[0]) : null; }
    async function press(id) { const a = menu.actions.find(v => v[0] === id); await page.mouse.click(a[1] + a[3] / 2, a[2] + a[4] / 2); await page.waitForTimeout(700); }

    await load('fx-smoke=expedition-play&run-review=1&review-flow=1&fx-smoke=remember');
    for (let i = 0; i < 80 && !(state && view && state.phase === 'Fighting'); i++) await page.waitForTimeout(250);
    check(state && state.phase === 'Fighting' && sword() && sword()[1] === 0 && sword()[2] === 2, 'a level 2 sword on slot 0 in a fighting expedition: ' + JSON.stringify(sword()));
    await page.keyboard.press('p'); await page.waitForTimeout(300);
    let ids = await openRing(); check(ids && ids.includes(11) && !ids.includes(16), 'level 2 ring offers the branch, not mastery: ' + ids);
    await press(11);
    check(sword()[2] === 3 && sword()[3] === 1, 'branch bought: ' + JSON.stringify(sword()));
    ids = await openRing(); check(ids && ids.includes(16), 'level 3 ring in an expedition offers mastery: ' + ids);
    const gold = state.gold; await page.screenshot({path: path.join(out, 'mastery-offer.png')});
    await press(16);
    check(state.gold === gold - 150, 'mastery costs 150 gold: ' + gold + ' -> ' + state.gold);
    ids = await openRing(); check(ids && !ids.includes(16), 'mastery is bought once: ' + ids);
    await page.keyboard.press('Escape'); await page.keyboard.press('p'); await page.waitForTimeout(2500); // let the run checkpoint save

    await load('fx-smoke=remember&run-review=1&review-flow=1');
    for (let i = 0; i < 250 && !rects.entry; i++) await page.waitForTimeout(100);
    check(!!rects.entry, 'resume entry after a reload');
    state = null; await page.mouse.click(rects.entry[0] + rects.entry[2] / 2, rects.entry[1] + rects.entry[3] / 2);
    for (let i = 0; i < 80 && !(state && state.phase === 'Fighting' && sword()); i++) await page.waitForTimeout(250);
    check(sword() && sword()[2] === 3 && sword()[3] === 1, 'the sword is still level 3 after the reload: ' + JSON.stringify(sword()));
    ids = await openRing(); check(ids && ids.length > 0 && !ids.includes(16), 'mastery is still bought after the reload: ' + ids);
    await page.screenshot({path: path.join(out, 'mastery-restored.png')});
    check(errors.length === 0, 'browser errors: ' + errors.join(' | '));
    console.log('PASS ' + checks + ' mastery Web checks: bought through the ring once for 150 gold and kept across a reload');
  } catch (e) { console.error(e.message || e); process.exitCode = 1; }
  finally { if (browser) await browser.close(); server.close(); }
})();
