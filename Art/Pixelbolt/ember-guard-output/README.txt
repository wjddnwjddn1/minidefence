ember-guard — Pixelbolt export

ember-guard.png: transparent RGBA sprite sheet
ember-guard.atlas.json: TexturePacker-style frames, named animation sequences and Aseprite-style frame tags
ember-guard.tres: Godot 4 SpriteFrames; it finds ember-guard.png in its own folder, so keep the two together
ember-guard.css: CSS classes with per-frame timing
frames/: individual PNGs
animations/: animated PNGs (APNG)
ember-guard.pixel.json: editable source
player.js: dependency-free Canvas player
preview.html: open directly in a browser, or serve this folder

Canvas: import { SpritePlayer } from './player.js';
const player = await SpritePlayer.load(canvas, './ember-guard.atlas.json');
player.play('idle');

Godot 4: copy ember-guard.png and ember-guard.tres into one folder of your project, give an AnimatedSprite2D the Sprite Frames ember-guard.tres and play("idle"). Set the node's Texture Filter to Nearest for crisp pixels.

CSS: <link rel="stylesheet" href="ember-guard.css">
<span class="pf-ember-guard pf-ember-guard-idle"></span>

Timing:
idle: 4 frames of 200 ms (5 fps)
walk: 8 frames of 80 ms (12.5 fps)
attack: 5 frames of 80 ms (12.5 fps), plays once
