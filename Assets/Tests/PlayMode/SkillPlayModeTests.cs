using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Skp.Core;
using Skp.Dimensions;
using Skp.Levels;
using Skp.Player;
using Skp.Save;
using Skp.Skills;
using UnityEngine;

namespace Skp.Tests
{
    /// <summary>
    /// One test (or more) per skill, in play mode, stepping the real 60 Hz simulation with scripted input and the
    /// project's SkillTuning asset: Double Jump, Lambong Veil and Greater Veil, Liwanag Bolt, Triple Bolt, Swift
    /// Dash, Heart Charm (through a LevelSession and a save reload) and Pakpak Glide, each also under Inverted
    /// gravity where it applies.
    /// </summary>
    public class SkillPlayModeTests
    {
        const float Px = 1f / 16f;
        PlayerTuning _t;
        DifficultyTable _d;
        SkillTuning St => SkillLibrary.Tuning;

        [SetUp]
        public void SetUp()
        {
            _t = ScriptableObject.CreateInstance<PlayerTuning>();
            _d = ScriptableObject.CreateInstance<DifficultyTable>();
            Assert.IsNotNull(SkillLibrary.Load(), "the SkillLibrary asset is in Resources");
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_t);
            Object.Destroy(_d);
        }

        /// <summary>A long floor (top at y = 0) with the spawn lantern at x = 0; a ceiling ledge (underside at y = 6) far right for Inverted.</summary>
        PlayerSim NewSim(params string[] skills)
        {
            var w = new PlayerWorld(new Rect(-100, -50, 400, 200));
            w.Ledges.Add(new Rect(-100, -1, 400, 1));
            w.Ledges.Add(new Ledge(new Rect(60, 6, 40, 0.5f), Dim.Inverted, LedgeKind.Inverted));
            w.Lanterns.Add(new Vector2(0, 0));
            var s = new PlayerSim(_t, _d.Get(DifficultyId.Standard), w) { Skills = new SkillSet(skills), SkillTuning = St };
            s.Invulnerable = 0;
            return s;
        }

        /// <summary>Puts Tala on the ceiling ledge in the Inverted dimension (gravity up), away from the lantern.</summary>
        static void Invert(PlayerSim s)
        {
            s.SetDimension(Dim.Inverted);
            s.Position = new Vector2(70, 6 - s.Size.y);
            s.Velocity = Vector2.zero;
            s.Step(default);
            Assert.IsTrue(s.Grounded, "standing on the ceiling");
            Assert.AreEqual(-1, s.GravitySign);
        }

        static PlayerInputFrame Hold(bool jumpPressed = false, bool jumpHeld = true, int move = 0) =>
            new PlayerInputFrame { JumpPressed = jumpPressed, JumpHeld = jumpHeld || jumpPressed, MoveX = move };

        // ------------------------------------------------------------ Double Jump

        [Test]
        public void DoubleJump_OncePerAirtime_NotOnTheGroundJumpFrame_ResetsOnLanding([Values(1, -1)] int gravity)
        {
            var s = NewSim(SkillIds.DoubleJump);
            if (gravity < 0) Invert(s); else s.Step(default);
            int gs = s.GravitySign;
            int air = 0, ground = 0;
            s.AirJumped += () => air++;
            s.Jumped += () => ground++;

            s.Step(Hold(jumpPressed: true));
            Assert.AreEqual(1, ground, "ground jump");
            Assert.AreEqual(0, air, "no air jump on the ground jump frame");
            Assert.AreEqual(gs * _t.jumpSpeed, s.Velocity.y, 1e-3f);
            Assert.AreEqual(0, s.AirJumps);

            for (int i = 0; i < 9; i++) s.Step(Hold());
            s.Step(Hold(jumpPressed: true));
            Assert.AreEqual(1, air, "second jump in mid-air");
            Assert.AreEqual(gs * St.doubleJumpSpeed, s.Velocity.y, 1e-3f, "about 92 percent of a ground jump, mirrored");
            Assert.AreEqual(1, s.AirJumps);

            for (int i = 0; i < 5; i++) s.Step(Hold());
            float vy = s.Velocity.y;
            s.Step(Hold(jumpPressed: true));
            Assert.AreEqual(1, air, "no third jump");
            Assert.Less(s.Velocity.y * gs, vy * gs + 1e-4f, "the press changed nothing");

            for (int i = 0; i < 200 && !s.Grounded; i++) s.Step(default);
            Assert.IsTrue(s.Grounded);
            Assert.AreEqual(0, s.AirJumps, "landing resets it");
        }

