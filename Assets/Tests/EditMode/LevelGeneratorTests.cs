using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Skp.Core;
using Skp.Levels;
using UnityEngine;

namespace Skp.Tests
{
    /// <summary>
    /// The C# generator against the prototype's own buildLevel() output
    /// (tests/prototype/fixtures/prototype_levels.json, dumped by tests/prototype/dump_levels.js).
    /// </summary>
    public class LevelGeneratorTests
    {
        [Serializable] class FPlat { public double x, y, w, h, bx, amp, sp, ph; public int m; public string kind; }
        [Serializable] class FHaz { public double x, y, w, h, ph; public int m; public string kind; public bool up, gaunt; }
        [Serializable] class FTo { public double x, y; }
        [Serializable] class FDoor { public double x, y, w, h; public int m, dim, room; public string kind; public bool up; public FTo to; }
        [Serializable] class FItem { public double x, y; public int m; public string kind; }
        [Serializable] class FEn { public string t; public double x, y, w, h, spd, min, max, cy, ph; public int m, hp; }
        [Serializable] class FLant { public double x, y; public int room; }
        [Serializable] class FWater { public double y, sp; }
        [Serializable]
        class FCase
        {
            public int lv, rooms, clueRoom; public string df; public string[] skills; public double W, H, y0; public bool needClue;
            public FPlat[] plats; public FHaz[] haz; public FDoor[] doors; public FItem[] items; public FEn[] ens; public FLant[] lants; public FWater water;
        }
        [Serializable] class FRoot { public string source; public FCase[] cases; }

