using TMPro;
using Skp.Skills;
using UnityEngine;
using UnityEngine.UI;

namespace Skp.UI
{
    /// <summary>
    /// Builds the shared widgets (PixelBox panels, labels, buttons, rows and stacks) from the
    /// <see cref="UiTheme"/>. The editor setup calls the screen builders, which use this kit, and saves
    /// the result as prefabs; nothing here runs per frame. Layout is anchors and layout groups only.
    /// </summary>
    public static class UiKit
    {
        /// <summary>Smallest interactive size on the 960x540 reference canvas: 48 dp on a 6.4" 20:9 phone (about 400 dpi).</summary>
        public const float MinTarget = 56f;

        public static UiTheme Theme => UiTheme.Load();
        public static SkillLibrary SkillArt => Theme.skillArt != null ? Theme.skillArt : SkillLibrary.Load();

        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5; // UI
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static RectTransform Stretch(RectTransform rt, float l = 0, float b = 0, float r = 0, float t = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(l, b);
            rt.offsetMax = new Vector2(-r, -t);
            return rt;
        }

        /// <summary>Anchor to a point of the parent (0..1) with a pivot and a fixed size.</summary>
        public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static Image Box(Transform parent, string name, float scale = -1)
        {
            var rt = Rect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = Theme.pixelBox;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = scale > 0 ? scale : Theme.boxScale;
            img.color = Color.white;
            // A restrained brass inlay gives every panel the same crafted frame, including modals and HUD cards.
            var inlay = Fill(rt, "BrassInlay", new Color(Theme.gold.r, Theme.gold.g, Theme.gold.b, 0.65f));
            inlay.rectTransform.anchorMin = new Vector2(0, 1);
            inlay.rectTransform.anchorMax = new Vector2(1, 1);
            inlay.rectTransform.pivot = new Vector2(0.5f, 1);
            inlay.rectTransform.offsetMin = new Vector2(14, -7);
            inlay.rectTransform.offsetMax = new Vector2(-14, -6);
            var layout = inlay.gameObject.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;
            return img;
        }

        public static Image Fill(Transform parent, string name, Color color)
        {
            var rt = Rect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = Theme.white;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        public static Image Picture(Transform parent, string name, Sprite sprite, Vector2 size)
        {
            var rt = Rect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            rt.sizeDelta = size;
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = size.x;
            le.preferredHeight = size.y;
            return img;
        }

        public static TextMeshProUGUI Label(Transform parent, string name, string text, float size, Color color,
                                            TextAlignmentOptions align = TextAlignmentOptions.Center, bool wrap = true)
        {
            var rt = Rect(parent, name);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (Theme.font != null) t.font = Theme.font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow; // overflow is detected by the layout audit, never hidden
            t.lineSpacing = 32f; // Press Start 2P has no built-in leading
            t.raycastTarget = false;
            return t;
        }

        /// <summary>A preferred-size text inside a layout group.</summary>
        public static TextMeshProUGUI Text(Transform parent, string name, string text, float size, Color color,
                                           TextAlignmentOptions align = TextAlignmentOptions.Center, float width = -1)
        {
            var t = Label(parent, name, text, size, color, align);
            var le = t.gameObject.AddComponent<LayoutElement>();
            if (width > 0) le.preferredWidth = width;
            return t;
        }

        public static PixelButton Button(Transform parent, string name, string text, Vector2 size, float fontSize = -1, bool arrow = false)
        {
            var th = Theme;
            var frame = Box(parent, name, arrow ? th.boxScale : th.buttonBoxScale);
            var go = frame.gameObject;
            if (arrow) frame.color = new Color(1, 1, 1, 0); // menu rows: the box around the menu is the frame
            var b = go.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.targetGraphic = frame;
            var nav = b.navigation;
            nav.mode = Navigation.Mode.Automatic;
            b.navigation = nav;

            var hi = Fill(go.transform, "Highlight", th.highlight);
            Stretch(hi.rectTransform, arrow ? 0 : 6, arrow ? 0 : 6, arrow ? 0 : 6, arrow ? 0 : 6);
            hi.enabled = false;

            var pb = go.AddComponent<PixelButton>();
            pb.button = b;
            pb.frame = frame;
            pb.highlight = hi;
            float fs = fontSize > 0 ? fontSize : th.bodySize;
            pb.label = Label(go.transform, "Label", text, fs, th.text, arrow ? TextAlignmentOptions.Left : TextAlignmentOptions.Center, false);
            Stretch(pb.label.rectTransform, arrow ? 44 : 8, 4, 8, 4);
            if (arrow)
            {
                pb.arrow = Label(go.transform, "Arrow", ">", fs, th.goldBright, TextAlignmentOptions.Center, false);
                Place(pb.arrow.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(12, 0), new Vector2(24, 24));
                pb.arrow.enabled = false;
            }

            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(Mathf.Max(size.x, MinTarget), Mathf.Max(size.y, MinTarget));
            var le = go.AddComponent<LayoutElement>();
            le.minHeight = le.preferredHeight = rt.sizeDelta.y;
            le.minWidth = le.preferredWidth = rt.sizeDelta.x;
            return pb;
        }

        public static T Stack<T>(Transform parent, string name, float spacing, RectOffset padding, TextAnchor align)
            where T : HorizontalOrVerticalLayoutGroup
        {
            var rt = Rect(parent, name);
            var g = rt.gameObject.AddComponent<T>();
            g.spacing = spacing;
            g.padding = padding ?? new RectOffset();
            g.childAlignment = align;
            g.childControlWidth = g.childControlHeight = true;
            g.childForceExpandWidth = g.childForceExpandHeight = false;
            return g;
        }

        public static VerticalLayoutGroup VStack(Transform parent, string name, float spacing = 8, RectOffset padding = null, TextAnchor align = TextAnchor.UpperCenter)
            => Stack<VerticalLayoutGroup>(parent, name, spacing, padding, align);

        public static HorizontalLayoutGroup HStack(Transform parent, string name, float spacing = 8, RectOffset padding = null, TextAnchor align = TextAnchor.MiddleCenter)
            => Stack<HorizontalLayoutGroup>(parent, name, spacing, padding, align);

        /// <summary>Makes a layout container size itself to its content.</summary>
        public static void Fit(Component c, bool horizontal = true, bool vertical = true)
        {
            var f = c.gameObject.GetComponent<ContentSizeFitter>() ?? c.gameObject.AddComponent<ContentSizeFitter>();
            f.horizontalFit = horizontal ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;
            f.verticalFit = vertical ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;
        }

        public static LayoutElement Size(Component c, float w = -1, float h = -1, float flexW = -1, float flexH = -1)
        {
            var le = c.gameObject.GetComponent<LayoutElement>() ?? c.gameObject.AddComponent<LayoutElement>();
            if (w >= 0) le.preferredWidth = le.minWidth = w;
            if (h >= 0) le.preferredHeight = le.minHeight = h;
            if (flexW >= 0) le.flexibleWidth = flexW;
            if (flexH >= 0) le.flexibleHeight = flexH;
            return le;
        }

        public static RectOffset Pad(int all) => new RectOffset(all, all, all, all);
        public static RectOffset Pad(int h, int v) => new RectOffset(h, h, v, v);

        /// <summary>A full-screen dark scrim that also swallows taps behind modal boxes.</summary>
        public static Image Scrim(Transform parent)
        {
            var img = Fill(parent, "Scrim", Theme.scrim);
            img.raycastTarget = true;
            Stretch(img.rectTransform);
            return img;
        }
    }
}