        [Test]
        public void DoubleJump_NeedsTheSkill_AndNeverFiresOnACoyoteJump()
        {
            var s = NewSim();
            s.Step(default);
            s.Step(Hold(jumpPressed: true));
            for (int i = 0; i < 8; i++) s.Step(Hold());
            float vy = s.Velocity.y;
            s.Step(Hold(jumpPressed: true));
            Assert.Less(s.Velocity.y, vy, "without Double Jump a mid-air press does nothing");

            // walk off a ledge and press inside the coyote window: that is a coyote (ground) jump, not an air jump
            var w = NewSim(SkillIds.DoubleJump);
            w.World.Ledges.Clear();
            w.World.Ledges.Add(new Rect(-100, -1, 101, 1)); // floor ends at x = 1
            w.Position = new Vector2(0.4f, 0);
            w.Step(default);
            int air = 0, ground = 0;
            w.AirJumped += () => air++;
            w.Jumped += () => ground++;
            for (int i = 0; i < 60 && w.Grounded; i++) w.Step(new PlayerInputFrame { MoveX = 1 });
            Assert.IsFalse(w.Grounded);
            w.Step(Hold(jumpPressed: true));
            Assert.AreEqual(1, ground, "coyote jump");
            Assert.AreEqual(0, air);
        }

        // ------------------------------------------------------------ Lambong Veil

        [Test]
        public void Veil_BlocksSpikesForExactlyItsDuration_ThenCooldownFromActivation([Values(false, true)] bool greater)
        {
            var s = greater ? NewSim(SkillIds.Veil, SkillIds.GreaterVeil) : NewSim(SkillIds.Veil);
            s.World.Hazards.Add(new Hazard(new Rect(-2, 0, 4, 7 * Px), Dim.All)); // spikes under Tala
            int duration = PlayerTuning.Ticks(greater ? St.greaterVeilDuration : St.veilDuration);
            int cooldown = PlayerTuning.Ticks(greater ? St.greaterVeilCooldown : St.veilCooldown);
            Assert.AreEqual(greater ? 300 : 180, duration);
            int started = 0;
            s.VeilStarted += () => started++;

            int vit = s.Vitality;
            s.Step(new PlayerInputFrame { VeilPressed = true });
            Assert.AreEqual(1, started);
            Assert.IsTrue(s.Veiled);
            int hurtAt = -1;
            for (int i = 1; i < duration + 5; i++)
            {
                s.Step(default);
                if (s.Vitality < vit) { hurtAt = i; break; }
            }
            Assert.AreEqual(duration, hurtAt, "immune for exactly the veil's duration, then the spikes bite");

            // cooldown counted from activation; presses before then do nothing
            var c = greater ? NewSim(SkillIds.Veil, SkillIds.GreaterVeil) : NewSim(SkillIds.Veil);
            int n = 0;
            c.VeilStarted += () => n++;
            c.Step(new PlayerInputFrame { VeilPressed = true });
            int again = -1;
            for (int i = 1; i < cooldown + 10; i++)
            {
                c.Step(new PlayerInputFrame { VeilPressed = true });
                if (n == 2) { again = i; break; }
            }
            Assert.AreEqual(cooldown, again, "the veil returns exactly one cooldown after it was raised");
        }

