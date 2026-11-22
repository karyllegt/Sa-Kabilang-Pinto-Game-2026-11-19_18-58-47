// R3: builds Tala's animation frames (girl and boy) as text grids from small body parts in the R2 style
// (option A: chunky, full 1 px ink outline added around the silhouette, two-tone shading, the gold parol in her hand).
// Each animation poses the same parts, so the boy and girl share one body and one timing.
// Writes Assets/Art/Source/Characters/spr_tala_<char>_<anim>.pxl.txt (built to PNG by SkpPixelText) and a 4x preview
// sheet. Run: node tools/tala_frames/make_tala_frames.js [previewPath]
// Every grid it writes can be touched up by hand afterwards; run this again only to regenerate from the parts.
const fs = require('fs'), path = require('path'), zlib = require('zlib');
const ROOT = path.resolve(__dirname, '../..');
const OUT = path.join(ROOT, 'Assets/Art/Source/Characters');
const W = 16, H = 18;
const GX = -1; // the whole body sits 1 px left of centre so the parol in her hand always fits the 16 px box

// ---- parts (fill only; '.' is clear; the ink outline is added afterwards) ----
const P = {
  headGirl: ['.hhhhh.', 'hhhhhhh', 'hhhssss', 'hhsskss', 'hhsssss', '.hhszs.'],
  headBoy:  ['.hhhhh.', 'hhhhhhh', 'hhhssss', 'hhsskss', '.hsssss', '..sszs.'],
  backHair: ['hh', 'hh', 'h.'],            // the girl's hair down her back
  torso:    ['wwwww', 'Wwwww', 'Wwwww', 'rrrrr'],
  skirt:    ['.rrRrr.', 'rrRrrRr'],         // red patadyong
  shorts:   ['rrRrr', 'rr.rr'],             // the boy's red shorts
  hand:     ['s'],
  parol:    ['..g..', '.gGg.', 'gGlGg', '.gGg.', '..g..'],
  parolBig: ['..G..', '.GlG.', 'GllLG', '.GlG.', '..G..'],
};
// legs (origin x=2, y=13), 4 rows; z = skin shadow (bare legs read against the skirt; keeps 12 colours a frame)
const LEGS = {
  stand: ['...z..z.', '...z..z.', '...z..z.', '...zz.zz'],
  runA:  ['...z..z.', '..z....z', '.z......', 'zz.....z'],
  runB:  ['...z.z..', '...z.z..', '...z.z..', '...zzzz.'],
  runC:  ['....zz..', '...z..z.', '..z...z.', '..zz..zz'],
  tuck:  ['...zz.zz', '....z.z.', '........', '........'],
  apex:  ['...z.z..', '..zz.zz.', '........', '........'],
  dangle:['...z..z.', '...z..z.', '...z..z.', '...z..z.'],
  bend:  ['..z....z', '..z....z', '..zz...zz', '........'],
  dash:  ['..z....z', '.z......z', 'zz.......', '........'],
  kneel: ['...zzzzz', '...z....', '........', '........'],
};

function blank() { return Array.from({ length: H }, () => Array(W).fill('.')); }
function draw(g, part, x, y) {
  x += GX;
  part.forEach((row, j) => [...row].forEach((c, i) => {
    const X = x + i, Y = y + j;
    if (c !== '.' && X >= 0 && X < W && Y >= 0 && Y < H) g[Y][X] = c;
  }));
}
function outline(g) {
  const o = g.map(r => r.slice());
  for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) {
    if (g[y][x] !== '.') continue;
    const n = [[1, 0], [-1, 0], [0, 1], [0, -1]].some(([dx, dy]) => {
      const X = x + dx, Y = y + dy;
      return X >= 0 && X < W && Y >= 0 && Y < H && g[Y][X] !== '.' && g[Y][X] !== 'k';
    });
    if (n) o[y][x] = 'k';
  }
  return o;
}
function recolor(g, map) { return g.map(r => r.map(c => (c in map ? map[c] : c))); }
function mask(g, keep) { return g.map((r, y) => r.map((c, x) => (keep(x, y) ? c : '.'))); }

// one pose: offsets for the upper body (head, hair, torso, hand, parol), the parol on its own, legs, extras
function pose(ch, o = {}) {
  const g = blank();
  const ux = o.ux || 0, uy = o.uy || 0;       // upper body
  const px = o.px || 0, py = o.py || 0;       // the parol relative to the hand
  const sy = o.sy || 0;                       // skirt/shorts
  const boy = ch === 'boy';
  draw(g, LEGS[o.legs || 'stand'], 2 + (o.lx || 0), 13 + (o.ly || 0));
  if (boy) draw(g, P.shorts, 5 + ux, 11 + uy + sy); else draw(g, P.skirt, 4 + ux, 11 + uy + sy);
  draw(g, P.torso, 5 + ux, 7 + uy);
  if (!boy) draw(g, P.backHair, 3 + ux, 7 + uy);
  draw(g, boy ? P.headBoy : P.headGirl, 3 + ux, 1 + uy);
  draw(g, P.hand, 10 + ux + (o.hx || 0), 9 + uy + (o.hy || 0));
  draw(g, o.big ? P.parolBig : P.parol, 11 + ux + px, 7 + uy + py);
  let r = outline(g);
  // chin line between head and collar, and the sash's lower edge stays readable
  if (o.closedEyes) r = r.map(row => row.map(c => c));
  return r;
}
function eyesClosed(g) {
  // the eye is the only ink pixel with skin on both sides in a row
  return g.map((row, y) => row.map((c, x) => (c === 'k' && row[x - 1] === 's' && row[x + 1] === 's' ? 'z' : c)));
}
function flicker(g) { return recolor(g, { l: 'G', G: 'l' }); }

