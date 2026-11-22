// R4 step 5: the key frame of each character (Anino, tikbalang, manananggal, Lola, the Bellkeeper) as text grids in
// the R2 style (fill colours here; the 1 px ink outline is added around the silhouette). For review before the other
// frames are drawn from them. Writes reference/art_samples/r4/<name>_key.pxl.txt and a preview sheet.
// Run: node tools/creature_frames/make_r4_keys.js <preview.png>
const fs = require('fs'), path = require('path'), zlib = require('zlib');
const ROOT = path.resolve(__dirname, '../..');
const OUT = path.join(ROOT, 'reference/art_samples/r4');

// fill-only grids, drawn bottom-aligned on their canvas (feet on the last row)
const KEYS = {
  anino: { size: [12, 13], fill: [
    '..nnnnnn..',
    '.nnnnnnnn.',
    '.nNnnnnnn.',
    '.nnpnnnpn.',
    '.nnnnnnnn.',
    '..nNnnnn..',
    '.nnnnnnnn.',
    'nNnnnnnnnn',
    'nNnnnnnnNn',
    '.nnnnnnnn.',
    '.nn.nnn.n.',
    '.n...n..n.',
  ] },
  tikbalang: { size: [20, 26], fill: [
    '..........hh........',
    '.........hhh22......',
    '........hh22222.....',
    '........h222d22.....',
    '........h2222222....',
    '.......hh22223333...',
    '.......h2222..333...',
    '......hh222.........',
    '......h2222.........',
    '.....22222222.......',
    '....2222222222......',
    '...221222222122.....',
    '...21.2222222.12....',
    '...2..1222221..2....',
    '..22..2222222..22...',
    '..3...1222221...3...',
    '......2222222.......',
    '......22...22.......',
    '......22...22.......',
    '......21...21.......',
    '......2.....2.......',
    '.....22.....22......',
    '.....2......2.......',
    '....44.....44.......',
  ] },
  manananggal: { size: [26, 20], fill: [
    '...........hhhh...........',
    '..........hhsssh..........',
    '..........hsdsds..........',
    '.NN.......hhsssh.......NN.',
    '.NNN.......hhsh.......NNN.',
    '..NNNn....zSSSSz....nNNN..',
    '...NNNnn.zSSSSSSz.nnNNN...',
    '....NNNnnnSSSSSSnnnNNN....',
    '.....NNNnnzSSSSznnNNN.....',
    '......NNNnnzSSzznNNN......',
    '.......NN.n.SSSS.n.NN.....',
    '...........SzzzS..........',
    '...........RRRRR..........',
    '...........DRDRD..........',
    '............D.D...........',
  ] },
  lola: { size: [14, 20], fill: [
    '.....pp.......',
    '....pppp......',
    '...pWWWWp.....',
    '...Wsssss.....',
    '...Wsskss.....',
    '...Wsssss.....',
    '....szzs......',
    '...mmmmmm.....',
    '..mmmmmmmm....',
    '..mMmmmmmms...',
    '..mMmmmmm.b...',
    '..MMmmmmM.b...',
    '...NNNNNN.b...',
    '...NnNNNN.b...',
    '...NNNNnN.b...',
    '...NNNNNN.b...',
    '...NnNNNN.b...',
    '....z..z..b...',
    '...zz..zz.b...',
  ] },
};

