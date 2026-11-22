// Every Tala animation at 48x54 (R3 frame counts), built from the approved hero grids in tala_hero3.js:
// the head, camisa, skirt or shorts and parol stay the approved pixels; legs are hand-drawn poses (LEGS) and each
// frame is a short spec (pose, body bob, far arm, hair and hem trail, parol offset, effects).
//   node tools/tala_hand/tala_anim.js preview <outDir>   -> one strip per animation + a GIF per animation (girl and boy, lit)
//   node tools/tala_hand/tala_anim.js export <dir>       -> RGBA PNGs spr_tala48_<girl|boy>_<anim>_<i>.png (lit parol)
const fs = require('fs'), path = require('path'), zlib = require('zlib');
const { PAL, BODY, BOY, PAROL, UNLIT, frame } = require('./tala_hero3');
const { canvas, blit, png, gif } = require('./img');

// ---- legs, drawn in near-leg colours; lead = the leg in front, rear = the other. Strokes: [row, col, pixels]
const foot = (r, c) => [[r, c, 'odLdo'], [r + 1, c, 'osLsSSo'], [r + 2, c, 'oLLLLLLo'], [r + 3, c, 'oddddddo']];
const LEGS = {
  contact: {
    lead: [[43, 24, 'oSszo'], [44, 25, 'oSszo'], [45, 26, 'oSszo'], [46, 27, 'oSszo'], [47, 28, 'oSszo'], [48, 29, 'oSszo'], [49, 29, 'oSszo'],
      [50, 29, 'odLdo'], [51, 29, 'osLsSSo'], [52, 29, 'oLLLLLLo'], [53, 30, 'odddddo']],
    rear: [[43, 21, 'ozsso'], [44, 20, 'ozsso'], [45, 19, 'ozsso'], [46, 18, 'ozsso'], [47, 17, 'ozsso'], [48, 16, 'ozsso'], [49, 15, 'ozsso'],
      [50, 14, 'odLdo'], [51, 14, 'osLsso'], [52, 16, 'oLLLLo'], [53, 17, 'odddo']],
  },
  down: {
    lead: [[43, 24, 'oSszo'], [44, 24, 'oSszo'], [45, 25, 'oSszo'], [46, 25, 'oSszo'], [47, 25, 'oSszo'], [48, 25, 'oSszo'], [49, 24, 'oSszo'], ...foot(50, 24)],
    rear: [[43, 21, 'ozsso'], [44, 20, 'ozsso'], [45, 19, 'ozsso'], [46, 18, 'ozsso'], [46, 13, 'ooooo'], [47, 13, 'odLsszsso'], [48, 13, 'odLzzzzzo'], [49, 14, 'ooooooo']],
  },
  passing: {
    lead: [[42, 23, 'oSszo'], [43, 23, 'oSszo'], [44, 23, 'oSszo'], [45, 23, 'oSszo'], [46, 23, 'oSszo'], [47, 23, 'oSszo'], [48, 23, 'oSszo'], [49, 23, 'oSszo'], ...foot(50, 23)],
    rear: [[42, 21, 'ozsso'], [43, 21, 'ozsso'], [44, 20, 'ozsso'], [45, 19, 'ozsso'], [46, 18, 'ozsso'], [47, 17, 'ozsso'],
      [48, 16, 'odLdo'], [49, 15, 'ozLsso'], [50, 16, 'oLLLo'], [51, 17, 'ooo']],
  },
  stand: {
    lead: [[42, 25, 'osszo'], [43, 25, 'osszo'], [44, 25, 'osszo'], [45, 25, 'osszo'], [46, 25, 'osszo'], [47, 25, 'oSszo'], [48, 25, 'oSszo'], [49, 25, 'oszzo'],
      [50, 25, 'odLdo'], [51, 25, 'osLsso'], [52, 25, 'oLLLLLLo'], [53, 25, 'oddddddo']],
    rear: [[42, 19, 'ozsso'], [43, 19, 'ozsso'], [44, 19, 'ozsso'], [45, 19, 'ozsso'], [46, 19, 'ozsso'], [47, 19, 'ozsso'], [48, 19, 'ozsso'], [49, 19, 'ozszo'],
      [50, 19, 'odLdo'], [51, 18, 'ozsLsso'], [52, 17, 'oLLLLLLo'], [53, 17, 'oddddddo']],
  },
  tuck: {
    lead: [[43, 24, 'oSszo'], [44, 25, 'oSszo'], [45, 26, 'oSszo'], [46, 26, 'oSszo'], [47, 25, 'oSszo'], [48, 24, 'odLdo'], [49, 23, 'osLsSSo'], [50, 23, 'oLLLLLLo'], [51, 23, 'oddddddo']],
    rear: [[43, 20, 'ozsso'], [44, 19, 'ozsso'], [45, 18, 'ozsso'], [46, 17, 'ozsso'], [47, 16, 'odLdo'], [48, 15, 'ozLsso'], [49, 16, 'oLLLLo'], [50, 17, 'oooo']],
  },
  dangle: {
    lead: [[43, 25, 'oSszo'], [44, 25, 'oSszo'], [45, 25, 'oSszo'], [46, 26, 'oSszo'], [47, 26, 'oSszo'], [48, 26, 'oSszo'], [49, 26, 'oSszo'],
      [50, 26, 'odLdo'], [51, 26, 'osLsSo'], [52, 27, 'oLLLo'], [53, 28, 'ooo']],
    rear: [[43, 20, 'ozsso'], [44, 20, 'ozsso'], [45, 20, 'ozsso'], [46, 19, 'ozsso'], [47, 19, 'ozsso'], [48, 18, 'ozsso'],
      [49, 18, 'odLdo'], [50, 17, 'osLsso'], [51, 18, 'oLLLo'], [52, 19, 'ooo']],
  },
  crouch: {
    lead: [[43, 25, 'oSszo'], [44, 26, 'oSszo'], [45, 27, 'oSszo'], [46, 28, 'oSszo'], [47, 28, 'oSszo'], [48, 27, 'oSszo'], [49, 26, 'oSszo'], ...foot(50, 26)],
    rear: [[43, 20, 'ozsso'], [44, 20, 'ozsso'], [45, 21, 'ozsso'], [46, 21, 'ozsso'], [47, 20, 'ozsso'], [48, 19, 'ozsso'], [49, 18, 'ozsso'],
      [50, 18, 'odLdo'], [51, 17, 'ozsLsso'], [52, 16, 'oLLLLLLo'], [53, 16, 'oddddddo']],
  },
  kneel: {
    lead: [[46, 26, 'oSszo'], [47, 27, 'oSszo'], [48, 28, 'oSszo'], [49, 28, 'oSszo'], [50, 28, 'oSszo'], [51, 28, 'osLsSSo'], [52, 28, 'oLLLLLLo'], [53, 28, 'oddddddo']],
    rear: [[46, 20, 'ozsso'], [47, 19, 'ozsso'], [48, 18, 'ozsso'], [49, 17, 'ozsso'], [50, 15, 'ozzsso'], [51, 13, 'oddozzzzo'], [52, 12, 'oLLooooooo'], [53, 12, 'oooo']],
  },
};
const FAR = { S: 's', s: 'z', z: 'Z', L: 'd', d: 'f' };           // the far leg sits in shadow
const DARK = { S: 's', s: 'z', z: 'Z', W: 'w', w: 'v', v: 'V', R: 'r', r: 'q', q: 'Q', h: 'g', k: 'h', H: 'k', e: 'E', L: 'd', d: 'f', F: 'f',
  Y: 'y', y: 'G', G: 'n', P: 'p', p: 'n', b: 'm', x: 'w', j: 'G' }; // one step into the dark (door)