const ANIMS = {
  idle: ch => [pose(ch), pose(ch, { py: -1 }), flicker(pose(ch, { uy: 1, sy: -1, py: -1 })), pose(ch, { py: -1 })],
  run: ch => [
    pose(ch, { legs: 'runA', uy: 0, px: 1 }), pose(ch, { legs: 'runB', uy: -1 }), pose(ch, { legs: 'runC', uy: 0, py: 1 }),
    pose(ch, { legs: 'runA', uy: 0, lx: 1, px: 1 }), flicker(pose(ch, { legs: 'runB', uy: -1 })), pose(ch, { legs: 'runC', uy: 0, py: 1, lx: 1 }),
  ],
  jump_rise: ch => [pose(ch, { legs: 'tuck', uy: 0, py: -1 }), pose(ch, { legs: 'tuck', py: -2 })],
  apex: ch => [pose(ch, { legs: 'apex', py: -2, big: true })],
  fall: ch => [pose(ch, { legs: 'dangle', py: -1 }), pose(ch, { legs: 'dangle', py: -2, hy: -1 })],
  land: ch => [pose(ch, { legs: 'bend', uy: 2, sy: -1, ly: 1, py: 1 }), pose(ch, { legs: 'stand', uy: 1, sy: -1 })],
  dash: ch => [
    pose(ch, { legs: 'dash', ux: 1, uy: 1, px: 1 }), dashLines(pose(ch, { legs: 'dash', ux: 1, uy: 1, px: 0, big: true })),
    dashLines(pose(ch, { legs: 'dash', ux: 1, uy: 1, px: 1 })),
  ],
  ward: ch => [pose(ch, { py: -2, px: -1, hy: -1 }), pose(ch, { py: -4, px: -2, hy: -2, big: true }), pose(ch, { py: -5, px: -2, hy: -2, big: true })],
  hurt: ch => [recolor(eyesClosed(pose(ch, { ux: -1, py: 1 })), { s: 'p', S: 'p', z: 'W', h: 'D', H: 'd', w: 'p', W: 'p', r: 'd', R: 'D' }), eyesClosed(pose(ch, { ux: -1, py: 1 }))],
  death: ch => [
    eyesClosed(pose(ch, { ux: -1, py: 1 })), eyesClosed(pose(ch, { legs: 'bend', uy: 2, sy: -1, ly: 1, py: 2 })),
    eyesClosed(pose(ch, { legs: 'kneel', uy: 4, sy: -2, ly: 0, py: 3 })),
    recolor(mask(eyesClosed(pose(ch, { legs: 'kneel', uy: 4, sy: -2, py: 3 })), (x, y) => (x + y) % 4 !== 0), { s: 'N', S: 'N', z: 'n', h: 'n', H: 'N', w: 'N', W: 'n', r: 'N', R: 'n', g: 'y', G: 'y', l: 'g', L: 'y' }),
    recolor(mask(pose(ch, { legs: 'kneel', uy: 4, sy: -2, py: 3 }), (x, y) => (x + 2 * y) % 3 === 0), { s: 'N', S: 'N', z: 'n', h: 'n', H: 'N', w: 'N', W: 'n', r: 'N', R: 'n', g: 'y', G: 'y', l: 'g', L: 'y', k: 'n' }),
  ],
  door: ch => [
    pose(ch),
    recolor(pose(ch, { py: -1 }), { w: 'W', s: 'S', r: 'R', h: 'h' }),
    mask(recolor(pose(ch, { py: -1 }), { w: 'N', W: 'n', s: 'N', S: 'n', z: 'n', r: 'n', R: 'n', h: 'n', H: 'N' }), (x) => x >= 3 && x <= 13),
    mask(recolor(pose(ch, { py: -1 }), { w: 'n', W: 'n', s: 'n', S: 'n', z: 'n', r: 'n', R: 'n', h: 'n', H: 'n', k: 'n' }), (x, y) => x >= 5 && x <= 11 && (x + y) % 2 === 0),
  ],
  glide: ch => [pose(ch, { legs: 'dangle', py: -6, px: -2, hy: -3, big: true }), pose(ch, { legs: 'dangle', uy: 1, py: -6, px: -2, hy: -3 })],
  bolt: ch => [pose(ch, { px: 1, py: -1, big: true, hx: 1 }), flare(pose(ch, { px: 1, py: -1, big: true, hx: 1 }))],
};
function dashLines(g) {
  const r = g.map(row => row.slice());
  for (const [y, len] of [[8, 3], [10, 2], [12, 3]]) for (let x = 0; x < len; x++) if (r[y][x] === '.') r[y][x] = 'N';
  return r;
}
function flare(g) {
  const r = g.map(row => row.slice());
  for (const [x, y] of [[15, 6], [15, 10], [14, 5], [14, 11]]) if (r[y] && r[y][x] === '.') r[y][x] = 'G';
  return r;
}

