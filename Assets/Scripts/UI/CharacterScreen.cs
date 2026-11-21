using TMPro;
using UnityEngine;

namespace Skp.UI
{
    /// <summary>Character select: Tala as a boy or a girl (same skills), with Back.</summary>
    public class CharacterScreen : UiScreen
    {
        public PixelButton back, boy, girl;

        public static CharacterScreen Build(Transform parent)
        {
            var th = UiKit.Theme;
            var root = ScreenParts.FullScreen(parent, "CharacterScreen");
            var s = root.gameObject.AddComponent<CharacterScreen>();
            s.back = ScreenParts.BackButton(root);

            var col = ScreenParts.Column(root, 12, 64);
            UiKit.Text(col.transform, "Title", Loc.Get("chars.title"), th.headingSize, th.text);
            UiKit.Text(col.transform, "Sub", Loc.Get("chars.sub"), th.smallSize, th.textDim);
            var row = UiKit.HStack(col.transform, "Cards", 96, UiKit.Pad(0, 8), TextAnchor.MiddleCenter);
            PixelButton Card(Sprite art, string key)
            {
                var c = UiKit.VStack(row.transform, key, 12, null, TextAnchor.UpperCenter);
                UiKit.Picture(c.transform, "Art", art, new Vector2(128, 144));
                return UiKit.Button(c.transform, "Button", Loc.Get(key), new Vector2(148, UiKit.MinTarget));
            }
            s.boy = Card(th.cardBoy, "chars.boy");
            s.girl = Card(th.cardGirl, "chars.girl");
            s.firstSelected = s.girl;
            root.gameObject.SetActive(false);
            return s;
        }

        public override bool OnBack() { back.Press(); return true; }
    }
}