const FLASH = c => (c === 'o' || c === '.' ? c : 'W');            // hurt flash
// far arm (behind the body): hidden while it swings forward, reaching back, or raised (glide)
const ARM = {
  girl: { hidden: [[29, 14, 'hkhho'], [30, 14, 'hhkho'], [31, 14, 'hkhho'], [32, 14, 'hhhho'], [33, 14, 'ho.o']],
    back: [[29, 14, 'ozsso'], [30, 13, 'ozsSoo'], [31, 12, 'ozsSoho'], [32, 11, 'osSSohho'], [33, 11, 'oszoo.o']] },
  boy: { hidden: [[29, 14, '....o'], [30, 14, '....o'], [31, 14, '....o'], [32, 14, '....o'], [33, 14, '....o']],
    back: [[29, 14, 'ozsso'], [30, 13, 'ozsSoo'], [31, 12, 'ozsSo.o'], [32, 11, 'osSSo..o'], [33, 11, 'oszo...o']] },
};

const paint = (g, strokes, map) => { for (const [r, c, str] of strokes) [...str].forEach((ch, k) => { if (ch !== '.' && g[r] && c + k >= 0 && c + k < 48) g[r][c + k] = map ? (typeof map === 'function' ? map(ch) : (map[ch] || ch)) : ch; }); };
const shiftRow = (row, x0, x1, dx) => { const seg = row.slice(x0, x1 + 1); for (let x = x0; x <= x1; x++) row[x] = '.'; seg.forEach((c, i) => { if (c !== '.' && x0 + i + dx >= 0 && x0 + i + dx < 48) row[x0 + i + dx] = c; }); };
// the hair run left of the arm grows to the left by n pixels (outline kept, no holes)
const trail = (row, n) => { const first = row.findIndex(c => c !== '.'); if (first < 0 || first > 13) return;
  for (let k = 0; k < n; k++) { if (first - k - 1 < 0) break; row[first - k - 1] = 'o'; row[first - k] = row[first + 1] === 'o' ? 'h' : row[first + 1]; } };
