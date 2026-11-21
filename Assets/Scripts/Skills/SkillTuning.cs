using UnityEngine;

namespace Skp.Skills
{
    /// <summary>
    /// Every skill number (docs/02 Feature 1), per second and in Unity units (1 unit = 16 px), converted from the
    /// prototype's per-frame values at 60 Hz. Durations are rounded to whole 60 Hz ticks by the simulation.
    /// </summary>
    [CreateAssetMenu(menuName = "Sa Kabilang Pinto/Skill Tuning", fileName = "SkillTuning")]
    public class SkillTuning : ScriptableObject
    {
        [Header("Double Jump (dj)")]
        [Tooltip("6.7 px/frame, about 92 percent of the 7.3 px/frame ground jump")] public float doubleJumpSpeed = 6.7f * 60f / 16f;
        [Tooltip("Extra jumps per airtime.")] public int airJumps = 1;

        [Header("Lambong Veil (veil) and Greater Veil (veil2)")]
        [Tooltip("180 frames")] public float veilDuration = 3f;
        [Tooltip("900 frames, counted from activation")] public float veilCooldown = 15f;
        [Tooltip("300 frames")] public float greaterVeilDuration = 5f;
        [Tooltip("600 frames")] public float greaterVeilCooldown = 10f;
        [Tooltip("The ring flickers in the last 60 frames.")] public float veilFlickerTime = 1f;

        [Header("Liwanag Bolt (bolt) and Triple Bolt (spread)")]
        [Tooltip("4.4 px/frame")] public float boltSpeed = 4.4f * 60f / 16f;
        [Tooltip("56 frames, about 0.9 s")] public float boltLifetime = 56f / 60f;
        public int boltFlameCost = 8;
        [Tooltip("16 frames, about 0.27 s")] public float boltCooldown = 16f / 60f;
        public int tripleBoltFlameCost = 12;
        [Tooltip("22 frames, about 0.37 s")] public float tripleBoltCooldown = 22f / 60f;
        [Tooltip("The outer rays, +- 0.24 rad (about 14 degrees)")] public float tripleBoltAngle = 0.24f * Mathf.Rad2Deg;
        [Tooltip("6 px ahead of Tala's center, in facing direction")] public float boltSpawnAhead = 6f / 16f;
        [Tooltip("170 frames: tikbalang and manananggal (1 hp each hit)")] public float boltCreatureStun = 170f / 60f;
        [Tooltip("140 frames")] public float boltShadowStun = 140f / 60f;
        [Tooltip("420 frames")] public float rootBurnTime = 7f;
        [Tooltip("600 frames")] public float thornBurnTime = 10f;
        public int burnScore = 20;
        [Tooltip("Bellkeeper damage per bolt, only while it is stunned (M7).")] public int bossBoltDamage = 1;

        [Header("Swift Dash (swift)")]
        public int swiftDashFlameCost = 8;
        [Tooltip("20 frames (34 without the skill)")] public float swiftDashCooldown = 20f / 60f;

        [Header("Heart Charm (heart)")]
        public int heartBonus = 1;

        [Header("Pakpak Glide (glide)")]
        [Tooltip("1.1 px/frame: the fastest fall while gliding")] public float glideFallSpeed = 1.1f * 60f / 16f;

        static SkillTuning _default;
        /// <summary>Built-in defaults for code paths (tests, dev rooms) that have no asset.</summary>
        public static SkillTuning Default
        {
            get
            {
                if (_default == null) { _default = CreateInstance<SkillTuning>(); _default.hideFlags = HideFlags.HideAndDontSave; }
                return _default;
            }
        }
    }
}
