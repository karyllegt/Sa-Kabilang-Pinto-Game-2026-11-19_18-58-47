using System;
using System.Collections;
using Skp.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Skp.UI
{
    /// <summary>
    /// The circle wipe between screens: the view closes to a point (0.35 s), the change happens behind
    /// black, then it opens from that point (0.35 s). Centered on the relevant point (the map node when
    /// entering a level). Runs on unscaled time (works while paused) and blocks every tap and key while
    /// it runs (<see cref="Busy"/> feeds <see cref="UiGate"/> and the input reader).
    /// </summary>
    public class CircleWipe : MonoBehaviour, IScreenWipe
    {
        public const string MaterialResource = "UI/CircleWipe";
        public float closeSeconds = 0.35f, openSeconds = 0.35f;

        Image _image;
        Material _mat;
        static readonly int CenterId = Shader.PropertyToID("_Center");
        static readonly int RadiusId = Shader.PropertyToID("_Radius");
        static readonly int PixelId = Shader.PropertyToID("_Pixel");

        public bool Busy { get; private set; }
        /// <summary>0 = fully open, 1 = fully closed.</summary>
        public float Closed { get; private set; }

        public static CircleWipe Create(Transform parent)
        {
            var go = new GameObject("CircleWipe", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            go.transform.SetParent(parent, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var w = go.AddComponent<CircleWipe>();
            var img = new GameObject("Mask", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            img.transform.SetParent(go.transform, false);
            UiKit.Stretch(img.rectTransform);
            var src = Resources.Load<Material>(MaterialResource);
            if (src != null) w._mat = new Material(src);
            img.material = w._mat;
            img.color = Color.white;
            w._image = img;
            w.SetClosed(0);
            return w;
        }

        void SetClosed(float c)
        {
            Closed = Mathf.Clamp01(c);
            if (_image == null) return;
            bool visible = Closed > 0f;
            _image.enabled = visible || Busy;
            _image.raycastTarget = Busy; // swallow taps during the whole wipe
            if (_mat == null) { _image.color = new Color(0, 0, 0, Closed); return; }
            float r = Mathf.Sqrt(Screen.width * (float)Screen.width + Screen.height * (float)Screen.height);
            // ease in-out so the circle reads as a deliberate close
            float e = Closed * Closed * (3 - 2 * Closed);
            _mat.SetFloat(RadiusId, r * (1 - e) - 1);
            _mat.SetFloat(PixelId, Mathf.Max(1, Mathf.Round(Screen.height / 270f)));
        }

        void SetFocus(Vector2 focus)
        {
            if (_mat != null) _mat.SetVector(CenterId, new Vector4(focus.x * Screen.width, focus.y * Screen.height, 0, 0));
        }

        public IEnumerator Close(Vector2 focus)
        {
            Busy = true;
            SetFocus(focus);
            for (float t = 0; t < closeSeconds; t += Time.unscaledDeltaTime)
            {
                SetClosed(t / closeSeconds);
                yield return null;
            }
            SetClosed(1);
        }

        public IEnumerator Open(Vector2 focus)
        {
            SetFocus(focus);
            for (float t = 0; t < openSeconds; t += Time.unscaledDeltaTime)
            {
                SetClosed(1 - t / openSeconds);
                yield return null;
            }
            Busy = false;
            SetClosed(0);
        }

        /// <summary>A wipe inside a scene (no load): close, run <paramref name="middle"/>, open. Dropped if one is running.</summary>
        public bool Run(Action middle, Vector2 focus)
        {
            if (Busy) return false;
            StartCoroutine(RunRoutine(middle, focus));
            return true;
        }

        IEnumerator RunRoutine(Action middle, Vector2 focus)
        {
            yield return Close(focus);
            try { middle?.Invoke(); }
            catch (Exception e) { Debug.LogException(e); }
            yield return null;
            yield return Open(focus);
        }
    }
}
