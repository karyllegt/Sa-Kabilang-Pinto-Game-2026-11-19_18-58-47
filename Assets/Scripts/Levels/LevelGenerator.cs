using System;
using System.Collections.Generic;
using System.Linq;

namespace Skp.Levels
{
    /// <summary>
    /// Generator options. <see cref="Prototype"/> reproduces the prototype exactly (the M4 fixtures). The default M7 mode
    /// keeps the prototype's draws and adds docs/02 Feature 4: glide gaps truly wide, veil gauntlets under a thorn curtain
    /// with the way on past the far end, Act III falling bells, secret flame nooks in long levels, slightly narrower
    /// ledges later on, and the ramp numbers from the DifficultyCurve.
    /// </summary>
    public class GenOptions
    {
        public bool Prototype;
        public double HazardPerLevel = .08, HazardPerRoom = .05, HazardCap = 2.4;
        public double EnemyPerLevel = .06, EnemyPerRoom = .04, EnemyCap = 2.2;
        public SkillObstacleTuning Obstacles;

        public static GenOptions PrototypeExact => new GenOptions { Prototype = true };

        public static GenOptions From(DifficultyCurve c, SkillObstacleTuning o)
        {
            var g = new GenOptions { Obstacles = o };
            if (c == null) return g;
            g.HazardPerLevel = Math.Round(c.hazardPerLevel, 6); g.HazardPerRoom = Math.Round(c.hazardPerRoom, 6); g.HazardCap = Math.Round(c.hazardCap, 6);
            g.EnemyPerLevel = Math.Round(c.enemyPerLevel, 6); g.EnemyPerRoom = Math.Round(c.enemyPerRoom, 6); g.EnemyCap = Math.Round(c.enemyCap, 6);
            return g;
        }
    }

