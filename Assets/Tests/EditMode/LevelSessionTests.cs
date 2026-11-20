using System.IO;
using System.Linq;
using NUnit.Framework;
using Skp.Core;
using Skp.Dimensions;
using Skp.Levels;
using Skp.Player;
using Skp.Save;
using UnityEngine;

namespace Skp.Tests
{
    /// <summary>M4: level session (exit lock, checkpoints, resume, completion), level mechanics and the completability bot.</summary>
    public class LevelSessionTests
    {
        PlayerTuning _t;
        DifficultyTable _table;
        DifficultyCurve _curve;
        string _dir;
        SaveService _save;

        [SetUp]
        public void SetUp()
        {
            _t = ScriptableObject.CreateInstance<PlayerTuning>();
            _table = ScriptableObject.CreateInstance<DifficultyTable>();
            _curve = ScriptableObject.CreateInstance<DifficultyCurve>();
            _dir = Path.Combine(Path.GetTempPath(), "skp_level_tests_" + System.Guid.NewGuid().ToString("N"));
            _save = new SaveService(_dir);
            _save.Load();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_t); Object.DestroyImmediate(_table); Object.DestroyImmediate(_curve);
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        LevelSession Session(int lv, string df = "standard", ResumeState resume = null) =>
            new LevelSession(LevelCatalog.Defaults()[lv], new LevelRequest { Level = lv, Difficulty = df, Resume = resume }, _t, _table, _curve, _save);

        // ------------------------------------------------------------ session

        [Test]
        public void Start_AtSpawnLantern_WithDifficultyRamp()
        {
            var s = Session(2, "nightmare");
            Assert.AreEqual(15f, s.Sim.Center.x, 1e-4f);
            Assert.AreEqual(0f, s.Sim.Position.y, 1e-4f);
            Assert.AreEqual(60, s.Sim.Invulnerable, "60 ticks of grace at level start");
            Assert.AreEqual(Mathf.RoundToInt(180 * (1 - 0.035f * 2)), s.Sim.Shadow.Delay, "shadow lag shortened for level 3");
            Assert.AreEqual(1.25f * 1.04f, s.Sim.EnemySpeedMult, 1e-5f);
            Assert.AreEqual(3, s.Sim.MaxVitality);
        }

        [Test]
        public void Exit_StaysLocked_UntilTheLetter_ThenCompletes()
        {
            var s = Session(0);
            int locked = 0; string msg = null; bool done = false;
            s.LockedExit += () => locked++;
            s.Message += m => msg = m;
            s.LevelCompleted += () => done = true;
            s.TryExit();
            Assert.AreEqual(1, locked);
            Assert.AreEqual(LevelSession.LockedMessage, msg);
            Assert.IsFalse(done);
            Assert.AreEqual(1, _save.Data.unlocked);

            // pick up the letter (any route) by standing on it
            var clue = s.World.Items.First(i => i.Kind == ItemKind.Clue && i.Mask == Dim.Ordinary);
            s.Sim.Position = new Vector2(clue.Rect.center.x - s.Sim.Size.x / 2, clue.Rect.y);
            s.Sim.Step(new PlayerInputFrame());
            Assert.IsTrue(s.Sim.HasClue);
            Assert.IsTrue(s.World.Items.Where(i => i.Kind == ItemKind.Clue).All(i => i.Got), "every copy of the letter is taken");

            int scoreBefore = s.Sim.Score;
            s.TryExit();
            Assert.IsTrue(done);
            Assert.AreEqual(scoreBefore + 500 + 5 * 50, s.Sim.Score, "completion bonus: 500 + 50 per life left");
            Assert.AreEqual(2, _save.Data.unlocked, "next node unlocked");
            Assert.IsFalse(_save.Data.cleared);
            Assert.IsTrue(_save.Data.HasContinue);
            Assert.AreEqual(1, _save.Data.cont.lv, "Continue starts level 2");
            Assert.AreEqual(0, _save.Data.cont.cp);
            Assert.AreEqual(2, new SaveService(_dir).Load().unlocked, "persisted: a fresh service reads the unlock");
        }