// the boy's legs start right under his shorts (row 39): stretch each leg's first stroke up to there
const fromRow = (strokes, top) => { const [r, c, s] = strokes[0]; const add = []; for (let y = top; y < r; y++) add.push([y, c, s]); return [...add, ...strokes]; };

/**
 * One frame. spec: pose (LEGS key), nearLeads, dy (body bob), arm (hidden|back), hair (trail px), hairUp (tips lift),
 * drag (hem trail px), flare (hem widens), tassel (tassel trail), pdx/pdy (hand and parol), eyes ('shut'),
 * dark (steps into shadow), shadow (door silhouette), flash, streaks (dash lines), glow (extra light around the parol), unlit.
 */
function pose(body, lit, spec) {
  const girl = body === BODY, P = spec.pose ? LEGS[spec.pose] : null, dy = spec.dy || 0;
  let g = body.map(r => [...r]);
  if (spec.eyes === 'shut') for (let y = 15; y <= 17; y++) for (let x = 18; x <= 28; x++) {
    if ('lxi'.includes(g[y][x])) g[y][x] = y === 16 ? 'l' : 'S';
  }
  if (girl && spec.hair) for (let y = 31; y <= 39; y++) trail(g[y], y >= 35 ? spec.hair + 1 : spec.hair);
  if (girl && spec.hairUp) for (let y = 34; y <= 39; y++) for (let x = 0; x < 16; x++) { // tips float up
    const c = g[y][x]; if (c === '.') continue; if (y - spec.hairUp >= 30 && g[y - spec.hairUp][x] === '.') g[y - spec.hairUp][x] = c; if (y >= 40 - spec.hairUp) g[y][x] = '.'; }
  if (spec.arm) paint(g, ARM[girl ? 'girl' : 'boy'][spec.arm]);
  if (girl && spec.drag) for (let y = 41; y <= 45; y++) shiftRow(g[y], 15, 36, -(y >= 43 ? spec.drag + (spec.drag > 1 ? 1 : 0) : spec.drag));
  if (girl && spec.flare) for (let y = 41; y <= 45; y++) { const w = y - 40 > spec.flare ? spec.flare : y - 40; const r = g[y];
    const L = r.findIndex((c, x) => x >= 15 && c !== '.'), R = r.length - 1 - [...r].reverse().findIndex((c, x) => 47 - x <= 38 && c !== '.');
    for (let k = 1; k <= w; k++) { if (L - k >= 0) r[L - k] = 'o'; if (R + k < 48) r[R + k] = 'o'; }
    for (let k = 0; k < w; k++) { r[L - k] = y === 45 ? 'o' : 'q'; r[R + k] = y === 45 ? 'o' : 'R'; } r[L - w] = 'o'; r[R + w] = 'o'; }
  // the arm reaching the moved hand (ward raises it, bolt thrusts it)
  const px = spec.pdx || 0, py = spec.pdy || 0;
  if (px || py) { const top = Math.min(26 + py, 28), right = 31 + Math.max(px, 0);
    for (let y = top; y <= 29; y++) for (let x = 29; x <= right; x++) g[y][x] = (y === top || y === 29 || x === right) ? 'o' : (y === 28 ? 'z' : 's'); }
  if (P) {
    const top = girl ? 46 : 39;
    for (let y = top; y < 54; y++) for (let x = 0; x < 48; x++) if (!girl || y >= 46) g[y][x] = '.';
    const legs = Array.from({ length: 54 }, () => Array(48).fill('.'));
    let [farS, nearS] = spec.nearLeads === false ? [P.lead, P.rear] : [P.rear, P.lead];
    if (!girl) { farS = fromRow(farS, 39 + Math.max(0, -dy)); nearS = fromRow(nearS, 39 + Math.max(0, -dy)); }
    paint(legs, farS, FAR); paint(legs, nearS);
    const sh = Array.from({ length: 54 }, (_, y) => (g[y - dy] ? [...g[y - dy]] : Array(48).fill('.')));
    for (let y = 0; y < 54; y++) for (let x = 0; x < 48; x++) if (sh[y][x] === '.' && legs[y][x] !== '.') sh[y][x] = legs[y][x];
    g = sh;
  } else if (dy) g = Array.from({ length: 54 }, (_, y) => (g[y - dy] ? [...g[y - dy]] : Array(48).fill('.')));
  let out = frame(g.map(r => r.join('')), spec.unlit ? UNLIT : PAROL, lit && !spec.unlit, { dy, girl, tasselTrail: spec.tassel || 0, pdx: px, pdy: py });
  if (spec.glow) { const cx = 41 + px, cy = 24 + dy + py; // a ring of sparks around the star
    for (const [a, b] of [[-8, 0], [8, 0], [0, -9], [-6, -6], [6, -6], [-6, 6], [6, 6], [0, 9]]) { const x = cx + a, y = cy + b; if (out[y] && x >= 0 && x < 48 && out[y][x] === '.') out[y][x] = 'Y'; } }
  if (spec.streaks) for (const [y, x0, n] of [[27, 2, 7], [33, 0, 9], [39, 3, 6], [45, 1, 8]]) for (let x = x0; x < x0 + n + spec.streaks; x++) if (out[y + dy] && out[y + dy][x] === '.') out[y + dy][x] = x === x0 ? 'v' : 'w';
  for (let k = 0; k < (spec.dark || 0); k++) out = out.map(r => r.map(c => DARK[c] || c));
  if (spec.flash) out = out.map(r => r.map(FLASH));
  // door: she steps into the dark doorway as a silhouette (1: deep shadow fill, 2: outline colour only)
  if (spec.shadow) out = out.map(r => r.map(c => (c === '.' ? c : spec.shadow === 1 && c !== 'o' ? 'g' : 'o')));
  return out;
}

