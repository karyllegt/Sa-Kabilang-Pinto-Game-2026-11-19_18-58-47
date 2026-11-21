using UnityEngine;

namespace Skp.Player
{
    /// <summary>
    /// Player numbers, per second and in Unity units (1 unit = 16 px = 1 tile).
    /// Converted from docs/PROTOTYPE_NUMBERS.md: per second = per frame x 60, units = px / 16,
    /// acceleration per second squared = per frame squared x 3600.
    /// The simulation steps at a fixed 60 Hz (<see cref="PlayerSim.TickRate"/>), so durations are
    /// rounded to whole ticks and the prototype's feel is reproduced exactly.
    /// Friction factors are kept "per 60 Hz tick" because that is how the prototype applies them.
    /// </summary>
    [CreateAssetMenu(menuName = "Sa Kabilang Pinto/Player Tuning", fileName = "PlayerTuning")]
    public class PlayerTuning : ScriptableObject
    {
        public const float PxPerUnit = 16f;

        [Header("Body (units)")]
        [Tooltip("Hitbox, 10 x 14 px.")]
        public Vector2 hitbox = new Vector2(10f / 16f, 14f / 16f);

        [Header("Run")]
        [Tooltip("1.95 px/frame")] public float runSpeed = 7.3125f;
        [Tooltip("0.6 px/frame^2")] public float groundAccel = 135f;
        [Tooltip("0.38 px/frame^2")] public float airAccel = 85.5f;
        [Tooltip("Velocity factor per 60 Hz tick with no input, on the ground.")] public float groundFrictionPerTick = 0.72f;
        [Tooltip("Velocity factor per 60 Hz tick with no input, in the air.")] public float airFrictionPerTick = 0.92f;
        [Tooltip("Above run speed (after a dash) the excess decays by this factor per tick.")] public float overspeedDecayPerTick = 0.9f;
        [Tooltip("0.05 px/frame: slower than this snaps to zero.")] public float stopSpeed = 0.1875f;

        [Header("Gravity and jump")]
        [Tooltip("0.42 px/frame^2")] public float gravity = 94.5f;
        [Tooltip("6.6 px/frame")] public float terminalFall = 24.75f;
        [Tooltip("7.3 px/frame: about 3.9 tiles at full hold")] public float jumpSpeed = 27.375f;
        [Tooltip("Upward speed is multiplied by this once when jump is released early.")] public float jumpCutFactor = 0.45f;
        [Tooltip("6 frames")] public float coyoteTime = 0.1f;
        [Tooltip("7 frames")] public float jumpBufferTime = 7f / 60f;

        [Header("Dash")]
        [Tooltip("11 frames")] public float dashDuration = 11f / 60f;
        [Tooltip("5.4 px/frame")] public float dashSpeed = 20.25f;
        [Tooltip("34 frames")] public float dashCooldown = 34f / 60f;
        public int dashFlameCost = 20;

        [Header("Ward")]
        [Tooltip("24 frames")] public float wardDuration = 0.4f;
        [Tooltip("46 px")] public float wardRadius = 46f / 16f;
        [Tooltip("72 frames")] public float wardCooldown = 1.2f;

        [Header("Anino shadow (Spirit dimension)")]
        [Tooltip("50 px: a ward stuns the shadow within this distance (center to center)")] public float shadowWardReach = 50f / 16f;
        [Tooltip("190 frames")] public float shadowWardStun = 190f / 60f;

        [Header("Hurt and death")]
        [Tooltip("3.2 px/frame, away from gravity")] public float knockbackUp = 12f;
        [Tooltip("2.4 px/frame, away from the source")] public float knockbackSide = 9f;
        [Tooltip("85 frames")] public float invulnerableTime = 85f / 60f;
        [Tooltip("110 frames on Story")] public float invulnerableTimeStory = 110f / 60f;
        [Tooltip("75 frames between death and respawn")] public float deathDelay = 1.25f;
        [Tooltip("100 frames of invulnerability after respawning")] public float respawnInvulnerableTime = 100f / 60f;
        [Tooltip("Flame is raised to at least this on respawn.")] public int respawnMinFlame = 60;

        [Header("Flame")]
        public int flameMax = 100;
        public int flamePerFragment = 5;
        public int flameCap = 160;
        [Tooltip("+1 flame every 90 frames")] public float flameRegenInterval = 1.5f;
        public int lanternFlame = 30;

        [Header("Checkpoints (lanterns)")]
        [Tooltip("14 px: horizontal reach to light a lantern")] public float lanternReachX = 14f / 16f;
        [Tooltip("6 px: vertical reach (feet near the lantern's base)")] public float lanternReachY = 6f / 16f;

        [Header("Landing")]
        [Tooltip("0.5 px: how far below a ledge top the feet may start and still land.")] public float landingTolerance = 0.5f / 16f;

        /// <summary>Seconds to whole 60 Hz ticks.</summary>
        public static int Ticks(float seconds) => Mathf.RoundToInt(seconds * PlayerSim.TickRate);
    }
}
