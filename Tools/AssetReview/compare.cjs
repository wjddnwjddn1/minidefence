// Shows candidate sprite frames next to the game's Tiny Swords units at the real in-game scale, on Tiny grass and sand,
// so a new sprite can be judged the way the player sees it. Unity draws 192 px troop frames at 112 map units and a
// 1280x720 window shows the 1280-unit map at about 0.69 px per unit; the picture is then enlarged 3x for viewing.
// usage: node Tools/AssetReview/compare.cjs out.png sheet.png frameW frameH footX footY frames [label]
//   frames: comma list of frame indexes in the sheet (left to right, top to bottom), e.g. 0,2,4
//   footX/footY: the foot point inside one frame (Tiny warrior is 102,136)
const fs = require('fs'), path = require('path');
const {chromium} = require('C:/Users/snome/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root = path.resolve(__dirname, '../..'), R = path.join(root, 'Assets/Resources');
const [out, sheet, fw, fh, footX, footY, frames, label] = process.argv.slice(2);
if (!frames) { console.error('usage: node Tools/AssetReview/compare.cjs out.png sheet.png frameW frameH footX footY frames [label]'); process.exit(2); }
const b64 = f => fs.readFileSync(f).toString('base64');
(async () => {
  const b = await chromium.launch({executablePath: 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe', headless: true});
  const p = await b.newPage();
  await p.setContent('<canvas id=c></canvas>');
  const data = await p.evaluate(async ([flat64, war64, arc64, sheet64, fw, fh, fx, fy, list, label]) => {
    const load = d => new Promise(r => { const i = new Image(); i.onload = () => r(i); i.src = 'data:image/png;base64,' + d; });
    const flat = await load(flat64), war = await load(war64), arc = await load(arc64), img = await load(sheet64);
    const s = 0.69 * 112 / 192, tile = 64 * 0.69, zoom = 3, cell = 46, n = list.length + 2, W = cell * n, H = 74;
    const c = document.getElementById('c'); c.width = W * zoom; c.height = H * zoom + 18;
    const g = c.getContext('2d'); g.imageSmoothingEnabled = false; g.fillStyle = '#111'; g.fillRect(0, 0, c.width, c.height);
    g.save(); g.translate(0, 18); g.scale(zoom, zoom);
    // grass everywhere, a sand road behind the feet line (Tilemap_Flat: grass centre 1,1; road 6,3)
    for (let x = 0; x < W; x += tile) for (let y = 0; y < H; y += tile) g.drawImage(flat, 64, 64, 64, 64, x, y, tile + .5, tile + .5);
    for (let x = 0; x < W; x += tile) g.drawImage(flat, 6 * 64, 3 * 64, 64, 64, x, 30, tile + .5, tile + .5);
    const foot = 58, cols = Math.max(1, Math.floor(img.width / fw));
    g.drawImage(war, 0, 0, 192, 192, cell / 2 - 102 * s, foot - 136 * s, 192 * s, 192 * s);
    g.drawImage(arc, 0, 0, 192, 192, cell * 1.5 - 99 * s, foot - 134 * s, 192 * s, 192 * s);
    list.forEach((f, i) => g.drawImage(img, (f % cols) * fw, Math.floor(f / cols) * fh, fw, fh, cell * (i + 2) + cell / 2 - fx * s, foot - fy * s, fw * s, fh * s));
    g.restore();
    g.fillStyle = '#fff'; g.font = '13px sans-serif'; g.fillText('Tiny warrior | Tiny archer | ' + label + ' frames ' + list.join(','), 6, 13);
    return c.toDataURL('image/png').split(',')[1];
  }, [b64(path.join(R, 'Tilemap_Flat.png')), b64(path.join(R, 'WarriorIdle.png')), b64(path.join(R, 'ArcherIdle.png')), b64(sheet), +fw, +fh, +footX, +footY, frames.split(',').map(Number), label || path.basename(sheet)]);
  fs.writeFileSync(out, Buffer.from(data, 'base64'));
  await b.close();
  console.log('wrote', out);
})();
