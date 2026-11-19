using System;
using System.Collections.Generic;

namespace Skp.Save
{
    /// <summary>
    /// Pure progression rules on <see cref="SaveData"/>. Every change to unlocks, clears and
    /// skills goes through here so the invariants hold everywhere:
    /// unlocked never decreases, cleared is only set by finishing the final level,
    /// skills never duplicate and are never removed.
    /// </summary>
    public static class ProgressRules
    {
        public const int LevelCount = 9;
        public const int FinalLevelIndex = LevelCount - 1;

        /// <summary>Raise the unlocked count; a lower value is ignored. Clamped to 1..LevelCount.</summary>
        public static void Unlock(SaveData s, int count)
        {
            s.unlocked = Clamp(Math.Max(s.unlocked, count), 1, LevelCount);
        }

        /// <summary>Record finishing level <paramref name="levelIndex"/> (0 based).</summary>
        public static void CompleteLevel(SaveData s, int levelIndex)
        {
            if (levelIndex < 0 || levelIndex > FinalLevelIndex) return; // endless or invalid: no unlock change
            Unlock(s, levelIndex + 2);
            if (levelIndex == FinalLevelIndex) s.cleared = true;
        }

        /// <summary>Adds a skill once. Returns true if it was new.</summary>
        public static bool AddSkill(SaveData s, string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            s.skills ??= new string[0];
            if (Array.IndexOf(s.skills, id) >= 0) return false;
            var list = new List<string>(s.skills) { id };
            s.skills = list.ToArray();
            return true;
        }

        public static bool HasSkill(SaveData s, string id) => s.skills != null && Array.IndexOf(s.skills, id) >= 0;

        /// <summary>Map node i is enterable iff i &lt; unlocked.</summary>
        public static bool IsNodeUnlocked(SaveData s, int i) => i >= 0 && i < s.unlocked;

        /// <summary>Map node i is cleared iff i &lt; unlocked-1, or it is the final level and the game is cleared.</summary>
        public static bool IsNodeCleared(SaveData s, int i) =>
            i >= 0 && (i < s.unlocked - 1 || (i == FinalLevelIndex && s.cleared));

        /// <summary>
        /// Makes loaded data valid: null fields filled, unlocked clamped, skills de-duplicated.
        /// Never lowers progress that is already valid.
        /// </summary>
        public static void Sanitize(SaveData s)
        {
            s.@char = s.@char == "boy" ? "boy" : "girl";
            if (string.IsNullOrEmpty(s.diff)) s.diff = "standard";
            s.cont ??= new ResumeState();
            s.unlocked = Clamp(s.unlocked, 1, LevelCount);
            if (s.best < 0) s.best = 0;
            var seen = new List<string>();
            foreach (var id in s.skills ?? new string[0])
                if (!string.IsNullOrEmpty(id) && !seen.Contains(id)) seen.Add(id);
            s.skills = seen.ToArray();
        }

        static int Clamp(int v, int lo, int hi) => v < lo ? lo : v > hi ? hi : v;
    }
}