        const string FixturePath = "tests/prototype/fixtures/prototype_levels.json";
        static FRoot _fixture;
        static FRoot Fixture => _fixture ??= JsonUtility.FromJson<FRoot>(File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), FixturePath)));

        static readonly LevelSpec[] Specs = LevelCatalog.Defaults();
        DifficultyTable _table;

        [SetUp] public void SetUp() => _table = ScriptableObject.CreateInstance<DifficultyTable>();
        [TearDown] public void TearDown() => UnityEngine.Object.DestroyImmediate(_table);

        LevelLayout Build(int lv, string df, string[] skills)
        {
            var d = _table.Get(df);
            var set = new HashSet<string>(skills ?? new string[0]);
            return LevelGenerator.Build(Specs[lv], lv, LevelSession.Exact(d.hazardMult), LevelSession.Exact(d.enemyMult), LevelSession.Exact(d.floodMult), set.Contains, 0, GenOptions.PrototypeExact);
        }

        [Test]
        public void Fixture_Has30Layouts()
        {
            Assert.AreEqual(30, Fixture.cases.Length);
        }

        [Test]
        public void Generator_MatchesPrototype_Exactly_ForEveryFixtureLayout()
        {
            var problems = new List<string>();
            foreach (var c in Fixture.cases)
            {
                string tag = $"L{c.lv + 1} {c.df} skills[{string.Join(",", c.skills)}]";
                var L = Build(c.lv, c.df, c.skills);
                void Eq(double a, double b, string what) { if (Math.Abs(a - b) > 1e-6) problems.Add($"{tag} {what}: unity {a} vs prototype {b}"); }
                void EqS(string a, string b, string what) { if (a != b) problems.Add($"{tag} {what}: unity {a} vs prototype {b}"); }

                Eq(L.W, c.W, "W"); Eq(L.H, c.H, "H"); Eq(L.Y0, c.y0, "y0");
                Eq(L.Rooms, c.rooms, "rooms"); Eq(L.ClueRoom, c.clueRoom, "clueRoom");
                if (L.NeedClue != c.needClue) problems.Add($"{tag} needClue");
                Eq(L.Plats.Count, c.plats.Length, "plat count");
                Eq(L.Haz.Count, c.haz.Length, "hazard count");
                Eq(L.Doors.Count, c.doors.Length, "door count");
                Eq(L.Items.Count, c.items.Length, "item count");
                Eq(L.Enemies.Count, c.ens.Length, "enemy count");
                Eq(L.Lanterns.Count, c.lants.Length, "lantern count");
                if (problems.Count > 0) continue;

                for (int i = 0; i < c.plats.Length; i++)
                {
                    var a = L.Plats[i]; var b = c.plats[i]; var w = $"plat {i}";
                    Eq(a.x, b.x, w + ".x"); Eq(a.y, b.y, w + ".y"); Eq(a.w, b.w, w + ".w"); Eq(a.h, b.h, w + ".h"); Eq(a.m, b.m, w + ".m"); EqS(a.kind, b.kind, w + ".kind");
                    Eq(a.bx, b.bx, w + ".bx"); Eq(a.amp, b.amp, w + ".amp"); Eq(a.sp, b.sp, w + ".sp"); Eq(a.ph, b.ph, w + ".ph");
                }
                for (int i = 0; i < c.haz.Length; i++)
                {
                    var a = L.Haz[i]; var b = c.haz[i]; var w = $"haz {i}";
                    Eq(a.x, b.x, w + ".x"); Eq(a.y, b.y, w + ".y"); Eq(a.w, b.w, w + ".w"); Eq(a.h, b.h, w + ".h"); Eq(a.m, b.m, w + ".m");
                    EqS(a.kind, b.kind, w + ".kind"); Eq(a.ph, b.ph, w + ".ph");
                    if (a.up != b.up || a.gaunt != b.gaunt) problems.Add($"{tag} {w} flags");
                }
                for (int i = 0; i < c.doors.Length; i++)
                {
                    var a = L.Doors[i]; var b = c.doors[i]; var w = $"door {i}";
                    Eq(a.x, b.x, w + ".x"); Eq(a.y, b.y, w + ".y"); Eq(a.w, b.w, w + ".w"); Eq(a.h, b.h, w + ".h"); Eq(a.m, b.m, w + ".m"); Eq(a.dim, b.dim, w + ".dim");
                    EqS(a.kind, b.kind, w + ".kind"); Eq(a.room, b.room, w + ".room");
                    if (a.up != b.up) problems.Add($"{tag} {w}.up");
                    if (b.up) { Eq(a.toX ?? -1, b.to.x, w + ".to.x"); Eq(a.toY ?? -1, b.to.y, w + ".to.y"); }
                }
                for (int i = 0; i < c.items.Length; i++)
                {
                    var a = L.Items[i]; var b = c.items[i]; var w = $"item {i}";
                    Eq(a.x, b.x, w + ".x"); Eq(a.y, b.y, w + ".y"); Eq(a.m, b.m, w + ".m"); EqS(a.kind, b.kind, w + ".kind");
                }
                for (int i = 0; i < c.ens.Length; i++)
                {
                    var a = L.Enemies[i]; var b = c.ens[i]; var w = $"enemy {i}";
                    EqS(a.t, b.t, w + ".t"); Eq(a.x, b.x, w + ".x"); Eq(a.y, b.y, w + ".y"); Eq(a.w, b.w, w + ".w"); Eq(a.h, b.h, w + ".h"); Eq(a.m, b.m, w + ".m");
                    Eq(a.hp, b.hp, w + ".hp"); Eq(a.spd, b.spd, w + ".spd"); Eq(a.min, b.min, w + ".min"); Eq(a.max, b.max, w + ".max");
                    if (a.t == "manan") { Eq(a.cy, b.cy, w + ".cy"); Eq(a.ph, b.ph, w + ".ph"); }
                }
                for (int i = 0; i < c.lants.Length; i++)
                {
                    var a = L.Lanterns[i]; var b = c.lants[i];
                    Eq(a.x, b.x, $"lantern {i}.x"); Eq(a.y, b.y, $"lantern {i}.y"); Eq(a.room, b.room, $"lantern {i}.room");
                }
                if (Specs[c.lv].flood) { Eq(L.Water.y, c.water.y, "water.y"); Eq(L.Water.sp, c.water.sp, "water.sp"); }
            }
            Assert.IsEmpty(problems, $"{problems.Count} differences, first: {string.Join(" | ", problems.Take(10))}");
        }

        [Test]
        public void Generation_IsDeterministic()
        {
            for (int lv = 0; lv < 9; lv++)
            {
                var a = Build(lv, "standard", null);
                var b = Build(lv, "standard", null);
                Assert.AreEqual(Summary(a), Summary(b), $"level {lv + 1}");
            }
        }

        static string Summary(LevelLayout L) =>
            string.Join(";", L.Plats.Select(p => $"{p.x},{p.y},{p.w},{p.m},{p.kind}")) + "|" +
            string.Join(";", L.Haz.Select(h => $"{h.x},{h.y},{h.kind}")) + "|" +
            string.Join(";", L.Items.Select(i => $"{i.x},{i.y},{i.kind}")) + "|" +
            string.Join(";", L.Enemies.Select(e => $"{e.x},{e.y},{e.t},{e.dir}"));

        [TestCase(0, 4, false)]
        [TestCase(1, 5, false)]
        [TestCase(2, 5, true)]
        public void Levels1to3_EveryRoomHasStartLedge_OrdinaryAndSpiritRoutes_Lantern_AndOneExit(int lv, int rooms, bool inverted)
        {
            var L = Build(lv, "standard", null);
            Assert.AreEqual(rooms, L.Rooms);
            double RH = 22 * 16;
            for (int r = 0; r < rooms; r++)
            {
                double ys = L.Y0 - r * RH, ye = ys - RH;
                Assert.IsTrue(L.Plats.Any(p => p.kind == "floor" && Math.Abs(p.y - ys) < 1e-6), $"room {r} start floor");
                Assert.IsTrue(L.Plats.Any(p => p.kind == "floor" && Math.Abs(p.y - ye) < 1e-6), $"room {r} top floor");
                Assert.GreaterOrEqual(L.Plats.Count(p => p.m == 1 && p.y < ys && p.y > ye), 3, $"room {r} ordinary route");
                Assert.GreaterOrEqual(L.Plats.Count(p => p.m == 2 && p.y < ys && p.y > ye), 3, $"room {r} spirit route");
                Assert.AreEqual(inverted ? 3 : 0, L.Plats.Count(p => p.m == 4 && p.y < ys && p.y > ye), $"room {r} inverted route");
                Assert.IsTrue(L.Doors.Any(d => d.room == r && d.kind == "enter" && d.dim == 2), $"room {r} spirit door");
                Assert.IsTrue(L.Lanterns.Any(l => l.room == r), $"room {r} lantern");
            }
            Assert.AreEqual(1, L.Doors.Count(d => d.kind == "exit"), "one exit");
            Assert.AreEqual(rooms - 1, L.Doors.Single(d => d.kind == "exit").room, "exit in the last room");
            Assert.AreEqual(rooms / 2, L.ClueRoom, "letter in the middle room");
            Assert.IsTrue(L.NeedClue);
            Assert.GreaterOrEqual(L.Items.Count(i => i.kind == "clue"), 2, "letter on the ordinary and spirit routes");
            Assert.AreEqual(3, L.Items.Count(i => i.kind == "shard"), "three flame fragments");
        }

        [Test]
        public void WorldConversion_FloorAtZero_YUp_DoorArrivalOnLantern()
        {
            var L = Build(2, "standard", null);
            var w = LevelWorldFactory.Build(L);
            Assert.AreEqual(0f, w.Ledges[0].Rect.yMax, 1e-5f, "ground floor top is y = 0");
            Assert.AreEqual(-3f, w.Bounds.yMin, 1e-5f);
            Assert.AreEqual(32f, w.Bounds.width, 1e-5f);
            Assert.AreEqual(new Vector2(15, 0), w.Lanterns[0]);
            Assert.AreEqual(22f, w.Lanterns[1].y, 1e-5f, "room 1 lantern stands on the floor 22 tiles up");
            var hanging = w.Doors.First(d => d.Hanging);
            Assert.IsTrue(hanging.To.HasValue);
            Assert.IsTrue(w.Lanterns.Contains(hanging.To.Value), "hanging home door returns to that room's lantern");
            Assert.IsTrue(w.NeedClue);
        }
    }
}
