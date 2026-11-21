using TMPro;
using UnityEngine;

namespace Skp.UI
{
    /// <summary>Game Over: the night takes Tala; Try Again or Map. Back goes to the map.</summary>
    public class GameOverScreen : UiScreen
    {
        public TextMeshProUGUI stats;
        public PixelButton retry, map;

        public static GameOverScreen Build(Transform parent)
        {
            var th = UiKit.Theme;
            var root = ScreenParts.FullScreen(parent, "GameOverScreen");
            var s = root.gameObject.AddComponent<GameOverScreen>();
            var v = ScreenParts.ModalBox(root, 520, 12);
            UiKit.Text(v.transform, "Title", Loc.Get("over.title"), th.headingSize, th.danger);
            s.stats = UiKit.Text(v.transform, "Stats", "", th.bodySize, th.text, TextAlignmentOptions.Center, 460);
            var row = UiKit.HStack(v.transform, "Buttons", 16);
            s.retry = UiKit.Button(row.transform, "Retry", Loc.Get("over.retry"), new Vector2(180, UiKit.MinTarget));
            s.map = UiKit.Button(row.transform, "Map", Loc.Get("over.map"), new Vector2(150, UiKit.MinTarget));
            s.firstSelected = s.retry;
            root.gameObject.SetActive(false);
            return s;
        }

        public override bool OnBack() { map.Press(); return true; }
    }
}
