using System.Collections.Generic;
using NUnit.Framework;
using Skp.Core;
using Skp.Dimensions;
using Skp.Levels;
using Skp.Player;
using UnityEngine;

namespace Skp.Tests
{
    /// <summary>M3: dimension masks, doors, gravity flip, the Anino shadow and state reset on death.</summary>
    public class DimensionTests
    {
        const float Px = 1f / 16f;
        PlayerTuning _t;
        DifficultyTable _d;

        [SetUp] public void SetUp() { _t = ScriptableObject.CreateInstance<PlayerTuning>(); _d = ScriptableObject.CreateInstance<DifficultyTable>(); }
        [TearDown] public void TearDown() { Object.DestroyImmediate(_t); Object.DestroyImmediate(_d); }

        static PlayerWorld World(params Vector2[] lanterns)
        {
            var w = new PlayerWorld(new Rect(-100, -200, 400, 400));
            w.Ledges.Add(new Rect(-100, -1, 400, 1)); // floor in every dimension, top y = 0
            if (lanterns.Length == 0) w.Lanterns.Add(new Vector2(-80, 0)); // spawn far away: a lantern always returns Tala to Ordinary
            foreach (var l in lanterns) w.Lanterns.Add(l);
            return w;
        }

        PlayerSim Sim(PlayerWorld w, DifficultyId d = DifficultyId.Standard) => new PlayerSim(_t, _d.Get(d), w);
        static PlayerInputFrame In(int move = 0, bool jumpP = false, bool jump = false, bool interact = false, bool ward = false) =>
            new PlayerInputFrame { MoveX = move, JumpPressed = jumpP, JumpHeld = jump || jumpP, InteractPressed = interact, WardPressed = ward };
        static void Run(PlayerSim s, int ticks, PlayerInputFrame f = default) { for (int i = 0; i < ticks; i++) s.Step(f); }
        static PlayerSim At(PlayerSim s, float centerX, float feetY) { s.Position = new Vector2(centerX - s.Size.x * 0.5f, feetY); s.Velocity = Vector2.zero; s.Grounded = false; return s; }

        // ------------------------------------------------------------ masks

        [Test]
        public void MaskLogic_EveryMaskInEveryDimension()
        {
            for (int m = 0; m <= 7; m++)
            foreach (var d in DimensionRules.Each)
            {
                bool expected = (m & (int)d) != 0;
                Assert.AreEqual(expected, DimensionRules.Active((Dim)m, d), $"mask {m} in {d}");
            }
            Assert.AreEqual(1, DimensionRules.GravitySign(Dim.Ordinary));
            Assert.AreEqual(1, DimensionRules.GravitySign(Dim.Spirit));
            Assert.AreEqual(-1, DimensionRules.GravitySign(Dim.Inverted));
            Assert.AreEqual(Dim.All, Dim.Ordinary | Dim.Spirit | Dim.Inverted);
            Assert.AreEqual(1, (int)Dim.Ordinary); Assert.AreEqual(2, (int)Dim.Spirit); Assert.AreEqual(4, (int)Dim.Inverted);
        }

        [Test]
        public void DimensionMember_GhostsOrHidesOutsideItsMask()
        {
            var go = new GameObject("m");
            var sr = go.AddComponent<SpriteRenderer>();
            var dm = go.AddComponent<DimensionMember>();
            dm.mask = Dim.Spirit; dm.ghost = DimensionMember.Ghost.Ledge; dm.renderers = new[] { sr };
            dm.Apply(Dim.Spirit);
            Assert.IsTrue(sr.enabled); Assert.AreEqual(1f, sr.color.a, 1e-4f);
            dm.Apply(Dim.Ordinary);
            Assert.IsTrue(sr.enabled); Assert.AreEqual(0.16f, sr.color.a, 1e-4f, "ledge ghost 16 percent");
            dm.ghost = DimensionMember.Ghost.Hidden; dm.Apply(Dim.Inverted);
            Assert.IsFalse(sr.enabled, "items and enemies vanish outside their dimension");
            dm.ghost = DimensionMember.Ghost.Door; dm.Apply(Dim.Ordinary);
            Assert.AreEqual(0.12f, sr.color.a, 1e-4f, "door ghost 12 percent");
            Object.DestroyImmediate(go);
        }

        // ------------------------------------------------------------ doors

