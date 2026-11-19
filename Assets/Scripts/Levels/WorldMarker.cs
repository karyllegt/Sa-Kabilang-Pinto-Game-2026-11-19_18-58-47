using UnityEngine;

namespace Skp.Levels
{
    /// <summary>A rectangle of level geometry: the transform position is its bottom-left corner.</summary>
    public class WorldMarker : MonoBehaviour
    {
        public enum Kind { Ledge, Hazard }

        public Kind kind;
        [Tooltip("Size in world units (1 unit = 1 tile = 16 px).")]
        public Vector2 size = new Vector2(3, 1);

        public Rect Rect => new Rect((Vector2)transform.position, size);

        void OnDrawGizmos()
        {
            Gizmos.color = kind == Kind.Ledge ? new Color(0.9f, 0.72f, 0.35f) : new Color(1f, 0.35f, 0.29f);
            Gizmos.DrawWireCube(Rect.center, size);
        }
    }
}
