// A-2/A-3: Tala at 3x detail (48x54 px = her 16x18 world box at 48 px per unit), drawn natively at game size by a
// jointed figure: every frame is a pose (joint angles, bob, hair sway), every body part a shape with its own colour
// ramp. Shading follows each part's surface direction against light from the upper left; the outline is a darker
// shade of what it borders (selective outline), and parts in front get an inner edge where they overlap parts behind.
// Writes Assets/Art/Game48/Characters/spr_tala48_<char>_<anim>_<n>.png and an optional preview sheet.
// Run: node tools/tala_rig/make_tala48.js [preview.png]
const fs = require('fs'), path = require('path'), zlib = require('zlib');
const ROOT = path.resolve(__dirname, '../..');
const OUT = path.join(ROOT, 'Assets/Art/Game48/Characters');
const W = 48, H = 54;

// ---- colour ramps: [highlight, base, shade, deep] ----
const hex = h => [1, 3, 5].map(i => parseInt(h.substr(i, 2), 16));
const R = {
  skin:   ['#f7cf9c', '#e0a46f', '#b97a4e', '#8a5434'].map(hex),
  hair:   ['#4a3226', '#2e1f18', '#1d130f', '#0f0a08'].map(hex),
  camisa: ['#fffaf0', '#f2e4c4', '#d4bf96', '#a8916a'].map(hex),
  embroid:['#e86a5a', '#c0392b', '#8a2420', '#5e1714'].map(hex),
  sash:   ['#ee5a48', '#c0392b', '#8a2420', '#5e1714'].map(hex),
  skirt:  ['#d0473a', '#a8322a', '#7a2420', '#521714'].map(hex),
  shorts: ['#6a5a8a', '#4a3f66', '#332b48', '#211c30'].map(hex),
  sandal: ['#a8743e', '#7a4f28', '#55361b', '#3a2410'].map(hex),
  parol:  ['#fff6c8', '#ffd36a', '#e8a83a', '#a8782e'].map(hex),
  frame:  ['#c08a44', '#8a5a2a', '#5a3820', '#3a2410'].map(hex),
  tassel: ['#5fd07a', '#2e9a4a', '#c0392b', '#e8b85a'].map(hex),
  eye:    ['#1d130f', '#1d130f', '#1d130f', '#1d130f'].map(hex),
  shine:  ['#ffffff', '#ffffff', '#ffffff', '#ffffff'].map(hex),
  blush:  ['#f4b08e', '#eb9a7e', '#d8846c', '#b8685a'].map(hex),
  mouth:  ['#7a2e26', '#5e211c', '#451712', '#2e0e0b'].map(hex),
  glow:   ['#fff8e0', '#ffe9a0', '#ffd36a', '#e8b85a'].map(hex),
};
const OUTLINE_DARKEN = 0.42;

// ---- geometry helpers ----
const rot = (x, y, a) => [x * Math.cos(a) - y * Math.sin(a), x * Math.sin(a) + y * Math.cos(a)];
function segDist(px, py, ax, ay, bx, by) {
  const dx = bx - ax, dy = by - ay, l = dx * dx + dy * dy || 1;
  const t = Math.max(0, Math.min(1, ((px - ax) * dx + (py - ay) * dy) / l));
  const cx = ax + t * dx, cy = ay + t * dy;
  return [Math.hypot(px - cx, py - cy), cx, cy];
}

