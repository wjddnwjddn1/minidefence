// The sword attack sheets the game loads (Assets/Resources/PixelFX): name, project effect id and options.
// Shared by render-attacks.mjs and Tests/PixelFxAttackEffects.mjs so the sheets and their check cannot drift apart.
export const fxDir = new URL('./fx/', import.meta.url);
export const entries = [
  ['SwordSlash', 'melee/eb-cut', {}],
  ['SwordSweep', 'melee/eb-whirl', { palette: ['#1d1230', '#3d1f6e', '#6a3a9c', '#9a5fd6', '#c79cf5', '#ecd6ff', '#ffffff'] }],
  ['SwordBreaker', 'melee/eb-cut', { palette: ['#3d1012', '#701a14', '#a8301a', '#e0561e', '#ff8a2a', '#ffc46a', '#ffffff'] }],
  // the travelling part of a sword cut: streaks along the ground towards the target (tinted per branch in the game)
  ['SwordRift', 'melee/eb-rift', { streaks: 5, spread: 0.3, seed: 12 }],
  // the multi-target branch throws a wider fan
  ['SwordRiftWide', 'melee/eb-rift', { streaks: 7, spread: 0.72, seed: 21 }],
  // release flashes for ranged troops (r45: the library's muzzle flash and glint, hurried to one attack)
  ['LaunchArrow', 'projectiles/eb-loose', { palette: ['#2a1a08', '#5a3a10', '#9a6a1c', '#d9a441', '#ffd070', '#ffe9a8', '#fff6d8', '#ffffff'] }],
  ['LaunchPowder', 'projectiles/eb-shot', {}],
  ['LaunchFrost', 'projectiles/eb-loose', { palette: ['#151a3d', '#212f6b', '#2c58aa', '#3a8bd8', '#62c8f4', '#a6ecff', '#dcfbff', '#ffffff'] }],
  ['TrailArrow', 'projectiles/eb-trail', {}],
  ['TrailPowder', 'projectiles/eb-trail', { mode: 'powder', palette: ['#211512','#9b3717','#ef7a22','#ffdb75'] }],
  ['TrailFrost', 'projectiles/eb-trail', { mode: 'frost', palette: ['#111d37','#3668a1','#75c5ed','#dbf8ff'] }],
  ['SkillSword', 'melee/eb-skill-rift', {}],
  ['SkillArrow', 'projectiles/eb-skill', { mode: 'pierce' }],
  ['SkillPowder', 'projectiles/eb-blast', {}],
  ['SkillFrost', 'projectiles/eb-nova', {}],
  ['SkillForge', 'melee/eb-forge', {}],
  ['RoundBomb', 'projectiles/eb-round-bomb', {}],
];
