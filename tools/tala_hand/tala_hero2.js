// Tala hero frame v2 (girl, idle) at 48x54, hand-pixelled. Each line paints one stroke: [row, startColumn, pixels].
// About 3 heads tall, slim, holding the parol up and forward on a bamboo stick; the parol is drawn unlit and lit.
// Run: node tools/tala_hand/tala_hero2.js <preview.png>
const fs = require('fs'), zlib = require('zlib');
const PAL = {
  o: '#2a1420', H: '#6e4a36', h: '#3c281e', g: '#241610',
  S: '#ffe2bc', s: '#ecb47e', z: '#c47e4e', Z: '#8e5234',
  l: '#2a1812', i: '#7a4a2c', x: '#ffffff', m: '#a8443a', b: '#f4a088',
  W: '#fffbf0', w: '#f1e3c6', v: '#cdb68e', e: '#c8463a',
  R: '#ef6650', r: '#c2392c', q: '#8a2a22', Q: '#5a1612', y2: '#e0b050',
  L: '#a8743c', d: '#6e4422', F: '#9a6a32', f: '#5a3418',
  Y: '#fffbe0', y: '#ffd860', G: '#f3a334', P: '#efe2bd', p: '#d0bc92', n: '#9c8762', T: '#53b46e', t: '#2b7a46',
};
const STROKES = [
  // hair crown and back of the head
  [2, 17, 'oooooo'], [3, 15, 'oohHHhhoo'], [4, 14, 'ohhHHHHhhho'], [5, 13, 'ohhhhHHhhhhho'], [6, 12, 'ohhhhhhhhhhhhho'],
  [7, 12, 'ohhhhhhhhhggghho'], [8, 11, 'orhhhhhhhggsssghho'], [9, 10, 'oRrqhhhhggsSSSsghho'], [10, 10, 'orrqhhhgsSSSSSSsgho'],
  // face: three-quarter front, two soft eyes (lash, glint, warm iris), blush, small smile
  [11, 11, 'oohhhgsSSSSSSSSsho'], [12, 12, 'ohhhgsSllSSSSllSso'], [13, 12, 'ohhhgsSxiSSSSxiSso'], [14, 12, 'ohhhgsSiiSSSSiiSso'],
  [15, 12, 'ohhhgsbbSSSSSSbbso'], [16, 12, 'ohhhhgzsssSmmsSsso'], [17, 13, 'ohhhhgzzsssssszo'], [18, 13, 'ohhhhhoozzzzzoo'],
  // hair falling behind the shoulders
  [19, 12, 'ohhhhhho'], [20, 11, 'ohhhhhhho'], [21, 10, 'ohhhhhhhho'], [22, 10, 'ohhhhhhhho'], [23, 10, 'ohhHhhhhho'], [24, 10, 'ohhHhhhhgo'],
  [25, 10, 'ohhhhhhggo'], [26, 11, 'ohhhhhggo'], [27, 11, 'ohhhhgo'], [28, 12, 'ohhggo'], [29, 12, 'ohgo'], [30, 13, 'oo'],
  // neck, collar trim, camisa with puffed sleeves
  [19, 20, 'ozso'], [20, 18, 'oeeezeeo'], [21, 17, 'owWWeweWwo'], [22, 16, 'owWWwwwwwWWo'], [23, 16, 'owWwwwwwwwWWo'],
  [24, 16, 'ovWwwwwwwwwvzo'], [25, 17, 'ovwwwwwwwwvzso'], [26, 17, 'ovwwwwwwwvozso'], [27, 18, 'owwwwwwwvo.ozso'],
  // the front arm bends forward and up, the hand holds the bamboo stick
  [25, 27, 'ozsso'], [24, 28, 'ozsSo'], [23, 29, 'osSso'], [22, 31, 'osSo'], [21, 32, 'osSo'], [20, 33, 'ossSo'], [19, 34, 'ooo'],
  // stick from the hand up and forward to where the parol hangs
  [19, 35, 'fF'], [18, 36, 'fF'], [17, 37, 'fF'], [16, 38, 'fFf'], [17, 40, 'f'], [18, 40, 'f'],
  // sash with a knot and tail on the back hip
  [28, 17, 'oqrRRRRRRRrqo'], [29, 17, 'oqrRRRRRRRrqo'], [30, 15, 'oqRrqrrrrrrrrqo'], [31, 14, 'oqRrqoqqqqqqqqqo'],
  [32, 14, 'oRrqo'], [33, 14, 'oRqo'], [34, 15, 'oqo'], [35, 15, 'oo'],
  // patadyong: checked red skirt, slight flare
  [32, 18, 'orRrqrrrrqro'], [33, 17, 'orRrrqrrrrqro'], [34, 17, 'oqqqqqqqqqqqo'], [35, 17, 'orRrrqrrrrqrro'],
  [36, 17, 'orRrrqrrrrqrro'], [37, 17, 'oqqqqqqqqqqqqo'], [38, 17, 'orRrrqrrrrqrro'], [39, 17, 'orrrrqrrrrqrro'],
  [40, 17, 'ooqqqqqqqqqqoo'],
  // the back hand peeks out by the hip
  [27, 16, 'oso'], [28, 15, 'oszo'],
  // legs (slight stance), bare skin with shade on the far side
  [41, 19, 'ozsso.osszo'], [42, 19, 'ozsso.osszo'], [43, 19, 'ozsso.oSszo'], [44, 19, 'ozsso.oSszo'], [45, 19, 'ozsso.osszo'],
  [46, 19, 'ozszo.oszzo'], [47, 19, 'ozszo.oszzo'],
  // sandals
  [48, 18, 'odLLdo.odLLLdo'], [49, 18, 'oLLLLLo.oLLLLLLo'], [50, 18, 'ooooooo.oooooooo'],
];
// the parol hangs from the stick end (col 40, row 18)
const PAROL = [
  '......f......', '.....fYf.....', '.....fyf.....', 'fffffyYyfffff', 'fGyyyYYYyyyGf', '.fGyyYYYyyGf.',
  '..fyyyYyyyf..', '.fyyyGfGyyyf.', '.fyyGf.fGyyf.', 'fGGff...ffGGf', 'fff.......fff', 'T...........T', 't...........t',
];
const UNLIT = PAROL.map(r => r.replace(/Y/g, 'p').replace(/y/g, 'P').replace(/G/g, 'n'));

function frame(parol) {
  const g = Array.from({ length: 54 }, () => Array(48).fill('.'));
  for (const [row, col, str] of STROKES) [...str].forEach((c, k) => { if (c !== '.' && g[row + 3]) g[row + 3][col + k] = c; });
  parol.forEach((r, j) => [...r].forEach((c, i) => { if (c !== '.' && g[19 + 3 + j] && 34 + i < 48) g[19 + 3 + j][34 + i] = c; }));
  return g;
}
const rgb = h => [1, 3, 5].map(k => parseInt(h.substr(k, 2), 16));
const frames = [frame(UNLIT), frame(PAROL)];
const S = 6, pad = 12, PW = pad + 2 * (48 + pad) + 2 * (48 * S + pad), PH = 54 * S + pad * 2;
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