// a shape: test(x,y) -> null or {mat, nx, ny} (surface normal, for shading); z order by list position
function capsule(mat, a, b, r, opt = {}) {
  return { mat, test(x, y) {
    const [d, cx, cy] = segDist(x, y, a[0], a[1], b[0], b[1]);
    const rr = r + (opt.taper ? opt.taper * ((Math.hypot(cx - a[0], cy - a[1])) / (Math.hypot(b[0] - a[0], b[1] - a[1]) || 1)) : 0);
    if (d > rr) return null;
    return { nx: (x - cx) / rr, ny: (y - cy) / rr };
  } };
}
function ellipse(mat, cx, cy, rx, ry, opt = {}) {
  return { mat, test(x, y) {
    const u = (x - cx) / rx, v = (y - cy) / ry;
    if (u * u + v * v > 1) return null;
    if (opt.clip && !opt.clip(x, y)) return null;
    return { nx: u, ny: v };
  } };
}
function poly(mat, pts, opt = {}) {
  const cx = pts.reduce((s, p) => s + p[0], 0) / pts.length, cy = pts.reduce((s, p) => s + p[1], 0) / pts.length;
  return { mat, test(x, y) {
    let inside = false;
    for (let i = 0, j = pts.length - 1; i < pts.length; j = i++) {
      const [xi, yi] = pts[i], [xj, yj] = pts[j];
      if ((yi > y) !== (yj > y) && x < (xj - xi) * (y - yi) / (yj - yi) + xi) inside = !inside;
    }
    if (!inside) return null;
    return { nx: (x - cx) / (opt.rx || 8), ny: (y - cy) / (opt.ry || 8), flat: opt.flat };
  } };
}
function star(mat, cx, cy, r, rin, a0 = -Math.PI / 2) {
  const pts = [];
  for (let i = 0; i < 10; i++) { const a = a0 + i * Math.PI / 5, rr = i % 2 ? rin : r; pts.push([cx + Math.cos(a) * rr, cy + Math.sin(a) * rr]); }
  return poly(mat, pts, { rx: r, ry: r });
}

