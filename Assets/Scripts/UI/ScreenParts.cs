using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Skp.UI
{
    /// <summary>Pieces shared by several screens (back button, heading, centered body column, modal box).</summary>
    public static class ScreenParts
    {
        public static RectTransform FullScreen(Transform parent, string name)
        {
            var rt = UiKit.Rect(parent, name);
            UiKit.Stretch(rt);
            return rt;
        }

        /// <summary>BACK at the top-left of the safe area.</summary>
        public static PixelButton BackButton(Transform parent)
        {
            var b = UiKit.Button(parent, "Back", Loc.Get("common.back"), new Vector2(104, UiKit.MinTarget), UiKit.Theme.smallSize);
            UiKit.Place((RectTransform)b.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(14, -12), ((RectTransform)b.transform).sizeDelta);
            return b;
        }

        /// <summary>A heading centered at the top, level with the back button.</summary>
        public static TextMeshProUGUI Heading(Transform parent, string text)
        {
            var th = UiKit.Theme;
            var t = UiKit.Label(parent, "Heading", text, th.headingSize, th.text, TextAlignmentOptions.Center, false);
            UiKit.Place(t.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -26), new Vector2(560, 32));
            return t;
        }

        /// <summary>A vertical column centered under the heading (from 76 px below the top to the bottom margin).</summary>
        public static VerticalLayoutGroup Column(Transform parent, float spacing = 10, float top = 80, float bottom = 12)
        {
            var v = UiKit.VStack(parent, "Column", spacing, null, TextAnchor.UpperCenter);
            UiKit.Stretch((RectTransform)v.transform, 16, bottom, 16, top);
            return v;
        }

        /// <summary>A modal PixelBox centered on a scrim, sized to its content (vertical stack inside).</summary>
        public static VerticalLayoutGroup ModalBox(Transform parent, float width, float spacing = 10)
        {
            UiKit.Scrim(parent);
            var box = UiKit.Box(parent, "Box");
            box.raycastTarget = true;
            var rt = box.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(width, 100);
            var v = box.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(24, 24, 20, 20);
            v.spacing = spacing;
            v.childAlignment = TextAnchor.UpperCenter;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = v.childForceExpandHeight = false;
            var f = box.gameObject.AddComponent<ContentSizeFitter>();
            f.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return v;
        }
    }
}
