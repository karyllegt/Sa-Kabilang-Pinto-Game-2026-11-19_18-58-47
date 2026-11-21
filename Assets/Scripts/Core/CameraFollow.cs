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
        /// <summary>While set, the view's top edge is held here (the Bellkeeper arena: prototype ty = arena floor - 240 px).</summary>
        public float? LockTopY { get; set; }

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
            float cy = LockTopY.HasValue ? LockTopY.Value - v.y * 0.5f : playerCenter.y + (0.5f - k) * v.y;
            cx = Clamp(cx, bounds.xMin + v.x * 0.5f, bounds.xMax - v.x * 0.5f);
            cy = Clamp(cy, bounds.yMin + v.y * 0.5f, bounds.yMax - v.y * 0.5f);
            return new Vector2(cx, cy);
        }

        public void Tick(Vector2 playerCenter, int gravitySign)
        {
            var t = Target(playerCenter, gravitySign);
            var p = Base;
            Base = new Vector3(Mathf.Lerp(p.x, t.x, easeX), Mathf.Lerp(p.y, t.y, easeY), p.z);
            ApplyPosition();
        }

        public void SnapTo(Vector2 playerCenter, int gravitySign)
        {
            var t = Target(playerCenter, gravitySign);
            Base = new Vector3(t.x, t.y, transform.position.z);
            ApplyPosition();
        }

        // ------------------------------------------------------------------ screen shake (M8)

        /// <summary>The followed position without shake.</summary>
        public Vector3 Base
        {
            get => _hasBase ? _base : transform.position;
            private set { _base = value; _hasBase = true; }
        }
        Vector3 _base;
        bool _hasBase;

        /// <summary>0 to 1; the offset grows with its square and decays over time.</summary>
        public float Trauma { get; private set; }
        /// <summary>Current shake offset (units), pixel-snapped.</summary>
        public Vector2 ShakeOffset { get; private set; }
        public FeelTuning feel;
        /// <summary>Tests force the setting without a GameRoot.</summary>
        public static bool? ShakeEnabledOverride;
        float _seed;

        public static bool ShakeEnabled
        {
            get
            {
                if (ShakeEnabledOverride.HasValue) return ShakeEnabledOverride.Value;
                var r = GameRoot.Instance;
                return r == null || r.Settings == null || r.Settings.Data.shake;
            }
        }

        FeelTuning Feel
        {
            get
            {
                if (feel == null) feel = Resources.Load<FeelTuning>(FeelTuning.ResourcePath);
                if (feel == null) feel = ScriptableObject.CreateInstance<FeelTuning>();
                return feel;
            }
        }

        /// <summary>Adds trauma, unless the Screen shake setting is off.</summary>
        public void AddTrauma(float amount)
        {
            if (amount <= 0 || !ShakeEnabled) return;
            Trauma = Mathf.Clamp01(Trauma + amount);
        }

        public void ClearShake() { Trauma = 0; ShakeOffset = Vector2.zero; ApplyPosition(); }

        /// <summary>Advances the shake (unscaled time: it settles even while paused).</summary>
        public void StepShake(float dt)
        {
            var f = Feel;
            if (!ShakeEnabled) Trauma = 0;
            if (Trauma <= 0) { if (ShakeOffset != Vector2.zero) { ShakeOffset = Vector2.zero; ApplyPosition(); } return; }
            _seed += dt * f.shakeFrequency;
            float a = Trauma * Trauma * f.maxShake;
            float x = (Mathf.PerlinNoise(_seed, 0.3f) * 2f - 1f) * a;
            float y = (Mathf.PerlinNoise(0.7f, _seed) * 2f - 1f) * a;
            ShakeOffset = new Vector2(Mathf.Round(x * 16f) / 16f, Mathf.Round(y * 16f) / 16f);
            Trauma = Mathf.Max(0f, Trauma - f.shakeDecay * dt);
            ApplyPosition();
        }

        void LateUpdate() { if (Application.isPlaying) StepShake(Time.unscaledDeltaTime); }

        void ApplyPosition()
        {
            var b = Base;
            transform.position = new Vector3(b.x + ShakeOffset.x, b.y + ShakeOffset.y, b.z);
        }

        static float Clamp(float v, float lo, float hi) => lo > hi ? (lo + hi) * 0.5f : Mathf.Clamp(v, lo, hi);
    }
}
