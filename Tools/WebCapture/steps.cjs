// Drives the debug Web build with real mouse and keyboard input and saves screenshots, in an isolated headless Edge.
// Read-only towards the project: it serves a build folder and writes only to the output folder you name.
// usage: node Tools/WebCapture/steps.cjs <jobs.json> <outDir> [jobName,jobName...]
//   WEBROOT=<folder>   build to serve (default Builds/Web). Copy the build first if another build may start meanwhile.
// jobs.json: [{"name":"camp","q":"fx-smoke=expedition-camp","size":[1280,720],"settle":5000,"log":"EXPEDITION",
//              "steps":[{"click":[x,y]},{"key":"Escape"},{"wait":500},{"until":"TEXT","timeout":60000},{"shot":"name","clip":[x,y,w,h]}]}]
//   A step may combine several actions; they run in the order click/key, until, wait, shot. "log" prints console lines containing it.
const fs = require('fs'), path = require('path'), http = require('http');
const {chromium} = require('C:/Users/snome/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const [jobsFile, out, onlyArg] = process.argv.slice(2);
if (!jobsFile || !out) { console.error('usage: node Tools/WebCapture/steps.cjs <jobs.json> <outDir> [names]'); process.exit(2); }
const root = path.resolve(process.env.WEBROOT || path.join(__dirname, '../../Builds/Web')), only = onlyArg ? onlyArg.split(',') : null;
const types = {'.html': 'text/html; charset=utf-8', '.js': 'application/javascript', '.wasm': 'application/wasm', '.data': 'application/octet-stream', '.css': 'text/css', '.png': 'image/png'};
const server = http.createServer((req, res) => {
  const rel = decodeURIComponent(new URL(req.url, 'http://127.0.0.1').pathname).replace(/^\/+/, ''), file = path.resolve(root, rel || 'index.html');
  if (!file.startsWith(root + path.sep) && file !== root) { res.writeHead(403).end(); return; }
  fs.readFile(file, (e, d) => { if (e) { res.writeHead(404).end(); return; } res.writeHead(200, {'Content-Type': types[path.extname(file)] || 'application/octet-stream', 'Cache-Control': 'no-store'}); res.end(d); });
});
const jobs = JSON.parse(fs.readFileSync(jobsFile, 'utf8')).filter(j => !only || only.includes(j.name));
const fit = (page, w, h) => page.evaluate(({w, h}) => { const c = document.querySelector('#unity-container'), v = document.querySelector('#unity-canvas'), f = document.querySelector('#unity-fullscreen-container'), s = 'width:' + w + 'px;height:' + h + 'px'; c.style.cssText = 'position:fixed;left:0;top:0;transform:none;' + s; f.style.cssText = s; v.style.cssText = s; document.querySelector('#unity-footer').style.display = 'none'; }, {w, h});
(async () => {
  let browser, failed = false;
  try {
    fs.mkdirSync(out, {recursive: true});
    await new Promise(r => server.listen(0, '127.0.0.1', r));
    browser = await chromium.launch({executablePath: 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe', headless: true, args: ['--use-gl=angle', '--use-angle=swiftshader', '--enable-webgl', '--enable-unsafe-swiftshader']});
    for (const job of jobs) {
      const [w, h] = job.size, ctx = await browser.newContext({viewport: {width: w, height: h}}), page = await ctx.newPage(), msgs = [], errs = [];
      page.on('pageerror', e => errs.push(e.message));
      page.on('console', m => { msgs.push(m.text()); if (m.type() === 'error') errs.push(m.text()); });
      await page.goto('http://127.0.0.1:' + server.address().port + '/' + (job.q ? '?' + job.q : ''), {waitUntil: 'domcontentloaded'});
      await page.waitForFunction(() => [...document.querySelectorAll('#unity-build-title button')].some(b => b.textContent === 'Unload'), null, {timeout: 120000});
      await fit(page, w, h);
      await page.waitForTimeout(job.settle || 5000);
      for (const s of job.steps) {
        if (s.click) await page.mouse.click(s.click[0], s.click[1]);
        if (s.key) await page.keyboard.press(s.key);
        if (s.until) { const t0 = Date.now(); while (!msgs.some(m => m.includes(s.until)) && Date.now() - t0 < (s.timeout || 60000)) await page.waitForTimeout(100); if (!msgs.some(m => m.includes(s.until))) { console.log(`${job.name}: timed out waiting for ${s.until}`); failed = true; } }
        if (s.wait) await page.waitForTimeout(s.wait);
        if (s.shot) await page.screenshot(Object.assign({path: path.join(out, `${job.name}-${s.shot}.png`)}, s.clip ? {clip: {x: s.clip[0], y: s.clip[1], width: s.clip[2], height: s.clip[3]}} : {}));
      }
      console.log(`${job.name} ${w}x${h} errors=${errs.length}${errs.length ? ' ' + errs.slice(0, 2).join(' | ') : ''}`);
      if (job.log) for (const m of msgs) if (m.includes(job.log)) console.log('  ' + m.slice(0, 300));
      if (errs.length) failed = true;
      await ctx.close();
    }
  } catch (e) { console.error(e); failed = true; } finally { if (browser) await browser.close(); server.close(); }
  process.exitCode = failed ? 1 : 0;
})();
