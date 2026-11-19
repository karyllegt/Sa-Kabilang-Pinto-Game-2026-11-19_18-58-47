using System;
using NUnit.Framework;
using Skp.Core;
using Skp.Player;
using UnityEngine;

namespace Skp.Tests
{
    /// <summary>
    /// The player simulation against numbers measured from the prototype's own step function
    /// (node runs of reference/prototype/index.html logic; see DECISIONS.md M2).
    /// 1 unit = 16 px; a tick is 1/60 s.
    /// </summary>
    public class PlayerSimTests
    {
        const float Px = 1f / 16f;
        PlayerTuning _t;
        DifficultyTable _d;

        [SetUp]
        public void SetUp()
        {
            _t = ScriptableObject.CreateInstance<PlayerTuning>();
            _d = ScriptableObject.CreateInstance<DifficultyTable>();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_t);
            UnityEngine.Object.DestroyImmediate(_d);
        }

        // ------------------------------------------------------------ helpers

        static PlayerWorld FlatWorld(params Vector2[] lanterns)
        {
            var w = new PlayerWorld(new Rect(-100, -200, 400, 400));
            w.Ledges.Add(new Rect(-100, -1, 400, 1)); // floor, top at y = 0
            if (lanterns.Length == 0) w.Lanterns.Add(new Vector2(10, 0));
            foreach (var l in lanterns) w.Lanterns.Add(l);
            return w;
        }

        PlayerSim NewSim(PlayerWorld w, DifficultyId d = DifficultyId.Standard) => new PlayerSim(_t, _d.Get(d), w);

        static PlayerInputFrame In(int move = 0, bool jumpP = false, bool jump = false, bool dash = false, bool ward = false) =>
            new PlayerInputFrame { MoveX = move, JumpPressed = jumpP, JumpHeld = jump || jumpP, DashPressed = dash, WardPressed = ward };

        // ------------------------------------------------------------ jump

        [Test]
        public void FullHoldJump_Apex3p9Tiles_AtTick16_LandsAtTick34()
        {
            var s = NewSim(FlatWorld());
            float y0 = s.Position.y, best = y0;
            int apexTick = -1, landTick = -1;
            for (int t = 0; t < 120; t++)
            {
                s.Step(In(jumpP: t == 0, jump: true));
                if (s.Position.y > best + 1e-6f) { best = s.Position.y; apexTick = t; }
                if (t > 0 && s.Grounded) { landTick = t; break; }
            }
            Assert.AreEqual(62.5f * Px, best - y0, 1e-4f, "apex height (62.5 px = 3.906 tiles)");
            Assert.AreEqual(16, apexTick, "apex tick (about 0.27 s)");
            Assert.AreEqual(34, landTick, "landing tick");
        }

        [TestCase(1, 19.3f)]
        [TestCase(6, 42.025f)]
        public void ReleasingEarly_GivesALowerJump(int holdTicks, float expectedPx)
        {
            var s = NewSim(FlatWorld());
            float y0 = s.Position.y, best = y0;
            for (int t = 0; t < 60; t++)
            {
                s.Step(In(jumpP: t == 0, jump: t < holdTicks));
                best = Mathf.Max(best, s.Position.y);
            }
            Assert.AreEqual(expectedPx * Px, best - y0, 1e-4f);
        }

        // Ledge from x = 0 (40 px wide by default), floor far below; run right off it.
        static PlayerWorld LedgeOverPit(float dropTo = -125f, float widthPx = 40f)
        {
            var w = new PlayerWorld(new Rect(-100, -200, 400, 400));
            w.Ledges.Add(new Rect(0, -1, widthPx * Px, 1));
            w.Ledges.Add(new Rect(-100, dropTo - 1, 400, 1));
            w.Lanterns.Add(new Vector2(20 * Px + 5 * Px, 0)); // player x = 20 px, as in the oracle run
            return w;
        }

        int FirstAirborneTick()
        {
            var s = NewSim(LedgeOverPit());
            for (int t = 0; t < 200; t++) { s.Step(In(move: 1)); if (!s.Grounded) return t; }
            throw new Exception("never left the ledge");
        }