// ---- the animations (frame counts and order match SkpTalaAnimSetup.Table)
const RUNF = (p, near, arm) => ({ pose: p, nearLeads: near, arm, dy: { contact: 0, down: 1, passing: -1 }[p], hair: p === 'down' ? 2 : 1, drag: p === 'down' ? 2 : 1, tassel: p === 'down' ? 2 : 1 });
const ANIMS = {
  idle: [{}, { idle: { breathe: true, tasselDx: -1 } }, { idle: { breathe: true, hairDx: -1, tasselDx: -1 } }, { idle: { hairDx: -1 } }],
  run: [RUNF('contact', true, 'hidden'), RUNF('down', true), RUNF('passing', true), RUNF('contact', false, 'back'), RUNF('down', false), RUNF('passing', false)],
  jump_rise: [{ pose: 'tuck', dy: -1, arm: 'back', drag: 0, tassel: 1, hairUp: 0 }, { pose: 'tuck', dy: -2, arm: 'back', tassel: 2 }],
  apex: [{ pose: 'tuck', dy: -2, flare: 1, hairUp: 1, tassel: 0 }],
  fall: [{ pose: 'dangle', dy: -1, flare: 2, hairUp: 2, arm: 'back', pdy: -1 }, { pose: 'dangle', dy: -1, flare: 2, hairUp: 3, arm: 'back', pdy: -2 }],
  land: [{ pose: 'crouch', dy: 3, flare: 1, tassel: 0 }, { pose: 'stand', dy: 1 }],
  dash: [{ pose: 'contact', dy: 1, hair: 3, drag: 2, tassel: 3, arm: 'back', streaks: 0 }, { pose: 'contact', dy: 1, hair: 4, drag: 3, tassel: 3, arm: 'back', streaks: 3 },
    { pose: 'down', dy: 1, hair: 2, drag: 2, tassel: 2 }],
  ward: [{ pdy: -2, glow: false, tassel: 1 }, { pdy: -4, glow: true, tassel: 2, arm: 'back' }, { pdy: -2, glow: true, tassel: 1 }],
  hurt: [{ flash: true, eyes: 'shut', pose: 'down', nearLeads: false, dy: 1 }, { eyes: 'shut', pose: 'down', nearLeads: false, dy: 1, hair: 2, tassel: 2 }],
  death: [{ eyes: 'shut', pose: 'crouch', dy: 3, hair: 1, tassel: 1 }, { eyes: 'shut', pose: 'kneel', dy: 5 }, { eyes: 'shut', pose: 'kneel', dy: 7, pdy: 2 },
    { eyes: 'shut', pose: 'kneel', dy: 8, pdy: 4, unlit: true }, { eyes: 'shut', pose: 'kneel', dy: 9, pdy: 5, unlit: true, tassel: 0 }],
  door: [{ pose: 'contact' }, { pose: 'passing', dy: -1, arm: 'hidden' }, { pose: 'stand', shadow: 1 }, { pose: 'stand', shadow: 2 }],
  glide: [{ pose: 'dangle', flare: 2, hair: 2, drag: 1, tassel: 2, arm: 'back' }, { pose: 'dangle', flare: 2, hair: 3, drag: 1, tassel: 3, arm: 'back', dy: 1 }],
  bolt: [{ pdy: -3, glow: true, tassel: 2, arm: 'back' }, { pdy: -1, tassel: 1 }],
};
function build(body, lit, anim) {
  return ANIMS[anim].map(spec => (spec.idle || Object.keys(spec).length === 0)
    ? frame(body, lit ? PAROL : UNLIT, lit, spec.idle || {}) : pose(body, lit, spec));
}

