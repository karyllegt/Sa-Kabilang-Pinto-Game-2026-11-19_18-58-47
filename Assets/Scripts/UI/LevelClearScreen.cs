using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Skp.UI
{
    /// <summary>Level Clear box: title, level name, Time / Score / Fragments tiles, the letter as a quote, Next Level, Replay, Map.</summary>
    public class LevelClearScreen : UiScreen
    {
        public TextMeshProUGUI title, levelName, time, score, fragments, clueLabel, clue;
        public GameObject clueGroup;
        public PixelButton next, replay, map;

        public static LevelClearScreen Build(Transform parent)
        {
            var th = UiKit.Theme;
            var root = ScreenParts.FullScreen(parent, "LevelClearScreen");
            var s = root.gameObject.AddComponent<LevelClearScreen>();
            var v = ScreenParts.ModalBox(root, 640, 8);
            s.title = UiKit.Text(v.transform, "Title", "LEVEL 1 CLEAR", th.headingSize, th.goldBright);
            s.levelName = UiKit.Text(v.transform, "Name", "Fiesta Night", th.bodySize, th.teal);
            var tiles = UiKit.HStack(v.transform, "Stats", 12);
            TextMeshProUGUI Tile(string key)
            {
                var b = UiKit.Box(tiles.transform, key, th.buttonBoxScale);
                var tv = b.gameObject.AddComponent<VerticalLayoutGroup>();
                tv.padding = UiKit.Pad(8, 10);
                tv.spacing = 6;
                tv.childControlWidth = tv.childControlHeight = true;
                tv.childAlignment = TextAnchor.MiddleCenter;
                UiKit.Size(b, 180);
                UiKit.Text(b.transform, "Label", Loc.Get(key), th.tinySize, th.textDim);
                return UiKit.Text(b.transform, "Value", "0", th.headingSize - 4, th.text);
            }
            s.time = Tile("clear.time");
            s.score = Tile("clear.score");
            s.fragments = Tile("clear.fragments");
            var cg = UiKit.VStack(v.transform, "ClueGroup", 6);
            s.clueGroup = cg.gameObject;
            s.clueLabel = UiKit.Text(cg.transform, "Label", Loc.Get("clear.clue"), th.tinySize, th.textDim);
            s.clue = UiKit.Text(cg.transform, "Text", "", th.bodySize, th.text, TextAlignmentOptions.Center, 580);
            var buttons = UiKit.HStack(v.transform, "Buttons", 12, UiKit.Pad(0, 4));
            s.next = UiKit.Button(buttons.transform, "Next", Loc.Get("clear.next"), new Vector2(176, UiKit.MinTarget));
            s.replay = UiKit.Button(buttons.transform, "Replay", Loc.Get("clear.replay"), new Vector2(150, UiKit.MinTarget));
            s.map = UiKit.Button(buttons.transform, "Map", Loc.Get("clear.map"), new Vector2(150, UiKit.MinTarget));
            s.firstSelected = s.next;
            root.gameObject.SetActive(false);
            return s;
        }

        public void Fill(string titleText, string sub, string timeText, string scoreText, string fragText, string clueText, string nextText, string mapText)
        {
            title.text = titleText;
            levelName.text = sub;
            time.text = timeText;
            score.text = scoreText;
            fragments.text = fragText;
            clueGroup.SetActive(!string.IsNullOrEmpty(clueText));
            clue.text = clueText ?? "";
            next.Text = nextText;
            map.Text = mapText;
        }

        public override bool OnBack() { map.Press(); return true; }
    }
}
