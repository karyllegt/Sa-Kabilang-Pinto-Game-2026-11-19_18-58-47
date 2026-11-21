using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Skp.UI
{
    /// <summary>
    /// Small boxed message for hints, locked doors, lanterns and fragments. Auto-dismisses (2.5 s by
    /// default), one at a time: a new toast replaces the old one. It has its own lane under the HUD's
    /// objective line so the two never crowd each other (docs/04 known layout issue 2).
    /// </summary>
    public class Toast : MonoBehaviour
    {
        public const float DefaultSeconds = 2.5f;
        public const float LaneTop = 132f;

        public Image box;
        public TextMeshProUGUI label;

        float _until;
        float _maxWidth = 640f;

        /// <summary>Moves the lane (top edge, from the top of the HUD) and caps the width (the HUD narrows it while skill chips show).</summary>
        public void SetLane(float top, float maxWidth)
        {
            top = Mathf.Max(LaneTop, top);
            maxWidth = Mathf.Min(640f, maxWidth - 36f);
            var rt = box.rectTransform;
            if (!Mathf.Approximately(rt.anchoredPosition.y, -top)) rt.anchoredPosition = new Vector2(0, -top);
            if (Mathf.Approximately(maxWidth, _maxWidth)) return;
            _maxWidth = maxWidth;
            if (Visible) Wrap();
        }

        void Wrap()
        {
            var le = label.GetComponent<LayoutElement>();
            le.preferredWidth = label.GetPreferredValues(label.text).x > _maxWidth ? _maxWidth : -1;
        }
        public string Current => box != null && box.gameObject.activeSelf ? label.text : null;
        public bool Visible => box != null && box.gameObject.activeSelf;

        public static Toast Build(Transform parent)
        {
            var th = UiKit.Theme;
            var root = UiKit.Rect(parent, "Toast");
            UiKit.Stretch(root);
            var t = root.gameObject.AddComponent<Toast>();
            t.box = UiKit.Box(root, "Box", th.buttonBoxScale);
            t.box.raycastTarget = false;
            var rt = t.box.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, -LaneTop);
            var h = t.box.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(18, 18, 12, 12);
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = false;
            UiKit.Fit(h);
            t.label = UiKit.Text(t.box.transform, "Label", "", th.smallSize, th.text);
            var le = t.label.GetComponent<LayoutElement>();
            le.preferredWidth = -1;
            t.label.textWrappingMode = TextWrappingModes.Normal;
            t.box.gameObject.SetActive(false);
            return t;
        }

        /// <summary>Shows <paramref name="text"/> for <paramref name="seconds"/> (unscaled), replacing any toast on screen.</summary>
        /// <summary>Raised with every message shown (tests count hints).</summary>
        public event System.Action<string> Shown;

        public void Show(string text, float seconds = DefaultSeconds)
        {
            if (string.IsNullOrEmpty(text)) return;
            Shown?.Invoke(text);
            label.text = text;
            // wrap long hints (640 px, or narrower beside the skill chips) instead of running off a narrow screen
            Wrap();
            box.gameObject.SetActive(true);
            _until = Time.unscaledTime + seconds;
        }

        public void Hide()
        {
            if (box != null) box.gameObject.SetActive(false);
        }

        void Update()
        {
            if (Visible && Time.unscaledTime >= _until) Hide();
        }
    }
}