// ---- RGBA PNG for the game (no partial alpha: '.' is fully clear)
const rgb = h => [1, 3, 5].map(k => parseInt(h.substr(k, 2), 16));
function pngRGBA(file, g) {
  const w = 48, h = 54, raw = Buffer.alloc((w * 4 + 1) * h);
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) { const c = g[y][x], o = y * (w * 4 + 1) + 1 + x * 4;
    if (c === '.') continue; const v = rgb(PAL[c]); raw[o] = v[0]; raw[o + 1] = v[1]; raw[o + 2] = v[2]; raw[o + 3] = 255; }
  const T = []; for (let n = 0; n < 256; n++) { let c = n; for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1; T[n] = c >>> 0; }
  const crc = b => { let c = 0xffffffff; for (const x of b) c = T[(c ^ x) & 255] ^ (c >>> 8); return (c ^ 0xffffffff) >>> 0; };
  const ch = (t, d) => { const l = Buffer.alloc(4); l.writeUInt32BE(d.length); const td = Buffer.concat([Buffer.from(t), d]); const c = Buffer.alloc(4); c.writeUInt32BE(crc(td)); return Buffer.concat([l, td, c]); };
  const ih = Buffer.alloc(13); ih.writeUInt32BE(w, 0); ih.writeUInt32BE(h, 4); ih[8] = 8; ih[9] = 6;
  fs.writeFileSync(file, Buffer.concat([Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]), ch('IHDR', ih), ch('IDAT', zlib.deflateSync(raw)), ch('IEND', Buffer.alloc(0))]));
}

module.exports = { ANIMS, build, pose, LEGS };
if (require.main === module) {
  const [mode, dir] = [process.argv[2], process.argv[3] || '.'];
  fs.mkdirSync(dir, { recursive: true });
  if (mode === 'export') {
    let n = 0;
    for (const [ch, body] of [['girl', BODY], ['boy', BOY]]) for (const anim of Object.keys(ANIMS))
      build(body, true, anim).forEach((g, i) => { pngRGBA(path.join(dir, `spr_tala48_${ch}_${anim}_${i}.png`), g); n++; });
    console.log(`${n} frames written to ${dir}`);
  } else {
    // one sheet: every animation is a row (girl lit), then the same rows for the boy; plus a GIF per animation
    const S = 3, pad = 6, names = Object.keys(ANIMS), maxF = 6;
    const sheet = canvas(pad + maxF * 2 * (48 * S + pad), pad + names.length * (54 * S + pad));
    names.forEach((a, r) => {
      const fg = build(BODY, true, a), fb = build(BOY, true, a);
      fg.forEach((g, i) => blit(sheet, g, pad + i * (48 * S + pad), pad + r * (54 * S + pad), S));
      fb.forEach((g, i) => blit(sheet, g, pad + (maxF + i) * (48 * S + pad), pad + r * (54 * S + pad), S));
      const anim = fg.map((g, i) => { const c = canvas(2 * (48 * 4) + 3 * pad, 54 * 4 + 2 * pad); blit(c, g, pad, pad, 4); blit(c, fb[i], 2 * pad + 48 * 4, pad, 4); return c; });
      gif(path.join(dir, `tala_${a}.gif`), anim, PAL, { idle: 17, run: 10, glide: 17, fall: 13 }[a] || 10);
    });
    png(path.join(dir, 'tala48_all_animations.png'), sheet, PAL);
    console.log('preview written to ' + dir);
  }
}
