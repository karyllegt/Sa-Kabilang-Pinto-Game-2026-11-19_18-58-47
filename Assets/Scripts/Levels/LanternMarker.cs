using UnityEngine;

namespace Skp.Levels
{
    /// <summary>A lantern checkpoint. Transform position = its center x and the surface it stands on. Order 0 is the spawn.</summary>
    public class LanternMarker : MonoBehaviour
    {
        public int order;
        [Tooltip("Optional: shown dim until lit (M4 adds the real lantern art).")]
        public SpriteRenderer flame;

        public void SetLit(bool lit)
        {
            if (flame != null) flame.color = lit ? Color.white : new Color(1f, 1f, 1f, 0.35f);
        }
    }
}