// ---- write the grids ----
const sheet = [];
fs.mkdirSync(OUT, { recursive: true });
for (const ch of ['girl', 'boy']) {
  for (const [anim, make] of Object.entries(ANIMS)) {
    const frames = make(ch);
    const name = `spr_tala_${ch}_${anim}`;
    const lines = [
      `# R3 Tala ${ch}, ${anim}: ${frames.length} frame(s). Made by tools/tala_frames/make_tala_frames.js from shared body parts`,
      `# in the R2 style (option A); safe to touch up by hand (then do not regenerate, or move the change into the script).`,
      `name: ${name}`, `size: ${W}x${H}`, 'out: Characters',
    ];
    for (const f of frames) { lines.push('frame'); for (const row of f) lines.push(row.join('')); }
    fs.writeFileSync(path.join(OUT, name + '.pxl.txt'), lines.join('\n') + '\n');
    sheet.push({ label: `${ch} ${anim}`, frames });
  }
}
console.log(`wrote ${sheet.length} grids to ${path.relative(ROOT, OUT)}`);

// ---- optional preview PNG: one row per animation, girl then boy, at 4x on the panel night colour ----
const previewPath = process.argv[2];
if (previewPath) {
  const pal = {};
  for (const l of fs.readFileSync(path.join(ROOT, 'Assets/Art/Source/palette.txt'), 'utf8').split(/\r?\n/)) {
    const t = l.trim().split(/\s+/);
    if (t.length >= 2 && t[1].startsWith('#')) pal[t[0]] = [1, 3, 5].map(i => parseInt(t[1].substr(i, 2), 16));
  }
  const S = 4, pad = 6, maxF = 6;
  const cw = W * S + pad, rh = H * S + pad;
  const half = sheet.length / 2;
  const PW = pad + 2 * (maxF * cw + pad * 3), PH = pad + half * rh;
  const img = Buffer.alloc(PW * PH * 3);
  for (let i = 0; i < PW * PH; i++) { img[i * 3] = 0x14; img[i * 3 + 1] = 0x10; img[i * 3 + 2] = 0x1c; }
  sheet.forEach((row, ri) => {
    const col = ri < half ? 0 : 1, r = ri % half;
    row.frames.forEach((f, fi) => {
      const ox = pad + col * (maxF * cw + pad * 3) + fi * cw, oy = pad + r * rh;
      for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) {
        const c = pal[f[y][x]];
        if (!c) continue;
        for (let dy = 0; dy < S; dy++) for (let dx = 0; dx < S; dx++) {
          const p = ((oy + y * S + dy) * PW + ox + x * S + dx) * 3;
          img[p] = c[0]; img[p + 1] = c[1]; img[p + 2] = c[2];
        }
      }
    });
  });
  const raw = Buffer.alloc((PW * 3 + 1) * PH);
  for (let y = 0; y < PH; y++) { raw[y * (PW * 3 + 1)] = 0; img.copy(raw, y * (PW * 3 + 1) + 1, y * PW * 3, (y + 1) * PW * 3); }
  const crcT = []; for (let n = 0; n < 256; n++) { let c = n; for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1; crcT[n] = c >>> 0; }
  const crc = b => { let c = 0xffffffff; for (const x of b) c = crcT[(c ^ x) & 255] ^ (c >>> 8); return (c ^ 0xffffffff) >>> 0; };
  const chunk = (t, d) => { const l = Buffer.alloc(4); l.writeUInt32BE(d.length); const td = Buffer.concat([Buffer.from(t), d]); const c = Buffer.alloc(4); c.writeUInt32BE(crc(td)); return Buffer.concat([l, td, c]); };
  const ihdr = Buffer.alloc(13); ihdr.writeUInt32BE(PW, 0); ihdr.writeUInt32BE(PH, 4); ihdr[8] = 8; ihdr[9] = 2;
  fs.mkdirSync(path.dirname(previewPath), { recursive: true });
  fs.writeFileSync(previewPath, Buffer.concat([Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]), chunk('IHDR', ihdr), chunk('IDAT', zlib.deflateSync(raw)), chunk('IEND', Buffer.alloc(0))]));
  console.log('preview ' + previewPath);
}