        static Door DoorAt(float x, Dim mask, Dim target, DoorKind kind = DoorKind.Enter, Vector2? to = null) =>
            new Door { Rect = new Rect(x - 0.5f, 0, 1, 28 * Px), Mask = mask, Target = target, Kind = kind, To = to };

        [Test]
        public void Doors_SwitchDimension_AndOnlyWorkInTheirOwnDimension()
        {
            var w = World();
            w.Doors.Add(DoorAt(2, Dim.Ordinary, Dim.Spirit));
            w.Doors.Add(DoorAt(2, Dim.Spirit, Dim.Ordinary, DoorKind.Home));
            w.Doors.Add(DoorAt(2, Dim.Inverted, Dim.Ordinary, DoorKind.Home)); // not usable from Ordinary
            var s = At(Sim(w), 2, 0);
            var changes = new List<Dim>();
            s.DimensionChanged += d => changes.Add(d);

            s.Step(In());
            Assert.AreEqual(0, s.NearDoor, "the Ordinary door is the usable one");
            s.Step(In(interact: true));
            Assert.AreEqual(Dim.Spirit, s.Dimension);
            s.Step(In());
            Assert.AreEqual(1, s.NearDoor, "in Spirit only the home door is usable");
            s.Step(In(interact: true));
            Assert.AreEqual(Dim.Ordinary, s.Dimension);
            CollectionAssert.AreEqual(new[] { Dim.Spirit, Dim.Ordinary }, changes);

            var away = At(Sim(World()), 30, 0);
            away.Step(In(interact: true));
            Assert.AreEqual(Dim.Ordinary, away.Dimension, "interact with no door does nothing");
            Assert.AreEqual(-1, away.NearDoor);
        }

        [Test]
        public void Door_WithArrival_Teleports_AndExitOnlyRequestsTheExit()
        {
            var w = World();
            w.Doors.Add(DoorAt(2, Dim.Ordinary, Dim.Ordinary, DoorKind.Home, to: new Vector2(10, 4)));
            var s = At(Sim(w), 2, 0);
            s.Step(In(interact: true));
            Assert.AreEqual(10f, s.Center.x, 1e-4f);
            Assert.AreEqual(4f, s.Position.y, 1e-4f);

            var w2 = World();
            w2.Doors.Add(new Door { Rect = new Rect(1.5f, 0, 1.25f, 2), Mask = Dim.All, Target = Dim.Ordinary, Kind = DoorKind.Exit });
            var e = At(Sim(w2), 2, 0);
            int exits = 0;
            e.ExitRequested += _ => exits++;
            e.SetDimension(Dim.Spirit);
            e.Step(In(interact: true));
            Assert.AreEqual(1, exits);
            Assert.AreEqual(Dim.Spirit, e.Dimension, "the exit does not change dimension; the level handles it");
        }

        // ------------------------------------------------------------ solid / harmful only in its dimension

        [TestCase(Dim.Ordinary, 0f)]
        [TestCase(Dim.Spirit, 3f)]
        [TestCase(Dim.Inverted, -999f)]
        public void Ledge_IsSolidOnlyInItsDimension(Dim current, float expectedFeetY)
        {
            var w = World();
            w.Ledges.Add(new MaskedRect(new Rect(3, 2, 4, 1), Dim.Spirit)); // top y = 3
            var s = At(Sim(w), 5, 4);
            s.SetDimension(current);
            Run(s, 90);
            if (current == Dim.Inverted) Assert.Greater(s.Position.y, 4f, "rises in Inverted gravity");
            else Assert.AreEqual(expectedFeetY, s.Position.y, 1e-4f);
        }

        [Test]
        public void Hazard_HarmsOnlyInItsDimension()
        {
            var w = World();
            w.Hazards.Add(new MaskedRect(new Rect(1.5f, 0, 1f, 7 * Px), Dim.Ordinary));
            var spirit = At(Sim(w), 2, 0);
            spirit.SetDimension(Dim.Spirit);
            spirit.Step(In());
            Assert.AreEqual(4, spirit.Vitality, "harmless in Spirit");
            var ord = At(Sim(w), 2, 0);
            ord.Step(In());
            Assert.AreEqual(3, ord.Vitality, "hurts in Ordinary");
        }

        // ------------------------------------------------------------ gravity flip

