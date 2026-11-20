using Skp.Dimensions;
using Skp.Player;
using UnityEngine;

namespace Skp.Levels
{
    /// <summary>
    /// A door. Transform position = bottom-left of its rectangle (top-left edge for a hanging door,
    /// which is still described by its bottom-left corner). Seen and used in <see cref="mask"/>,
    /// leads to <see cref="target"/>.
    /// </summary>
    public class DoorMarker : MonoBehaviour
    {
        public DoorKind kind = DoorKind.Enter;
        public Dim mask = Dim.Ordinary;
        public Dim target = Dim.Spirit;
        [Tooltip("16 x 28 px for dimension doors, 20 x 32 px for the exit.")]
        public Vector2 size = new Vector2(1f, 28f / 16f);
        public bool hanging;
        public bool teleport;
        [Tooltip("Arrival point (center x, feet y) when teleport is on.")]
        public Vector2 arrival;

        public Door ToDoor() => new Door
        {
            Rect = new Rect((Vector2)transform.position, size),
            Mask = mask,
            Target = target,
            Kind = kind,
            To = teleport ? arrival : (Vector2?)null,
            Hanging = hanging,
        };

        void OnDrawGizmos()
        {
            Gizmos.color = DimensionRules.Color(kind == DoorKind.Enter ? target : Dim.Ordinary);
            var r = new Rect((Vector2)transform.position, size);
            Gizmos.DrawWireCube(r.center, size);
            if (teleport) Gizmos.DrawLine(r.center, arrival);
        }
    }
}
