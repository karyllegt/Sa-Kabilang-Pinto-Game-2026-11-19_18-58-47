using TMPro;
using UnityEngine;

namespace Skp.UI
{
    /// <summary>Difficulty select: Story, Standard, Nightmare, and Endless Nightmare (locked until the story is cleared).</summary>
    public class DifficultyScreen : UiScreen
    {
        public PixelButton back, story, standard, nightmare, endless;
        public TextMeshProUGUI endlessDesc;

        public static DifficultyScreen Build(Transform parent)
        {
            var th = UiKit.Theme;
            var root = ScreenParts.FullScreen(parent, "DifficultyScreen");
            var s = root.gameObject.AddComponent<DifficultyScreen>();
            s.back = ScreenParts.BackButton(root);
            ScreenParts.Heading(root, Loc.Get("diff.title"));

            var col = ScreenParts.Column(root, 12, 84);
            PixelButton Row(string key, out TextMeshProUGUI desc)
            {
                var r = UiKit.HStack(col.transform, key, 18, null, TextAnchor.MiddleLeft);
                var b = UiKit.Button(r.transform, "Button", Loc.Get(key), new Vector2(272, UiKit.MinTarget));
                desc = UiKit.Text(r.transform, "Desc", Loc.Get(key + ".desc"), th.smallSize, th.textDim, TextAlignmentOptions.MidlineLeft, 440);
                UiKit.Size(desc, 440, 60); // fixed rows: two lines of description beside each button
                return b;
            }
            s.story = Row("diff.story", out _);
            s.standard = Row("diff.standard", out _);
            s.nightmare = Row("diff.nightmare", out _);
            s.endless = Row("diff.endless", out s.endlessDesc);
            s.firstSelected = s.standard;
            root.gameObject.SetActive(false);
            return s;
        }

        public void Bind(bool storyCleared, int best)
        {
            endless.Interactable = storyCleared;
            endlessDesc.text = storyCleared ? Loc.Format("diff.endless.desc", best) : Loc.Get("diff.endless.locked");
        }

        public override bool OnBack() { back.Press(); return true; }
    }
}
