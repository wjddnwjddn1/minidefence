import { spawnSync } from 'node:child_process';
import { resolve, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { mkdirSync, existsSync } from 'node:fs';
const root=resolve(fileURLToPath(new URL('../..',import.meta.url)));
const [id,...input]=process.argv.slice(2);
const force=input.includes('--force'), withActor=input.includes('--with-actor');
const args=input.filter(a=>a!=='--force'&&a!=='--with-actor');
if(!id||!/^([a-z0-9-]+)\/([a-z0-9-]+)$/.test(id)){console.error('Usage: node Tools/PixelFX/render.mjs category/effect [--fps N] [--seed N] [--opts JSON]');process.exit(2);}
if(args.some(a=>['--out','--sheet','--gif','--mp4'].includes(a))){console.error('Output and sheet mode are managed by this wrapper.');process.exit(2);}
const dest=join(root,'Assets','PixelFX','Generated');
if(!force && existsSync(join(dest,id,id.split('/')[1]+'.png'))){console.error('Output exists; pass --force to replace it.');process.exit(2);}
mkdirSync(dest,{recursive:true});
const result=spawnSync(process.execPath,[join(root,'ThirdParty','PixelFX','tools','render.mjs'),id,'--sheet',...(withActor?[]:['--no-actor']),'--out',dest,...args],{cwd:join(root,'ThirdParty','PixelFX'),stdio:'inherit'});if(result.error)throw result.error;process.exit(result.status??1);
