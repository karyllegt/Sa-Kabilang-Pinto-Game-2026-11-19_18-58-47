// Tala idle, 4 frames at 6 fps (10 ticks each), built from the approved hero grids in tala_hero3.js.
// 0 rest, 1 breathe (chest, hand and parol drop 1 px; tassels trail), 2 breathe with the hair tips swaying, 3 rise back.
// Run: node tools/tala_hand/tala_idle.js <outDir>   -> tala_idle_strip.png and tala_idle.gif
const path = require('path');
const { PAL, BODY, BOY, PAROL, UNLIT, frame } = require('./tala_hero3');
const { canvas, blit, png, gif } = require('./img');
const IDLE = [{}, { breathe: true, tasselDx: -1 }, { breathe: true, hairDx: -1, tasselDx: -1 }, { hairDx: -1 }];
const out = process.argv[2] || '.';
const sets = [[BODY, UNLIT, false], [BODY, PAROL, true], [BOY, UNLIT, false], [BOY, PAROL, true]];
const only = process.argv[3] === 'girl' ? sets.slice(0, 2) : sets;
const rows = only.map(([b, p, l]) => IDLE.map(o => frame(b, p, l, o)));
// strip: each row of the sheet is one look, frames left to right at 1x then 5x
const S = 5, pad = 10, cw = 48 * S;
const strip = canvas(pad + 4 * (48 + pad) + 4 * (cw + pad), pad + rows.length * (54 * S + pad));
rows.forEach((fr, r) => fr.forEach((g, i) => { const y = pad + r * (54 * S + pad); blit(strip, g, pad + i * (48 + pad), y, 1); blit(strip, g, pad + 4 * (48 + pad) + i * (cw + pad), y, S); }));
png(path.join(out, 'tala_idle_strip.png'), strip, PAL);
// looping preview: the looks side by side, 6 fps
const anim = [0, 1, 2, 3].map(i => { const c = canvas(pad + rows.length * (cw + pad), 54 * S + 2 * pad); rows.forEach((fr, r) => blit(c, fr[i], pad + r * (cw + pad), pad, S)); return c; });
gif(path.join(out, 'tala_idle.gif'), anim, PAL, 17);
console.log('idle written to ' + out);
