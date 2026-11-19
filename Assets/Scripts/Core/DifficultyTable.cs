using System;
using UnityEngine;

namespace Skp.Core
{
    public enum DifficultyId { Story, Standard, Nightmare, Endless }

    /// <summary>One row of the prototype's difficulty table (docs/PROTOTYPE_NUMBERS.md).</summary>
    [Serializable]
    public class DifficultyMode
    {
        public DifficultyId id;
        public string displayName;
        public int vitality;
        [Tooltip("-1 = infinite lives")] public int lives;
        public float speedMult;
        [Tooltip("Shadow chase delay in 60 Hz frames")] public int chaseFrames;
        public float floodMult;
        public int bossHp;
        public float hazardMult;
        public float enemyMult;

        public bool InfiniteLives => lives < 0;
        public string SaveKey => id.ToString().ToLowerInvariant();
    }

    [CreateAssetMenu(menuName = "Sa Kabilang Pinto/Difficulty Table", fileName = "DifficultyTable")]
    public class DifficultyTable : ScriptableObject
    {
        public DifficultyMode[] modes = Defaults();

        public DifficultyMode Get(DifficultyId id)
        {
            foreach (var m in modes) if (m.id == id) return m;
            throw new ArgumentException("No difficulty " + id);
        }

        /// <summary>Accepts the save-file keys "story", "standard", "nightmare", "endless".</summary>
        public DifficultyMode Get(string saveKey)
        {
            foreach (var m in modes) if (m.SaveKey == saveKey) return m;
            return Get(DifficultyId.Standard);
        }

        public static DifficultyMode[] Defaults() => new[]
        {
            new DifficultyMode { id = DifficultyId.Story,     displayName = "Story",             vitality = 6, lives = -1, speedMult = 0.8f,  chaseFrames = 300, floodMult = 0.7f, bossHp = 8,  hazardMult = 0.6f, enemyMult = 0.6f },
            new DifficultyMode { id = DifficultyId.Standard,  displayName = "Standard",          vitality = 4, lives = 5,  speedMult = 1f,    chaseFrames = 240, floodMult = 1f,   bossHp = 12, hazardMult = 1f,   enemyMult = 1f },
            new DifficultyMode { id = DifficultyId.Nightmare, displayName = "Nightmare",         vitality = 3, lives = 3,  speedMult = 1.25f, chaseFrames = 180, floodMult = 1.4f, bossHp = 16, hazardMult = 1.4f, enemyMult = 1.4f },
            new DifficultyMode { id = DifficultyId.Endless,   displayName = "Endless Nightmare", vitality = 4, lives = 3,  speedMult = 1.15f, chaseFrames = 200, floodMult = 1.2f, bossHp = 12, hazardMult = 1.3f, enemyMult = 1.3f },
        };
    }
}
