// Tiny image writers for the hand-pixelled previews: RGB PNG and a looping palette GIF (no dependencies).
const fs = require('fs'), zlib = require('zlib');
const T = []; for (let n = 0; n < 256; n++) { let c = n; for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1; T[n] = c >>> 0; }
const crc = b => { let c = 0xffffffff; for (const x of b) c = T[(c ^ x) & 255] ^ (c >>> 8); return (c ^ 0xffffffff) >>> 0; };
const ch = (t, d) => { const l = Buffer.alloc(4); l.writeUInt32BE(d.length); const td = Buffer.concat([Buffer.from(t), d]); const c = Buffer.alloc(4); c.writeUInt32BE(crc(td)); return Buffer.concat([l, td, c]); };
const rgb = h => [1, 3, 5].map(k => parseInt(h.substr(k, 2), 16));

// canvas of palette letters; '.' shows the background colour
function canvas(w, h) { return { w, h, px: Array.from({ length: h }, () => Array(w).fill('.')) }; }
function blit(cv, grid, x0, y0, s) {
  grid.forEach((row, y) => row.forEach((c, x) => { if (c === '.') return;
    for (let dy = 0; dy < s; dy++) for (let dx = 0; dx < s; dx++) cv.px[y0 + y * s + dy][x0 + x * s + dx] = c; }));
}
function png(path, cv, PAL, bg = '#241a2e') {
  const { w, h, px } = cv; const raw = Buffer.alloc((w * 3 + 1) * h);
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) { const c = px[y][x]; const v = rgb(c === '.' ? bg : PAL[c]); if (!v || isNaN(v[0])) throw new Error('unknown ' + c); v.forEach((b, k) => { raw[y * (w * 3 + 1) + 1 + x * 3 + k] = b; }); }
  const ih = Buffer.alloc(13); ih.writeUInt32BE(w, 0); ih.writeUInt32BE(h, 4); ih[8] = 8; ih[9] = 2;
  fs.writeFileSync(path, Buffer.concat([Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]), ch('IHDR', ih), ch('IDAT', zlib.deflateSync(raw)), ch('IEND', Buffer.alloc(0))]));
}
// frames: canvases of the same size; delayCs: hundredths of a second per frame
function gif(path, frames, PAL, delayCs, bg = '#241a2e') {
  const keys = ['.', ...Object.keys(PAL)]; if (keys.length > 256) throw new Error('too many colours');
  const index = Object.fromEntries(keys.map((k, i) => [k, i]));
  const { w, h } = frames[0]; const out = [];
  const u16 = n => [n & 255, n >> 8];
  out.push(...Buffer.from('GIF89a'), ...u16(w), ...u16(h), 0xf7, 0, 0);
  for (let i = 0; i < 256; i++) out.push(...(i < keys.length ? rgb(i === 0 ? bg : PAL[keys[i]]) : [0, 0, 0]));
  out.push(0x21, 0xff, 11, ...Buffer.from('NETSCAPE2.0'), 3, 1, 0, 0, 0);
  for (const f of frames) {
    out.push(0x21, 0xf9, 4, 0, ...u16(delayCs), 0, 0, 0x2c, 0, 0, 0, 0, ...u16(w), ...u16(h), 0, 8);
    // uncompressed LZW: 9-bit literal codes with a clear code before the table can grow
    const bytes = []; let acc = 0, bits = 0; const put = c => { acc |= c << bits; bits += 9; while (bits >= 8) { bytes.push(acc & 255); acc >>= 8; bits -= 8; } };
    let n = 0; put(256);
    for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) { if (n === 250) { put(256); n = 0; } put(index[f.px[y][x]]); n++; }
    put(257); if (bits > 0) bytes.push(acc & 255);
    for (let i = 0; i < bytes.length; i += 255) { const b = bytes.slice(i, i + 255); out.push(b.length, ...b); }
    out.push(0);
  }
  out.push(0x3b); fs.writeFileSync(path, Buffer.from(out));
}
module.exports = { canvas, blit, png, gif };