        [Test]
        public void CoyoteJump_WorksFor5TicksAfterLeavingALedge_NotOn6th()
        {
            int leave = FirstAirborneTick();
            Assert.AreEqual(11, leave, "oracle: airborne on tick 11");
            for (int k = 1; k <= 6; k++)
            {
                var s = NewSim(LedgeOverPit());
                int jumpedAt = -1;
                s.Jumped += () => { if (jumpedAt < 0) jumpedAt = (int)s.Tick - 1; };
                for (int t = 0; t <= leave + 20; t++) s.Step(In(move: 1, jumpP: t == leave + k, jump: t >= leave + k));
                if (k <= 5) Assert.AreEqual(leave + k, jumpedAt, $"coyote press +{k}");
                else Assert.AreEqual(-1, jumpedAt, "press +6 is too late");
            }
        }

        [Test]
        public void BufferedJump_PressUpTo5TicksBeforeLanding_JumpsTheTickAfter()
        {
            // drop 3 tiles (48 px) onto a floor
            Func<PlayerSim> make = () => NewSim(LedgeOverPit(-3f, 30f)); // oracle: 30 px ledge, floor 48 px below
            var probe = make();
            int land = -1; bool air = false;
            for (int t = 0; t < 400; t++)
            {
                probe.Step(In(move: t < 30 ? 1 : 0));
                if (!probe.Grounded) air = true;
                if (air && probe.Grounded) { land = t; break; }
            }
            Assert.AreEqual(20, land, "oracle: lands on tick 20");

            foreach (var (early, shouldJump) in new[] { (4, true), (5, true), (6, false), (7, false) })
            {
                var s = make();
                int jumpedAt = -1;
                s.Jumped += () => { if (jumpedAt < 0) jumpedAt = (int)s.Tick - 1; };
                int press = land - early;
                for (int t = 0; t <= land + 10; t++) s.Step(In(move: t < 30 ? 1 : 0, jumpP: t == press, jump: t >= press));
                Assert.AreEqual(shouldJump ? land + 1 : -1, jumpedAt, $"press {early} ticks before landing");
            }
        }

        // ------------------------------------------------------------ dash

        [Test]
        public void Dash_Covers59p4Px_Costs20Flame_UntouchableFor10Ticks()
        {
            var s = NewSim(FlatWorld());
            float x0 = s.Position.x;
            int immune = 0;
            for (int t = 0; t < 11; t++)
            {
                s.Step(In(dash: t == 0));
                if (!s.CanBeHurt()) immune++;
            }
            Assert.AreEqual(59.4f * Px, s.Position.x - x0, 1e-4f, "dash distance");
            Assert.AreEqual(80, s.Flame, "flame cost");
            Assert.AreEqual(10, immune, "untouchable ticks");
            Assert.IsTrue(s.CanBeHurt(), "vulnerable again after the dash");
        }

        [Test]
        public void Dash_RespectsCooldown_AndNeedsEnoughFlame()
        {
            var s = NewSim(FlatWorld());
            int dashes = 0;
            s.Dashed += () => dashes++;
            s.Step(In(dash: true));                                   // tick 0: dash, cooldown 34
            for (int t = 1; t < 34; t++) s.Step(In(dash: t == 20));   // press during cooldown: ignored
            Assert.AreEqual(1, dashes);
            s.Step(In(dash: true));                                   // tick 34: cooldown over
            Assert.AreEqual(2, dashes);

            var poor = NewSim(FlatWorld());
            poor.Flame = 19;
            poor.Step(In(dash: true));
            Assert.AreEqual(0, poor.DashTicks, "no dash under 20 flame");
            Assert.AreEqual(19, poor.Flame);
        }

        // ------------------------------------------------------------ ward

        [Test]
        public void Ward_PulsesWith46PxRadius_Cooldown72Ticks()
        {
            var s = NewSim(FlatWorld());
            int pulses = 0; float radius = 0;
            s.Warded += (c, r) => { pulses++; radius = r; };
            s.Step(In(ward: true));
            Assert.AreEqual(1, pulses);
            Assert.AreEqual(46f * Px, radius, 1e-5f);
            Assert.AreEqual(24, s.WardTicks);
            for (int t = 1; t < 72; t++) s.Step(In(ward: t == 71));
            Assert.AreEqual(1, pulses, "tick 71 is still cooling down");
            s.Step(In(ward: true));
            Assert.AreEqual(2, pulses, "ready on tick 72");
        }

        // ------------------------------------------------------------ hurt

