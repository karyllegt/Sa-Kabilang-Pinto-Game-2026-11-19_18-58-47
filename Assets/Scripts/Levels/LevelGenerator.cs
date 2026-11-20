using System;
using System.Collections.Generic;
using System.Linq;

namespace Skp.Levels
{
    /// <summary>
    /// Port of the prototype's buildLevel() and route() (reference/prototype/index.html), statement by
    /// statement and in the same random-draw order, so a given level, difficulty and skill set produces
    /// exactly the prototype's layout (checked against tests/prototype/fixtures/prototype_levels.json).
    /// Deterministic: the seed depends only on the level index (or the endless loop).
    /// </summary>
    public static class LevelGenerator
    {
        const int T = LevelLayout.T, LW = LevelLayout.LW, ORD = 1, SPI = 2, INV = 4, ALL = 7;

        /// <param name="lv">0-based level index, or -1 for an Endless Nightmare loop.</param>
        /// <param name="hazardMult">Difficulty hazard multiplier (Story 0.6, Standard 1, Nightmare 1.4, Endless 1.3).</param>
        /// <param name="enemyMult">Difficulty enemy multiplier.</param>
        /// <param name="floodMult">Difficulty flood multiplier.</param>
        /// <param name="hasSkill">Skills owned when the level is built (skill-aware obstacles).</param>
        public static LevelLayout Build(LevelSpec cfg, int lv, double hazardMult, double enemyMult, double floodMult,
                                        Func<string, bool> hasSkill, int loop = 0)
        {
            hasSkill ??= _ => false;
            double seed = (lv >= 0 ? lv + 1 : 60 + loop) * 2654435.0 + 17;
            var R = new SeededRandom(seed);
            var R2 = new SeededRandom(seed * 3 + 7);
            var RS = new SeededRandom(seed * 5 + 11);
            int lvE = lv >= 0 ? lv : 8 + loop;
            int RH = 22 * T, rooms = cfg.rooms, arena = cfg.boss ? 19 * T : 0;
            double Hpx = (rooms * 22 + 5) * T + arena, y0 = Hpx - 3 * T;

            var L = new LevelLayout { Spec = cfg, LevelIndex = lv, Loop = loop, W = LW * T, H = Hpx, Y0 = y0, Rooms = rooms };
            LPlat P(double x, double y, double w, int m, string kind = "wood", double h = 0)
            {
                var o = new LPlat { x = x, y = y, w = w, h = h == 0 ? 8 : h, m = m, kind = kind };
                L.Plats.Add(o);
                return o;
            }

            int clueRoom = cfg.endless ? -1 : rooms / 2;
            L.ClueRoom = cfg.boss ? Math.Max(0, rooms - 2) : clueRoom;
            L.NeedClue = !cfg.endless && !cfg.boss;
            P(0, y0, LW * T, ALL, "floor", 3 * T);
            L.Lanterns.Add(new LLantern { x = 15 * T, y = y0, room = -1 });
            double enMul, hzMul;
            double lvS = 1 + .08 * Math.Max(0, lv);
            int shardsLeft = 3;

            for (int r = 0; r < rooms; r++)
            {
                double ys = y0 - r * RH, ye = ys - RH;
                bool last = r == rooms - 1;
                double rmS = 1 + .05 * r;
                hzMul = Math.Min(2.4, hazardMult * lvS * rmS);
                enMul = Math.Min(2.2, enemyMult * (1 + .06 * Math.Max(0, lv)) * (1 + .04 * r));
                P(0, ye, LW * T, ALL, "floor");
                // doors on the start ledge
                L.Doors.Add(new LDoor { x = 6 * T, y = ys - 28, w = 16, h = 28, kind = "enter", dim = SPI, m = ORD, room = r });
                L.Doors.Add(new LDoor { x = 6 * T, y = ys - 28, w = 16, h = 28, kind = "home", dim = ORD, m = SPI, room = r });
                if (cfg.inverted) L.Doors.Add(new LDoor { x = 24 * T, y = ys - 28, w = 16, h = 28, kind = "enter", dim = INV, m = ORD, room = r });
                // lantern on the room's top floor
                double lx = (last && !cfg.boss) ? 9 * T : 15 * T;
                L.Lanterns.Add(new LLantern { x = lx, y = ye, room = r });
                if (last && !cfg.boss) L.Doors.Add(new LDoor { x = 24 * T, y = ye - 32, w = 20, h = 32, kind = "exit", dim = ORD, m = ALL, room = r });

                // ---- ordinary route
                int act = cfg.act;
                bool hasClue = r == L.ClueRoom && L.NeedClue;
                var oo = new RouteOpts { dyMin = 1, dyMax = 3, gap1 = 3, gap2 = 2, wMin = act > 1 ? 3 : 4, wMax = 7 };
                oo.dashAt = ((act > 1 || hasSkill("swift")) && R.Next() < .5) ? 1 + (int)Math.Floor(R.Next() * 3) : -1;
                if (!hasClue || r != L.ClueRoom)
                {   // the letter room stays kind to the skills the player has not met
                    oo.RS = RS;
                    oo.gaunt = hasSkill("veil") && lvE >= 2 ? Math.Min(.5, .3 + .03 * lvE) : 0;
                    oo.gW = hasSkill("veil2") ? 12 : 9;
                    oo.dj = hasSkill("dj") && lvE >= 1 ? Math.Min(.5, .14 + .05 * lvE) : 0;
                    oo.wide = hasSkill("glide") && lvE >= 3 ? Math.Min(.3, .1 + .03 * lvE) : 0;
                }
                var ord = Route(R, ys, ye, oo);
                for (int i = 0; i < ord.Count; i++)
                {
                    var l = ord[i];
                    double px = l.x0 * T, pw = l.w * T;
                    string kind = "wood";
                    if (i > 0 && !l.dash && cfg.crumble != 0 && R2.Next() < cfg.crumble * hzMul && l.w <= 5 && !(hasClue && i == ord.Count - 1)) kind = "crumble";
                    l.pl = P(px, l.y, pw, ORD, kind);
                    if (l.dash && i > 0)
                    {
                        var pv = ord[i - 1];
                        L.Items.Add(new LItem { x = pv.x0 * T + pv.w * T / 2.0 - 5, y = pv.y - 14, m = ORD, kind = "flame" });
                    }
                }
                for (int i = 0; i < ord.Count; i++)
                {
                    var l = ord[i];
                    if (i > 0 && l.kind == "dj") L.Items.Add(new LItem { x = l.x0 * T + l.w * T / 2.0 - 5, y = l.y - 14, m = ORD, kind = "flame" });
                    if (i > 0 && l.kind == "gaunt")
                    {
                        double sx = (l.x0 + 1) * T;
                        L.Haz.Add(new LHaz { x = sx, y = l.y - 7, w = (l.w - 2) * T, h = 7, m = ORD, kind = "spike", gaunt = true });
                        L.Features.Add("gaunt");
                        L.Items.Add(new LItem { x = (l.x0 + l.w / 2.0) * T - 5, y = l.y - 24, m = ORD, kind = "flame" });
                    }
                    if (l.kind == "dj") L.Features.Add("dj");
                    if (l.kind == "wide") L.Features.Add("wide");
                    // bolt thorn wall on a narrow ledge, toward the next ledge
                    if (i > 0 && i < ord.Count - 1 && hasSkill("bolt") && lvE >= 3 && l.kind == null && l.w <= 8 && l.w >= 3 && !l.dash
                        && !(hasClue && i == ord.Count - 1) && RS.Next() < Math.Min(.7, .45 + .03 * lvE))
                    {
                        var nx = ord[i + 1];
                        int d = (nx.x0 + nx.w / 2.0) > (l.x0 + l.w / 2.0) ? 1 : -1;
                        double tx = d > 0 ? (l.x0 + l.w - 1) * T : l.x0 * T;
                        int ii = i;
                        bool clear = !(nx.x0 * T < tx + T && (nx.x0 + nx.w) * T > tx)
                                     && !ord.Where((o2, k) => k != ii && o2.x0 * T < tx + T && (o2.x0 + o2.w) * T > tx && o2.y < l.y + 8 && o2.y > l.y - 140).Any();
                        if (clear)
                        {
                            L.Haz.Add(new LHaz { x = tx, y = l.y - 130, w = T, h = 130, m = ORD, kind = "thorns" });
                            L.Features.Add("thorn");
                            l.thorn = true;
                        }
                    }
                    if (i == 0 || l.dash || l.pl.kind == "crumble" || l.kind == "gaunt" || l.thorn) continue;
                    double cxp = (l.x0 + l.w / 2.0) * T;
                    double roll = R2.Next();
                    if (l.w >= 5 && roll < cfg.spike * hzMul)
                        L.Haz.Add(new LHaz { x = cxp - 12, y = l.y - 7, w = 24, h = 7, m = ORD, kind = "spike" });
                    else if (cfg.root != 0 && l.w >= 4 && roll < cfg.spike * hzMul + cfg.root * hzMul)
                        L.Haz.Add(new LHaz { x = cxp - 6, y = l.y - 16, w = 12, h = 16, m = ORD, kind = "root", ph = Math.Floor(R2.Next() * 150) });
                    else if (l.w >= 5 && R2.Next() < .45 * enMul && i < ord.Count)
                    {
                        var types = (cfg.enemies ?? new string[0]).Where(e => e != "manan").ToArray();
                        int ti = (int)Math.Floor(R2.Next() * types.Length);
                        string ty = ti < types.Length ? types[ti] : "anino";
                        if (ty == "tik" && l.w < 6) L.Enemies.Add(MakeEnemy("anino", l, ORD, L.Enemies.Count));
                        else L.Enemies.Add(MakeEnemy(ty, l, ORD, L.Enemies.Count));
                        if (hasSkill("spread") && lvE >= 5 && l.w >= 6 && RS.Next() < .5) L.Enemies.Add(MakeEnemy("anino", l, ORD, L.Enemies.Count));
                    }
                    if (R2.Next() < .5) L.Items.Add(new LItem { x = cxp + (R2.Next() < .5 ? -20 : 20) - 5, y = l.y - 14, m = ORD, kind = "flame" });
                }
                if (hasClue)
                {
                    var l = ord[ord.Count - 1];
                    L.Items.Add(new LItem { x = (l.x0 + l.w / 2.0) * T - 4, y = l.y - 16, m = ORD, kind = "clue" });
                }

                // ---- spirit route
                var so = new RouteOpts { dyMin = 1, dyMax = 2, gap1 = 3, gap2 = 3, wMin = 3, wMax = 5, dashAt = int.MinValue };
                var spi = Route(R, ys, ye, so);
                for (int i = 0; i < spi.Count; i++)
                {
                    var l = spi[i];
                    P(l.x0 * T, l.y, l.w * T, SPI, "spirit");
                    if (i > 0 && R2.Next() < .6) L.Items.Add(new LItem { x = (l.x0 + l.w / 2.0) * T - 5, y = l.y - 14, m = SPI, kind = "flame" });
                }
                if (hasClue)
                {
                    var l = spi[spi.Count - 1];
                    L.Items.Add(new LItem { x = (l.x0 + l.w / 2.0) * T - 4, y = l.y - 16, m = SPI, kind = "clue" });
                }
                if (shardsLeft > 0 && r < 3 && !cfg.endless)
                {
                    var l = spi[spi.Count / 2];
                    L.Items.Add(new LItem { x = (l.x0 + l.w / 2.0) * T - 4, y = l.y - 14, m = SPI, kind = "shard" });
                    shardsLeft--;
                }

                // ---- inverted route (ceiling ledges, gravity up)
                if (cfg.inverted)
                {
                    int w = 9, x0 = Clamp(24 - 4, 1, LW - w - 1);
                    double y = ys - 6 * T;
                    CeilLedge prev = null;
                    var ce = new List<CeilLedge>();
                    for (int i = 0; i < 3; i++)
                    {
                        if (prev != null)
                        {
                            w = 6 + (int)Math.Floor(R.Next() * 3);
                            bool pref = R.Next() < .5;
                            bool canR = prev.x1 - 1 + w <= LW - 1, canL = prev.x0 + 1 - w >= 1;
                            bool dirR = pref;
                            if (dirR && !canR) dirR = false;
                            if (!dirR && !canL) dirR = true;
                            x0 = dirR ? prev.x1 - 1 : prev.x0 + 1 - w;
                            prev.dirR = dirR;
                        }
                        var cl = new CeilLedge { x0 = x0, w = w, y = y, x1 = x0 + w };
                        ce.Add(cl);
                        prev = cl;
                        y -= 6 * T;
                    }
                    var lastL = ce[2];
                    lastL.dirR = (lastL.x0 + lastL.x1) / 2.0 < LW / 2.0;
                    for (int i = 0; i < ce.Count; i++)
                    {
                        var l = ce[i];
                        P(l.x0 * T, l.y, l.w * T, INV, "inv");
                        if (l.w >= 6 && R2.Next() < (cfg.spike + .15) * hzMul && i > 0)
                            L.Haz.Add(new LHaz { x = (l.x0 + l.w / 2.0) * T - 12, y = l.y + 8, w = 24, h = 7, m = INV, kind = "spike", up = true });
                        else if (i < 2 && R2.Next() < .5)
                            L.Items.Add(new LItem { x = (l.x0 + l.w / 2.0) * T - 5, y = l.y + 11, m = INV, kind = "flame" });
                    }
                    if (hasClue)
                    {
                        var l = ce[1];
                        L.Items.Add(new LItem { x = (l.x0 + l.w / 2.0) * T - 4, y = l.y + 11, m = INV, kind = "clue" });
                    }
                    if (shardsLeft > 0 && r < 3 && !cfg.endless && R2.Next() < .5)
                    {
                        var l = ce[2];
                        L.Items.Add(new LItem { x = (l.x0 + l.w / 2.0) * T - 5, y = l.y + 11, m = INV, kind = "shard" });
                        shardsLeft--;
                    }
                    double hx = lastL.dirR ? Clamp((lastL.x1 + 2) * T, 2 * T, 29 * T) : Clamp((lastL.x0 - 3) * T, 2 * T, 29 * T);
                    L.Doors.Add(new LDoor { x = hx, y = ye + 8, w = 16, h = 28, kind = "home", dim = ORD, m = INV, up = true, toX = lx, toY = ye, room = r });
                }

                // ---- flyers
                if (cfg.HasEnemy("manan") && r > 0 && R2.Next() < .8 * enMul)
                {
                    double fx = (6 + R2.Next() * 20) * T;
                    L.Enemies.Add(new LEnemy { t = "manan", x = fx, y = ys - RH * .55, w = 20, h = 14, m = ORD | INV, hp = 2, ph = R2.Next() * 6 });
                }
                if (cfg.HasEnemy("manan") && r > 1 && enMul > 1.2 && R2.Next() < .5)
                {
                    double fx = (6 + R2.Next() * 20) * T;
                    L.Enemies.Add(new LEnemy { t = "manan", x = fx, y = ys - RH * .3, w = 20, h = 14, m = ORD | INV, hp = 2, ph = R2.Next() * 6 });
                }
            }
            foreach (var e in L.Enemies) if (e.t == "manan") { e.cx = e.x; e.cy = e.y; }

            // ---- boss arena
            if (cfg.boss)
            {
                double yf = y0 - rooms * RH, top = yf - 19 * T + 8;
                L.BossArena = new LBossArena { yf = yf, top = top, triggerY = yf - 6 * T };
                foreach (var (bx, by, amp, sp, ph) in new[] { (5.0 * T, yf - 4 * T, 12.0 * T, .015, 0.0), (18.0 * T, yf - 7 * T, 12.0 * T, .012, 2.0) })
                {
                    var pl = P(bx, by, 5 * T, ORD, "moving");
                    pl.bx = bx; pl.amp = amp * .7; pl.sp = sp; pl.ph = ph;
                }
            }
            if (cfg.flood) L.Water = new LWater { y = y0 + 8 * T, sp = (cfg.floodSpeed != 0 ? cfg.floodSpeed : .1) * floodMult };
            return L;
        }

