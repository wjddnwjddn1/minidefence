// Ink outlines keep the skill shapes readable over bright terrain.
import { fit, ease } from '../../../../ThirdParty/PixelFX/src/core/index.js';
export const meta={
  name:'Ember Bastion skill burst',
  size:[196,144],origin:[58,72],duration:0.45,fps:18,loop:false,
  options:{mode:'pierce',palette:['#251826','#9b3f24','#f09240','#fff1b0']},
};
function shard(p,x,y,dx,dy,width,colors){
  const angle=Math.atan2(dy,dx),nx=-Math.sin(angle)*width,ny=Math.cos(angle)*width;
  p.poly([[x-nx,y-ny],[x+dx,y+dy],[x+nx,y+ny],[x-dx*.24,y-dy*.24]],colors[0]);
  p.poly([[x-nx*.55,y-ny*.55],[x+dx*.88,y+dy*.88],[x+nx*.55,y+ny*.55],[x-dx*.12,y-dy*.12]],colors[2]);
  p.poly([[x,y],[x+dx*.82,y+dy*.82],[x+nx*.32,y+ny*.32]],colors[3]);
}
export function draw(p,t,o){
  const c=fit(o.palette,4);
  if(t>=.40)return;
  const progress=Math.min(1,t/.4),fade=1-ease.inQuad(progress);
  if(o.mode==='pierce'){
    for(let i=0;i<3;i++){
      const advance=ease.outCubic(Math.min(1,t/.15))*20-i*12;
      shard(p,10+advance,0,84*fade,0,Math.max(3,(10-i*2)*fade),c);
    }
    for(let i=0;i<6;i++)shard(p,32+i*10,(i%2?1:-1)*(8+progress*16),14*fade,(i%2?1:-1)*7,2.5,c);
    return;
  }
  const blast=o.mode==='blast',forge=o.mode==='forge';
  const total=forge?8:blast?10:12;
  const spread=ease.outCubic(Math.min(1,t/.21))*(blast?47:42);
  for(let i=0;i<total;i++){
    const a=i*Math.PI*2/total,rx=Math.cos(a),ry=Math.sin(a);
    const y=ry*spread*(forge?.65:.85);
    shard(p,rx*spread,y,rx*(blast?18:13)*fade,ry*14*fade-(forge?10:0),Math.max(2,(blast?12:8)*fade),c);
  }
  if(t<.24){
    const r=(blast?20:14)*fade;
    p.poly([[-r,0],[0,-r],[r,0],[0,r]],c[0]);
    p.poly([[-r*.65,0],[0,-r*.68],[r*.65,0],[0,r*.68]],c[3]);
  }
}