        [Test]
        public void Knockback_PushesAwayFromTheSource_AndUp()
        {
            var s = NewSim(FlatWorld());
            Assert.IsTrue(s.TryHurt(1, s.Center.x + 1f));
            Assert.AreEqual(new Vector2(-9f, 12f), s.Velocity, "2.4 px/f away, 3.2 px/f up");
            Assert.IsFalse(s.Grounded);

            var r = NewSim(FlatWorld());
            r.TryHurt(1, r.Center.x - 1f);
            Assert.AreEqual(9f, r.Velocity.x);

            var back = NewSim(FlatWorld());
            back.Facing = 1;
            back.TryHurt(1, null);
            Assert.AreEqual(-9f, back.Velocity.x, "no source: pushed backwards");
        }

        [TestCase(DifficultyId.Standard, 85)]
        [TestCase(DifficultyId.Story, 110)]
        public void Invulnerability_LastsExactly(DifficultyId d, int ticks)
        {
            var s = NewSim(FlatWorld(), d);
            s.TryHurt(1, null);
            for (int t = 1; t < ticks; t++)
            {
                s.Step(In());
                Assert.IsFalse(s.CanBeHurt(), $"still protected after {t} ticks");
                Assert.IsFalse(s.TryHurt(1, null));
            }
            s.Step(In());
            Assert.IsTrue(s.CanBeHurt(), $"hurtable after {ticks} ticks");
        }

        [Test]
        public void CanBeHurt_TruthTable()
        {
            var s = NewSim(FlatWorld());
            Assert.IsTrue(s.CanBeHurt(), "none");
            s.Invulnerable = 1; Assert.IsFalse(s.CanBeHurt(), "invulnerable"); s.Invulnerable = 0;
            s.DashTicks = 1; Assert.IsFalse(s.CanBeHurt(), "dashing"); s.DashTicks = 0;
            s.VeilTicks = 1; Assert.IsFalse(s.CanBeHurt(), "veil"); s.VeilTicks = 0;
            s.Die(); Assert.IsFalse(s.CanBeHurt(), "dead");
        }

        [Test]
        public void Spikes_HurtOnContact_ThroughCanBeHurt()
        {
            var w = FlatWorld(new Vector2(0, 0));
            w.Hazards.Add(new Rect(1.5f, 0, 1.5f, 7 * Px));
            var s = NewSim(w);
            int hits = 0;
            s.Hurt += _ => hits++;
            for (int t = 0; t < 60 && hits == 0; t++) s.Step(In(move: 1));
            Assert.AreEqual(1, hits);
            Assert.AreEqual(3, s.Vitality);
            Assert.Less(s.Velocity.x, 0, "knocked back to the left, away from the spikes");
        }

        // ------------------------------------------------------------ death, lanterns, lives

        [Test]
        public void Lantern_LightsOnce_GivesFlameAndHeart_BecomesRespawnPoint()
        {
            var w = FlatWorld(new Vector2(0, 0), new Vector2(3, 0));
            var s = NewSim(w);
            int lit = -1;
            s.LanternLit += k => lit = k;
            s.Vitality = 2; s.Flame = 40;
            for (int t = 0; t < 120 && lit < 0; t++) s.Step(In(move: 1));
            Assert.AreEqual(1, lit);
            Assert.AreEqual(1, s.Checkpoint);
            Assert.AreEqual(3, s.Vitality, "+1 vitality");
            Assert.GreaterOrEqual(s.Flame, 70, "+30 flame");

            for (int i = 0; i < 4; i++) { s.Invulnerable = 0; s.TryHurt(1, null); }
            Assert.AreEqual(PlayerLife.Dead, s.Life);
            for (int t = 0; t < 75; t++) s.Step(In());
            Assert.AreEqual(PlayerLife.Playing, s.Life, "respawned after 75 ticks");
            Assert.AreEqual(3f, s.Center.x, 1e-4f, "at lantern 1");
            Assert.AreEqual(0f, s.Position.y, 1e-4f);
            Assert.AreEqual(s.MaxVitality, s.Vitality);
            Assert.AreEqual(100, s.Invulnerable);
            Assert.GreaterOrEqual(s.Flame, 60);
        }

