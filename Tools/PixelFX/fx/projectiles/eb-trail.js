import { ease, fit } from '../../../../ThirdParty/PixelFX/src/core/index.js';

export const meta = {
  name: 'Ember Bastion projectile wake',
  size: [68, 32], origin: [53, 16], duration: 0.12, fps: 30, loop: false,
  options: { mode: 'arrow', palette: ['#21171e','#98502e','#ffab55','#fff2ba'] },
};

export function draw(p, t, options) {
  const [ink, dark, bright, core] = fit(options.palette, 4);
  const fade = 1 - ease.inQuad(Math.min(1, t / 0.1));
  if (fade <= 0) return;
  if (options.mode === 'arrow') {
    const length = 43 * fade;
    p.poly([[-length, 1],[-7,-5],[1,-2],[1,3],[-12,5]], ink);
    p.poly([[-length+3,1],[-8,-3],[-2,0],[-9,3]], bright);
    p.poly([[-length*0.64,1],[-8,-1],[-3,0],[-9,2]], core);
    return;
  }
  for (let i=0;i<3;i++) {
    const x = -9 - i * 10 - t * 52;
    const y = [1,-5,6][i] * (1 + t * 4);
    const radius = (6-i) * fade;
    if (radius < 1) continue;
    p.poly([[x-radius,y],[x,y-radius],[x+radius+3,y-2],[x+1,y+radius]],ink);
    p.poly([[x-radius+2,y],[x,y-radius+2],[x+radius,y-2],[x+1,y+radius-2]],i===0?core:bright);
    if(options.mode==='frost')p.poly([[x-radius+2,y],[x+1,y+1],[x+1,y+radius-2]],dark);
  }
}
