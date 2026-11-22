// Tala hero frame v3 (girl, idle) at 48x54, hand-pixelled as a full grid (one character per pixel, '.' = empty).
// The parol and its stick are an overlay so the same body works with the unlit and the lit parol.
// Run: node tools/tala_hand/tala_hero3.js <preview.png>
const fs = require('fs'), zlib = require('zlib');
const PAL = {
  o: '#2a1420',                                   // warm dark outline
  g: '#1e120e', h: '#3a261c', k: '#56392a', H: '#7a5240', // hair: deep, base, mid, highlight
  S: '#fbd6ac', s: '#e8a978', z: '#c07a4e', Z: '#8a4e32', // skin
  l: '#2a1812', i: '#7a4a2c', x: '#ffffff', m: '#a8443a', b: '#f29a84',
  W: '#fffbf0', w: '#f0e2c4', v: '#cdb48c', V: '#9c825e', // camisa
  e: '#d04a3c', E: '#9a2c24',                       // camisa trim
  R: '#e8604a', r: '#bf3a2c', q: '#8a2a22', Q: '#5a1612', j: '#e8b85a', // red cloth, gold thread
  L: '#b07c44', d: '#6e4422', F: '#a87838', f: '#5a3418', // sandals, bamboo
  Y: '#fffbe0', y: '#ffd860', G: '#f3a334', P: '#f1e5c4', p: '#d6c39a', n: '#a08a64',
};
const BODY = [
//0         1         2         3         4
//012345678901234567890123456789012345678901234567
 '................................................', // 0
 '................................................', // 1
 '................................................', // 2
 '................................................', // 3
 '.................ooooooo........................', // 4
 '...............oohhkkkhhoo......................', // 5
 '..............ohhkHHHkkhhho.....................', // 6
 '.............ohhkHHkkkHHkhho....................', // 7
 '............ohhkkhhhhhkHkhhho...................', // 8
 '............ohhhhhhhhhhhkhhhhho.................', // 9
 '............ohhhkhhhhhhhhhkhhho.................', // 10
 '............ohhkhhhhhhhhhhhhkho.................', // 11
 '............ohkhhhhghhhhshhhgho.................', // 12
 '............ohkhhhgshssShsSSSho.................', // 13
 '...........ohhkhhgsSSSSSSSSSSso.................', // 14
 '...........ohkhhgsSllSSSSllSSso.................', // 15
 '...........ohkhhgsSxiSSSSxiSSso.................', // 16
 '...........ohhkhgsSiiSSSSiiSSo..................', // 17
 '..........ohhkhhgsbbSSSzSSbbso..................', // 18
 '..........ohhkhhhgzsssSmmsSsso..................', // 19
 '.........ohhkhhhhgzzsssssszzo...................', // 20
 '.........ohkhhhhhhoozzzzzzoo....................', // 21
 '.........ohkhhhhhhho.ozso.......................', // 22
 '........ohkhhhhhhoowWeezeeWwoo..................', // 23
 '........ohkhhhhoWWwvvwwwWWWwvWWwo...............', // 24
 '........ohkhhhoWWwwvvwwwwWWwvWWwwo..............', // 25
 '........ohkkhhowwwvvvvwwwwWwvwWwvo..............', // 26
 '........ohkhhhovvvVvvvwwwwwwvvwvVo..............', // 27
 '........ohkhhhoeeeEovvwwwwwvoeeeEo..............', // 28
 '.........ohkhhozssovvwwwwwvozsSo................', // 29
 '.........ohkhhozsSoRqRRRRRrqo...................', // 30
 '.........ohkhhozsSoqRqrrrrrqo...................', // 31
 '.........ohhkhosSSoqoqqqqqqro...................', // 32
 '..........ohkhoszoRqoRqRRRRqRo..................', // 33
 '..........ohhkho.orqorqrrrrqRo..................', // 34
 '...........ohkho.oqorrqrrrrqrRo.................', // 35
 '...........ohhho.ooqrrqrrrrqrRo.................', // 36
 '............ohho.orrRRqRRRRqRRRo................', // 37
 '............ohgo.oqqrrQrrrrqqrRo................', // 38
 '.............oo.oQqrrrQrrrrqqrRo................', // 39
 '................oQqrrrQrrrrqqrrRo...............', // 40
 '................oQrRRRQRRRRqrRRRo...............', // 41
 '................oQqrrrqqrrrqqrrRo...............', // 42
 '...............oqQrrrrqqrrrqqrrrro..............', // 43
 '...............oQQqqqqQQqqqQQqqqqo..............', // 44
 '................ooooooooooooooooo...............', // 45
 '...................ozsso.osszo..................', // 46
 '...................ozsso.osSzo..................', // 47
 '...................ozsso.oSszo..................', // 48
 '...................ozszo.oszzo..................', // 49
 '...................odLdo.odLdo..................', // 50
 '..................ozsLsso.osLsso................', // 51
 '.................oLLLLLLo.oLLLLLLo..............', // 52
 '.................oddddddo.oddddddo..............', // 53
];
// the boy: same face, short hair with an ear and nape, no ribbon, slimmer sleeves, red shorts and longer legs
const BOY_ROWS = {
  23: '..................owWeezeeWwoo..................',
  14: '............ohkhhgsSSSSSSSSSSso.................',
  15: '.............oszgsSllSSSSllSSso.................',
  16: '.............oSzgsSxiSSSSxiSSso.................',
  17: '.............oszssSiiSSSSiiSSo..................',
  18: '..............ozssbbSSSzSSbbso..................',
  19: '...............ozzzsssSmmsSsso..................',
  20: '................ozzzsssssszzo...................',
  21: '..................oozzzzzzoo....................',
  22: '.....................ozso.......................',
  32: '..............osSSoqqqqqqqqqqo..................',
  33: '..............oszoorrrrrrrrRRo..................',
  34: '..................oqrrrrrrrrRo..................',
  35: '..................oqrrrrqrrrRo..................',
  36: '..................oqrrrrqrrrRo..................',
  37: '..................oqrrrooqrrRo..................',
  38: '..................oQqqqo.oqqqo..................',
  39: '...................ozsso.osszo..................',
  40: '...................ozsso.osszo..................',
  41: '...................ozzso.oSszo..................',
  42: '...................ozsso.oSszo..................',
  43: '...................ozsso.osszo..................',
  44: '...................ozsso.osszo..................',
  45: '...................ozsso.osszo..................',
  46: '...................ozsso.osszo..................',
  47: '...................ozsso.osszo..................',
  48: '...................ozszo.oszzo..................',
  49: '...................ozszo.oszzo..................',
};
const BOY = BODY.map((r, y) => {
  if (BOY_ROWS[y]) return BOY_ROWS[y];
  let a = [...r];
  if (y >= 9 && y <= 13) a = a.map((c, x) => (x < 12 ? '.' : c));
  if (y >= 22 && y <= 31) a = a.map((c, x) => (x < 14 && /[ohkgH]/.test(c)) || (x === 14 && /[hkgH]/.test(c)) ? '.' : c);
  if (y >= 23 && y <= 28) { const i = a.indexOf('o', 13); if (i === 14 && y >= 25) { a[14] = '.'; a[15] = 'o'; } }
  if (y >= 25 && y <= 28) { const j = a.lastIndexOf('o'); if (j >= 32) { a[j] = '.'; a[j - 1] = 'o'; } }
  return a.join('');
});
// the front hand holds the stick; the stick rises to the parol's centre (overlay, drawn on top of the body)
const BOW = [[8, 11, 'oo.oo'], [9, 10, 'oRroRRo'], [10, 10, 'oRrqrro'], [11, 11, 'ooqoo'], [12, 12, 'oro'], [13, 12, 'oqo'], [14, 12, 'o.o']];
const STICK = [[29, 32, 'oo'], [28, 31, 'oSSo'], [27, 32, 'osSo'], [26, 33, 'oo'],
  [30, 31, 'f'], [29, 31, 'f'], [27, 35, 'fF'], [26, 35, 'fF'], [25, 36, 'fF']];