        [Test]
        public void Lantern_SavesCheckpoint_AndContinueResumesThere()
        {
            var s = Session(1);
            var lantern = s.World.Lanterns[2];
            s.Sim.Position = new Vector2(lantern.x - s.Sim.Size.x / 2, lantern.y);
            s.Sim.Vitality = 2; s.Sim.Score = 777;
            s.Sim.Step(new PlayerInputFrame());
            Assert.AreEqual(2, s.Sim.Checkpoint);
            var cont = new SaveService(_dir).Load().cont;
            Assert.IsTrue(cont.active);
            Assert.AreEqual(1, cont.lv);
            Assert.AreEqual(2, cont.cp);
            Assert.AreEqual(3, cont.vit, "lantern +1 vitality is saved");
            Assert.AreEqual(877, cont.score, "lantern +100 score is saved");

            var resumed = Session(1, "standard", cont);
            Assert.AreEqual(2, resumed.Sim.Checkpoint);
            Assert.AreEqual(lantern.x, resumed.Sim.Center.x, 1e-4f);
            Assert.AreEqual(lantern.y, resumed.Sim.Position.y, 1e-4f);
            Assert.AreEqual(3, resumed.Sim.Vitality);
            Assert.AreEqual(877, resumed.Sim.Score);
            Assert.AreEqual(GameSession.FromSave(_save.Data).Level, 1, "Continue picks the saved level");
        }

        [Test]
        public void Death_RespawnsAtTheLastLantern()
        {
            var s = Session(0);
            var lantern = s.World.Lanterns[1];
            s.Sim.Position = new Vector2(lantern.x - s.Sim.Size.x / 2, lantern.y);
            s.Sim.Step(new PlayerInputFrame());
            s.Sim.Die();
            for (int i = 0; i < 75; i++) s.Sim.Step(new PlayerInputFrame());
            Assert.AreEqual(PlayerLife.Playing, s.Sim.Life);
            Assert.AreEqual(lantern.x, s.Sim.Center.x, 1e-4f);
            Assert.AreEqual(4, s.Sim.Lives);
        }

        [Test]
        public void GameOver_ClearsTheResumePoint()
        {
            var s = Session(0, "nightmare");
            s.SaveCheckpoint();
            Assert.IsTrue(_save.Data.HasContinue);
            bool over = false;
            s.GameOver += () => over = true;
            for (int d = 0; d < 4; d++) { s.Sim.Die(); for (int i = 0; i < 75; i++) s.Sim.Step(new PlayerInputFrame()); }
            Assert.IsTrue(over);
            Assert.IsFalse(new SaveService(_dir).Load().HasContinue);
        }

        [Test]
        public void FinalLevel_SetsCleared_AndClearsContinue()
        {
            var spec = LevelCatalog.Defaults()[8];
            var s = new LevelSession(spec, new LevelRequest { Level = 8, Difficulty = "standard" }, _t, _table, _curve, _save);
            ProgressRules.Unlock(_save.Data, 9);
            s.TryExit(); // the boss level has no letter to find
            Assert.IsTrue(s.Completed);
            Assert.IsTrue(_save.Data.cleared);
            Assert.IsFalse(_save.Data.HasContinue);
        }

        // ------------------------------------------------------------ mechanics

        static PlayerWorld Flat()
        {
            var w = new PlayerWorld(new Rect(-50, -50, 200, 200));
            w.Ledges.Add(new Ledge(new Rect(-50, -1, 200, 1), Dim.All, LedgeKind.Floor));
            w.Lanterns.Add(new Vector2(-40, 0));
            return w;
        }

        PlayerSim Sim(PlayerWorld w) => new PlayerSim(_t, _table.Get(DifficultyId.Standard), w);
        static PlayerSim At(PlayerSim s, float cx, float y) { s.Position = new Vector2(cx - s.Size.x / 2, y); s.Grounded = false; return s; }

