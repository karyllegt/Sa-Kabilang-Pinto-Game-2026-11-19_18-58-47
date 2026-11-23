using Skp.Dimensions;
using TMPro;
using UnityEngine;

namespace Skp.Levels
{
    /// <summary>
    /// R1: the view part of one level, boss or map prefab. It names the renderers the code drives and holds the sprites for
    /// each state (creature frames, root phases, burnt thorns, the locked and open exit, the Bellkeeper's looks), so the art
    /// lives in the prefab, not in code. Artists change the prefab; the views only pick a state.
    /// </summary>
    public class LevelPiece : MonoBehaviour
    {
        [Tooltip("The renderer the code moves, sizes, tints and switches.")]
        public SpriteRenderer main;
        [Tooltip("Other renderers of this piece (the lantern post, the map corner's second bar, the Lola marker box).")]
        public SpriteRenderer[] extra = new SpriteRenderer[0];
        [Tooltip("Sprites per state, in the order the view documents (frames, phases, locked/open...).")]
        public Sprite[] states = new Sprite[0];
        [Tooltip("Source frames face left rather than the default right.")]
        public bool artFacesLeft;
        [Tooltip("Optional presentation width in world units, independent of simulation geometry.")]
        public float presentationWidth;
        [Tooltip("Set when this piece follows the dimension rules.")]
        public DimensionMember dimension;
        [Tooltip("Text, for map labels.")]
        public TextMeshPro label;
        [Tooltip("Line, for the Bellkeeper's toll ring.")]
        public LineRenderer line;

        public int StateCount => states != null ? states.Length : 0;

        /// <summary>Shows state i when it has a sprite (no allocation, no change when already shown).</summary>
        public void SetState(int i)
        {
            if (main == null || states == null || i < 0 || i >= states.Length) return;
            var s = states[i];
            if (s != null && main.sprite != s) main.sprite = s;
            if (s != null && presentationWidth > 0 && s.bounds.size.x > 0)
            {
                var scale = main.transform.localScale;
                scale.x = presentationWidth / s.bounds.size.x;
                main.transform.localScale = scale;
            }
        }

        /// <summary>Creates a copy of a prefab under a parent at a world position.</summary>
        public static LevelPiece Spawn(LevelPiece prefab, Transform parent, Vector3 worldPos)
        {
            var p = Object.Instantiate(prefab, parent, false);
            p.transform.position = worldPos;
            return p;
        }
    }
}
