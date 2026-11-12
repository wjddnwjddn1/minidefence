// Puts the same crop of several screenshots side by side with their file names, optionally enlarged (nearest neighbour).
// usage: node Tools/WebCapture/montage.cjs out.png x y w h scale in1.png in2.png ...
const fs = require('fs'), path = require('path');
const {chromium} = require('C:/Users/snome/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
(async () => {
  const [out, x, y, w, h, s, ...ins] = process.argv.slice(2);
  if (!ins.length) { console.error('usage: node Tools/WebCapture/montage.cjs out.png x y w h scale in1.png ...'); process.exit(2); }
  const b = await chromium.launch({executablePath: 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe', headless: true});
  const p = await b.newPage();
  await p.setContent('<canvas id=c></canvas>');
  const data = await p.evaluate(async ([imgs, names, X, Y, W, H, S]) => {
    const load = d => new Promise(r => { const i = new Image(); i.onload = () => r(i); i.src = 'data:image/png;base64,' + d; });
    const c = document.getElementById('c'), lab = 14;
    c.width = imgs.length * (W * S + 4); c.height = H * S + lab;
    const g = c.getContext('2d'); g.imageSmoothingEnabled = false; g.fillStyle = '#111'; g.fillRect(0, 0, c.width, c.height);
    for (let i = 0; i < imgs.length; i++) { const im = await load(imgs[i]), x0 = i * (W * S + 4); g.fillStyle = '#fff'; g.font = '11px sans-serif'; g.fillText(names[i], x0 + 3, 11); g.drawImage(im, X, Y, W, H, x0, lab, W * S, H * S); }
    return c.toDataURL('image/png').split(',')[1];
  }, [ins.map(f => fs.readFileSync(f).toString('base64')), ins.map(f => path.basename(f, '.png')), +x, +y, +w, +h, +s]);
  fs.writeFileSync(out, Buffer.from(data, 'base64'));
  await b.close();
})();