        [Test]
        public void Veil_BlocksEveryDamageSource_WhileUp()
        {
            var s = NewSim(SkillIds.Veil);
            s.Position = new Vector2(20, 0);
            var blocked = new HashSet<DamageSource>();
            s.VeilBlocked += src => blocked.Add(src);
            s.World.Hazards.Add(new Hazard(new Rect(19, 0, 3, 7 * Px), Dim.All));
            s.World.Hazards.Add(new Hazard(new Rect(19.5f, 0, 1, 1), Dim.All, HazardKind.Root) { Phase = 110 }); // striking from tick 0 to 40
            s.World.Enemies.Add(new Enemy { Kind = EnemyKind.Anino, Position = new Vector2(19.8f, 0), Size = new Vector2(1, 1), Mask = Dim.All, Hp = 1, MinX = 15, MaxX = 25 });
            s.SetDimension(Dim.Spirit);
            s.SetChaseDelay(3);
            int vit = s.Vitality;
            s.Step(new PlayerInputFrame { VeilPressed = true });
            for (int i = 0; i < 30; i++)
            {
                s.Step(default);
                // water ticks and the Bellkeeper (M7) call the same gate
                s.TryHurt(1, null, DamageSource.Water);
                s.TryHurt(1, null, DamageSource.Boss);
            }
            Assert.AreEqual(vit, s.Vitality, "nothing gets through the veil");
            CollectionAssert.IsSupersetOf(blocked, new[] { DamageSource.Spike, DamageSource.Root, DamageSource.Enemy, DamageSource.Shadow, DamageSource.Water, DamageSource.Boss });
        }

        [Test]
        public void Veil_NeedsTheSkill()
        {
            var s = NewSim();
            s.Step(new PlayerInputFrame { VeilPressed = true });
            Assert.IsFalse(s.Veiled);
        }

        // ------------------------------------------------------------ Liwanag Bolt

        PlayerSim BoltSim(params string[] extra)
        {
            var s = NewSim(new[] { SkillIds.Bolt }.Concat(extra).ToArray());
            s.Position = new Vector2(20, 0);
            s.Facing = 1;
            s.Step(default);
            return s;
        }

        static Enemy Creature(EnemyKind k, float x, int hp = 1, Dim mask = Dim.Ordinary) =>
            new Enemy { Kind = k, Position = new Vector2(x, 0), Size = new Vector2(12 * Px, 14 * Px), Mask = mask, Hp = hp, MinX = x, MaxX = x + 12 * Px };

        static List<BoltTarget> Fire(PlayerSim s, int ticks = 80)
        {
            var hits = new List<BoltTarget>();
            s.BoltHit += (_, t) => hits.Add(t);
            s.Step(new PlayerInputFrame { BoltPressed = true });
            for (int i = 0; i < ticks; i++) s.Step(default);
            return hits;
        }

        [Test]
        public void Bolt_KillsAnAnino_CostsFlame_AndHasItsCooldown()
        {
            var s = BoltSim();
            var e = Creature(EnemyKind.Anino, 24);
            s.World.Enemies.Add(e);
            int flame = s.Flame;
            int fired = 0;
            s.BoltFired += n => fired += n;
            var hits = new List<BoltTarget>();
            s.BoltHit += (_, t) => hits.Add(t);
            s.Step(new PlayerInputFrame { BoltPressed = true });
            Assert.AreEqual(1, fired);
            Assert.AreEqual(flame - St.boltFlameCost, s.Flame, "8 flame");
            Assert.AreEqual(PlayerTuning.Ticks(St.boltCooldown), s.BoltCooldown);
            s.Step(new PlayerInputFrame { BoltPressed = true });
            Assert.AreEqual(1, fired, "still cooling down");
            for (int i = 0; i < 60; i++) s.Step(default);
            Assert.IsTrue(e.Dead, "an Anino dies to one bolt");
            CollectionAssert.Contains(hits, BoltTarget.Anino);
            Assert.AreEqual(1, s.Kills);
        }

        [Test]
        public void Bolt_StunsTikbalangAndManananggal_ForItsTime([Values(EnemyKind.Tikbalang, EnemyKind.Manananggal)] EnemyKind kind)
        {
            var s = BoltSim();
            var e = Creature(kind, 24, hp: 3);
            e.Home = new Vector2(24, e.Size.y); // a manananggal hovers about its home (top-left): on Tala's line
            s.World.Enemies.Add(e);
            var hits = Fire(s, 30);
            CollectionAssert.Contains(hits, BoltTarget.Creature);
            Assert.IsFalse(e.Dead);
            Assert.AreEqual(2, e.Hp, "1 hp per bolt");
            int stun = PlayerTuning.Ticks(St.boltCreatureStun);
            Assert.Greater(e.Stun, stun - 31);
            Assert.LessOrEqual(e.Stun, stun);
        }

