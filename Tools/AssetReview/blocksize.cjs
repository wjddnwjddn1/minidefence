// Detects whether an upscaled pixel-art image sits on an integer block grid: histogram of same-colour run lengths
// along rows and columns. A clean N-times nearest-neighbour upscale gives runs that are all multiples of N.
// usage: node Tools/AssetReview/blocksize.cjs image.png
const fs = require('fs');
const {chromium} = require('C:/Users/snome/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
(async () => {
  const b = await chromium.launch({executablePath: 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe', headless: true});
  const p = await b.newPage();
  for (const f of process.argv.slice(2)) {
    const r = await p.evaluate(async (d) => {
      const img = await new Promise(res => { const i = new Image(); i.onload = () => res(i); i.src = 'data:image/png;base64,' + d; });
      const c = document.createElement('canvas'); c.width = img.width; c.height = img.height; const g = c.getContext('2d'); g.drawImage(img, 0, 0);
      const D = g.getImageData(0, 0, img.width, img.height).data, W = img.width, H = img.height, key = (x, y) => { const i = (y * W + x) * 4; return D[i + 3] < 128 ? -1 : (D[i] << 16) | (D[i + 1] << 8) | D[i + 2]; };
      const runs = {};
      const scan = (n, m, at) => { for (let a = 0; a < n; a++) { let len = 1; for (let b = 1; b <= m; b++) { if (b < m && at(a, b) === at(a, b - 1)) len++; else { if (at(a, b - 1) !== -1) runs[len] = (runs[len] || 0) + 1; len = 1; } } } };
      scan(H, W, (y, x) => key(x, y)); scan(W, H, (x, y) => key(x, y));
      const total = Object.values(runs).reduce((s, v) => s + v, 0);
      let best = 1, bestShare = 0;
      for (let n = 2; n <= 16; n++) { let ok = 0; for (const k in runs) if (k % n === 0) ok += runs[k]; const share = ok / total; if (share > 0.9 && n > best) { best = n; bestShare = share; } }
      const top = Object.entries(runs).sort((a, b) => b[1] - a[1]).slice(0, 8).map(([k, v]) => k + ':' + v).join(' ');
      return {size: W + 'x' + H, block: best, share: +bestShare.toFixed(3), topRuns: top};
    }, fs.readFileSync(f).toString('base64'));
    console.log(f.split(/[\/]/).slice(-2).join('/'), JSON.stringify(r));
  }
  await b.close();
})();