// ---- the figure ----
// pose: { bob, lean, hip:[backThigh, backKnee, frontThigh, frontKnee] (radians, 0 = straight down), arm:[backSh, backEl, frontSh, frontEl],
//         sway (hair), parol:[dx,dy] extra, eyes:'open'|'shut', mouth:'calm'|'open'|'grit', flash:bool, squash }
function figure(ch, p) {
  const boy = ch === 'boy';
  const s = [];
  const by = p.bob || 0, lean = p.lean || 0, sq = p.squash || 0;
  const hip = [23 + lean * 0.5, 34 + by + sq];
  const sh = [23 + lean * 1.5, 21 + by + sq * 1.4];     // shoulder line centre
  const head = [24 + lean * 2, 12 + by + sq * 1.6];
  const L = (o, a1, a2, l1, l2) => { // leg from hip offset o: thigh angle a1, knee bend a2
    const h = [hip[0] + o, hip[1]];
    const [kx, ky] = rot(0, l1, a1), k = [h[0] + kx, h[1] + ky];
    const [fx, fy] = rot(0, l2, a1 + a2), f = [k[0] + fx, k[1] + fy];
    return { h, k, f };
  };
  const legLen1 = 9 - sq * 0.3, legLen2 = 9 - sq * 0.3;
  const back = L(-2, p.hip[0], p.hip[1], legLen1, legLen2), front = L(2, p.hip[2], p.hip[3], legLen1, legLen2);
  const A = (o, a1, a2) => {
    const s0 = [sh[0] + o, sh[1] + 1];
    const [ex, ey] = rot(0, 5.6, a1), e = [s0[0] + ex, s0[1] + ey];
    const [hx, hy] = rot(0, 5.2, a1 + a2), h = [e[0] + hx, e[1] + hy];
    return { s0, e, h };
  };
  const bArm = A(-6, p.arm[0], p.arm[1]), fArm = A(5, p.arm[2], p.arm[3]);
  const sway = p.sway || 0;

  // back to front
  if (!boy) s.push(poly('hair', [[head[0] - 7, head[1] - 2], [head[0] - 2, head[1] + 2], [head[0] - 3 - sway, head[1] + 17], [head[0] - 9 - sway * 1.6, head[1] + 19], [head[0] - 11 - sway, head[1] + 8]], { rx: 6, ry: 10 }));
  s.push(capsule('skin', bArm.s0, bArm.e, 2.1), capsule('skin', bArm.e, bArm.h, 1.9), ellipse('skin', bArm.h[0], bArm.h[1] + 0.5, 2.2, 2));
  s.push(capsule('skin', back.h, back.k, 2.6), capsule('skin', back.k, back.f, 2.2), ellipse('sandal', back.f[0] + 1.6, back.f[1] + 1, 3.4, 1.8));
  s.push(capsule('skin', front.h, front.k, 2.7), capsule('skin', front.k, front.f, 2.3), ellipse('sandal', front.f[0] + 1.6, front.f[1] + 1, 3.6, 1.9));
  if (boy) s.push(poly('shorts', [[hip[0] - 7, hip[1] - 4], [hip[0] + 7, hip[1] - 4], [front.k[0] + 3, front.k[1] - 2], [hip[0] + 0.5, hip[1] + 1.5], [back.k[0] - 3, back.k[1] - 2]], { rx: 7, ry: 6 }));
  else s.push(poly('skirt', [[hip[0] - 7, hip[1] - 4], [hip[0] + 7, hip[1] - 4], [hip[0] + 9 + lean, hip[1] + 7 - sq], [hip[0] - 9 + lean * 0.4, hip[1] + 7 - sq]], { rx: 8, ry: 6 }));
  // torso (camisa) and sash
  s.push(poly('camisa', [[sh[0] - 7, sh[1] - 1], [sh[0] + 6, sh[1] - 1], [hip[0] + 7, hip[1] - 3], [hip[0] - 7, hip[1] - 3]], { rx: 7, ry: 7 }));
  s.push(ellipse('camisa', sh[0] - 6, sh[1] + 1.5, 3.2, 3), ellipse('camisa', sh[0] + 5, sh[1] + 1.5, 3.2, 3)); // puffed sleeves
  s.push(poly('sash', [[hip[0] - 7.5, hip[1] - 6], [hip[0] + 7.5, hip[1] - 6], [hip[0] + 7.5, hip[1] - 2.5], [hip[0] - 7.5, hip[1] - 2.5]], { rx: 8, ry: 2, flat: true }));
  s.push(poly('sash', [[hip[0] - 6, hip[1] - 3], [hip[0] - 3, hip[1] - 3], [hip[0] - 6 - sway * 0.6, hip[1] + 4], [hip[0] - 9 - sway, hip[1] + 3]], { rx: 3, ry: 4 }));
  s.push(ellipse('embroid', sh[0] - 1, sh[1] + 4, 1.4, 1.2), ellipse('embroid', sh[0] + 2.5, sh[1] + 6, 1.1, 1), ellipse('embroid', sh[0] - 3, sh[1] + 7, 1, 1));
  // head
  s.push(ellipse('skin', head[0] + 0.5, head[1] + 7.5, 2.2, 2.5)); // neck
  s.push(ellipse('skin', head[0], head[1], 9.2, 9.4));
  s.push(ellipse('hair', head[0] - 1.2, head[1] - 2.8, 9.9, 8, { clip: (x, y) => y < head[1] - 1.5 + (x - head[0] < -2 ? 9 : x - head[0] > 4 ? -1 : 0) }));
  s.push(poly('hair', [[head[0] + 1, head[1] - 8], [head[0] + 9.5, head[1] - 3.5], [head[0] + 8, head[1] - 1], [head[0] + 5.5, head[1] - 3], [head[0] + 3.5, head[1] - 2], [head[0] + 2.5, head[1] - 4]], { rx: 5, ry: 4 })); // fringe
  if (boy) s.push(ellipse('hair', head[0] - 6.5, head[1] + 1, 3.4, 5.5),
    poly('hair', [[head[0] - 3, head[1] - 9], [head[0] - 1, head[1] - 12], [head[0] + 1, head[1] - 9]], { rx: 2, ry: 2 }),
    poly('hair', [[head[0] - 7, head[1] - 6], [head[0] - 8, head[1] - 10], [head[0] - 4, head[1] - 8.5]], { rx: 2, ry: 2 }));
  else s.push(ellipse('sash', head[0] - 6.5, head[1] - 5.5, 2, 1.8)); // red ribbon
  // face
  const ex = head[0] + 4.6, ey = head[1] + 0.8;
  if (p.eyes === 'shut') s.push(capsule('eye', [ex - 1.3, ey + 0.8], [ex + 1.3, ey + 1.2], 0.6));
  else s.push(poly('eye', [[ex - 1.2, ey - 1.6], [ex + 1.4, ey - 1.6], [ex + 1.4, ey + 2], [ex - 1.2, ey + 2]], { flat: true }), poly('shine', [[ex + 0.1, ey - 1.1], [ex + 1.1, ey - 1.1], [ex + 1.1, ey - 0.1], [ex + 0.1, ey - 0.1]], { flat: true }));
  s.push(capsule('hair', [ex - 1.8, ey - 3.4], [ex + 1.8, ey - 3.8], 0.55)); // brow
  if (p.mouth === 'open') s.push(ellipse('mouth', head[0] + 6.2, head[1] + 5.6, 1.1, 1.3));
  else if (p.mouth === 'grit') s.push(capsule('mouth', [head[0] + 4.8, head[1] + 5.6], [head[0] + 7.2, head[1] + 5.4], 0.6));
  else s.push(capsule('mouth', [head[0] + 5.4, head[1] + 5.6], [head[0] + 6.8, head[1] + 5.4], 0.45));
  s.push(ellipse('blush', head[0] + 2.2, head[1] + 3.6, 1.6, 0.9));
  // front arm and the parol on its string
  const pd = p.parol || [0, 0];
  const pc = [fArm.h[0] + 3 + pd[0], fArm.h[1] + 6 + pd[1]];
  s.push(capsule('frame', fArm.h, [pc[0], pc[1] - 5], 0.45));
  const pr = p.bigParol ? 6.6 : 5.8;
  s.push(star('frame', pc[0], pc[1], pr, pr * 0.5));
  s.push(star('parol', pc[0], pc[1], pr - 1.4, pr * 0.5 - 0.8));
  s.push(ellipse('glow', pc[0], pc[1], p.glow ? 2.4 : 1.6, p.glow ? 2.4 : 1.6));
  s.push(capsule('tassel', [pc[0] - 3, pc[1] + 3], [pc[0] - 3.5, pc[1] + 7], 0.6), capsule('tassel', [pc[0] + 3, pc[1] + 3], [pc[0] + 3.5, pc[1] + 7], 0.6));
  s.push(capsule('skin', fArm.s0, fArm.e, 2.1), capsule('skin', fArm.e, fArm.h, 1.9), ellipse('skin', fArm.h[0], fArm.h[1], 2.2, 2.1));
  s.push(ellipse('camisa', fArm.s0[0], fArm.s0[1] + 0.5, 3.2, 3)); // front sleeve over the shoulder
  return s;
}