    /// <summary>
    /// Port of the prototype's buildLevel() and route() (reference/prototype/index.html), statement by
    /// statement and in the same random-draw order, so a given level, difficulty and skill set produces
    /// exactly the prototype's layout in <see cref="GenOptions.Prototype"/> mode (checked against
    /// tests/prototype/fixtures/prototype_levels.json). Deterministic: the seed depends only on the level index
    /// (or the endless loop).
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
                                        Func<string, bool> hasSkill, int loop = 0, GenOptions opts = null)
        {
            hasSkill ??= _ => false;
            opts ??= new GenOptions();
            bool m7 = !opts.Prototype;
            var ot = opts.Obstacles != null ? opts.Obstacles : SkillObstacleTuning.Default;
            double seed = (lv >= 0 ? lv + 1 : 60 + loop) * 2654435.0 + 17;
            var R = new SeededRandom(seed);
            var R2 = new SeededRandom(seed * 3 + 7);
            var RS = new SeededRandom(seed * 5 + 11);
            var RB = new SeededRandom(seed * 7 + 13); // M7 additions only: the prototype's own draws stay untouched
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
            double lvS = 1 + opts.HazardPerLevel * Math.Max(0, lv);
            int shardsLeft = 3;

            for (int r = 0; r < rooms; r++)
            {
                double ys = y0 - r * RH, ye = ys - RH;
                bool last = r == rooms - 1;
                double rmS = 1 + opts.HazardPerRoom * r;
                hzMul = Math.Min(opts.HazardCap, hazardMult * lvS * rmS);
                enMul = Math.Min(opts.EnemyCap, enemyMult * (1 + opts.EnemyPerLevel * Math.Max(0, lv)) * (1 + opts.EnemyPerRoom * r));
                L.RoomMults.Add((hzMul, enMul));
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
                var oo = new RouteOpts { dyMin = 1, dyMax = 3, gap1 = 3, gap2 = 2, wMin = act > 1 ? 3 : 4, wMax = m7 && lvE >= ot.narrowFromLevel ? 6 : 7, M7 = m7, WideGap = ot.glideGap, DashGap = ot.dashGap, HasDj = hasSkill("dj"), HasGlide = hasSkill("glide") };
                oo.dashAt = ((act > 1 || hasSkill("swift")) && R.Next() < .5) ? 1 + (int)Math.Floor(R.Next() * 3) : -1;
                if (!hasClue || r != L.ClueRoom)
                {   // the letter room stays kind to the skills the player has not met
                    oo.RS = RS;
                    oo.gaunt = hasSkill("veil") && lvE >= 2 ? Math.Min(.5, .3 + .03 * lvE) : 0;
                    oo.gW = hasSkill("veil2") ? (m7 ? ot.greaterGauntletWidth : 12) : (m7 ? ot.gauntletWidth : 9);
                    oo.dj = hasSkill("dj") && lvE >= 1 ? Math.Min(.5, .14 + .05 * lvE) : 0;
                    oo.wide = hasSkill("glide") && lvE >= 3 ? Math.Min(.3, .1 + .03 * lvE) : 0;
                }
                var ord = Route(R, ys, ye, oo);
                if (m7)
                    for (int i = 0; i < ord.Count; i++)
                    {
                        // a gauntlet's curtain must not cut through another ledge of the route; the way on must exist
                        var l = ord[i];
                        if (l.kind != "gaunt") continue;
                        double cx0 = (l.x0 + 1) * T, cx1 = (l.x0 + l.w - 1) * T, cy0 = l.y - ot.curtainGap - ot.curtainHeight, cy1 = l.y - ot.curtainGap;
                        double ly0 = l.y + 8, ly1 = Math.Min(ys, l.y + 8 + ot.curtainBelow); // under the ledge, down to the room floor at most
                        // other ledges keep two tiles of headroom under a curtain (room to jump onto them)
                        bool Hits(RouteLedge o2, double y0, double y1) => o2.x0 * T < cx1 && (o2.x0 + o2.w) * T > cx0 && o2.y - 2 * T < y1 && o2.y + 8 > y0;
                        bool blocked = i + 1 >= ord.Count || ord.Where((o2, k) => k != i && (Hits(o2, cy0, cy1) || Hits(o2, ly0, ly1))).Any();
                        if (blocked) l.kind = null;
                    }
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
                        var spikes = new LHaz { x = sx, y = l.y - 7, w = (l.w - 2) * T, h = 7, m = ORD, kind = "spike", gaunt = true };
                        L.Haz.Add(spikes);
                        L.Features.Add("gaunt");
                        L.Items.Add(new LItem { x = (l.x0 + l.w / 2.0) * T - 5, y = l.y - 24, m = ORD, kind = "flame" });
                        if (m7)
                        {
                            L.Haz.Add(new LHaz { x = sx, y = l.y - ot.curtainGap - ot.curtainHeight, w = (l.w - 2) * T, h = ot.curtainHeight, m = ORD, kind = "curtain" });
                            // and under it, so nobody slips beneath the gauntlet
                            double below = Math.Min(ys, l.y + 8 + ot.curtainBelow) - (l.y + 8);
                            if (below > 4) L.Haz.Add(new LHaz { x = sx, y = l.y + 8, w = (l.w - 2) * T, h = below, m = ORD, kind = "curtain" });
                            L.Obstacles.Add(new LObstacle { type = "veil", skill = "veil", room = r, From = ord[i - 1].pl, Over = l.pl, To = ord[i + 1].pl, Hazard = spikes });
                        }
                    }
                    if (m7 && i > 0 && l.kind == "dj") L.Obstacles.Add(new LObstacle { type = "dj", skill = "dj", room = r, From = ord[i - 1].pl, To = l.pl });
                    if (m7 && i > 0 && l.kind == "wide") L.Obstacles.Add(new LObstacle { type = "glide", skill = "glide", room = r, From = ord[i - 1].pl, To = l.pl });
                    if (m7 && i > 0 && l.dash) L.Obstacles.Add(new LObstacle { type = "dash", skill = "dash", room = r, From = ord[i - 1].pl, To = l.pl });
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
                        // M7: no earlier ledge of this room reaches past the wall (the way on is only through it)
                        if (m7 && clear) clear = nx.kind == null && !nx.dash && !ord[i - 1].thorn && l.pl.kind != "crumble"; // M7: the way on is a plain step, walls never come two in a row, nor stand on a crumbling ledge
                        if (clear)
                        {
                            var wall = new LHaz { x = tx, y = l.y - 130, w = T, h = 130, m = ORD, kind = "thorns" };
                            L.Haz.Add(wall);
                            L.Features.Add("thorn");
                            l.thorn = true;
                            if (m7) L.Obstacles.Add(new LObstacle { type = "bolt", skill = "bolt", room = r, From = l.pl, Over = l.pl, To = nx.pl, Hazard = wall });
                        }
                    }
                    if (i == 0 || l.dash || l.pl.kind == "crumble" || l.kind == "gaunt" || l.thorn) continue;
                    if (m7 && (l.kind == "dj" || l.kind == "wide")) continue; // M7: a skill landing is a reward, not a trap
                    if (m7 && i + 1 < ord.Count && (ord[i + 1].kind == "dj" || ord[i + 1].kind == "wide" || ord[i + 1].dash)) continue; // nor is its take-off
                    if (m7 && (ord[i - 1].thorn || ord[i - 1].kind == "gaunt")) continue; // the way on past a wall or a gauntlet is clear
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
                        if (hasSkill("spread") && lvE >= 5 && l.w >= 6 && RS.Next() < .5)
                        {
                            L.Enemies.Add(MakeEnemy("anino", l, ORD, L.Enemies.Count));
                            L.Features.Add("pack");
                            if (m7) L.Obstacles.Add(new LObstacle { type = "pack", skill = "spread", room = r, From = l.pl, Over = l.pl, To = l.pl });
                        }
                    }
                    if (R2.Next() < .5) L.Items.Add(new LItem { x = cxp + (R2.Next() < .5 ? -20 : 20) - 5, y = l.y - 14, m = ORD, kind = "flame" });
                }
                if (hasClue)
                {
                    var l = ord[ord.Count - 1];
                    L.Items.Add(new LItem { x = (l.x0 + l.w / 2.0) * T - 4, y = l.y - 16, m = ORD, kind = "clue" });
                }

                if (m7) AddBellsAndNooks(L, cfg, ord, r, lvE, hzMul, rooms, RB, ot);

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
            // M7: wide gaps are exactly WideGap tiles; after a gauntlet the way on starts past its far end
            public bool M7;
            public int WideGap = 14, DashGap = 6;
            public bool HasDj, HasGlide;
            public int gx0 = -1, gx1 = -1, gFar;
        }

        /// <summary>
        /// Act III falling bells on free ordinary ledges, and in long levels a small hidden ledge with two flame stars
        /// above the route (own random stream, so the prototype's draws are unchanged).
        /// </summary>
        static void AddBellsAndNooks(LevelLayout L, LevelSpec cfg, List<RouteLedge> ord, int r, int lvE, double hzMul, int rooms, SeededRandom RB, SkillObstacleTuning ot)
        {
            bool Free(RouteLedge l) =>
                l.pl.kind != "crumble" && l.kind == null && !l.thorn &&
                !L.Haz.Any(h => h.x < (l.x0 + l.w) * T && h.x + h.w > l.x0 * T && Math.Abs(h.y + h.h - l.y) < 20);
            if (cfg.act == 3 && !cfg.boss)
            {
                for (int i = 1; i < ord.Count; i++)
                {
                    var l = ord[i];
                    double roll = RB.Next();
                    if (!Free(l) || l.w < 4 || roll >= ot.bellChance * hzMul) continue;
                    double x = (l.x0 + l.w / 2.0) * T;
                    double top = l.y - ot.bellDrop * T;
                    foreach (var o in ord) if (o != l && o.x0 * T < x + 7 && (o.x0 + o.w) * T > x - 7 && o.y < l.y && o.y + 8 > top) top = o.y + 8;
                    if (l.y - top < 4 * T) continue; // no room to fall
                    int period = Math.Max(150, ot.bellPeriod - 10 * Math.Max(0, lvE - 6));
                    L.Bells.Add(new LBell { x = x, top = top, ground = l.y, period = period, phase = (int)Math.Floor(RB.Next() * period) });
                    L.Features.Add("bell");
                }
            }
            if (rooms >= 6 && ord.Count >= 3 && RB.Next() < ot.nookChance)
            {
                int i = 1 + (int)Math.Floor(RB.Next() * (ord.Count - 2));
                var l = ord[i];
                bool right = RB.Next() < .5;
                int nx = right ? l.x0 + l.w + 1 : l.x0 - 3;
                double ny = l.y - ot.nookHeight * T;
                if (nx < 1 || nx + 2 > LW - 1) return;
                double x0 = nx * T, x1 = (nx + 2) * T;
                bool clash = L.Plats.Any(p => p.x < x1 + T && p.x + p.w > x0 - T && p.y < ny + 3 * T && p.y + p.h > ny - 3 * T)
                             || L.Haz.Any(h => h.x < x1 && h.x + h.w > x0 && h.y < ny + 2 * T && h.y + h.h > ny - 3 * T);
                if (clash) return;
                L.Plats.Add(new LPlat { x = x0, y = ny, w = 2 * T, h = 8, m = ORD, kind = "wood" });
                L.Items.Add(new LItem { x = x0 + 3, y = ny - 14, m = ORD, kind = "flame" });
                L.Items.Add(new LItem { x = x0 + 18, y = ny - 14, m = ORD, kind = "flame" });
                L.Features.Add("nook");
            }
        }

        public class RouteLedge
        {
            public int x0, w;
            public double y;
            public bool dash, thorn, gate;
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
                        if (dy >= (o.M7 ? 5 : 4)) kind = "dj"; else dy = o.dyMin; // M7: 5 tiles, well past a single jump
                    }
                    else if (o.wide != 0 && q < o.gaunt + o.dj + o.wide) { dy = 1; kind = "wide"; }
                }
                if (o.M7 && kind == "wide")
                {
                    // a real glide gap: exactly WideGap tiles from the last ledge, on whichever side has room
                    var opt = new List<int>();
                    if (pvx1 + o.WideGap + w <= LW - 1) opt.Add(pvx1 + o.WideGap);
                    if (pvx0 - o.WideGap - w >= 1) opt.Add(pvx0 - o.WideGap - w);
                    if (opt.Count > 0) pick = opt[(int)Math.Floor(R.Next() * opt.Count)];
                    else kind = null;
                }
                // M7: the step after an obstacle never comes back toward where Tala started it (no way around it)
                int afterGaunt = o.M7 && o.gx0 >= 0 && outL.Count > 0 && outL[outL.Count - 1].gate ? o.gFar : 0;
                bool afterGauntlet = afterGaunt != 0 && outL[outL.Count - 1].kind == "gaunt";
                if (afterGauntlet && kind != null) { kind = null; pick = null; dy = o.dyMin + dy % (o.dyMax - o.dyMin + 1); } // the way on from a gauntlet is a plain step
                if (o.dashAt == outL.Count && outL.Count > 0 && !(o.M7 && kind == "wide" && pick != null))
                {
                    dy = 1;
                    var opt = new List<int>();
                    int dg = o.M7 ? o.DashGap + (o.HasDj ? 3 : 0) : 5; // M7: past a double jump too, once Tala has one
                    if (pvx1 + dg + w <= LW - 1) opt.Add(pvx1 + dg);
                    if (pvx0 - dg - w >= 1) opt.Add(pvx0 - dg - w);
                    if (opt.Count > 0) { pick = opt[(int)Math.Floor(R.Next() * opt.Count)]; dash = true; if (o.M7) kind = null; }
                    if (o.M7 && o.HasGlide && dash) { pick = null; dash = false; } // a glide crosses any dash gap: none once Tala has one // M7: a dash gap is only that
                }
                if (afterGaunt != 0 && dash) { pick = null; dash = false; } // the way on from a gauntlet is a plain step
                for (int k = 0; k < 50 && pick == null; k++)
                {
                    int x0 = 2 + (int)Math.Floor(R.Next() * (LW - 4 - w + 1));
                    int gap = Math.Max(0, Math.Max(x0 - pvx1, pvx0 - (x0 + w)));
                    int allowed = kind == "wide" ? 5 : kind == "dj" ? 2 : (dy >= 2 ? o.gap2 : o.gap1);
                    // after a gauntlet the next ledge starts past its far safe tile (it cannot be reached from the near end)
                    bool clearOfSpikes = !(o.M7 && kind == "gaunt") || pvx1 <= x0 + 1 || pvx0 >= x0 + w - 1;
                    bool farOk = clearOfSpikes && afterGaunt == 0
                                 || (afterGauntlet ? (afterGaunt > 0 ? x0 >= o.gx1 - 1 : x0 + w <= o.gx0 + 1)
                                                   : (afterGaunt > 0 ? x0 >= o.gx0 : x0 + w <= o.gx1));
                    if (gap <= allowed && farOk && Math.Abs((x0 + w / 2.0) - (pvx0 + pvx1) / 2.0) >= 2) pick = x0;
                }
                if (pick == null && afterGauntlet)
                {
                    int x0 = afterGaunt > 0 ? o.gx1 - 1 : o.gx0 + 1 - w;
                    if (x0 >= 1 && x0 + w <= LW - 1) pick = x0;
                    else outL[outL.Count - 1].kind = null; // no room on the far side: the gauntlet becomes a plain ledge
                }
                if (pick == null && o.M7 && kind == "gaunt") { kind = null; o.hadG = false; } // no room for a fair gauntlet here
                if (pick == null) pick = Clamp((int)Math.Floor((pvx0 + pvx1) / 2.0 - w / 2.0 + 0.5) + 3, 2, LW - 2 - w);
                y -= dy * T;
                outL.Add(new RouteLedge { x0 = pick.Value, w = w, y = y, dash = dash, kind = kind });
                var placed = outL[outL.Count - 1];
                if (o.M7 && (kind == "dj" || kind == "wide" || dash))
                {
                    // the next step stays on the far side of this ledge, seen from the last one
                    placed.gate = true;
                    o.gx0 = pick.Value; o.gx1 = pick.Value + w;
                    double prevC = (pvx0 + pvx1) / 2.0, c = pick.Value + w / 2.0;
                    o.gFar = prevC < c ? 1 : prevC > c ? -1 : (pick.Value + w + 3 <= LW - 1 ? 1 : -1);
                }
                if (o.M7 && kind == "gaunt")
                {
                    placed.gate = true;
                    // Tala arrives from the previous ledge's side; the far end is the other one (or the side with room)
                    o.gx0 = pick.Value; o.gx1 = pick.Value + w;
                    double prevC = (pvx0 + pvx1) / 2.0, c = pick.Value + w / 2.0;
                    o.gFar = prevC <= c ? 1 : -1;
                    // no room past the far end: no gauntlet here (the way on would be on Tala's own side)
                    if ((o.gFar > 0 && o.gx1 + 2 > LW - 1) || (o.gFar < 0 && o.gx0 - 3 < 1)) { placed.kind = null; placed.gate = false; o.hadG = false; }
                }
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
