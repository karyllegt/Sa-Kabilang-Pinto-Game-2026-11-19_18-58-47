using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Skp.UI
{
    /// <summary>
    /// Title: logo, tagline, the left-aligned menu box (New Journey, Continue, Level Map, Settings,
    /// How to Play, Credits) and the title art. The selected row shows the gold arrow and highlight;
    /// Continue is greyed (not hidden) without a save.
    /// </summary>
    public class TitleScreen : UiScreen
    {
        public PixelButton newJourney, continueButton, levelMap, settings, howToPlay, credits;
        public Image art;

        public PixelButton[] Rows => new[] { newJourney, continueButton, levelMap, settings, howToPlay, credits };

        public static TitleScreen Build(Transform parent)
        {
            var th = UiKit.Theme;
            var root = ScreenParts.FullScreen(parent, "TitleScreen");
            var s = root.gameObject.AddComponent<TitleScreen>();

            var col = UiKit.VStack(root, "Column", 8, null, TextAnchor.UpperLeft);
            var crt = (RectTransform)col.transform;
            UiKit.Place(crt, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(48, 10), new Vector2(420, 400));
            UiKit.Fit(col);
            var logo = UiKit.Text(col.transform, "Logo", Loc.Get("title.logo").Replace("\n", " "), 24, th.text, TextAlignmentOptions.Left);
            logo.textWrappingMode = TextWrappingModes.NoWrap;
            UiKit.Text(col.transform, "Tagline", Loc.Get("title.tagline"), th.smallSize, th.gold, TextAlignmentOptions.Left);

            var box = UiKit.Box(col.transform, "MenuBox");
            var v = box.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(14, 14, 14, 14);
            v.spacing = 0;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            UiKit.Size(box, 340);
            UiKit.Fit(v, false, true);

            PixelButton Row(string key) => UiKit.Button(box.transform, key, Loc.Get(key), new Vector2(312, UiKit.MinTarget - 2), th.bodySize, arrow: true);
            s.newJourney = Row("title.new");
            s.continueButton = Row("title.continue");
            s.levelMap = Row("title.map");
            s.settings = Row("title.settings");
            s.howToPlay = Row("title.how");
            s.credits = Row("title.credits");

            s.art = UiKit.Picture(root, "TitleArt", th.titleArt, new Vector2(256, 256));
            UiKit.Place(s.art.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-56, 10), new Vector2(256, 256));
            Object.DestroyImmediate(s.art.GetComponent<LayoutElement>());

            var foot = UiKit.Label(root, "Footer", Loc.Get("title.footer"), th.tinySize, th.textDim, TextAlignmentOptions.Center, false);
            UiKit.Place(foot.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 10), new Vector2(620, 16));

            s.firstSelected = s.newJourney;
            return s;
        }

        /// <summary>Greys Continue when there is nothing to continue, and starts the selection on the right row.</summary>
        public void Bind(bool canContinue)
        {
            continueButton.Interactable = canContinue;
            firstSelected = canContinue ? continueButton : newJourney;
        }
    }
}