        class RouteOpts
        {
            public int dyMin, dyMax, gap1, gap2, wMin, wMax, dashAt, gW;
            public SeededRandom RS;
            public double gaunt, dj, wide;
            public bool hadG;
        }

        public class RouteLedge
        {
            public int x0, w;
            public double y;
            public bool dash, thorn;
            public string kind;
            internal LPlat pl;
        }

        class CeilLedge { public int x0, w, x1; public double y; public bool dirR; }

        static List<RouteLedge> Route(SeededRandom R, double ys, double ye, RouteOpts o)
        {
            var outL = new List<RouteLedge>();
            double y = ys;
            int pvx0 = 0, pvx1 = LW, guard = 0;
            while (y - ye > o.dyMax * T && guard++ < 40)
            {
                int dy = o.dyMin + (int)Math.Floor(R.Next() * (o.dyMax - o.dyMin + 1));
                int w = o.wMin + (int)Math.Floor(R.Next() * (o.wMax - o.wMin + 1));
                int? pick = null;
                bool dash = false;
                string kind = null;
                if (o.RS != null && outL.Count > 0)
                {
                    double q = o.RS.Next();
                    if (o.gaunt != 0 && !o.hadG && q < o.gaunt) { w = o.gW; kind = "gaunt"; o.hadG = true; }
                    else if (o.dj != 0 && q < o.gaunt + o.dj)
                    {
                        dy = Math.Min(5, (int)Math.Floor((y - ye) / T) - 1);
                        if (dy >= 4) kind = "dj"; else dy = o.dyMin;
                    }
                    else if (o.wide != 0 && q < o.gaunt + o.dj + o.wide) { dy = 1; kind = "wide"; }
                }
                if (o.dashAt == outL.Count && outL.Count > 0)
                {
                    dy = 1;
                    var opt = new List<int>();
                    if (pvx1 + 5 + w <= LW - 1) opt.Add(pvx1 + 5);
                    if (pvx0 - 5 - w >= 1) opt.Add(pvx0 - 5 - w);
                    if (opt.Count > 0) { pick = opt[(int)Math.Floor(R.Next() * opt.Count)]; dash = true; }
                }
                for (int k = 0; k < 50 && pick == null; k++)
                {
                    int x0 = 2 + (int)Math.Floor(R.Next() * (LW - 4 - w + 1));
                    int gap = Math.Max(0, Math.Max(x0 - pvx1, pvx0 - (x0 + w)));
                    int allowed = kind == "wide" ? 5 : kind == "dj" ? 2 : (dy >= 2 ? o.gap2 : o.gap1);
                    if (gap <= allowed && Math.Abs((x0 + w / 2.0) - (pvx0 + pvx1) / 2.0) >= 2) pick = x0;
                }
                if (pick == null) pick = Clamp((int)Math.Floor((pvx0 + pvx1) / 2.0 - w / 2.0 + 0.5) + 3, 2, LW - 2 - w);
                y -= dy * T;
                outL.Add(new RouteLedge { x0 = pick.Value, w = w, y = y, dash = dash, kind = kind });
                pvx0 = pick.Value; pvx1 = pick.Value + w;
            }
            return outL;
        }

        static LEnemy MakeEnemy(string t, RouteLedge l, int m, int index)
        {
            // The prototype picks the starting direction with Math.random(); here it alternates by index (deterministic).
            int dir = index % 2 == 0 ? 1 : -1;
            var e = new LEnemy { t = t, m = m, dir = dir, min = l.x0 * T + 4, max = (l.x0 + l.w) * T - 4 };
            if (t == "anino") { e.w = 10; e.h = 11; e.x = (l.x0 + l.w / 2.0) * T - 5; e.y = l.y - 11; e.hp = 1; e.spd = .55; }
            else { e.w = 14; e.h = 24; e.x = (l.x0 + l.w / 2.0) * T - 7; e.y = l.y - 24; e.hp = 3; e.spd = .5; }
            return e;
        }

        static int Clamp(int v, int lo, int hi) => v < lo ? lo : v > hi ? hi : v;
        static double Clamp(double v, double lo, double hi) => v < lo ? lo : v > hi ? hi : v;
    }
}