        [Test]
        public void Bolt_BurnsRootsAndThornWalls_ForTheSetTime([Values(HazardKind.Root, HazardKind.Thorns)] HazardKind kind)
        {
            var s = BoltSim();
            var h = new Hazard(kind == HazardKind.Root ? new Rect(23, 0, 12 * Px, 1) : new Rect(23, 0, 1, 130 * Px), Dim.Ordinary, kind);
            s.World.Hazards.Add(h);
            long hitTick = -1;
            s.BoltHit += (_, t) => { if (t == BoltTarget.Root || t == BoltTarget.Thorns) hitTick = s.Tick; };
            int score = s.Score;
            s.Step(new PlayerInputFrame { BoltPressed = true });
            for (int i = 0; i < 40 && hitTick < 0; i++) s.Step(default);
            Assert.Greater(hitTick, 0, "the bolt reached the " + kind);
            Assert.AreEqual(score + St.burnScore, s.Score);
            int expect = PlayerTuning.Ticks(kind == HazardKind.Root ? St.rootBurnTime : St.thornBurnTime);
            Assert.AreEqual(kind == HazardKind.Root ? 420 : 600, expect);
            int burnt = 1; // the hit tick already counted one
            while (h.BurnTicks > 0 && burnt < 2000) { s.Step(default); burnt++; }
            Assert.AreEqual(expect, burnt, "burnt for exactly the set time");
        }

        [Test]
        public void Bolt_IsStoppedByALedgeOfThisDimension_NotByOneOfAnother()
        {
            var s = BoltSim();
            var wall = new Ledge(new Rect(22, 0, 1, 2), Dim.Ordinary);
            s.World.Ledges.Add(wall);
            var e = Creature(EnemyKind.Anino, 25);
            s.World.Enemies.Add(e);
            var hits = Fire(s);
            Assert.AreEqual(BoltTarget.Ledge, hits.Single());
            Assert.IsFalse(e.Dead, "the creature behind the ledge is safe");

            var t = BoltSim();
            t.World.Ledges.Add(new Ledge(new Rect(22, 0, 1, 2), Dim.Spirit));
            var e2 = Creature(EnemyKind.Anino, 25);
            t.World.Enemies.Add(e2);
            Fire(t);
            Assert.IsTrue(e2.Dead, "a Spirit ledge does not stop a bolt in the waking world");
        }

        [Test]
        public void Bolt_RefusesToFireWithTooLittleFlame()
        {
            var s = BoltSim();
            s.Flame = St.boltFlameCost - 1;
            int warned = 0;
            s.NotEnoughFlame += () => warned++;
            s.Step(new PlayerInputFrame { BoltPressed = true });
            Assert.AreEqual(0, s.Bolts.Count);
            Assert.AreEqual(St.boltFlameCost - 1, s.Flame);
            Assert.AreEqual(1, warned);
            Assert.AreEqual(0, s.BoltCooldown);
        }

        [Test]
        public void Bolt_ExpiresAfterItsLifetime_AndNeedsTheSkill()
        {
            var s = BoltSim();
            int at = -1;
            s.BoltHit += (_, t) => { if (t == BoltTarget.Expired) at = (int)s.Tick; };
            s.Step(new PlayerInputFrame { BoltPressed = true });
            int fired = (int)s.Tick;
            float x0 = s.Bolts[0].Position.x;
            s.Step(default);
            Assert.AreEqual(4.4f * Px, s.Bolts[0].Position.x - x0, 1e-4f, "4.4 px per frame");
            for (int i = 0; i < 80; i++) s.Step(default);
            Assert.AreEqual(PlayerTuning.Ticks(St.boltLifetime), at - fired + 1, "56 ticks of flight");

            var n = NewSim();
            n.Step(new PlayerInputFrame { BoltPressed = true });
            Assert.AreEqual(0, n.Bolts.Count);
        }

