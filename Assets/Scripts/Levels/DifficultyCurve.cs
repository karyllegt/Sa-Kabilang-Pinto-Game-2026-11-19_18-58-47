using UnityEngine;

namespace Skp.Levels
{
    /// <summary>
    /// Level-to-level and room-to-room ramp (docs/02 Feature 3, docs/PROTOTYPE_NUMBERS.md).
    /// Shell in M1; the multiplier functions and their tests arrive in M7.
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
    }
}