        [Test]
        public void Inverted_FallsUp_LandsOnUnderside_JumpMirrors()
        {
            var w = World();
            w.Ledges.Add(new MaskedRect(new Rect(-100, 5, 400, 1), Dim.Inverted)); // underside y = 5
            var s = Sim(w);
            s.SetDimension(Dim.Inverted);
            Assert.AreEqual(-1, s.GravitySign);
            for (int i = 0; i < 120 && !(s.Grounded && s.Position.y > 1); i++) s.Step(In());
            Assert.IsTrue(s.Grounded);
            Assert.AreEqual(5f, s.Position.y + s.Size.y, 1e-4f, "head against the underside");

            float top0 = s.Position.y + s.Size.y, lowest = top0;
            for (int t = 0; t < 60; t++)
            {
                s.Step(In(jumpP: t == 0, jump: true));
                lowest = Mathf.Min(lowest, s.Position.y + s.Size.y);
            }
            Assert.AreEqual(62.5f * Px, top0 - lowest, 1e-4f, "the full jump goes 62.5 px DOWN");
        }

        [Test]
        public void Inverted_KnockbackPushesAgainstGravity()
        {
            var s = Sim(World());
            s.SetDimension(Dim.Inverted);
            s.TryHurt(1, null);
            Assert.AreEqual(-12f, s.Velocity.y, 1e-4f, "down, away from the ceiling");
        }

        // ------------------------------------------------------------ the Anino shadow

        [TestCase(DifficultyId.Story, 300)]
        [TestCase(DifficultyId.Standard, 240)]
        [TestCase(DifficultyId.Nightmare, 180)]
        [TestCase(DifficultyId.Endless, 200)]
        public void ShadowDelay_ComesFromTheDifficultyTable(DifficultyId d, int frames)
        {
            Assert.AreEqual(frames, Sim(World(), d).Shadow.Delay);
        }

        [Test]
        public void ShadowDelay_ShortensPerLevel_WithAFloor()
        {
            var c = ScriptableObject.CreateInstance<DifficultyCurve>();
            Assert.AreEqual(240, c.ChaseFrames(240, 0));
            Assert.AreEqual(Mathf.RoundToInt(240 * (1 - 0.035f * 8)), c.ChaseFrames(240, 8));
            Assert.AreEqual(100, c.ChaseFrames(120, 8), "never below 100 frames");
            for (int l = 1; l < 9; l++) Assert.LessOrEqual(c.ChaseFrames(240, l), c.ChaseFrames(240, l - 1));
            Object.DestroyImmediate(c);
        }

        [TestCase(30)]
        [TestCase(100)]
        public void Shadow_ReplaysThePath_ExactlyNTicksLate(int n)
        {
            var s = At(Sim(World()), 0, 0);
            s.SetDimension(Dim.Spirit);
            s.SetChaseDelay(n);
            s.Invulnerable = 100000; // keep the path clean of knockback
            var path = new List<Vector2>();
            for (int t = 0; t < n + 80; t++)
            {
                // run right, hop now and then, turn around once
                int move = t < n + 40 ? 1 : -1;
                s.Step(In(move: move, jumpP: t % 25 == 0, jump: t % 25 < 10));
                path.Add(s.Position);
                if (t < n) Assert.IsFalse(s.Shadow.Active, $"no shadow before {n} ticks (t={t})");
                else
                {
                    Assert.IsTrue(s.Shadow.Active);
                    Assert.AreEqual(path[t - n], s.Shadow.Current.Position, $"t={t}: shadow is where Tala was {n} ticks ago");
                }
            }
        }

        [Test]
        public void Shadow_HurtsOnContact_ThroughCanBeHurt()
        {
            var s = At(Sim(World()), 2, 0);
            s.SetDimension(Dim.Spirit);
            s.SetChaseDelay(10);
            int hits = 0;
            s.Hurt += _ => hits++;
            Run(s, 11); // standing still: the shadow appears on top of her
            Assert.AreEqual(1, hits);
            Assert.AreEqual(3, s.Vitality);
            Run(s, 60);
            Assert.AreEqual(1, hits, "invulnerability window protects her (CanBeHurt)");

            var dashing = At(Sim(World()), 2, 0);
            dashing.SetDimension(Dim.Spirit);
            dashing.SetChaseDelay(1);
            dashing.Invulnerable = 0;
            dashing.DashTicks = 50; // untouchable while dashing
            int dh = 0; dashing.Hurt += _ => dh++;
            for (int i = 0; i < 5; i++) { dashing.DashTicks = 50; dashing.Step(In()); }
            Assert.AreEqual(0, dh);
        }