// the Bellkeeper (48x56): a bronze bell body with a stone face, rope arms and the clapper as its foot, drawn by shape
function bellkeeper() {
  const W = 48, H = 56, g = Array.from({ length: H }, () => Array(W).fill('.'));
  const cx = 23.5;
  const set = (x, y, c) => { if (x >= 0 && x < W && y >= 0 && y < H) g[y][x] = c; };
  // crown loop (rows 1-6)
  for (let y = 1; y <= 6; y++) for (let x = 0; x < W; x++) {
    const d = Math.hypot(x - cx, y - 4.5);
    if (d <= 3.6 && d >= 1.6) set(x, y, x < cx ? 'X' : 'x');
  }
  // body: dome to flared lip (rows 7-45)
  for (let y = 7; y <= 45; y++) {
    const t = (y - 7) / 38;
    const half = y >= 42 ? 22 : 7 + 13 * Math.pow(t, 0.7) + (t > 0.85 ? (t - 0.85) * 18 : 0);
    for (let x = 0; x < W; x++) {
      const dx = x - cx;
      if (Math.abs(dx) > half) continue;
      const u = dx / half;                              // -1 left (lit) .. +1 right (shadow)
      let c = u < -0.35 ? 'X' : u < 0.55 ? 'x' : 'b';
      if (y === 15 || y === 40) c = u < 0.55 ? 'G' : 'y'; // the two raised bands
      if (y >= 43) c = u < 0.4 ? 'x' : 'b';             // the lip
      set(x, y, c);
    }
  }
  // stone face (rows 18-36), an oval mask on the front of the bell
  for (let y = 18; y <= 36; y++) for (let x = 0; x < W; x++) {
    const nx = (x - cx) / 8.5, ny = (y - 27) / 9.5;
    if (nx * nx + ny * ny > 1) continue;
    set(x, y, nx < -0.3 ? 'v' : nx < 0.5 ? 'C' : 'c');
  }
  for (const [x, y] of [[19, 25], [20, 25], [27, 25], [28, 25]]) set(x, y, 'k');        // brows
  for (const [x, y] of [[19, 26], [20, 26], [27, 26], [28, 26]]) set(x, y, 'd');        // eyes
  for (let x = 20; x <= 27; x++) set(x, 32, 'k');                                       // mouth
  set(19, 33, 'k'); set(28, 33, 'k'); // corners down: stern, not friendly
  for (let y = 27; y <= 30; y++) set(24, y, y === 30 ? 'c' : 'v');                      // nose
  // rope arms from the shoulders, hanging out and down to knotted hands
  for (let i = 0; i <= 12; i++) {
    const y = 24 + i, l = Math.round(cx - 16 - i * 0.55), r = Math.round(cx + 16 + i * 0.55);
    for (const x of [l, l + 1]) set(x, y, i % 3 === 0 ? 'W' : 'w');
    for (const x of [r - 1, r]) set(x, y, i % 3 === 0 ? 'W' : 'w');
  }
  for (const [x0, y0] of [[2, 37], [42, 37]]) for (let y = 0; y < 3; y++) for (let x = 0; x < 4; x++) set(x0 + x, y0 + y, (x + y) % 2 ? 'W' : 'b');
  // the clapper as its foot (rows 46-55)
  for (let y = 46; y <= 55; y++) {
    const half = y < 50 ? 1 : 3 - Math.abs(y - 52.5) * 0.6;
    for (let x = 0; x < W; x++) if (Math.abs(x - cx) <= half) set(x, y, x < cx ? 'X' : 'x');
  }
  return { size: [W, H], grid: g };
}

function outline(g, ink = 'k') {
  const H = g.length, W = g[0].length, o = g.map(r => r.slice());
  for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) {
    if (g[y][x] !== '.') continue;
    if ([[1, 0], [-1, 0], [0, 1], [0, -1]].some(([dx, dy]) => { const X = x + dx, Y = y + dy; return X >= 0 && X < W && Y >= 0 && Y < H && g[Y][X] !== '.' && g[Y][X] !== ink; })) o[y][x] = ink;
  }
  return o;
}
function place({ size: [W, H], fill }) {
  const g = Array.from({ length: H }, () => Array(W).fill('.'));
  const top = H - fill.length;
  fill.forEach((row, j) => [...row.padEnd(W, '.').slice(0, W)].forEach((c, i) => { if (c !== '.') g[top + j][i] = c; }));
  return g;
}