// ---- rasterise: id buffer (which shape), shade from normals, then outlines ----
const LIGHT = (() => { const l = [-0.62, -0.78]; return l; })();
function render(shapes, opt = {}) {
  const id = new Int16Array(W * H).fill(-1), nrm = new Float32Array(W * H * 2), flags = new Uint8Array(W * H);
  shapes.forEach((sh, i) => {
    for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) {
      const r = sh.test(x + 0.5, y + 0.5);
      if (!r) continue;
      id[y * W + x] = i; nrm[(y * W + x) * 2] = r.nx; nrm[(y * W + x) * 2 + 1] = r.ny; flags[y * W + x] = r.flat ? 1 : 0;
    }
  });
  const px = new Uint8Array(W * H * 4);
  for (let i = 0; i < W * H; i++) {
    if (id[i] < 0) continue;
    const sh = shapes[id[i]], ramp = R[sh.mat];
    const nx = nrm[i * 2], ny = nrm[i * 2 + 1];
    const edge = Math.min(1, Math.hypot(nx, ny));
    let lit = -(nx * LIGHT[0] + ny * LIGHT[1]);              // +1 facing the light
    let t = flags[i] ? (ny < -0.2 ? 0 : ny > 0.4 ? 2 : 1) : lit > 0.45 && edge > 0.3 ? 0 : lit < -0.35 && edge > 0.45 ? 2 : 1;
    if (!flags[i] && edge > 0.92 && lit < -0.1) t = 3;       // core shadow on the far rim
    const x = i % W, y = (i / W) | 0;
    if (sh.mat === 'hair' && t <= 1 && (x * 2 + y) % 5 === 0) t++;           // strands
    if ((sh.mat === 'skirt' || sh.mat === 'shorts') && t <= 1 && x % 4 === 1) t = 2; // folds
    if (sh.mat === 'camisa' && t === 1 && y % 6 === 0 && x % 2 === 0) t = 2;   // weave
    if (sh.mat === 'glow') t = edge > 0.7 ? 3 : edge > 0.45 ? 2 : edge > 0.2 ? 1 : 0;
    const c = ramp[Math.max(0, Math.min(3, t))];
    px[i * 4] = c[0]; px[i * 4 + 1] = c[1]; px[i * 4 + 2] = c[2]; px[i * 4 + 3] = 255;
  }
  // eye shine and blush: small marks keep their own colour
  shapes.forEach((sh, i) => {
    if (sh.mat !== 'eye' && sh.mat !== 'embroid') return;
  });
  // selective outline: transparent pixels next to the figure take a darkened copy of the neighbour
  const out = px.slice();
  for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) {
    const i = y * W + x;
    if (id[i] >= 0) {
      // inner edge: a pixel of a part behind, touching a part in front of a different material, darkens
      const me = id[i];
      for (const [dx, dy] of [[1, 0], [-1, 0], [0, 1], [0, -1]]) {
        const X = x + dx, Y = y + dy; if (X < 0 || Y < 0 || X >= W || Y >= H) continue;
        const o = id[Y * W + X];
        if (o > me && shapes[o].mat !== shapes[me].mat && !['eye', 'embroid', 'glow', 'frame', 'shine', 'blush', 'mouth'].includes(shapes[o].mat) && !['glow'].includes(shapes[me].mat)) {
          const ramp = R[shapes[me].mat]; const c = ramp[3];
          out[i * 4] = c[0]; out[i * 4 + 1] = c[1]; out[i * 4 + 2] = c[2];
          break;
        }
      }
      continue;
    }
    let best = -1;
    for (const [dx, dy] of [[1, 0], [-1, 0], [0, 1], [0, -1]]) {
      const X = x + dx, Y = y + dy; if (X < 0 || Y < 0 || X >= W || Y >= H) continue;
      const o = id[Y * W + X]; if (o >= 0 && shapes[o].mat !== 'glow' && o > best) best = o;
    }
    if (best < 0) continue;
    const c = R[shapes[best].mat][3];
    if (opt.flash) { out[i * 4] = 255; out[i * 4 + 1] = 250; out[i * 4 + 2] = 235; out[i * 4 + 3] = 255; continue; } // hurt: a white rim, the body keeps its colours
    out[i * 4] = Math.round(c[0] * OUTLINE_DARKEN); out[i * 4 + 1] = Math.round(c[1] * OUTLINE_DARKEN); out[i * 4 + 2] = Math.round(c[2] * OUTLINE_DARKEN); out[i * 4 + 3] = 255;
  }
  // eye shine
  if (opt.fade) for (let i = 0; i < W * H; i++) if (out[i * 4 + 3]) {
    const x = i % W, y = (i / W) | 0;
    if (((x * 7 + y * 13) % 10) < opt.fade * 10) out[i * 4 + 3] = 0;
    else { const g = (out[i * 4] + out[i * 4 + 1] + out[i * 4 + 2]) / 3; out[i * 4] = g * 0.5 + 20; out[i * 4 + 1] = g * 0.45 + 14; out[i * 4 + 2] = g * 0.6 + 30; }
  }
  if (opt.dark) for (let i = 0; i < W * H; i++) if (out[i * 4 + 3]) { out[i * 4] *= 1 - opt.dark; out[i * 4 + 1] *= 1 - opt.dark; out[i * 4 + 2] = out[i * 4 + 2] * (1 - opt.dark) + 30 * opt.dark; }
  return out;
}

