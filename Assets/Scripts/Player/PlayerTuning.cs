using UnityEngine;

namespace Skp.Player
{
    /// <summary>
    /// Player movement numbers, kept in the prototype's units (pixels and 60 Hz frames) so they
    /// can be compared with docs/PROTOTYPE_NUMBERS.md one to one. The controller (M2) converts:
    /// per second = per frame x 60, units = pixels / 16.
    /// </summary>
    [CreateAssetMenu(menuName = "Sa Kabilang Pinto/Player Tuning", fileName = "PlayerTuning")]
    public class PlayerTuning : ScriptableObject
    {
        [Header("Body (px)")]
        public Vector2 size = new Vector2(10, 14);

        [Header("Run (px/frame)")]
        public float maxRunSpeed = 1.95f;
        public float groundAccel = 0.6f;
        public float airAccel = 0.38f;
        [Tooltip("Velocity multiplier per frame with no input.")] public float groundFriction = 0.72f;
        public float airFriction = 0.92f;

        [Header("Gravity and jump (px/frame)")]
        public float gravity = 0.42f;
        public float terminalFall = 6.6f;
        public float jumpImpulse = 7.3f;
        [Tooltip("Upward speed multiplier applied once when jump is released early.")] public float jumpCut = 0.45f;
        public int coyoteFrames = 6;
        public int jumpBufferFrames = 7;

        [Header("Dash")]
        public int dashFrames = 11;
        public float dashSpeed = 5.4f;
        public int dashCooldownFrames = 34;
        public int dashFlameCost = 20;

        [Header("Ward")]
        public int wardFrames = 24;
        public float wardRadius = 46f;
        public int wardCooldownFrames = 72;

        [Header("Hurt")]
        public float knockbackY = -3.2f;
        public float knockbackX = 2.4f;
        public int invulnerableFrames = 85;
        public int invulnerableFramesStory = 110;

        [Header("Flame")]
        public int flameMax = 100;
        public int flamePerFragment = 5;
        public int flameCap = 160;
        public int flameRegenFrames = 90;
        public int lanternFlame = 30;
    }
}
