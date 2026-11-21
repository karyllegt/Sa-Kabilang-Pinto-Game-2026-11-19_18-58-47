using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Skp.Core;
using Skp.Dimensions;
using Skp.Enemies;
using Skp.Levels;
using Skp.Player;
using Skp.Save;
using Skp.Skills;
using UnityEditor;
using UnityEngine;

namespace Skp.Tests
{
    /// <summary>
    /// M7 without scenes: the long game and its ramp (docs/02 Feature 3), the skill obstacles (Feature 4), the flood,
    /// the manananggal, Act III bells, solid thorn walls, the Bellkeeper fight, difficulty modes, and the completability
    /// bots for all nine levels.
    /// </summary>
    public class M7Tests
    {
        const float Px = 1f / 16f;
        PlayerTuning _t;
        DifficultyTable _table;
        DifficultyCurve _curve;
        LevelConfig[] _levels;
        string _dir;

        [SetUp]
        public void SetUp()
        {
            _t = AssetDatabase.LoadAssetAtPath<PlayerTuning>("Assets/Data/PlayerTuning.asset");
            _table = AssetDatabase.LoadAssetAtPath<DifficultyTable>("Assets/Data/DifficultyTable.asset");
            _curve = AssetDatabase.LoadAssetAtPath<DifficultyCurve>("Assets/Data/DifficultyCurve.asset");
            _levels = Enumerable.Range(0, 9).Select(i => AssetDatabase.LoadAssetAtPath<LevelConfig>($"Assets/Data/Levels/LevelConfig_{i + 1:00}.asset")).ToArray();
            _dir = Path.Combine(Path.GetTempPath(), "skp_m7_" + System.Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown() { if (Directory.Exists(_dir)) Directory.Delete(_dir, true); }

        static readonly string[] Diffs = { "story", "standard", "nightmare", "endless" };

        LevelLayout Layout(int lv, string diff, string[] owned) => SkpObstacleReport.Layout(lv, diff, owned);

        SaveService Save(params string[] skills)
        {
            var s = new SaveService(_dir);
            s.Load();
            s.Data.skills = skills;
            s.Data.unlocked = 9;
            return s;
        }

        LevelSession Session(int lv, string diff = "standard", params string[] skills) =>
            new LevelSession(_levels[lv].spec, new LevelRequest { Level = lv, Difficulty = diff }, _t, _table, _curve, Save(skills));

        // ------------------------------------------------------------ Feature 3: length and ramp

        [Test]
        public void RoomsPerLevel_Are_4_5_5_6_6_7_7_8_ThenTheBossArena()
        {
            int[] expect = { 4, 5, 5, 6, 6, 7, 7, 8, 4 };
            for (int lv = 0; lv < 9; lv++)
                foreach (var diff in Diffs)
                {
                    var L = Layout(lv, diff, SkillIds.All);
                    Assert.AreEqual(expect[lv], L.Rooms, $"L{lv + 1} {diff}");
                    Assert.AreEqual(expect[lv] + 1, L.Lanterns.Count, "a lantern per room");
                    Assert.AreEqual(lv == 8, L.BossArena != null, "the Bellkeeper's arena on level 9 only");
                }
        }

        [Test]
        public void Multipliers_FollowTheCurve_RiseRoomToRoom_AndNeverPassTheirCaps()
        {
            Assert.AreEqual(0.08f, _curve.hazardPerLevel, 1e-6f); Assert.AreEqual(0.05f, _curve.hazardPerRoom, 1e-6f); Assert.AreEqual(2.4f, _curve.hazardCap, 1e-6f);
            Assert.AreEqual(0.06f, _curve.enemyPerLevel, 1e-6f); Assert.AreEqual(0.04f, _curve.enemyPerRoom, 1e-6f); Assert.AreEqual(2.2f, _curve.enemyCap, 1e-6f);
            for (int lv = 0; lv < 9; lv++)
                foreach (var diff in Diffs)
                {
                    var d = _table.Get(diff);
                    var L = Layout(lv, diff, SkillIds.All);
                    for (int r = 0; r < L.Rooms; r++)
                    {
                        var (hz, en) = L.RoomMults[r];
                        Assert.LessOrEqual(hz, 2.4 + 1e-9); Assert.LessOrEqual(en, 2.2 + 1e-9);
                        Assert.AreEqual(_curve.HazardMult(d.hazardMult, lv, r), hz, 1e-4, $"L{lv + 1} {diff} room {r + 1} hazard");
                        Assert.AreEqual(_curve.EnemyMult(d.enemyMult, lv, r), en, 1e-4, $"L{lv + 1} {diff} room {r + 1} enemies");
                        if (r > 0) { Assert.GreaterOrEqual(hz, L.RoomMults[r - 1].hazard); Assert.GreaterOrEqual(en, L.RoomMults[r - 1].enemy); }
                    }
                }
            // shadow 3.5 percent shorter per level (never under 100 frames), flood +5 percent, enemies +2 percent
            for (int lv = 0; lv < 9; lv++)
            {
                Assert.AreEqual(Mathf.Max(100, Mathf.RoundToInt(240 * (1 - 0.035f * lv))), _curve.ChaseFrames(240, lv));
                Assert.GreaterOrEqual(_curve.ChaseFrames(120, lv), 100);
                Assert.AreEqual(1 + 0.05f * lv, _curve.FloodFactor(lv), 1e-5f);
                Assert.AreEqual(1.25f * (1 + 0.02f * lv), _curve.EnemySpeed(1.25f, lv), 1e-5f);
            }
        }

        /// <summary>Hazards, creatures and bells per ordinary ledge of a room (the ramp is per ledge; rooms differ in how many ledges they have).</summary>
        static double Count(LevelLayout L, int room)
        {
            double ys = L.Y0 - room * LevelLayout.RoomTiles * LevelLayout.T, ye = ys - LevelLayout.RoomTiles * LevelLayout.T;
            int ledges = L.Plats.Count(p => p.m == 1 && p.kind != "floor" && p.y < ys && p.y >= ye);
            // a skill obstacle is a challenge too (its ledges carry no other hazard)
            int things = L.Haz.Count(h => h.m == 1 && h.kind != "curtain" && !h.gaunt && h.kind != "thorns" && h.y < ys && h.y >= ye) + L.Enemies.Count(e => e.y < ys && e.y >= ye)
                         + L.Bells.Count(b => b.ground < ys && b.ground >= ye) + L.Obstacles.Count(o => o.room == room);
            return things / (double)System.Math.Max(1, ledges);
        }

        static double Slope(IList<double> v)
        {
            int n = v.Count; double mx = (n - 1) / 2.0, my = v.Average(), num = 0, den = 0;
            for (int i = 0; i < n; i++) { num += (i - mx) * (v[i] - my); den += (i - mx) * (i - mx); }
            return den == 0 ? 0 : num / den;
        }

        [Test]
        public void HazardsAndEnemies_RiseRoomToRoom_AndLevelToLevel_OnAverage()
        {
            var perLevel = new List<double>();
            var slopes = new List<double>();
            for (int lv = 0; lv < 8; lv++)
            {
                var L0 = Layout(lv, "standard", ObstacleCheck.OwnedBefore(lv));
                var rooms = new double[L0.Rooms];
                foreach (var diff in Diffs)
                {
                    var L = Layout(lv, diff, ObstacleCheck.OwnedBefore(lv));
                    for (int r = 0; r < L.Rooms; r++) rooms[r] += Count(L, r) / (double)Diffs.Length;
                }
                slopes.Add(Slope(rooms));
                TestContext.WriteLine($"L{lv + 1}: hazards + creatures per ledge, by room {string.Join(", ", rooms.Select(x => x.ToString("0.00")))}, slope {Slope(rooms):0.00}");
                perLevel.Add(rooms.Average());
            }
            TestContext.WriteLine("hazards + creatures per ordinary ledge, levels 1-8: " + string.Join(", ", perLevel.Select(x => x.ToString("0.00"))));
            // the room ramp (+5 and +4 percent a room) is small next to one layout's randomness: average many layouts,
            // the eight levels and thirty Endless loops (each its own seed) at every difficulty
            var curve = _curve;
            for (int loop = 0; loop < 30; loop++)
                foreach (var diff in Diffs)
                {
                    var d = _table.Get(diff);
                    var L = LevelGenerator.Build(LevelCatalog.Endless(loop), -1, LevelSession.Exact(d.hazardMult), LevelSession.Exact(d.enemyMult), LevelSession.Exact(d.floodMult),
                                                 id => true, loop, GenOptions.From(curve, curve.obstacles));
                    var rooms = new double[L.Rooms];
                    for (int r = 0; r < L.Rooms; r++) rooms[r] = Count(L, r);
                    slopes.Add(Slope(rooms));
                }
            TestContext.WriteLine($"average room-to-room slope over {slopes.Count} layouts: {slopes.Average():0.000}");
            Assert.GreaterOrEqual(slopes.Average(), 0, "on average the rooms get busier toward the top of a level");
            Assert.Greater(Slope(perLevel), 0, "busier level to level");
        }

        // ------------------------------------------------------------ Feature 4: obstacle statistics

        static readonly Dictionary<string, int> FirstLevel = new Dictionary<string, int>
        {
            ["dj"] = 1, ["veil"] = 2, ["bolt"] = 3, ["dash"] = 3, ["pack"] = 6, ["glide"] = 8,
        };

        [Test]
        public void Obstacles_AppearOnlyFromTheirLevel_NeverInTheLetterRoom_AndGrowDenser()
        {
            var density = new List<double>();
            for (int lv = 0; lv < 9; lv++)
            {
                double total = 0;
                foreach (var diff in Diffs)
                {
                    var L = Layout(lv, diff, ObstacleCheck.OwnedBefore(lv));
                    foreach (var o in L.Obstacles)
                    {
                        Assert.GreaterOrEqual(lv, FirstLevel[o.type], $"{o.type} in L{lv + 1}");
                        if (o.type == "dj" || o.type == "veil" || o.type == "glide") Assert.AreNotEqual(L.ClueRoom, L.NeedClue ? o.room : -1, $"{o.type} in the letter room of L{lv + 1}");
                    }
                    total += L.Obstacles.Count / (double)L.Rooms;
                }
                density.Add(total / Diffs.Length);
            }
            TestContext.WriteLine("skill obstacles per room, levels 1-9: " + string.Join(", ", density.Select(x => x.ToString("0.00"))));
            // rising on average: a positive trend, and no level more than 15 percent under the densest one before it
            double best = 0;
            for (int lv = 0; lv < 8; lv++) { Assert.GreaterOrEqual(density[lv], best * 0.85, $"density L{lv + 1}"); best = System.Math.Max(best, density[lv]); }
            Assert.Greater(Slope(density), 0);
        }

        [Test, Timeout(1200000)]
        public void EverySkillObstacle_CrossedWithItsSkill_BlockedWithout()
        {
            var s = SkpObstacleReport.Run();
            TestContext.WriteLine(SkpObstacleReport.Write(s));
            foreach (var t in new[] { "dj", "veil", "bolt", "dash", "glide", "pack" })
                Assert.Greater(s.Generated(t), 0, t + " obstacles are generated");
            foreach (var t in s.Types)
                Assert.GreaterOrEqual(s.Rate(t), SkpObstacleReport.Summary.Required(t),
                    $"{t}: {s.Passed(t)}/{s.Generated(t)}\n" + string.Join("\n", s.Cases.Where(c => c.Type == t && !c.Ok)));
        }

        // ------------------------------------------------------------ completability

        [Test]
        [Timeout(900000)]
        public void EveryLevel_CompletableWithTheBaseKitOnStory_WithAllSkills_AndAllSkillLevelsStillHaveTheBaseKitFallback([NUnit.Framework.Range(0, 8)] int lv)
        {
            var story = _table.Get("story");
            void Check(string what, LevelLayout L, DifficultyMode d, ISkillSet skills)
            {
                var fails = LevelBot.RunRooms(L, _t, d, skills);
                Assert.IsEmpty(fails, $"L{lv + 1} {what}:\n" + string.Join("\n", fails));
            }
            Check("base kit on Story", Layout(lv, "story", new string[0]), story, null);
            Check("all skills", Layout(lv, "standard", SkillIds.All), _table.Get("standard"), SkillSet.AllSkills());
            // the Spirit and Inverted routes always stay completable with the base kit, whatever the level was built for
            Check("built for all skills, played with the base kit", Layout(lv, "story", SkillIds.All), story, null);
            // a level's own letter is reachable with the skills owned before it (the letter's room stays kind)
            var before = ObstacleCheck.OwnedBefore(lv);
            Check("with the skills owned before it", Layout(lv, "standard", before), _table.Get("standard"), new SkillSet(before));
        }

        // ------------------------------------------------------------ difficulty modes

        [Test]
        public void DifficultyModes_Vitality_6_4_3_4_Lives_AndBossHealth()
        {
            var expect = new Dictionary<string, (int vit, int lives, int boss)>
            {
                ["story"] = (6, -1, 8), ["standard"] = (4, 5, 12), ["nightmare"] = (3, 3, 16), ["endless"] = (4, 3, 12),
            };
            foreach (var kv in expect)
            {
                var s = Session(8, kv.Key);
                Assert.AreEqual(kv.Value.vit, s.Sim.MaxVitality, kv.Key);
                Assert.AreEqual(kv.Value.lives < 0, s.Sim.InfiniteLives, kv.Key);
                if (kv.Value.lives >= 0) Assert.AreEqual(kv.Value.lives, s.Sim.Lives, kv.Key);
                Assert.AreEqual(kv.Value.boss, s.Boss.MaxHp, kv.Key);
                Assert.AreEqual(kv.Value.vit + 1, Session(8, kv.Key, SkillIds.Heart).Sim.MaxVitality, kv.Key + " with the Heart Charm");
            }
        }

        // ------------------------------------------------------------ the flood (level 5)

        [Test]
        public void Flood_RisesFasterEachLevel_BurnsEvery36Ticks_ThroughTheVeilGate_AndResetsOnRespawn()
        {
            var s = Session(4);
            var f = s.World.Flood;
            Assert.IsNotNull(f, "level 5 floods");
            Assert.IsNull(Session(3).World.Flood);
            Assert.AreEqual(s.World.Lanterns[0].y - Flood.BelowLantern, f.Y, 1e-4f, "starts ten tiles under the lantern");
            Assert.AreEqual(0.1f / 16f * 1.0f * 1.2f, f.Speed, 1e-6f, "0.1 px per frame on Standard, +5 percent per level (level 5: x1.2)");
            float y0 = f.Y;
            s.Sim.Step(default);
            Assert.AreEqual(y0 + f.Speed, f.Y, 1e-5f);

            var sim = s.Sim;
            f.Y = sim.Position.y + 2; // Tala under water
            sim.Invulnerable = 0;
            int vit = sim.Vitality, hurts = 0;
            sim.Hurt += _ => hurts++;
            for (int i = 0; i < 36; i++) sim.Step(default);
            Assert.AreEqual(1, hurts, "one burn in 36 ticks");
            Assert.AreEqual(DamageSource.Water, sim.LastDamageSource);
            Assert.Less(sim.Vitality, vit);
            sim.Invulnerable = 0;
            sim.VeilTicks = 200;
            for (int i = 0; i < 72; i++) sim.Step(default);
            Assert.AreEqual(1, hurts, "the veil keeps the water off");

            sim.Die();
            for (int i = 0; i < 100 && sim.Life != PlayerLife.Playing; i++) sim.Step(default);
            Assert.AreEqual(s.World.Lanterns[sim.Checkpoint].y - Flood.BelowLantern, f.Y, 0.05f, "back under the lantern after death");
        }

        // ------------------------------------------------------------ creatures and bells

        [Test]
        public void Manananggal_Hovers_DivesAtTala_FliesBack_AndIsStunnedByWardAndBolt()
        {
            var w = new PlayerWorld(new Rect(-50, -10, 100, 60));
            w.Ledges.Add(new Rect(-50, -1, 100, 1));
            w.Lanterns.Add(new Vector2(-30, 0));
            var e = new Enemy { Kind = EnemyKind.Manananggal, Size = new Vector2(20 * Px, 14 * Px), Mask = Dim.Ordinary | Dim.Inverted, Hp = 2, Home = new Vector2(0, 8) };
            e.Position = new Vector2(0, 8 - e.Size.y);
            w.Enemies.Add(e);
            var s = new PlayerSim(_t, _table.Get("standard"), w) { Skills = SkillSet.AllSkills(), SkillTuning = SkillLibrary.Tuning };
            s.Position = new Vector2(-30, 0);
            for (int i = 0; i < 30; i++) s.Step(default);
            Assert.AreEqual(FlightState.Hover, e.Flight, "far away: it hovers");
            Assert.LessOrEqual(Mathf.Abs(e.Position.x - e.Home.x), 55 * Px + 1e-3f);
            s.Position = new Vector2(e.Position.x, 5); // 3 tiles under it
            s.Invulnerable = 100000;
            s.Step(default);
            Assert.AreEqual(FlightState.Dive, e.Flight, "within 130 px: it dives");
            Vector2 TopLeft() => new Vector2(e.Position.x, e.Position.y + e.Size.y);
            float d0 = Vector2.Distance(TopLeft(), e.DiveTarget);
            for (int i = 0; i < 10; i++) s.Step(default);
            Assert.Less(Vector2.Distance(TopLeft(), e.DiveTarget), d0, "toward where Tala was");
            for (int i = 0; i < 400 && e.Flight != FlightState.Hover; i++) s.Step(default);
            Assert.AreEqual(FlightState.Hover, e.Flight, "back home");
            Assert.Greater(e.Cool, 100, "then it rests");

            s.Position = new Vector2(e.Center.x - 0.3f, e.Position.y);
            s.WardCooldown = 0;
            s.Step(new PlayerInputFrame { WardPressed = true });
            Assert.AreEqual(1, e.Hp, "ward: 1 hp");
            Assert.Greater(e.Stun, 0, "and stunned");
            float y = e.Position.y;
            s.Step(default);
            Assert.Less(e.Position.y, y, "a stunned manananggal sinks");
            s.SetDimension(Dim.Spirit);
            Assert.IsFalse(DimensionRules.Active(e.Mask, s.Dimension), "it cannot follow into the Spirit world");
        }

        [Test]
        public void ActIII_Bells_WarnThenFall_Hurt_AndTheVeilBlocksThem()
        {
            var L = Layout(6, "standard", ObstacleCheck.OwnedBefore(6));
            Assert.Greater(L.Bells.Count, 0, "level 7 rings with falling bells");
            Assert.AreEqual(0, Layout(2, "standard", SkillIds.All).Bells.Count, "none before Act III");
            var w = new PlayerWorld(new Rect(-50, -10, 100, 60));
            w.Ledges.Add(new Rect(-50, -1, 100, 1));
            w.Lanterns.Add(new Vector2(-30, 0));
            var bell = new BellDrop { X = 0.3f, TopY = 6, GroundY = 0, Period = 120, Phase = 0, WarnTicks = 40 };
            w.Bells.Add(bell);
            var s = new PlayerSim(_t, _table.Get("standard"), w);
            s.Position = new Vector2(0, 0);
            s.Invulnerable = 0;
            bool warned = false;
            int hurts = 0;
            s.Hurt += _ => hurts++;
            for (int i = 0; i < 240 && hurts == 0; i++) { s.Step(default); warned |= bell.Warning; }
            Assert.IsTrue(warned, "the red mark shows first");
            Assert.AreEqual(1, hurts, "a falling bell hurts");
            Assert.AreEqual(DamageSource.Bell, s.LastDamageSource);

            var v = new PlayerSim(_t, _table.Get("standard"), w);
            v.Position = Vector2.zero;
            v.VeilTicks = 100000;
            int vh = 0;
            v.Hurt += _ => vh++;
            for (int i = 0; i < 300; i++) v.Step(default);
            Assert.AreEqual(0, vh, "the veil keeps bells off");
        }

        [Test]
        public void ThornWalls_AreSolid_EvenToADash_UntilABoltBurnsThem()
        {
            PlayerSim Sim(params string[] skills)
            {
                var w = new PlayerWorld(new Rect(-50, -10, 100, 60));
                w.Ledges.Add(new Rect(-50, -1, 100, 1));
                w.Lanterns.Add(new Vector2(-30, 0));
                w.Hazards.Add(new Hazard(new Rect(3, 0, 1, 130 * Px), Dim.Ordinary, HazardKind.Thorns));
                var s = new PlayerSim(_t, _table.Get("standard"), w) { Skills = new SkillSet(skills), SkillTuning = SkillLibrary.Tuning };
                s.Position = new Vector2(0, 0);
                s.Invulnerable = 0;
                return s;
            }
            var a = Sim();
            for (int i = 0; i < 80; i++) a.Step(new PlayerInputFrame { MoveX = 1, DashPressed = i == 5 });
            Assert.Less(a.Position.x + a.Size.x, 3.01f, "a dash stops at the wall");
            var b = Sim(SkillIds.Bolt);
            b.Facing = 1;
            b.Step(new PlayerInputFrame { BoltPressed = true });
            for (int i = 0; i < 80; i++) b.Step(new PlayerInputFrame { MoveX = 1 });
            Assert.Greater(b.Position.x, 4f, "burnt: the way is open");
        }

        // ------------------------------------------------------------ the Bellkeeper

        LevelSession BossFight(string diff = "standard", params string[] skills)
        {
            var s = Session(8, diff, skills);
            var sim = s.Sim;
            var arena = s.World.Arena;
            Assert.IsNotNull(arena);
            Assert.IsFalse(s.Boss.Active, "asleep until the arena lantern");
            var lantern = s.World.Lanterns[s.Layout.Rooms];
            sim.Position = new Vector2(lantern.x - sim.Size.x / 2, lantern.y);
            sim.Invulnerable = 0;
            sim.Step(default);
            Assert.IsTrue(s.Boss.Active, "lighting the arena lantern wakes it");
            return s;
        }

        void RunTo(LevelSession s, BossState state, int max = 2000)
        {
            for (int i = 0; i < max && s.Boss.State != state; i++) s.Sim.Step(default);
            Assert.AreEqual(state, s.Boss.State);
        }

        [Test]
        public void Bellkeeper_CyclesWaveBellsToll_TheTollHurtsUnlessParried_AndTheVeilBlocksItsBlows()
        {
            var s = BossFight();
            var sim = s.Sim;
            sim.Invulnerable = 100000;
            RunTo(s, BossState.Idle);
            RunTo(s, BossState.Wave);
            RunTo(s, BossState.Bells);
            RunTo(s, BossState.Toll);
            // unparried toll: it hurts (through the one damage gate)
            sim.Invulnerable = 0;
            int vit = sim.Vitality;
            for (int i = 0; i < 80 && s.Boss.State == BossState.Toll; i++) sim.Step(default);
            Assert.AreEqual(vit - 1, sim.Vitality, "the toll hurts when not parried");
            Assert.AreEqual(DamageSource.Boss, sim.LastDamageSource);
            Assert.AreEqual(s.Boss.MaxHp, s.Boss.Hp, "and the Bellkeeper is untouched");

            // the shockwave, under the veil
            var v = BossFight("standard", SkillIds.Veil);
            v.Sim.Invulnerable = 100000;
            RunTo(v, BossState.Wave);
            v.Sim.Invulnerable = 0;
            v.Sim.VeilCooldown = 0;
            v.Sim.Step(new PlayerInputFrame { VeilPressed = true });
            int vv = v.Sim.Vitality;
            for (int i = 0; i < 170; i++) { v.Sim.Step(default); if (v.Sim.VeilTicks == 0) break; }
            Assert.AreEqual(vv, v.Sim.Vitality, "nothing gets through the veil");
        }

        [Test]
        public void Bellkeeper_ParryStuns_WardAndBoltHurtOnlyWhileStunned_BarToZero_LastDoor_Cleared()
        {
            var s = BossFight("story", SkillIds.Bolt);
            var sim = s.Sim;
            var boss = s.Boss;
            sim.Invulnerable = 1000000;
            sim.Flame = 100;
            // a bolt outside the stun does nothing
            RunTo(s, BossState.Idle);
            sim.Position = new Vector2(boss.X - 2.5f, boss.FloorY);
            sim.Facing = 1;
            for (int i = 0; i < 10; i++) { sim.Position = new Vector2(boss.X - 2.5f, boss.Core.y - sim.Size.y / 2); sim.Velocity = Vector2.zero; sim.Step(default); }
            int hp = boss.Hp;
            sim.Step(new PlayerInputFrame { BoltPressed = true });
            for (int i = 0; i < 20; i++) { sim.Position = new Vector2(boss.X - 2.5f, boss.Core.y - sim.Size.y / 2); sim.Velocity = Vector2.zero; sim.Step(default); }
            Assert.AreEqual(hp, boss.Hp, "a bolt does nothing unless it is stunned");

            int parries = 0, guard = 0;
            boss.Parried += () => parries++;
            while (!boss.Dead && guard++ < 20)
            {
                RunTo(s, BossState.Toll, 3000);
                // stand under it and WARD as the ring closes
                for (int i = 0; i < 80 && boss.State == BossState.Toll; i++)
                {
                    sim.Position = new Vector2(boss.X - sim.Size.x / 2, boss.FloorY);
                    sim.WardCooldown = 0;
                    sim.Step(new PlayerInputFrame { WardPressed = boss.TollTicks == 66 });
                }
                if (boss.Dead) break;
                Assert.AreEqual(BossState.Stun, boss.State, "parried: stunned");
                int stunned = boss.Hp;
                // a fresh ward next to it while stunned: 1 damage; then a bolt: 1 more
                for (int i = 0; i < 40; i++) { sim.Position = new Vector2(boss.X - sim.Size.x / 2, boss.FloorY); sim.Step(default); }
                sim.WardCooldown = 0;
                sim.Step(new PlayerInputFrame { WardPressed = true });
                if (boss.Dead) break;
                Assert.AreEqual(stunned - 1, boss.Hp, "ward while stunned");
                sim.BoltCooldown = 0;
                sim.Facing = 1;
                sim.Position = new Vector2(boss.X - 1.2f, boss.Core.y - sim.Size.y / 2);
                sim.Velocity = Vector2.zero;
                sim.Step(new PlayerInputFrame { BoltPressed = true });
                for (int i = 0; i < 8; i++) { sim.Position = new Vector2(boss.X - 1.2f, boss.Core.y - sim.Size.y / 2); sim.Velocity = Vector2.zero; sim.Step(default); }
                if (boss.Dead) break;
                Assert.AreEqual(stunned - 2, boss.Hp, "bolt while stunned");
            }
            Assert.IsTrue(boss.Dead, "the bar reaches zero");
            Assert.AreEqual(0f, boss.Fraction);
            Assert.Greater(parries, 0);
            for (int i = 0; i < 130; i++) sim.Step(default);
            var exit = s.World.Doors.Where(d => d.Kind == DoorKind.Exit).ToList();
            Assert.AreEqual(1, exit.Count, "the last door opens");
            bool done = false;
            s.LevelCompleted += () => done = true;
            sim.Position = new Vector2(exit[0].Rect.center.x - sim.Size.x / 2, exit[0].Rect.y);
            sim.Step(default);
            sim.Step(new PlayerInputFrame { InteractPressed = true });
            Assert.IsTrue(done, "through the last door");
            var reloaded = new SaveService(_dir);
            reloaded.Load();
            Assert.IsTrue(reloaded.Data.cleared, "cleared is saved");
            Assert.IsFalse(reloaded.Data.HasContinue);
        }

        [Test]
        public void Bellkeeper_WakesAtOnce_WhenContinuingFromTheArenaLantern()
        {
            var save = Save();
            var spec = _levels[8].spec;
            var probe = new LevelSession(spec, new LevelRequest { Level = 8, Difficulty = "standard" }, _t, _table, _curve, save);
            int arena = probe.Layout.Rooms;
            var s = new LevelSession(spec, new LevelRequest { Level = 8, Difficulty = "standard", Resume = new ResumeState { active = true, lv = 8, cp = arena, vit = 4, flame = 100, lives = 5 } },
                                     _t, _table, _curve, save);
            Assert.IsTrue(s.Boss.Active, "no soft lock on Continue");
        }
    }
}
