// The sword attack sheets the game loads (Assets/Resources/PixelFX): name, project effect id and options.
// Shared by render-attacks.mjs and Tests/PixelFxAttackEffects.mjs so the sheets and their check cannot drift apart.
export const fxDir = new URL('./fx/', import.meta.url);
export const entries = [
  ['SwordSlash', 'melee/eb-slash', { mode: 0, palette: ['#141826', '#3a5a9c', '#7fb4f0', '#d6ecff', '#ffffff'], seed: 4 }],
  ['SwordSweep', 'melee/eb-slash', { mode: 1, palette: ['#1d1230', '#6a3a9c', '#b77fe6', '#ecd6ff', '#ffffff'], seed: 9 }],
  ['SwordBreaker', 'melee/eb-slash', { mode: 2, palette: ['#241308', '#b3541a', '#f29a2e', '#ffe08a', '#ffffff'], seed: 6 }],
];