fs.mkdirSync(OUT, { recursive: true });
const sheet = [];
for (const [name, k] of Object.entries(KEYS)) sheet.push({ name, grid: outline(place(k), name === 'anino' ? 'T' : 'k') }); // the Anino: a Spirit-teal rim reads on the night
const bk = bellkeeper();
sheet.push({ name: 'bellkeeper', grid: outline(bk.grid) });
for (const { name, grid } of sheet) {
  const colours = new Set(grid.flat().filter(c => c !== '.'));
  const lines = [`# R4 key frame: ${name} (${colours.size} colours). For review; the other frames are drawn from it once approved.`,
    `name: spr_${name}_key`, `size: ${grid[0].length}x${grid.length}`, 'out: Creatures', 'frame', ...grid.map(r => r.join(''))];
  fs.writeFileSync(path.join(OUT, `${name}_key.pxl.txt`), lines.join('\n') + '\n');
  console.log(`${name}: ${grid[0].length}x${grid.length}, ${colours.size} colours`);
}

// preview: each key at 1x then 4x, on the panel night colour and on a light wood strip (readability)
const previewPath = process.argv[2];
if (previewPath) {
  const pal = {};
  for (const l of fs.readFileSync(path.join(ROOT, 'Assets/Art/Source/palette.txt'), 'utf8').split(/\r?\n/)) {
    const t = l.trim().split(/\s+/);
    if (t.length >= 2 && t[1].startsWith('#')) pal[t[0]] = [1, 3, 5].map(i => parseInt(t[1].substr(i, 2), 16));
  }
  const S = 4, pad = 8;
  let PW = pad, PH = 0;
  for (const { grid } of sheet) { PW += grid[0].length * (S + 1) + pad * 2; PH = Math.max(PH, grid.length * S); }
  PH = PH * 2 + pad * 3;
  const img = Buffer.alloc(PW * PH * 3);
  for (let y = 0; y < PH; y++) for (let x = 0; x < PW; x++) {
    const p = (y * PW + x) * 3, wood = y > PH / 2;
    img[p] = wood ? 0x5a : 0x14; img[p + 1] = wood ? 0x38 : 0x10; img[p + 2] = wood ? 0x20 : 0x1c;
  }
  let ox = pad;
  for (const { grid } of sheet) {
    const w = grid[0].length, h = grid.length;
    for (const [bx, s, by] of [[ox, 1, pad], [ox + w + pad, S, pad], [ox, 1, PH / 2 + pad], [ox + w + pad, S, PH / 2 + pad]])
      for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) {
        const c = pal[grid[y][x]]; if (!c) continue;
        for (let dy = 0; dy < s; dy++) for (let dx = 0; dx < s; dx++) {
          const p = ((by + y * s + dy) * PW + bx + x * s + dx) * 3;
          if (p >= 0 && p < img.length) { img[p] = c[0]; img[p + 1] = c[1]; img[p + 2] = c[2]; }
        }
      }
    ox += w * (S + 1) + pad * 2;
  }
  const raw = Buffer.alloc((PW * 3 + 1) * PH);
  for (let y = 0; y < PH; y++) img.copy(raw, y * (PW * 3 + 1) + 1, y * PW * 3, (y + 1) * PW * 3);
  const crcT = []; for (let n = 0; n < 256; n++) { let c = n; for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1; crcT[n] = c >>> 0; }
  const crc = b => { let c = 0xffffffff; for (const x of b) c = crcT[(c ^ x) & 255] ^ (c >>> 8); return (c ^ 0xffffffff) >>> 0; };
  const chunk = (t, d) => { const l = Buffer.alloc(4); l.writeUInt32BE(d.length); const td = Buffer.concat([Buffer.from(t), d]); const c = Buffer.alloc(4); c.writeUInt32BE(crc(td)); return Buffer.concat([l, td, c]); };
  const ihdr = Buffer.alloc(13); ihdr.writeUInt32BE(PW, 0); ihdr.writeUInt32BE(PH, 4); ihdr[8] = 8; ihdr[9] = 2;
  fs.mkdirSync(path.dirname(previewPath), { recursive: true });
  fs.writeFileSync(previewPath, Buffer.concat([Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]), chunk('IHDR', ihdr), chunk('IDAT', zlib.deflateSync(raw)), chunk('IEND', Buffer.alloc(0))]));
  console.log('preview ' + previewPath);
}
