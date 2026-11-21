using System.IO;
using System.Linq;
using NUnit.Framework;
using Skp.Core;
using Skp.Dimensions;
using Skp.Levels;
using Skp.Player;
using Skp.Save;
using Skp.Skills;
using UnityEditor;
using UnityEngine;

namespace Skp.Tests
{
    /// <summary>
    /// M6 skill data and rules without a scene: the SkillTuning asset matches the spec table, the eight
    /// SkillDefinitions are complete and wired to their levels, SkillService persists without duplicates or
    /// removals, and CanBeHurt() is the one gate for every damage source (truth table).
    /// </summary>
    public class SkillTests
    {
        const float Px = 1f / 16f;

        static SkillTuning Asset => AssetDatabase.LoadAssetAtPath<SkillTuning>("Assets/Data/SkillTuning.asset");

        static float PxPerFrame(float unitsPerSecond) => unitsPerSecond * 16f / 60f;

        [Test]
        public void TuningAsset_LoadsAndMatchesTheSpecTable()
        {
            var t = Asset;
            Assert.IsNotNull(t, "Assets/Data/SkillTuning.asset");
            Assert.AreSame(t, SkillLibrary.Load().tuning, "the library points at the asset");
            var p = AssetDatabase.LoadAssetAtPath<PlayerTuning>("Assets/Data/PlayerTuning.asset");

            // Double Jump: about 92 percent of a ground jump, one per airtime
            Assert.AreEqual(6.7f, PxPerFrame(t.doubleJumpSpeed), 1e-4f);
            Assert.AreEqual(0.92f, t.doubleJumpSpeed / p.jumpSpeed, 0.01f);
            Assert.AreEqual(1, t.airJumps);
            // Lambong Veil 3 s / 15 s; Greater Veil 5 s / 10 s; flicker in the last second
            Assert.AreEqual(180, PlayerTuning.Ticks(t.veilDuration));
            Assert.AreEqual(900, PlayerTuning.Ticks(t.veilCooldown));
            Assert.AreEqual(300, PlayerTuning.Ticks(t.greaterVeilDuration));
            Assert.AreEqual(600, PlayerTuning.Ticks(t.greaterVeilCooldown));
            Assert.AreEqual(60, PlayerTuning.Ticks(t.veilFlickerTime));
            // Liwanag Bolt: 4.4 px/frame, about 0.9 s, 8 flame, about 0.27 s
            Assert.AreEqual(4.4f, PxPerFrame(t.boltSpeed), 1e-4f);
            Assert.AreEqual(56, PlayerTuning.Ticks(t.boltLifetime));
            Assert.AreEqual(0.9f, t.boltLifetime, 0.05f);
            Assert.AreEqual(8, t.boltFlameCost);
            Assert.AreEqual(16, PlayerTuning.Ticks(t.boltCooldown));
            Assert.AreEqual(0.27f, t.boltCooldown, 0.01f);
            Assert.AreEqual(2.8f, t.boltCreatureStun, 0.05f);
            Assert.AreEqual(2.3f, t.boltShadowStun, 0.05f);
            Assert.AreEqual(7f, t.rootBurnTime, 1e-4f);
            Assert.AreEqual(10f, t.thornBurnTime, 1e-4f);
            Assert.AreEqual(1, t.bossBoltDamage);
            // Triple Bolt: -14, 0, +14 degrees, 12 flame, about 0.37 s
            Assert.AreEqual(14f, t.tripleBoltAngle, 0.3f);
            Assert.AreEqual(12, t.tripleBoltFlameCost);
            Assert.AreEqual(0.37f, t.tripleBoltCooldown, 0.01f);
            // Swift Dash: 20 -> 8 flame, 34 -> 20 frames
            Assert.AreEqual(20, p.dashFlameCost);
            Assert.AreEqual(34, PlayerTuning.Ticks(p.dashCooldown));
            Assert.AreEqual(8, t.swiftDashFlameCost);
            Assert.AreEqual(20, PlayerTuning.Ticks(t.swiftDashCooldown));
            // Heart Charm +1, Pakpak Glide 1.1 px/frame
            Assert.AreEqual(1, t.heartBonus);
            Assert.AreEqual(1.1f, PxPerFrame(t.glideFallSpeed), 1e-4f);
        }