        [Test]
        public void Lives_Standard5_Nightmare3_GameOverAfterTheLast_StoryNever()
        {
            var n = NewSim(FlatWorld(), DifficultyId.Nightmare);
            Assert.AreEqual(3, n.Lives);
            bool over = false;
            n.OutOfLives += () => over = true;
            for (int death = 1; death <= 4; death++)
            {
                n.Die();
                for (int t = 0; t < 75; t++) n.Step(In());
                if (death < 4) Assert.AreEqual(PlayerLife.Playing, n.Life, $"death {death}");
            }
            Assert.IsTrue(over);
            Assert.AreEqual(PlayerLife.GameOver, n.Life);

            Assert.AreEqual(5, NewSim(FlatWorld(), DifficultyId.Standard).Lives);
            var story = NewSim(FlatWorld(), DifficultyId.Story);
            Assert.IsTrue(story.InfiniteLives);
            for (int death = 0; death < 20; death++) { story.Die(); for (int t = 0; t < 75; t++) story.Step(In()); }
            Assert.AreEqual(PlayerLife.Playing, story.Life);
        }

        [Test]
        public void DifficultyTable_Vitality_6_4_3_4()
        {
            Assert.AreEqual(6, NewSim(FlatWorld(), DifficultyId.Story).MaxVitality);
            Assert.AreEqual(4, NewSim(FlatWorld(), DifficultyId.Standard).MaxVitality);
            Assert.AreEqual(3, NewSim(FlatWorld(), DifficultyId.Nightmare).MaxVitality);
            Assert.AreEqual(4, NewSim(FlatWorld(), DifficultyId.Endless).MaxVitality);
            Assert.AreEqual(DifficultyId.Nightmare, _d.Get("nightmare").id);
            Assert.AreEqual(DifficultyId.Standard, _d.Get("unknown").id, "unknown key falls back to Standard");
        }

        [Test]
        public void Flame_Regenerates1Every90Ticks()
        {
            var s = NewSim(FlatWorld());
            s.Flame = 50;
            for (int t = 0; t < 89; t++) s.Step(In());
            Assert.AreEqual(50, s.Flame);
            s.Step(In());
            Assert.AreEqual(51, s.Flame);
        }

        [Test]
        public void Run_ReachesButNeverExceedsMaxSpeed()
        {
            var s = NewSim(FlatWorld());
            float top = 0;
            for (int t = 0; t < 60; t++) { s.Step(In(move: 1)); top = Mathf.Max(top, s.Velocity.x); }
            Assert.AreEqual(_t.runSpeed, top, 1e-4f, "1.95 px/frame");
            for (int t = 0; t < 30; t++) s.Step(In());
            Assert.AreEqual(0f, s.Velocity.x, "friction stops her");
        }

        // ------------------------------------------------------------ frame rate independence

        [TestCase(30)]
        [TestCase(60)]
        [TestCase(144)]
        [TestCase(37)]
        public void SameJump_AtAnyFrameRate(int fps)
        {
            var (apex, apexTick, x) = SimulateFrames(fps);
            var (refApex, refTick, refX) = SimulateFrames(60);
            Assert.AreEqual(62.5f * Px, apex, 1e-4f);
            Assert.AreEqual(refApex, apex, 1e-6f);
            Assert.AreEqual(refTick, apexTick);
            Assert.AreEqual(refX, x, 1e-5f, "running distance identical too");
        }

        (float apex, long apexTick, float x) SimulateFrames(int fps)
        {
            var s = NewSim(FlatWorld());
            var clock = new FixedTickClock();
            var latch = new PlayerInputLatch();
            float y0 = s.Position.y, best = 0; long bestTick = 0;
            float dt = 1f / fps;
            // 1.5 s of real time: jump pressed on the first frame and held, running right throughout.
            int frames = Mathf.CeilToInt(1.5f * fps);
            for (int f = 0; f < frames; f++)
            {
                latch.Collect(new PlayerInputFrame { MoveX = 1, JumpPressed = f == 0, JumpHeld = true });
                int n = clock.Advance(dt);
                for (int i = 0; i < n && s.Tick < 90; i++)
                {
                    s.Step(latch.Consume());
                    if (s.Position.y - y0 > best + 1e-6f) { best = s.Position.y - y0; bestTick = s.Tick; }
                }
            }
            Assert.AreEqual(90, s.Tick, $"{fps} fps ran 90 ticks in 1.5 s");
            return (best, bestTick, s.Position.x);
        }
    }
}