// ---- poses per animation ----
const D = Math.PI / 180;
const stand = { hip: [3 * D, 0, -3 * D, 0], arm: [10 * D, -10 * D, -35 * D, -40 * D] };
const P = (o) => Object.assign({}, stand, o);
const RUN = [ // thigh/knee back, thigh/knee front, arms, bob
  [32, 30, -38, 20, 30, -30, -70, -20, 0], [12, 70, -22, 10, 15, -20, -50, -30, -1], [-10, 40, 0, 10, 0, -10, -35, -40, -1],
  [-38, 20, 32, 30, -25, -10, -10, -50, 0], [-22, 10, 12, 70, -10, -20, -25, -45, -1], [0, 10, -10, 40, 0, -10, -35, -40, -1],
].map(([a, b, c, d, e, f, g, h, bob]) => P({ hip: [a * D, b * D, c * D, d * D], arm: [e * D, f * D, g * D, h * D], bob, lean: 1, sway: 1 + (bob ? 1 : 0) }));
const ANIMS = {
  idle: [P({}), P({ bob: 0, sway: 0.5 }), P({ bob: 1, squash: 0, sway: 1, parol: [0, 1] }), P({ sway: 0.5 })],
  run: RUN,
  jump_rise: [P({ hip: [25 * D, 50 * D, -10 * D, 30 * D], arm: [-40 * D, -20 * D, -80 * D, -30 * D], sway: -1 }), P({ hip: [30 * D, 70 * D, -5 * D, 50 * D], arm: [-50 * D, -20 * D, -90 * D, -30 * D], sway: -2 })],
  apex: [P({ hip: [20 * D, 60 * D, -15 * D, 50 * D], arm: [-60 * D, -10 * D, -100 * D, -20 * D], sway: -1, bigParol: true, glow: true })],
  fall: [P({ hip: [10 * D, 20 * D, -10 * D, 10 * D], arm: [-80 * D, 10 * D, -120 * D, -10 * D], sway: -3 }), P({ hip: [5 * D, 15 * D, -12 * D, 20 * D], arm: [-90 * D, 10 * D, -125 * D, -10 * D], sway: -4 })],
  land: [P({ hip: [30 * D, -60 * D, -30 * D, 60 * D].map(v => v * 0.5), squash: 3, arm: [20 * D, -20 * D, -20 * D, -40 * D], sway: 2 }), P({ squash: 1, sway: 1 })],
  dash: [P({ hip: [40 * D, 40 * D, -45 * D, 20 * D], arm: [50 * D, 10 * D, -60 * D, -10 * D], lean: 3, sway: 3 }), P({ hip: [45 * D, 45 * D, -50 * D, 15 * D], arm: [60 * D, 10 * D, -70 * D, -10 * D], lean: 4, sway: 4, glow: true }), P({ hip: [30 * D, 40 * D, -35 * D, 20 * D], arm: [40 * D, 10 * D, -50 * D, -20 * D], lean: 2, sway: 2 })],
  ward: [P({ arm: [10 * D, -10 * D, -100 * D, -30 * D], parol: [-2, -2] }), P({ arm: [10 * D, -10 * D, -150 * D, -10 * D], parol: [-3, -4], glow: true, bigParol: true }), P({ arm: [10 * D, -10 * D, -165 * D, 0], parol: [-4, -5], glow: true, bigParol: true })],
  hurt: [P({ lean: -2, eyes: 'shut', mouth: 'grit', arm: [40 * D, 0, 10 * D, -30 * D], sway: 3, flash: true }), P({ lean: -2, eyes: 'shut', mouth: 'grit', arm: [40 * D, 0, 10 * D, -30 * D], sway: 2 })],
  death: [P({ lean: -2, eyes: 'shut', mouth: 'open', sway: 3 }), P({ squash: 4, eyes: 'shut', hip: [40 * D, -80 * D, -40 * D, 80 * D].map(v => v * 0.6), sway: 2 }),
    P({ squash: 9, eyes: 'shut', hip: [70 * D, -150 * D, -10 * D, 100 * D], arm: [20 * D, 0, 20 * D, -10 * D] }),
    P({ squash: 9, eyes: 'shut', hip: [70 * D, -150 * D, -10 * D, 100 * D], arm: [20 * D, 0, 20 * D, -10 * D], fade: 0.35 }),
    P({ squash: 9, eyes: 'shut', hip: [70 * D, -150 * D, -10 * D, 100 * D], arm: [20 * D, 0, 20 * D, -10 * D], fade: 0.75 })],
  door: [P({}), P({ dark: 0.25 }), P({ dark: 0.55, parol: [-1, -1] }), P({ dark: 0.8, fade: 0.4 })],
  glide: [P({ hip: [10 * D, 30 * D, -5 * D, 25 * D], arm: [-150 * D, 0, -170 * D, 0], parol: [-5, -10], glow: true, sway: -3 }), P({ bob: 1, hip: [12 * D, 35 * D, -8 * D, 30 * D], arm: [-145 * D, 0, -165 * D, 0], parol: [-5, -10], glow: true, sway: -4 })],
  bolt: [P({ arm: [10 * D, -10 * D, -80 * D, 0], parol: [2, -2], bigParol: true, glow: true, mouth: 'open' }), P({ arm: [10 * D, -10 * D, -88 * D, 0], parol: [3, -3], bigParol: true, glow: true, mouth: 'open', lean: 1 })],
};

