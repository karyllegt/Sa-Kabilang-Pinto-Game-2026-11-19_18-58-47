using UnityEngine;

namespace Skp.Levels
{
    /// <summary>
    /// Level-to-level and room-to-room ramp (docs/02 Feature 3, docs/PROTOTYPE_NUMBERS.md).
    /// The generator reads the hazard and enemy numbers (LevelGenerator GenOptions.From); the session applies the shadow,
    /// flood and enemy-speed ramps. Difficulty-mode multipliers apply on top.
    /// </summary>
    [CreateAssetMenu(menuName = "Sa Kabilang Pinto/Difficulty Curve", fileName = "DifficultyCurve")]
    public class DifficultyCurve : ScriptableObject
    {
        [Header("Per level index L")]
        public float hazardPerLevel = 0.08f;
        public float enemyPerLevel = 0.06f;
        public float chaseDelayPerLevel = -0.035f;
        public float floodPerLevel = 0.05f;
        public float enemySpeedPerLevel = 0.02f;

        [Header("Per room index r")]
        public float hazardPerRoom = 0.05f;
        public float enemyPerRoom = 0.04f;

        [Header("Caps and floors")]
        public float hazardCap = 2.4f;
        public float enemyCap = 2.2f;
        [Tooltip("Shadow chase delay floor, in 60 Hz frames.")]
        public int minChaseFrames = 100;

        [Header("Skill obstacles")]
        public SkillObstacleTuning obstacles;

        public float HazardMult(float difficulty, int levelIndex, int room) =>
            Mathf.Min(hazardCap, difficulty * (1f + hazardPerLevel * Mathf.Max(0, levelIndex)) * (1f + hazardPerRoom * room));

        public float EnemyMult(float difficulty, int levelIndex, int room) =>
            Mathf.Min(enemyCap, difficulty * (1f + enemyPerLevel * Mathf.Max(0, levelIndex)) * (1f + enemyPerRoom * room));

        /// <summary>Flood rise: +5 percent per level.</summary>
        public float FloodFactor(int levelIndex) => 1f + floodPerLevel * Mathf.Max(0, levelIndex);

        /// <summary>Creature speed: the difficulty's speed, +2 percent per level.</summary>
        public float EnemySpeed(float difficulty, int levelIndex) => difficulty * (1f + enemySpeedPerLevel * Mathf.Max(0, levelIndex));

        /// <summary>Shadow lag for a level: the difficulty's frames x (1 - 0.035 L), never below the floor.</summary>
        public int ChaseFrames(int difficultyFrames, int levelIndex) =>
            Mathf.Max(minChaseFrames, Mathf.RoundToInt(difficultyFrames * (1f + chaseDelayPerLevel * Mathf.Max(0, levelIndex))));
    }
}
