using UnityEngine;

namespace Skp.Levels
{
    /// <summary>
    /// Skill obstacle numbers (docs/02 Feature 4), in tiles unless noted. The generator's M7 mode uses them on top of
    /// the prototype's placement chances, so every obstacle truly needs its skill on the ordinary route.
    /// </summary>
    [CreateAssetMenu(menuName = "Sa Kabilang Pinto/Skill Obstacle Tuning", fileName = "SkillObstacleTuning")]
    public class SkillObstacleTuning : ScriptableObject
    {
        [Header("Pakpak Glide")]
        [Tooltip("Horizontal gap of a glide gap. Wider than a run, jump, double jump and dash can carry Tala; a glide crosses it.")]
        public int glideGap = 14;

        [Header("Swift Dash")]
        [Tooltip("Horizontal gap of a dash gap, one tile up: more than a running jump, less than a jump and a dash.")]
        public int dashGap = 6;

        [Header("Lambong Veil gauntlet")]
        [Tooltip("Gauntlet ledge width; 12 once the Greater Veil is owned (one safe tile at each end).")]
        public int gauntletWidth = 9, greaterGauntletWidth = 12;
        [Tooltip("The violet thorn curtain over the spikes starts this high above the ledge (px) ...")]
        public int curtainGap = 28;
        [Tooltip("... and is this tall (px), higher than a double jump can carry Tala over it.")]
        public int curtainHeight = 96;
        [Tooltip("... and hangs this far under the ledge too (px, to the room floor at most), so nobody passes beneath.")]
        public int curtainBelow = 128;

        [Header("Act III falling bells")]
        [Tooltip("Chance per ordinary ledge (times the hazard multiplier).")]
        public float bellChance = 0.22f;
        [Tooltip("Ticks between drops at level 7; 10 fewer per level after it.")]
        public int bellPeriod = 240;
        [Tooltip("Bells start this many tiles above their ledge (or under the next ledge above).")]
        public int bellDrop = 10;

        [Header("Secret flame nooks (levels with 6 or more rooms)")]
        public float nookChance = 0.4f;
        public int nookHeight = 4;

        [Header("Width")]
        [Tooltip("From this level index on, ordinary ledges are at most one tile narrower.")]
        public int narrowFromLevel = 4;

        static SkillObstacleTuning _default;
        public static SkillObstacleTuning Default
        {
            get
            {
                if (_default == null) { _default = CreateInstance<SkillObstacleTuning>(); _default.hideFlags = HideFlags.HideAndDontSave; }
                return _default;
            }
        }
    }
}
