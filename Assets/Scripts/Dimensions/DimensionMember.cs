using UnityEngine;

namespace Skp.Dimensions
{
    /// <summary>
    /// Makes an object's sprites follow the current dimension, like the prototype's drawWorld():
    /// fully drawn when its mask contains the dimension, otherwise a faint ghost (ledges 16 percent
    /// in their dimension colour, doors 12 percent, hazards 12 percent red) or hidden (items, enemies).
    /// </summary>
    public class DimensionMember : MonoBehaviour
    {
        public enum Ghost { Hidden, Ledge, Door, Hazard, AlwaysVisible }

        public Dim mask = Dim.All;
        public Ghost ghost = Ghost.Ledge;
        [Tooltip("Door colour when shown as a ghost (the door's target dimension).")]
        public Dim ghostTint = Dim.Ordinary;
        public SpriteRenderer[] renderers;

        Color[] _base;

        void Awake() => CacheColors();

        void CacheColors()
        {
            if (renderers == null || renderers.Length == 0) renderers = GetComponentsInChildren<SpriteRenderer>(true);
            _base = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) _base[i] = renderers[i] != null ? renderers[i].color : Color.white;
        }

        public bool IsActiveIn(Dim current) => ghost == Ghost.AlwaysVisible || DimensionRules.Active(mask, current);

        public void Apply(Dim current)
        {
            if (_base == null || _base.Length != renderers.Length) CacheColors();
            bool on = IsActiveIn(current);
            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (r == null) continue;
                if (on) { r.enabled = true; r.color = _base[i]; continue; }
                switch (ghost)
                {
                    case Ghost.Hidden: r.enabled = false; break;
                    case Ghost.Ledge: r.enabled = true; r.color = WithAlpha(DimensionRules.GhostColor(mask), 0.16f); break;
                    case Ghost.Door: r.enabled = true; r.color = WithAlpha(DimensionRules.Color(ghostTint), 0.12f); break;
                    case Ghost.Hazard: r.enabled = true; r.color = new Color(1f, 0.35f, 0.29f, 0.12f); break;
                }
            }
        }

        static Color WithAlpha(Color c, float a) { c.a = a; return c; }
    }
}
