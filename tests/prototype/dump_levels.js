// Dumps the browser prototype's generated level layouts (buildLevel) to JSON, as an oracle for
// the Unity generator port (Assets/Scripts/Levels/LevelGenerator.cs).
// Usage: npm i puppeteer-core ; node dump_levels.js <index.html> <chrome> <out.json>
const puppeteer = require('puppeteer-core');
const path = require('path');
const fs = require('fs');
const HTML = 'file://' + path.resolve(process.argv[2] || '../../reference/prototype/index.html');
const CHROME = process.argv[3] || '/usr/bin/google-chrome';
const OUT = process.argv[4] || 'fixtures/prototype_levels.json';
(async () => {
  const b = await puppeteer.launch({ executablePath: CHROME, headless: 'new', args: ['--no-sandbox', '--disable-gpu'] });
  const p = await b.newPage();
  await p.goto(HTML);
  await new Promise(r => setTimeout(r, 600));
  const out = await p.evaluate(() => {
    const S = __skp, ALL = ['dj', 'veil', 'bolt', 'swift', 'heart', 'spread', 'veil2', 'glide'];
    const cases = [];
    for (const skills of [[], ALL]) for (const df of ['story', 'standard', 'nightmare'])
      for (let lv = 0; lv < 9; lv++) {
        if (df !== 'standard' && lv > 2) continue;
        S.save.skills = skills.slice();
        const L = S.buildLevel(lv, df, 0);
        cases.push({
          lv, df, skills: skills.slice(), W: L.W, H: L.H, y0: L.y0, rooms: L.rooms, clueRoom: L.clueRoom, needClue: L.needClue,
          feat: L.feat,
          plats: L.plats.map(o => ({ x: o.x, y: o.y, w: o.w, h: o.h, m: o.m, kind: o.kind, bx: o.bx || 0, amp: o.amp || 0, sp: o.sp || 0, ph: o.ph || 0 })),
          haz: L.haz.map(o => ({ x: o.x, y: o.y, w: o.w, h: o.h, m: o.m, kind: o.kind, up: !!o.up, ph: o.ph || 0, gaunt: !!o.gaunt })),
          doors: L.doors.map(o => ({ x: o.x, y: o.y, w: o.w, h: o.h, m: o.m, kind: o.kind, dim: o.dim, up: !!o.up, to: o.to || null, room: o.room })),
          items: L.items.map(o => ({ x: o.x, y: o.y, m: o.m, kind: o.kind })),
          ens: L.ens.map(o => ({ t: o.t, x: o.x, y: o.y, w: o.w, h: o.h, m: o.m, hp: o.hp, spd: o.spd || 0, min: o.min || 0, max: o.max || 0, cy: o.cy || 0, ph: o.ph || 0 })),
          lants: L.lants.map(o => ({ x: o.x, y: o.y, room: o.room })),
          water: L.water ? { y: L.water.y, sp: L.water.sp } : null,
          bossArena: L.bossArena || null,
        });
      }
    S.save.skills = [];
    return cases;
  });
  fs.mkdirSync(path.dirname(OUT), { recursive: true });
  fs.writeFileSync(OUT, JSON.stringify({ source: 'reference/prototype/index.html buildLevel', cases: out }));
  console.log('wrote', out.length, 'layouts to', OUT);
  await b.close();
})();