        [Test]
        public void Ward_StunsTheShadowFor190Ticks_NoDamageMeanwhile()
        {
            var s = At(Sim(World()), 2, 0);
            s.SetDimension(Dim.Spirit);
            s.SetChaseDelay(10);
            s.Invulnerable = 1000;
            Run(s, 12);
            Assert.IsTrue(s.Shadow.Active);
            int stuns = 0; s.ShadowStunned += () => stuns++;
            s.Step(In(ward: true));
            Assert.AreEqual(1, stuns);
            Assert.AreEqual(190, s.Shadow.StunTicks);
            s.Invulnerable = 0;
            int hits = 0; s.Hurt += _ => hits++;
            for (int t = 1; t < 190; t++) { s.Step(In()); Assert.AreEqual(0, hits, $"stunned at +{t}"); }
            s.Step(In());
            Assert.AreEqual(1, hits, "the shadow bites again when the stun ends");
        }

        [Test]
        public void Ward_OutOfReach_DoesNotStun_BoltHookStuns140()
        {
            var s = At(Sim(World()), 0, 0);
            s.SetDimension(Dim.Spirit);
            s.SetChaseDelay(40);
            s.Invulnerable = 1000;
            Run(s, 60, In(move: 1)); // the shadow trails about 78 px behind
            Assert.Greater(Vector2.Distance(s.Center, s.ShadowRect.center), 50 * Px);
            s.Step(In(move: 1, ward: true));
            Assert.AreEqual(0, s.Shadow.StunTicks);
            Assert.IsTrue(s.StunShadow(PlayerTuning.Ticks(Skp.Skills.SkillTuning.Default.boltShadowStun)));
            Assert.AreEqual(140, s.Shadow.StunTicks);
            Assert.IsFalse(s.StunShadow(140), "no re-stun while stunned");
        }

        [Test]
        public void Shadow_ExistsOnlyInSpirit_AndForgetsItsTrailOnDimensionChange()
        {
            var s = At(Sim(World()), 2, 0);
            s.SetChaseDelay(5);
            Run(s, 20);
            Assert.IsFalse(s.Shadow.Active, "no shadow in the Waking World");
            s.SetDimension(Dim.Spirit);
            Run(s, 6);
            Assert.IsTrue(s.Shadow.Active);
            s.SetDimension(Dim.Ordinary);
            Assert.IsFalse(s.Shadow.Active);
        }

        // ------------------------------------------------------------ lanterns and death

        [Test]
        public void Lantern_ReturnsToOrdinary_ButNotFromInverted()
        {
            var w = World(new Vector2(-80, 0), new Vector2(3, 0));
            var s = At(Sim(w), 0, 0);
            s.SetDimension(Dim.Spirit);
            for (int t = 0; t < 120 && s.Dimension == Dim.Spirit; t++) s.Step(In(move: 1));
            Assert.AreEqual(Dim.Ordinary, s.Dimension);
            Assert.AreEqual(1, s.Checkpoint);

            var inv = Sim(World(new Vector2(0, 0)));
            inv.SetDimension(Dim.Inverted);
            inv.Step(In());
            Assert.AreEqual(Dim.Inverted, inv.Dimension, "lanterns do nothing in Inverted gravity");
        }

        [TestCase(Dim.Spirit)]
        [TestCase(Dim.Inverted)]
        public void NoDimensionStateLeaks_AfterDeathAndRespawn(Dim dim)
        {
            var s = At(Sim(World()), 2, 0);
            s.SetDimension(dim);
            if (dim == Dim.Spirit) { s.SetChaseDelay(5); s.Invulnerable = 1000; Run(s, 8); s.StunShadow(190); }
            var changes = new List<Dim>();
            s.DimensionChanged += d => changes.Add(d);
            s.Die();
            Run(s, 75);
            Assert.AreEqual(PlayerLife.Playing, s.Life);
            Assert.AreEqual(Dim.Ordinary, s.Dimension);
            Assert.AreEqual(1, s.GravitySign);
            Assert.IsFalse(s.Shadow.Active);
            Assert.AreEqual(0, s.Shadow.StunTicks);
            Assert.AreEqual(-1, s.NearDoor);
            CollectionAssert.AreEqual(new[] { Dim.Ordinary }, changes, "the HUD and audio hear about the reset");
        }
    }
}
