using TMPro;
using UnityEngine;

namespace Skp.UI
{
    /// <summary>The ending after the last bell: Dawn, the ending text, the run's stats, Main Menu.</summary>
    public class EndingScreen : UiScreen
    {
        public TextMeshProUGUI body, stats;
        public PixelButton menu;

        public static EndingScreen Build(Transform parent)
        {
            var th = UiKit.Theme;
            var root = ScreenParts.FullScreen(parent, "EndingScreen");
            var s = root.gameObject.AddComponent<EndingScreen>();
            var v = ScreenParts.ModalBox(root, 700, 14);
            UiKit.Text(v.transform, "Title", Loc.Get("ending.title"), th.titleSize, th.goldBright);
            s.body = UiKit.Text(v.transform, "Body", Skp.Levels.LevelCatalog.Ending, th.bodySize, th.text, TextAlignmentOptions.Center, 640);
            s.stats = UiKit.Text(v.transform, "Stats", "", th.smallSize, th.textDim, TextAlignmentOptions.Center, 640);
            s.menu = UiKit.Button(v.transform, "Menu", Loc.Get("ending.menu"), new Vector2(220, UiKit.MinTarget));
            s.firstSelected = s.menu;
            root.gameObject.SetActive(false);
            return s;
        }

        public override bool OnBack() { menu.Press(); return true; }
    }
}