// ---- PNG writer (RGBA) ----
const crcT = []; for (let n = 0; n < 256; n++) { let c = n; for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1; crcT[n] = c >>> 0; }
const crc = b => { let c = 0xffffffff; for (const x of b) c = crcT[(c ^ x) & 255] ^ (c >>> 8); return (c ^ 0xffffffff) >>> 0; };
const chunk = (t, d) => { const l = Buffer.alloc(4); l.writeUInt32BE(d.length); const td = Buffer.concat([Buffer.from(t), d]); const c = Buffer.alloc(4); c.writeUInt32BE(crc(td)); return Buffer.concat([l, td, c]); };
function png(w, h, rgba, channels = 4) {
  const raw = Buffer.alloc((w * channels + 1) * h);
  for (let y = 0; y < h; y++) Buffer.from(rgba.buffer, rgba.byteOffset + y * w * channels, w * channels).copy(raw, y * (w * channels + 1) + 1);
  const ihdr = Buffer.alloc(13); ihdr.writeUInt32BE(w, 0); ihdr.writeUInt32BE(h, 4); ihdr[8] = 8; ihdr[9] = channels === 4 ? 6 : 2;
  return Buffer.concat([Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]), chunk('IHDR', ihdr), chunk('IDAT', zlib.deflateSync(raw)), chunk('IEND', Buffer.alloc(0))]);
}

