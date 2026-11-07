// Measures pixel-art sprite sheets the way the asset review compares them with Tiny Swords:
// opaque box of a frame, partial-alpha pixels, distinct opaque colours, darkest colour (outline), frame count.
// usage: node Tools/AssetReview/measure.cjs <frameW> <frameH> <sheet.png> [more.png ...]
const fs = require('fs'), path = require('path');
const {chromium} = require('C:/Users/snome/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const [fw, fh, ...files] = process.argv.slice(2);
(async () => {
  const b = await chromium.launch({executablePath: 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe', headless: true});
  const p = await b.newPage();
  for (const f of files) {
    const r = await p.evaluate(async ([d, fw, fh]) => {
      const img = await new Promise(res => { const i = new Image(); i.onload = () => res(i); i.src = 'data:image/png;base64,' + d; });
      const c = document.createElement('canvas'); c.width = img.width; c.height = img.height; const g = c.getContext('2d'); g.drawImage(img, 0, 0);
      const data = g.getImageData(0, 0, img.width, img.height).data, frames = Math.floor(img.width / fw) * Math.floor(img.height / fh);
      let x0 = 1e9, y0 = 1e9, x1 = -1, y1 = -1, partial = 0, dark = null, darkL = 999; const colors = new Set();
      for (let y = 0; y < fh; y++) for (let x = 0; x < fw; x++) { const i = (y * img.width + x) * 4, a = data[i + 3]; if (a <= 16) continue; if (a < 240) partial++; x0 = Math.min(x0, x); y0 = Math.min(y0, y); x1 = Math.max(x1, x); y1 = Math.max(y1, y); colors.add((data[i] << 16) | (data[i + 1] << 8) | data[i + 2]); const l = data[i] * .3 + data[i + 1] * .59 + data[i + 2] * .11; if (l < darkL) { darkL = l; dark = [data[i], data[i + 1], data[i + 2]]; } }
      const hex = v => '#' + v.toString(16).padStart(6, '0'), lum = v => ((v >> 16) & 255) * .3 + ((v >> 8) & 255) * .59 + (v & 255) * .11;
      return {sheet: img.width + 'x' + img.height, frames, frame0: {box: [x0, y0, x1 - x0 + 1, y1 - y0 + 1], footY: y1, partialAlpha: partial, colors: colors.size, darkest: '#' + dark.map(v => v.toString(16).padStart(2, '0')).join(''), palette: colors.size <= 24 ? [...colors].sort((a, b) => lum(a) - lum(b)).map(hex) : 'more than 24 colours'}};
    }, [fs.readFileSync(f).toString('base64'), +fw, +fh]);
    console.log(path.basename(f).padEnd(28), JSON.stringify(r));
  }
  await b.close();
})();