        [Test]
        public void Bolt_StunsTheSpiritShadow()
        {
            var s = BoltSim();
            s.SetDimension(Dim.Spirit);
            s.SetChaseDelay(40);
            for (int i = 0; i < 70; i++) s.Step(new PlayerInputFrame { MoveX = 1 });
            for (int i = 0; i < 10; i++) s.Step(default);
            Assert.IsTrue(s.Shadow.Active, "the shadow follows");
            s.Facing = -1;
            var hits = Fire(s, 40);
            CollectionAssert.Contains(hits, BoltTarget.Shadow);
            Assert.Greater(s.Shadow.StunTicks, 0);
            Assert.LessOrEqual(s.Shadow.StunTicks, PlayerTuning.Ticks(St.boltShadowStun));
        }

        [Test]
        public void Bolt_WorksUnderInvertedGravity()
        {
            var s = NewSim(SkillIds.Bolt);
            Invert(s);
            s.Facing = 1;
            var e = new Enemy { Kind = EnemyKind.Anino, Position = new Vector2(74, 6 - 14 * Px), Size = new Vector2(12 * Px, 14 * Px), Mask = Dim.Inverted, Hp = 1, MinX = 74, MaxX = 75 };
            s.World.Enemies.Add(e);
            Fire(s, 40);
            Assert.IsTrue(e.Dead, "the bolt flies at Tala's center while she hangs from the ceiling");
        }

        [Test]
        public void TripleBolt_FiresThreeRaysInAFan_AtItsCostAndCooldown()
        {
            var s = BoltSim(SkillIds.TripleBolt);
            int flame = s.Flame;
            s.Step(new PlayerInputFrame { BoltPressed = true });
            Assert.AreEqual(3, s.Bolts.Count);
            Assert.AreEqual(flame - St.tripleBoltFlameCost, s.Flame, "12 flame");
            Assert.AreEqual(PlayerTuning.Ticks(St.tripleBoltCooldown), s.BoltCooldown);
            var angles = s.Bolts.Select(b => Mathf.Atan2(b.Velocity.y, b.Velocity.x) * Mathf.Rad2Deg).OrderBy(a => a).ToArray();
            Assert.AreEqual(-14f, angles[0], 0.5f);
            Assert.AreEqual(0f, angles[1], 1e-3f);
            Assert.AreEqual(14f, angles[2], 0.5f);
            foreach (var b in s.Bolts) Assert.AreEqual(St.boltSpeed, b.Velocity.magnitude, 1e-3f);

            // an Anino hovering above the line of a single bolt: only the fan's upper ray reaches it
            Enemy Above() { var e = Creature(EnemyKind.Anino, 26); e.Position.y = 1.5f; return e; }
            var single = BoltSim();
            var a = Above();
            single.World.Enemies.Add(a);
            Fire(single);
            Assert.IsFalse(a.Dead, "a single bolt flies under it");
            var t = BoltSim(SkillIds.TripleBolt);
            var b2 = Above();
            var low = Creature(EnemyKind.Anino, 22);
            t.World.Enemies.Add(b2);
            t.World.Enemies.Add(low);
            Fire(t);
            Assert.IsTrue(b2.Dead && low.Dead, "one fan takes both");
            Assert.AreEqual(2, t.Kills);
        }

        // ------------------------------------------------------------ Swift Dash

        [Test]
        public void SwiftDash_CostsLessAndRechargesFaster([Values(false, true)] bool swift)
        {
            var s = swift ? NewSim(SkillIds.Swift) : NewSim();
            s.Step(default);
            int flame = s.Flame;
            s.Step(new PlayerInputFrame { DashPressed = true });
            Assert.AreEqual(flame - (swift ? 8 : 20), s.Flame);
            Assert.AreEqual(swift ? 20 : 34, s.DashCooldown);
            int again = -1;
            for (int i = 1; i < 60; i++)
            {
                int f = s.Flame;
                s.Step(new PlayerInputFrame { DashPressed = true });
                if (s.Flame < f) { again = i; break; }
            }
            Assert.AreEqual(swift ? 20 : 34, again, "next dash after the cooldown");
        }

        // ------------------------------------------------------------ Heart Charm

