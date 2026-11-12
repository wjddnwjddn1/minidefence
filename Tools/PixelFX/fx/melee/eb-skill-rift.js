import { meta as base, draw as drawRift } from './eb-rift.js';

export const meta = {
  ...base,
  name: 'Ember Bastion skill rift',
  desc: 'A heavy, longer sword skill with eight waves of inked stone splinters.',
  duration: 0.45,
  fps: 18,
  options: { ...base.options, streaks: 9, spread: .45, seed: 43 },
};

export function draw(p, t, options) {
  drawRift(p, t * 0.35 / 0.45, options);
}
