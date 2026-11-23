using TMPro;
using UnityEngine;

namespace Skp.UI
{
    /// <summary>Credits: concept, folklore, font license.</summary>
    public class CreditsScreen : UiScreen
    {
        public PixelButton back;

        public static CreditsScreen Build(Transform parent)
        {
            var th = UiKit.Theme;
            var root = ScreenParts.FullScreen(parent, "CreditsScreen");
            var s = root.gameObject.AddComponent<CreditsScreen>();
            s.back = ScreenParts.BackButton(root);
            ScreenParts.Heading(root, Loc.Get("credits.title"));
            var col = ScreenParts.Column(root, 12, 84);
            var card = UiKit.Box(col.transform, "Card");
            var v = card.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            v.padding = UiKit.Pad(22, 18);
            v.childControlWidth = v.childControlHeight = true;
            UiKit.Size(card, 760);
            UiKit.Text(card.transform, "Body", Loc.Get("credits.body"), th.smallSize, th.text, TextAlignmentOptions.TopLeft);
            s.firstSelected = s.back;
            root.gameObject.SetActive(false);
            return s;
        }

        public override bool OnBack() { back.Press(); return true; }
    }
}
