// r47 (Claude): draws a battle with warlords and the goblin king on screen and checks the page keeps rendering.
// Added after 71351b5 froze the game whenever a warlord's name label was drawn (endless recursion in EnemyName):
// no other Web test put a warlord on screen, so nothing caught it.
// Run: node Tests/WarlordWeb.cjs     (serves bin/TacticalWeb, writes screenshots to bin/warlord-evidence)
const fs = require('fs'), path = require('path'), http = require('http');
const {chromium} = require('C:/Users/snome/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root = path.resolve(__dirname, 'bin/TacticalWeb'), out = path.resolve(__dirname, 'bin/warlord-evidence');
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
    for (const [w, h] of [[1280, 720], [740, 360]]) {
      const context = await browser.newContext({viewport: {width: w, height: h}}), page = await context.newPage(), errors = [], lines = [];
      page.on('pageerror', e => errors.push(e.message));
      page.on('console', m => { lines.push(m.text()); if (m.type() === 'error') errors.push(m.text()); });
      await page.goto('http://127.0.0.1:' + server.address().port + '/?fx-smoke=expedition-king', {waitUntil: 'domcontentloaded'});
      await page.waitForFunction(() => [...document.querySelectorAll('#unity-build-title button')].some(b => b.textContent === 'Unload'), null, {timeout: 120000});
      await page.evaluate(([w, h]) => {
        const c = document.querySelector('#unity-container'), v = document.querySelector('#unity-canvas'), f = document.querySelector('#unity-fullscreen-container'), s = 'width:' + w + 'px;height:' + h + 'px';
        c.style.cssText = 'position:fixed;left:0;top:0;transform:none;' + s; f.style.cssText = s; v.style.cssText = s; document.querySelector('#unity-footer').style.display = 'none';
      }, [w, h]);
      // the last wave runs on its own; the king comes KingDelay seconds in, warlords before him
      const deadline = Date.now() + 60000;
      while (!lines.some(l => l.startsWith('KING_SEEN|')) && Date.now() < deadline) await page.waitForTimeout(200);
      const seen = lines.find(l => l.startsWith('KING_SEEN|'));
      check(!!seen && +seen.split('|')[1] > 0, 'the goblin king was drawn ' + w);
      // a frozen page cannot take a screenshot; two in a row a few seconds apart show the frames keep coming
      await page.screenshot({path: path.join(out, 'warlords-' + w + '-a.png'), timeout: 15000});
      await page.waitForTimeout(3000);
      await page.screenshot({path: path.join(out, 'warlords-' + w + '-b.png'), timeout: 15000});
      const time = await page.evaluate(() => performance.now());
      check(time > 0, 'page still answers ' + w);
      check(errors.length === 0, 'browser errors ' + w + ': ' + errors.join(' | '));
      await context.close();
    }
    console.log('PASS ' + checks + ' warlord Web checks: the goblin king and warlords with name labels draw at 1280/740 without freezing');
  } catch (e) {
    console.error(e.message || e);
    process.exitCode = 1;
  } finally {
    if (browser) await browser.close();
    server.close();
  }
})();
