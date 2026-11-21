using TMPro;
using UnityEngine;

namespace Skp.UI
{
    /// <summary>Pause box: Resume, Settings, To Lantern, Restart Level, Save and Quit. Back resumes.</summary>
    public class PauseScreen : UiScreen
    {
        public PixelButton resume, settings, toLantern, restart, quit;

        public static PauseScreen Build(Transform parent)
        {
            var th = UiKit.Theme;
            var root = ScreenParts.FullScreen(parent, "PauseScreen");
            var s = root.gameObject.AddComponent<PauseScreen>();
            var v = ScreenParts.ModalBox(root, 300, 6);
            UiKit.Text(v.transform, "Title", Loc.Get("pause.title"), th.headingSize, th.gold);
            PixelButton B(string key) => UiKit.Button(v.transform, key, Loc.Get(key), new Vector2(252, UiKit.MinTarget));
            s.resume = B("pause.resume");
            s.settings = B("pause.settings");
            s.toLantern = B("pause.lantern");
            s.restart = B("pause.restart");
            s.quit = B("pause.quit");
            s.firstSelected = s.resume;
            root.gameObject.SetActive(false);
            return s;
        }

        public override bool OnBack() { resume.Press(); return true; }
    }
}
