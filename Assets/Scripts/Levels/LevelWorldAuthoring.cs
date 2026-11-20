using System.Linq;
using Skp.Player;
using UnityEngine;

namespace Skp.Levels
{
    /// <summary>
    /// Scene-side description of a playable area: collects the ledge, hazard and lantern
    /// markers under it into a <see cref="PlayerWorld"/>. Used by the M2 test room now and by
    /// the level generator (M4), which creates the same markers.
    /// </summary>
    public class LevelWorldAuthoring : MonoBehaviour
    {
        [Tooltip("Level area in world units (x, y = bottom-left).")]
        public Rect bounds = new Rect(0, -3, 40, 24);

        public PlayerWorld Build()
        {
            var w = new PlayerWorld(bounds);
            foreach (var m in GetComponentsInChildren<WorldMarker>())
            {
                switch (m.kind)
                {
                    case WorldMarker.Kind.Ledge: w.Ledges.Add(new MaskedRect(m.Rect, m.mask)); break;
                    case WorldMarker.Kind.Hazard: w.Hazards.Add(new MaskedRect(m.Rect, m.mask)); break;
                }
            }
            foreach (var l in GetComponentsInChildren<LanternMarker>().OrderBy(l => l.order))
                w.Lanterns.Add(l.transform.position);
            foreach (var d in GetComponentsInChildren<DoorMarker>()) w.Doors.Add(d.ToDoor());
            return w;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.9f, 0.72f, 0.35f, 0.6f);
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }
}
