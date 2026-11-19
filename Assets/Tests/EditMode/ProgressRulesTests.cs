using NUnit.Framework;
using Skp.Save;

namespace Skp.Tests
{
    public class ProgressRulesTests
    {
        [Test]
        public void Unlock_IgnoresLowerValues_AndClamps()
        {
            var s = new SaveData();
            ProgressRules.Unlock(s, 4);
            ProgressRules.Unlock(s, 2);
            Assert.AreEqual(4, s.unlocked);
            ProgressRules.Unlock(s, 99);
            Assert.AreEqual(ProgressRules.LevelCount, s.unlocked);
        }

        [Test]
        public void Cleared_OnlySetByFinishingTheFinalLevel()
        {
            var s = new SaveData();
            for (int i = 0; i < ProgressRules.FinalLevelIndex; i++)
            {
                ProgressRules.CompleteLevel(s, i);
                Assert.IsFalse(s.cleared, $"level {i + 1} must not set cleared");
                Assert.AreEqual(i + 2, s.unlocked);
            }
            ProgressRules.CompleteLevel(s, -1); // endless loop: no effect
            Assert.IsFalse(s.cleared);
            ProgressRules.CompleteLevel(s, ProgressRules.FinalLevelIndex);
            Assert.IsTrue(s.cleared);
            Assert.AreEqual(ProgressRules.LevelCount, s.unlocked);
        }

        [Test]
        public void AddSkill_NoDuplicates()
        {
            var s = new SaveData();
            Assert.IsTrue(ProgressRules.AddSkill(s, "dj"));
            Assert.IsFalse(ProgressRules.AddSkill(s, "dj"));
            Assert.IsFalse(ProgressRules.AddSkill(s, ""));
            Assert.IsFalse(ProgressRules.AddSkill(s, null));
            Assert.AreEqual(1, s.skills.Length);
            Assert.IsTrue(ProgressRules.HasSkill(s, "dj"));
            Assert.IsFalse(ProgressRules.HasSkill(s, "veil"));
        }

        // Spec: node i unlocked iff i < unlocked; cleared iff i < unlocked-1 or (i == 8 and cleared).
        [Test]
        public void NodeStates_MatchSpec_ForEveryUnlockCount()
        {
            for (int unlocked = 1; unlocked <= ProgressRules.LevelCount; unlocked++)
            foreach (bool cleared in new[] { false, true })
            {
                var s = new SaveData { unlocked = unlocked, cleared = cleared };
                for (int i = 0; i < ProgressRules.LevelCount; i++)
                {
                    Assert.AreEqual(i < unlocked, ProgressRules.IsNodeUnlocked(s, i), $"unlock u={unlocked} i={i}");
                    Assert.AreEqual(i < unlocked - 1 || (i == 8 && cleared), ProgressRules.IsNodeCleared(s, i), $"clear u={unlocked} c={cleared} i={i}");
                }
            }
        }

        [Test]
        public void Sanitize_FixesNullsAndBadValues()
        {
            var s = new SaveData { unlocked = 0, @char = "dragon", diff = null, cont = null, skills = null, best = -5 };
            ProgressRules.Sanitize(s);
            Assert.AreEqual(1, s.unlocked);
            Assert.AreEqual("girl", s.@char);
            Assert.AreEqual("standard", s.diff);
            Assert.IsNotNull(s.cont);
            Assert.IsNotNull(s.skills);
            Assert.AreEqual(0, s.best);
        }
    }
}