fs.mkdirSync(OUT, { recursive: true });
const rows = [];
let count = 0;
for (const ch of ['girl', 'boy']) for (const [anim, poses] of Object.entries(ANIMS)) {
  const frames = poses.map(p => render(figure(ch, p), p));
  frames.forEach((f, i) => { fs.writeFileSync(path.join(OUT, `spr_tala48_${ch}_${anim}_${i}.png`), png(W, H, f)); count++; });
  rows.push({ ch, anim, frames });
}
console.log(`wrote ${count} frames to ${path.relative(ROOT, OUT)}`);

const preview = process.argv[2];
if (preview) { // 3x preview, girl rows left, boy rows right, on night violet
  const S = 3, pad = 6, maxF = 6, cw = W * S + pad, half = rows.length / 2;
  const PW = pad + 2 * (maxF * cw + pad * 4), PH = pad + half * (H * S + pad);
  const img = new Uint8Array(PW * PH * 3);
  for (let i = 0; i < PW * PH; i++) { img[i * 3] = 0x24; img[i * 3 + 1] = 0x1a; img[i * 3 + 2] = 0x2e; }
  rows.forEach((r, ri) => {
    const col = ri < half ? 0 : 1, row = ri % half;
    r.frames.forEach((f, fi) => {
      const ox = pad + col * (maxF * cw + pad * 4) + fi * cw, oy = pad + row * (H * S + pad);
      for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) {
        const i = (y * W + x) * 4; if (!f[i + 3]) continue;
        for (let dy = 0; dy < S; dy++) for (let dx = 0; dx < S; dx++) { const p = ((oy + y * S + dy) * PW + ox + x * S + dx) * 3; img[p] = f[i]; img[p + 1] = f[i + 1]; img[p + 2] = f[i + 2]; }
      }
    });
  });
  fs.mkdirSync(path.dirname(preview), { recursive: true });
  fs.writeFileSync(preview, png(PW, PH, img, 3));
  console.log('preview ' + preview);
}
