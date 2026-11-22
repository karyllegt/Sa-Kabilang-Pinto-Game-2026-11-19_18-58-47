// Hand-pixelled Tala hero frame (girl, idle) at 48x54, with the parol unlit and lit. Every pixel is placed by hand
// in the grids below (no formulas), on a designed palette. This frame is the quality gate: the animation frames are
// drawn from it only after it is approved. Run: node tools/tala_hand/tala_hero.js <preview.png>
const fs = require('fs'), zlib = require('zlib');

const PAL = {
  o: '#2a1420', // outline, warm dark
  H: '#6a4632', h: '#3a261d', g: '#24160f', // hair: highlight, base, shade
  S: '#ffe0b8', s: '#eab07a', z: '#c07a4c', Z: '#8e5234', // skin
  k: '#1c0f0c', x: '#ffffff', i: '#6b3b26', m: '#9a3a2c', b: '#f29a80', // eye line, glint, iris, mouth, blush
  W: '#fffbf0', w: '#f0e2c4', v: '#cbb48c', V: '#9a8160', e: '#d0483a', // camisa and embroidery
  R: '#ef6650', r: '#c2392c', q: '#86221c', Q: '#5a1612', // red sash and patadyong
  L: '#a8743c', l: '#6e4422', // sandals
  f: '#5a3418', F: '#8a5a2a', // bamboo frame and stick
  Y: '#fffbe0', y: '#ffd860', G: '#f3a334', // lit paper: core, glow, edge
  P: '#efe2bd', p: '#c9b48a', n: '#9c8762', // unlit paper
  T: '#53b46e', t: '#2b7a46', // tassels
};

// 48 columns x 54 rows. '.' is clear.
const BODY = [
  '................................................', // 0
  '................................................',
  '..................oooooooo......................',
  '...............oooHHhhhhhhooo...................',
  '.............oohhHHHHhhhhhhhhoo.................',
  '............ohhhHHHHhhhhhhhhhhho................', // 5
  '...........ohhhhhHHhhhhhhhhhhhhho...............',
  '..........ohhhhhhhhhhhhhhhhhhhhhho..............',
  '.........orrohhhhhhhhhhhhhhggghhhho.............',
  '.........oRrqohhhhhhhhhhhhggsssghhho............',
  '.........orrqohhhhhhhhhhhgsSSSsssghho...........', // 10
  '..........ooohhhhhhhhhhgsSSSSSsssshho...........',
  '...........ohhhhhhhhhhgsSSSSSSssskkho...........',
  '...........ohhhhhhhhhgsSSSSSSSsskxkho...........',
  '...........ohhhhhhhhgssskksSSSSskiiko...........',
  '...........ohhhhhhhhgssskisSSSSskiiko...........', // 15
  '...........ohhhhhhhhgsssSSSSSSSsskkso...........',
  '...........ohhhhhhhhgzsbbbsSSSSsssssso..........',
  '...........ohhhhhhhhhgzsssssssmmsssso...........',
  '............ohhhhhhhhgzzsssssssssszo............',
  '............ohhhhhhhhhgzzzssssszzzo.............', // 20
  '............ohhhhhhhhhoooozssszoo...............',
  '...........ohhhhhhhhhhooeeezzzeeoo..............',
  '..........ohhhhhhhhhhowWWweWeWewwwo.............',
  '.........ohhhhhhhhhhowWWWwwwwwwwwWwo............',
  '........ohhhhhhhhhhhowWWwwwwwwwwWWwo............', // 25
  '........ohhhhhhhhhhhovWwwwwwwwwwwwvvo...........',
  '........ohhhhhhhhhhhovvwwwwwwwwwwwvzzso.........',
  '.........ohhhhhhhhggovwwwwwwwwwwwvozsso.........',
  '.........ohhhhhhhgoovwwwwwwwwwwwvozsso..........',
  '..........ohhhhhgooqrrRRRRRRRRRrrqozso..........', // 30
  '..........ogggoqqrrRRRRRRRRRRRRrrqoosSo.........',
  '..........oqrRRrqqrrrrrrrrrrrrrrqqo.osSoFFF.....',
  '.........oqRRrqoorrrrrRrrrrRrrrrrqo..ooFFFf.....',
  '.........oqrrrqo.orRRrrrrrrrrrrrrqo.....Ff......',
  '.........oqrrqo..oRRrrrrrrrrrrrrrqo.....Ff......', // 35
  '.........oqrqo...oqqqqqqqqqqqqqqqqo.....Ff......',
  '..........oqo....oRRrrrrrrrrrrrrrqo......f......',
  '..........oo.....orRrrrrrrrrrrrrrqo.............',
  '................orRrrrrrrrrrrrrrrrqo............',
  '................oqqqqqqqqqqqqqqqqqqo............', // 40
  '................oRRrrrrrrrrrrrrrrrqo............',
  '................orrrrrrrrrrrrrrrrqqo............',
  '................ooqqqqqqqqqqqqqqqqoo............',
  '..................ozzso....ozsso................',
  '..................osszo....oszzo................', // 45
  '..................osszo....oszzo................',
  '..................oSszo....oSszo................',
  '..................osszo....oszzo................',
  '..................oszzo....oszzo................',
  '.................ollLLlo..ollLLlo...............', // 50
  '.................oLLLLLLo.oLLLLLLo..............',
  '.................oooooooo.oooooooo..............',
  '................................................',
];

