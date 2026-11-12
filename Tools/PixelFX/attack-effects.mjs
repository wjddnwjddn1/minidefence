// The sword attack sheets the game loads (Assets/Resources/PixelFX): name, project effect id and options.
// Shared by render-attacks.mjs and Tests/PixelFxAttackEffects.mjs so the sheets and their check cannot drift apart.
export const fxDir = new URL('./fx/', import.meta.url);
export const entries = [
  ['SwordSlash', 'melee/eb-slash', { mode: 0, palette: ['#141826', '#3a5a9c', '#7fb4f0', '#d6ecff', '#ffffff'], seed: 4 }],
  ['SwordSweep', 'melee/eb-slash', { mode: 1, palette: ['#1d1230', '#6a3a9c', '#b77fe6', '#ecd6ff', '#ffffff'], seed: 9 }],
  ['SwordBreaker', 'melee/eb-slash', { mode: 2, palette: ['#241308', '#b3541a', '#f29a2e', '#ffe08a', '#ffffff'], seed: 6 }],
  // the travelling part of a sword cut: streaks along the ground towards the target (tinted per branch in the game)
  ['SwordRift', 'melee/eb-rift', { streaks: 5, spread: 0.3, seed: 12 }],
  // the multi-target branch throws a wider fan
  ['SwordRiftWide', 'melee/eb-rift', { streaks: 7, spread: 0.72, seed: 21 }],
  // release flashes for ranged troops (thin: true = small one-frame peak is fine, the check asks for less)
  ['LaunchArrow', 'projectiles/eb-launch', { palette: ['#1d1012', '#a8321c', '#e8662a', '#ffc46a', '#ffffff'], spread: 0.3, wedges: 3, seed: 2 }],
  ['LaunchPowder', 'projectiles/eb-launch', { palette: ['#241308', '#8a3a10', '#e07a1e', '#ffd070', '#ffffff'], spread: 0.9, wedges: 5, seed: 5 }],
  ['LaunchFrost', 'projectiles/eb-launch', { palette: ['#0e1830', '#2f6aa8', '#58b4e6', '#c8f2ff', '#ffffff'], spread: 0.6, wedges: 4, seed: 8 }],
  ['TrailArrow', 'projectiles/eb-trail', {}],
  ['TrailPowder', 'projectiles/eb-trail', { mode: 'powder', palette: ['#211512','#9b3717','#ef7a22','#ffdb75'] }],
  ['TrailFrost', 'projectiles/eb-trail', { mode: 'frost', palette: ['#111d37','#3668a1','#75c5ed','#dbf8ff'] }],
];
