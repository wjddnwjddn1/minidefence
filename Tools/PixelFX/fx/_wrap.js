// Plays a vendored PixelFX effect faster and shorter so it fits one attack of Ember Bastion (r45).
// The library effect is untouched: only its clock runs `rate` times faster and the sheet is cut at `duration`.
// `fade` seconds before the cut the whole picture fades, for effects whose smoke would otherwise still hang there.
export function hurry(lib, { name, desc, rate, duration, fps = 20, origin, fade = 0, skip = 0, options = {} }) {
  const meta = { ...lib.meta, name, desc, duration, fps, loop: false, origin: origin ?? lib.meta.origin, options: { ...lib.meta.options, ...options } };
  const create = (o) => {
    const inner = lib.create ? lib.create(o) : lib;
    // `skip` seconds of the library effect are run before the first frame (a slow gathering the game has no time for)
    if (skip > 0 && inner.update) for (let i = 0, n = Math.ceil(skip * 120); i < n; i++) inner.update(skip / n, o);
    const dim = (p, t) => { if (fade > 0) p.fade = Math.max(0, Math.min(1, (duration - t) / fade)); };
    const made = { draw: (p, t, opts) => { dim(p, t); inner.draw(p, skip + t * rate, opts); } };
    if (inner.update) made.update = (dt, opts) => { const n = Math.max(1, Math.ceil(rate)); for (let i = 0; i < n; i++) inner.update(dt * rate / n, opts); };
    if (inner.drawBack) made.drawBack = (p, t, opts) => { dim(p, t); inner.drawBack(p, skip + t * rate, opts); };
    return made;
  };
  return { meta, create };
}