        [Test]
        public void Definitions_AllEight_InTeachingOrder_WiredToTheirLevels()
        {
            var lib = SkillLibrary.Load();
            Assert.IsNotNull(lib, "Resources/Skills/SkillLibrary");
            CollectionAssert.AreEqual(SkillIds.All, lib.skills.Select(s => s.id).ToArray());
            for (int i = 0; i < 8; i++)
            {
                var d = lib.skills[i];
                Assert.AreEqual(i, d.level, d.id);
                Assert.IsNotEmpty(d.displayName, d.id);
                Assert.IsNotEmpty(d.filipinoName, d.id);
                Assert.IsNotEmpty(d.description, d.id);
                Assert.IsNotEmpty(d.tag, d.id);
                Assert.IsNotEmpty(d.shortLabel, d.id);
                Assert.IsNotNull(d.icon, d.id + " icon");
                Assert.AreEqual(SkillIds.IsActive(d.id), d.type == SkillType.Active, d.id + " type");
                if (d.IsActive) Assert.IsNotEmpty(d.keyHint, d.id + " key");
                var cfg = AssetDatabase.LoadAssetAtPath<LevelConfig>($"Assets/Data/Levels/LevelConfig_{i + 1:00}.asset");
                Assert.AreSame(d, cfg.skill, $"level {i + 1} teaches {d.id}");
                Assert.AreEqual(d.id, cfg.spec.skill);
            }
            var boss = AssetDatabase.LoadAssetAtPath<LevelConfig>("Assets/Data/Levels/LevelConfig_09.asset");
            Assert.IsNull(boss.skill, "the Bellkeeper level has no letter");
            Assert.AreEqual("LAMBONG VEIL (Lambong)", lib.Get("veil").FullName);
        }

        [Test]
        public void SfxBank_HasADistinctCueForEverySkill_AndTheFanfare()
        {
            var bank = Resources.Load<Skp.Audio.SfxBank>(Skp.Audio.SfxBank.ResourcePath);
            Assert.IsNotNull(bank);
            var cues = new[] { "skill_dj", "skill_veil", "skill_veil_end", "skill_bolt", "skill_bolt3", "bolt_hit", "burn", "skill_glide", "skill_learned", "skill_known" };
            foreach (var c in cues) Assert.IsTrue(bank.TryGet(c, out _), "cue " + c);
            var clips = cues.Select(c => { bank.TryGet(c, out var e); return e.clip; }).ToArray();
            Assert.AreEqual(clips.Length, clips.Distinct().Count(), "every cue has its own sound");
            foreach (var d in SkillLibrary.Load().skills)
                if (!string.IsNullOrEmpty(d.cue)) Assert.IsTrue(bank.TryGet(d.cue, out _), d.id + " cue " + d.cue);
        }

        // ------------------------------------------------------------ SkillService

        static string TempDir() => Path.Combine(Path.GetTempPath(), "skp_skill_" + System.Guid.NewGuid().ToString("N"));

        [Test]
        public void SkillService_Persists_Reloads_NeverDuplicates_NeverRemoves()
        {
            var dir = TempDir();
            try
            {
                var save = new SaveService(dir);
                save.Load();
                var skills = new SkillService(save);
                string learned = null;
                skills.Learned += id => learned = id;
                Assert.IsFalse(skills.Has("dj"));
                Assert.IsTrue(skills.Learn("dj"));
                Assert.AreEqual("dj", learned);
                Assert.IsFalse(skills.Learn("dj"), "second time: already known");
                Assert.IsTrue(skills.Learn("veil"));
                CollectionAssert.AreEqual(new[] { "dj", "veil" }, skills.Owned);

                // saved immediately: a fresh service on the same folder sees both
                var again = new SaveService(dir);
                again.Load();
                CollectionAssert.AreEqual(new[] { "dj", "veil" }, again.Data.skills);
                Assert.IsTrue(new SkillService(again).Has("veil"));

                // a stale copy without skills can never remove them
                again.Data.skills = new string[0];
                again.Save();
                var third = new SaveService(dir);
                third.Load();
                CollectionAssert.IsSupersetOf(third.Data.skills, new[] { "dj", "veil" });
                Assert.AreEqual(2, third.Data.skills.Length, "no duplicates");

                // only deleting the save forgets them
                third.Delete();
                var fresh = new SaveService(dir);
                fresh.Load();
                Assert.AreEqual(0, new SkillService(fresh).Count);
            }
            finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
        }