        [Test]
        public void CrumblingLedge_Falls42TicksAfterLanding_ReturnsAfter220()
        {
            var w = Flat();
            var c = new Ledge(new Rect(0, 2, 3, 0.5f), Dim.Ordinary, LedgeKind.Crumble);
            w.Ledges.Add(c);
            var s = At(Sim(w), 1.5f, 2.5f);
            int fell = 0; s.LedgeCrumbled += _ => fell++;
            s.Step(new PlayerInputFrame());
            Assert.AreSame(c, s.Standing);
            Assert.AreEqual(42, c.CrumbleTimer);
            for (int i = 0; i < 41; i++) s.Step(new PlayerInputFrame());
            Assert.IsFalse(c.IsGone);
            s.Step(new PlayerInputFrame());
            Assert.IsTrue(c.IsGone);
            Assert.AreEqual(1, fell);
            for (int i = 0; i < 30; i++) s.Step(new PlayerInputFrame());
            Assert.AreEqual(0f, s.Position.y, 1e-4f, "fell through to the floor");
            for (int i = 0; i < 220; i++) s.Step(new PlayerInputFrame());
            Assert.IsFalse(c.IsGone, "back after 220 ticks");
        }

        [Test]
        public void MovingLedge_CarriesTala()
        {
            var w = Flat();
            var m = new Ledge(new Rect(5, 3, 5, 0.5f), Dim.Ordinary, LedgeKind.Moving) { BaseX = 5, Amplitude = 8.4f, Speed = .015f, Phase = 0 };
            w.Ledges.Add(m);
            var s = At(Sim(w), 7.5f, 3.5f);
            s.Step(new PlayerInputFrame());
            Assert.AreSame(m, s.Standing);
            float offset = s.Position.x - m.Rect.x;
            for (int i = 0; i < 60; i++) s.Step(new PlayerInputFrame());
            Assert.AreSame(m, s.Standing, "still on board");
            Assert.AreEqual(offset, s.Position.x - m.Rect.x, 0.02f, "moved with the ledge");
            Assert.Greater(Mathf.Abs(m.Rect.x - 5f), 0.5f, "the ledge did move");
        }

        [Test]
        public void Pickups_Flame_Fragment_Letter()
        {
            var w = Flat();
            w.Items.Add(new Item { Rect = new Rect(0, 0, 0.9f, 0.9f), Mask = Dim.Ordinary, Kind = ItemKind.Flame });
            w.Items.Add(new Item { Rect = new Rect(2, 0, 0.9f, 0.9f), Mask = Dim.Ordinary, Kind = ItemKind.Shard });
            w.Items.Add(new Item { Rect = new Rect(4, 0, 0.9f, 0.9f), Mask = Dim.Spirit, Kind = ItemKind.Clue });
            var s = At(Sim(w), 0.4f, 0);
            s.Flame = 50;
            s.Step(new PlayerInputFrame());
            Assert.AreEqual(65, s.Flame, "+15 flame");
            Assert.AreEqual(10, s.Score);
            At(s, 2.4f, 0); s.Step(new PlayerInputFrame());
            Assert.AreEqual(1, s.Shards);
            Assert.AreEqual(105, s.MaxFlame, "max flame +5");
            Assert.AreEqual(105, s.Flame, "refilled");
            At(s, 4.4f, 0); s.Step(new PlayerInputFrame());
            Assert.IsFalse(s.HasClue, "the Spirit letter cannot be taken from the Waking World");
            s.SetDimension(Dim.Spirit); s.Step(new PlayerInputFrame());
            Assert.IsTrue(s.HasClue);
            Assert.AreEqual(10 + 200 + 150, s.Score);
        }

