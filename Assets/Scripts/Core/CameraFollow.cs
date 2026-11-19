using UnityEngine;

namespace Skp.Core
{
    /// <summary>
    /// Prototype camera: eases toward the player once per 60 Hz tick (12 percent in x, 10 percent in y),
    /// keeps the player a little below center (above center in Inverted gravity) and never shows
    /// outside the level bounds.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        public Camera cam;
        public Rect bounds = new Rect(0, -3, 40, 24);
        [Range(0, 1)] public float easeX = 0.12f;
        [Range(0, 1)] public float easeY = 0.1f;
        [Tooltip("Player height in the view, from the bottom (prototype: 0.42 of the screen).")]
        [Range(0, 1)] public float playerHeightInView = 0.42f;

        Vector2 ViewSize
        {
            get
            {
                var c = cam != null ? cam : Camera.main;
                float h = c != null ? c.orthographicSize * 2f : 270f / 16f;
                float w = c != null ? h * c.aspect : 480f / 16f;
                return new Vector2(w, h);
            }
        }

        public Vector2 Target(Vector2 playerCenter, int gravitySign)
        {
            var v = ViewSize;
            float k = gravitySign > 0 ? playerHeightInView : 1f - playerHeightInView;
            float cx = playerCenter.x;
            float cy = playerCenter.y + (0.5f - k) * v.y;
            cx = Clamp(cx, bounds.xMin + v.x * 0.5f, bounds.xMax - v.x * 0.5f);
            cy = Clamp(cy, bounds.yMin + v.y * 0.5f, bounds.yMax - v.y * 0.5f);
            return new Vector2(cx, cy);
        }

        public void Tick(Vector2 playerCenter, int gravitySign)
        {
            var t = Target(playerCenter, gravitySign);
            var p = transform.position;
            transform.position = new Vector3(Mathf.Lerp(p.x, t.x, easeX), Mathf.Lerp(p.y, t.y, easeY), p.z);
        }

        public void SnapTo(Vector2 playerCenter, int gravitySign)
        {
            var t = Target(playerCenter, gravitySign);
            transform.position = new Vector3(t.x, t.y, transform.position.z);
        }

        static float Clamp(float v, float lo, float hi) => lo > hi ? (lo + hi) * 0.5f : Mathf.Clamp(v, lo, hi);
    }
}
