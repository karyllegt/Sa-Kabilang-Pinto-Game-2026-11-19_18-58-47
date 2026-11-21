using UnityEngine;

namespace Skp.UI
{
    /// <summary>
    /// Fits its RectTransform to the device safe area (notches, rounded corners, gesture bars). Every
    /// screen's content lives under one of these; backgrounds and scrims stay full screen.
    /// Tests and the screenshot audit simulate a notch through <see cref="Simulated"/> (normalized 0..1).
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        /// <summary>When set, used instead of Screen.safeArea: the safe rect as fractions of the screen.</summary>
        public static Rect? Simulated;

        Rect _applied = new Rect(-1, -1, -1, -1);

        public static Rect NormalizedSafeArea()
        {
            if (Simulated.HasValue) return Simulated.Value;
            if (Screen.width <= 0 || Screen.height <= 0) return new Rect(0, 0, 1, 1);
            var sa = Screen.safeArea;
            return new Rect(sa.x / Screen.width, sa.y / Screen.height, sa.width / Screen.width, sa.height / Screen.height);
        }

        void OnEnable() => Apply();
        void Update() => Apply();

        public void Apply()
        {
            var n = NormalizedSafeArea();
            if (n == _applied) return;
            _applied = n;
            var rt = (RectTransform)transform;
            rt.anchorMin = n.min;
            rt.anchorMax = n.max;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