        // ------------------------------------------------------------ CanBeHurt truth table

        static PlayerSim Sim()
        {
            var w = new PlayerWorld(new Rect(-50, -50, 200, 200));
            w.Ledges.Add(new Rect(-50, -1, 200, 1));
            w.Lanterns.Add(new Vector2(0, 0));
            var s = new PlayerSim(ScriptableObject.CreateInstance<PlayerTuning>(), ScriptableObject.CreateInstance<DifficultyTable>().Get(DifficultyId.Standard), w)
            {
                Skills = SkillSet.AllSkills(),
            };
            s.Invulnerable = 0;
            return s;
        }

        static readonly DamageSource[] Sources =
            { DamageSource.Spike, DamageSource.Root, DamageSource.Thorns, DamageSource.Enemy, DamageSource.Shadow, DamageSource.Water, DamageSource.Boss };

        [Test]
        public void CanBeHurt_TruthTable_ForEveryDamageSource([Values("veil", "dash", "invulnerable", "none")] string state)
        {
            foreach (var src in Sources)
            {
                var s = Sim();
                if (state == "veil") s.VeilTicks = 10;
                if (state == "dash") s.DashTicks = 5;
                if (state == "invulnerable") s.Invulnerable = 30;
                bool expect = state == "none";
                Assert.AreEqual(expect, s.CanBeHurt(), $"{state}: CanBeHurt");
                int before = s.Vitality;
                bool blocked = false;
                s.VeilBlocked += _ => blocked = true;
                Assert.AreEqual(expect, s.TryHurt(1, null, src), $"{state} vs {src}");
                Assert.AreEqual(expect ? before - 1 : before, s.Vitality, $"{state} vs {src}: vitality");
                Assert.AreEqual(src, s.LastDamageSource);
                Assert.AreEqual(state == "veil", blocked, $"{state} vs {src}: veil block reported");
            }
        }

        /// <summary>The real hazard, creature and shadow code paths all report their source, so none bypasses TryHurt.</summary>
        [Test]
        public void EveryWorldDamagePath_GoesThroughTryHurt()
        {
            void Expect(DamageSource src, System.Action<PlayerSim> build)
            {
                var s = Sim();
                build(s);
                for (int i = 0; i < 400 && s.Vitality == s.MaxVitality; i++) s.Step(default);
                Assert.Less(s.Vitality, s.MaxVitality, src + " hurts");
                Assert.AreEqual(src, s.LastDamageSource, src + " went through TryHurt");

                var v = Sim();
                build(v);
                v.VeilTicks = 100000;
                for (int i = 0; i < 400; i++) v.Step(default);
                Assert.AreEqual(v.MaxVitality, v.Vitality, src + " is blocked by the veil");
                Assert.AreEqual(src, v.LastDamageSource);
            }
            Expect(DamageSource.Spike, s => s.World.Hazards.Add(new Hazard(new Rect(-1, 0, 2, 7 * Px), Dim.All)));
            Expect(DamageSource.Root, s => s.World.Hazards.Add(new Hazard(new Rect(-1, 0, 1, 1), Dim.All, HazardKind.Root)));
            Expect(DamageSource.Thorns, s => s.World.Hazards.Add(new Hazard(new Rect(-0.5f, 0, 1, 8), Dim.All, HazardKind.Thorns)));
            Expect(DamageSource.Enemy, s => s.World.Enemies.Add(new Enemy { Kind = EnemyKind.Anino, Position = new Vector2(-0.2f, 0), Size = new Vector2(1, 1), Mask = Dim.All, Hp = 1, MinX = -5, MaxX = 5 }));
            Expect(DamageSource.Shadow, s =>
            {
                s.Position = new Vector2(20, 0); // away from the lantern (a lantern returns Tala to Ordinary)
                s.SetDimension(Dim.Spirit);
                s.SetChaseDelay(5);
                s.Invulnerable = 0;
            });
        }
    }
}