const PAROL = [ // 13x18, top-left at (35, 18): star frame, centre ring, facet light, two tassels
  '......f......',
  '.....fYf.....',
  '.....fYf.....',
  '....fYYyf....',
  'fffffyYyfffff',
  'fYYyyyGyyyyGf',
  '.fGyyGYGyyGf.',
  '..fGyyGyyGf..',
  '..fyyyyyyGf..',
  '.fyyyGfGyyGf.',
  '.fyyGf.fGyGf.',
  'fyGff...ffGGf',
  'fff.......fff',
  '.R.........R.',
  '.r.........r.',
  '.R.........R.',
  '.r.........j.',
  '.j...........',
];
const UNLIT = PAROL.map(r => r.replace(/Y/g, 'p').replace(/y/g, 'P').replace(/G/g, 'n'));
const LIT_TONE = { s: 'S', z: 's', Z: 'z', w: 'W', v: 'w', V: 'v', r: 'R', q: 'r', h: 'k', k: 'H', g: 'h', L: 'L', d: 'L' };

// opts (for animation): breathe = chest drops 1 px (the camisa loses row 27, everything above and the hand,
// stick and parol move down 1); hairDx = the tips of the girl's back hair (rows 35-39) sway sideways; tasselDx = tassels sway
function frame(body, parol, lit, opts = {}) {
  for (const [n, r] of body.entries()) if (r.length !== 48) throw new Error(`row ${n} has ${r.length}`);
  let rows = body.map(r => [...r]);
  if (opts.hairDx && body === BODY) for (let y = 35; y <= 39; y++) {
    const r = rows[y], part = r.slice(0, 16), sh = Array(16).fill('.');
    part.forEach((c, x) => { if (c !== '.' && x + opts.hairDx >= 0 && x + opts.hairDx < 16) sh[x + opts.hairDx] = c; });
    rows[y] = [...sh, ...r.slice(16)];
  }
  const dy = opts.breathe ? 1 : 0;
  if (dy) rows = [Array(48).fill('.'), ...rows.slice(0, 27), ...rows.slice(28)];
  const g = rows;
  for (const [row, col, str] of [...(body === BODY ? BOW : []), ...STICK]) [...str].forEach((c, k) => { if (c !== '.') g[row + dy][col + k] = c; });
  parol.forEach((r, j) => [...r].forEach((c, i) => { if (c === '.') return; const x = 35 + i + (j >= 15 ? (opts.tasselDx || 0) : 0); if (x < 48) g[18 + j + dy][x] = c; }));
  if (lit) for (let y = 0; y < 54; y++) for (let x = 0; x < 34; x++) {
    const c = g[y][x]; if (!LIT_TONE[c]) continue;
    // only pixels that face the parol (right-hand side of each run) catch the light
    let edge = false; for (let t = 1; t <= 3; t++) { const n = g[y][x + t]; if (n === '.' || n === 'o' || n === undefined) { edge = true; break; } }
    const d = Math.hypot(x - 41, (y - 24 - dy) * 1.15);
    if (d < 11 || (edge && d < 20)) g[y][x] = LIT_TONE[c];
  }
  return g;
}
module.exports = { PAL, BODY, BOY, PAROL, UNLIT, frame };
if (require.main === module) {
const rgb = h => [1, 3, 5].map(k => parseInt(h.substr(k, 2), 16));
const frames = [frame(BODY, UNLIT, false), frame(BODY, PAROL, true), frame(BOY, UNLIT, false), frame(BOY, PAROL, true)];
const S = 6, pad = 12, PW = pad + 4 * (48 + pad) + 4 * (48 * S + pad), PH = 54 * S + pad * 2;
const img = Buffer.alloc(PW * PH * 3);
for (let i = 0; i < PW * PH; i++) { img[i * 3] = 0x24; img[i * 3 + 1] = 0x1a; img[i * 3 + 2] = 0x2e; }
let ox = pad;
const draw = (g, x0, s) => g.forEach((row, y) => row.forEach((c, x) => {
  if (c === '.') return; const col = PAL[c]; if (!col) throw new Error('unknown ' + c); const v = rgb(col);
  for (let dy = 0; dy < s; dy++) for (let dx = 0; dx < s; dx++) { const p = ((pad + y * s + dy) * PW + x0 + x * s + dx) * 3; img[p] = v[0]; img[p + 1] = v[1]; img[p + 2] = v[2]; }
}));
frames.forEach(f => { draw(f, ox, 1); ox += 48 + pad; });
frames.forEach(f => { draw(f, ox, S); ox += 48 * S + pad; });
const raw = Buffer.alloc((PW * 3 + 1) * PH);
for (let y = 0; y < PH; y++) img.copy(raw, y * (PW * 3 + 1) + 1, y * PW * 3, (y + 1) * PW * 3);
const T = []; for (let n = 0; n < 256; n++) { let c = n; for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1; T[n] = c >>> 0; }
const crc = b => { let c = 0xffffffff; for (const x of b) c = T[(c ^ x) & 255] ^ (c >>> 8); return (c ^ 0xffffffff) >>> 0; };
const ch = (t, d) => { const l = Buffer.alloc(4); l.writeUInt32BE(d.length); const td = Buffer.concat([Buffer.from(t), d]); const c = Buffer.alloc(4); c.writeUInt32BE(crc(td)); return Buffer.concat([l, td, c]); };
const ih = Buffer.alloc(13); ih.writeUInt32BE(PW, 0); ih.writeUInt32BE(PH, 4); ih[8] = 8; ih[9] = 2;
fs.writeFileSync(process.argv[2], Buffer.concat([Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]), ch('IHDR', ih), ch('IDAT', zlib.deflateSync(raw)), ch('IEND', Buffer.alloc(0))]));
console.log('preview ' + process.argv[2]);
}