        [Test]
        public void Anino_Patrols_BouncesAtEdges_HurtsOnContact_WardKillsIt()
        {
            var w = Flat();
            var e = new Enemy { Kind = EnemyKind.Anino, Position = new Vector2(2, 0), Size = new Vector2(10 / 16f, 11 / 16f), Mask = Dim.Ordinary, Hp = 1, Speed = .55f, Dir = 1, MinX = 1.25f, MaxX = 4.75f };
            w.Enemies.Add(e);
            var s = At(Sim(w), 10, 0);
            bool turned = false;
            for (int i = 0; i < 200; i++) { s.Step(new PlayerInputFrame()); if (e.Dir < 0) turned = true; }
            Assert.IsTrue(turned, "turns at the patrol edge");
            Assert.GreaterOrEqual(e.Position.x, e.MinX - 1e-4f);
            Assert.LessOrEqual(e.Position.x + e.Size.x, e.MaxX + 1e-4f);

            At(s, e.Center.x, 0);
            s.Step(new PlayerInputFrame());
            Assert.AreEqual(3, s.Vitality, "contact hurts through CanBeHurt");
            s.Invulnerable = 1000;
            At(s, e.Center.x + 1, 0);
            s.Step(new PlayerInputFrame { WardPressed = true });
            Assert.IsTrue(e.Dead, "a ward kills an Anino");
            Assert.AreEqual(1, s.Kills);
        }

        [Test]
        public void Root_StrikesOnlyInItsWindow_Thorns_BlockUntilBurnt()
        {
            var w = Flat();
            var root = new Hazard(new Rect(0, 0, 0.75f, 1), Dim.Ordinary, HazardKind.Root) { Phase = 0 };
            w.Hazards.Add(root);
            var s = At(Sim(w), 0.375f, 0);
            s.Invulnerable = 0;
            int hits = 0; s.Hurt += _ => hits++;
            for (int i = 0; i < 108; i++) { s.Step(new PlayerInputFrame()); s.Position = new Vector2(0.375f - s.Size.x / 2, 0); }
            Assert.AreEqual(0, hits, "dormant and warning phases are safe");
            for (int i = 0; i < 4; i++) { s.Step(new PlayerInputFrame()); s.Position = new Vector2(0.375f - s.Size.x / 2, 0); }
            Assert.AreEqual(1, hits, "strikes at tick 110 of 150");

            var w2 = Flat();
            var thorns = new Hazard(new Rect(3, 0, 1, 130 / 16f), Dim.Ordinary, HazardKind.Thorns);
            w2.Hazards.Add(thorns);
            var t = At(Sim(w2), 3.5f, 0);
            t.Step(new PlayerInputFrame());
            Assert.AreEqual(3, t.Vitality);
            thorns.BurnTicks = 600; t.Invulnerable = 0;
            t.Step(new PlayerInputFrame());
            Assert.AreEqual(3, t.Vitality, "burnt thorns let her through");
        }

        // ------------------------------------------------------------ the completability bot

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void Bot_CompletesLevel_FromSpawn_OnStandard_WithTheBaseKit(int lv)
        {
            var d = _table.Get("standard");
            var layout = LevelGenerator.Build(LevelCatalog.Defaults()[lv], lv, LevelSession.Exact(d.hazardMult), LevelSession.Exact(d.enemyMult), LevelSession.Exact(d.floodMult), _ => false);
            var bot = new LevelBot(layout, _t, d);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var r = bot.Run();
            TestContext.WriteLine($"Level {lv + 1}: {r} in {sw.ElapsedMilliseconds} ms");
            Assert.IsTrue(r.ClueReachable, "letter reachable: " + r);
            Assert.IsTrue(r.ExitReachableAfterClue, "exit reachable after the letter: " + r);
            for (int i = 0; i < r.LanternReachable.Length; i++) Assert.IsTrue(r.LanternReachable[i], $"lantern {i} (room {i - 1} top) unreachable: {r}");
            // survivable: hits on the gentlest run stay below starting vitality plus one heart per lantern lit on the way
            Assert.Less(r.HitsOnExitPath, d.vitality + layout.Rooms, "too many unavoidable hits: " + r);
        }
    }
}
