import { fit } from '../../../../ThirdParty/PixelFX/src/core/index.js';

const colors = fit(['#171219', '#66616b', '#b7824e', '#15111a', '#84909b', '#4b505d', '#211b26', '#302d36', '#be4c2a', '#fff2aa', '#e98a42'], 11);

export const meta = {
  name: 'Black round bomb',
  size: [32,32], origin: [16,19], duration: 0.4, fps: 15, loop: true,
};

export function draw(p,t) {
  const frame = Math.floor(t*15);
  // Dark iron with a readable lit edge, raised rim and short hemp fuse.
  p.poly([[-4,-8],[-3,-11],[1,-11],[3,-8]], colors[0]);
  p.poly([[-2,-9],[-2,-11],[1,-11],[2,-9]], colors[1]);
  p.poly([[0,-11],[0,-14],[3,-16],[7,-15],[7,-13],[4,-14],[2,-13],[2,-11]], colors[2]);
  p.shade(-9,-9,9,9,(x,y)=>{
    const d=x*x+y*y;
    if(d>81)return undefined;
    if(d>64)return colors[3];
    if((x+3)**2+(y+4)**2<8)return colors[4];
    if((x+3)**2+(y+3)**2<27)return colors[5];
    return x-y>6?colors[6]:colors[7];
  });
  const radius=frame%3===0?3:2;
  p.poly([[7-radius,-14],[7,-14-radius],[7+radius,-14],[7,-14+radius]],colors[8]);
  p.dot(7,-14,colors[9]);
  if(frame%2===0)p.dot(10,-17,colors[10]);
}