        [Test]
        public void HeartCharm_AddsExactlyOneHeart_OnceAndAgainAfterReload()
        {
            var dir = Path.Combine(Path.GetTempPath(), "skp_heart_" + System.Guid.NewGuid().ToString("N"));
            try
            {
                var save = new SaveService(dir);
                save.Load();
                var spec = LevelCatalog.Defaults()[4];
                Assert.AreEqual(SkillIds.Heart, spec.skill);
                var req = new LevelRequest { Level = 4, Difficulty = "standard" };

                var a = new LevelSession(spec, req, _t, _d, null, save, St);
                int baseHearts = _d.Get(DifficultyId.Standard).vitality;
                Assert.AreEqual(baseHearts, a.Sim.MaxVitality);
                a.Sim.Vitality = baseHearts - 1;
                TakeLetter(a);
                Assert.AreEqual(baseHearts + 1, a.Sim.MaxVitality, "one more heart at once");
                Assert.AreEqual(baseHearts, a.Sim.Vitality, "and it is filled");
                Assert.AreEqual(SkillIds.Heart, a.LearnedThisRun);

                var reloaded = new SaveService(dir);
                reloaded.Load();
                Assert.IsTrue(ProgressRules.HasSkill(reloaded.Data, SkillIds.Heart), "saved immediately");
                var b = new LevelSession(spec, req, _t, _d, null, reloaded, St);
                Assert.AreEqual(baseHearts + 1, b.Sim.MaxVitality, "every later level start");
                Assert.AreEqual(baseHearts + 1, b.Sim.Vitality);
                TakeLetter(b);
                Assert.AreEqual(baseHearts + 1, b.Sim.MaxVitality, "a replay does not add it twice");
                Assert.IsFalse(b.LetterSkillWasNew);
                Assert.IsNull(b.LearnedThisRun);
                Assert.AreEqual(1, reloaded.Data.skills.Count(x => x == SkillIds.Heart));
            }
            finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
        }

        static void TakeLetter(LevelSession s)
        {
            var clue = s.World.Items.First(i => i.Kind == ItemKind.Clue && i.Mask == Dim.Ordinary);
            s.Sim.Invulnerable = 100000;
            s.Sim.Position = new Vector2(clue.Rect.center.x - s.Sim.Size.x / 2, clue.Rect.y);
            for (int i = 0; i < 5 && !s.Sim.HasClue; i++) s.Sim.Step(default);
            Assert.IsTrue(s.Sim.HasClue, "letter taken");
        }

        // ------------------------------------------------------------ Pakpak Glide

        [Test]
        public void Glide_CapsTheFall_WhileHeld_EndsOnReleaseAndOnLanding([Values(1, -1)] int gravity)
        {
            var s = NewSim(SkillIds.Glide);
            if (gravity < 0) Invert(s); else s.Step(default);
            int gs = s.GravitySign;
            float cap = St.glideFallSpeed;
            s.Step(Hold(jumpPressed: true));
            bool glided = false;
            for (int i = 0; i < 60; i++)
            {
                s.Step(Hold());
                if (s.Gliding)
                {
                    glided = true;
                    Assert.AreEqual(-gs * cap, s.Velocity.y, 1e-4f, "falls no faster than 1.1 px per frame (mirrored)");
                }
                Assert.LessOrEqual(-s.Velocity.y * gs, cap + 1e-4f);
            }
            Assert.IsTrue(glided, "the glide engaged while falling");
            s.Step(default);
            Assert.IsFalse(s.Gliding, "released");
            for (int i = 0; i < 4; i++) s.Step(default);
            Assert.Greater(-s.Velocity.y * gs, cap, "falls fast again");

            var g = NewSim(SkillIds.Glide);
            if (gravity < 0) Invert(g); else g.Step(default);
            g.Step(Hold(jumpPressed: true));
            for (int i = 0; i < 600 && !g.Grounded; i++) g.Step(Hold());
            Assert.IsTrue(g.Grounded, "landed while still holding");
            Assert.IsFalse(g.Gliding, "the glide ends on the ground");

            var n = NewSim();
            if (gravity < 0) Invert(n); else n.Step(default);
            n.Step(Hold(jumpPressed: true));
            float fastest = 0;
            for (int i = 0; i < 60; i++) { n.Step(Hold()); fastest = Mathf.Max(fastest, -n.Velocity.y * gs); }
            Assert.Greater(fastest, cap, "without the skill holding jump does not glide");
        }
    }
}