// The parol hangs from the stick end (col 43, row 37): a five-point star with a bamboo frame and two tassels.
const PAROL_LIT = [
  '......f......',
  '.....fYf.....',
  '.....fyf.....',
  'fffffyYyfffff',
  'fGyyyYYYyyyGf',
  '.fGyyYYYyyGf.',
  '..fyyyYyyyf..',
  '.fyyyGfGyyyf.',
  '.fyyGf.fGyyf.',
  'fGGff...ffGGf',
  'fff.......fff',
  'T...........T',
  't...........t',
];
const PAROL_UNLIT = PAROL_LIT.map(r => r.replace(/Y/g, 'p').replace(/y/g, 'P').replace(/G/g, 'n'));

function compose(parol) {
  const g = BODY.map(r => r.padEnd(48, '.').slice(0, 48).split(''));
  const ox = 35, oy = 38; // star top (index 6) under the stick end, col 41
  parol.forEach((row, j) => [...row].forEach((c, i) => { if (c !== '.' && g[oy + j] && ox + i < 48) g[oy + j][ox + i] = c; }));
  return g;
}

// ---- preview: unlit and lit at 1x and 6x on night violet ----
const rgb = h => [1, 3, 5].map(k => parseInt(h.substr(k, 2), 16));
const frames = [compose(PAROL_UNLIT), compose(PAROL_LIT)];
const S = 6, pad = 12, Wd = 48, Ht = 54;
const PW = pad + (Wd + pad) * 2 + (Wd * S + pad) * 2, PH = Ht * S + pad * 2;
const img = Buffer.alloc(PW * PH * 3);
for (let i = 0; i < PW * PH; i++) { img[i * 3] = 0x24; img[i * 3 + 1] = 0x1a; img[i * 3 + 2] = 0x2e; }
let ox = pad;
const draw = (g, x0, y0, s) => g.forEach((row, y) => row.forEach((c, x) => {
  if (c === '.') return;
  const col = PAL[c]; if (!col) throw new Error('unknown symbol ' + c);
  const v = rgb(col);
  for (let dy = 0; dy < s; dy++) for (let dx = 0; dx < s; dx++) { const p = ((y0 + y * s + dy) * PW + x0 + x * s + dx) * 3; img[p] = v[0]; img[p + 1] = v[1]; img[p + 2] = v[2]; }
}));
frames.forEach(f => { draw(f, ox, pad, 1); ox += Wd + pad; });
frames.forEach(f => { draw(f, ox, pad, S); ox += Wd * S + pad; });
const raw = Buffer.alloc((PW * 3 + 1) * PH);
for (let y = 0; y < PH; y++) img.copy(raw, y * (PW * 3 + 1) + 1, y * PW * 3, (y + 1) * PW * 3);
const T = []; for (let n = 0; n < 256; n++) { let c = n; for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1; T[n] = c >>> 0; }
const crc = b => { let c = 0xffffffff; for (const x of b) c = T[(c ^ x) & 255] ^ (c >>> 8); return (c ^ 0xffffffff) >>> 0; };
const ch = (t, d) => { const l = Buffer.alloc(4); l.writeUInt32BE(d.length); const td = Buffer.concat([Buffer.from(t), d]); const c = Buffer.alloc(4); c.writeUInt32BE(crc(td)); return Buffer.concat([l, td, c]); };
const ih = Buffer.alloc(13); ih.writeUInt32BE(PW, 0); ih.writeUInt32BE(PH, 4); ih[8] = 8; ih[9] = 2;
fs.writeFileSync(process.argv[2], Buffer.concat([Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]), ch('IHDR', ih), ch('IDAT', zlib.deflateSync(raw)), ch('IEND', Buffer.alloc(0))]));
console.log('preview ' + process.argv[2]);
