using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Skp.UI
{
    /// <summary>
    /// The level map's overlay: top bar (Back, act name, difficulty toggle, Talk) and a short bottom
    /// info panel (level number and name, act and state, ENTER/REPLAY). Taps on the map itself go
    /// through <see cref="TapArea"/> to the <see cref="MapController"/>. The info panel is kept short
    /// (docs/04 known layout issue 3); node numbers sit above the nodes.
    /// </summary>
    public class MapScreen : UiScreen
    {
        public const float InfoHeight = 84f;

        public PixelButton back, difficulty, talk, enter;
        public TextMeshProUGUI act, levelName, levelSub;
        public RectTransform topBar, infoPanel;
        public MapTapArea tapArea;

        public Action BackRequested;

        public static MapScreen Build(Transform parent)
        {
            var th = UiKit.Theme;
            var root = ScreenParts.FullScreen(parent, "MapScreen");
            var s = root.gameObject.AddComponent<MapScreen>();

            // tap catcher behind the bars (the map itself)
            var tap = UiKit.Fill(root, "MapTapArea", new Color(0, 0, 0, 0));
            UiKit.Stretch(tap.rectTransform);
            tap.raycastTarget = true;
            tap.transform.SetAsFirstSibling();
            s.tapArea = tap.gameObject.AddComponent<MapTapArea>();

            // top bar
            var bar = UiKit.Rect(root, "TopBar");
            bar.anchorMin = new Vector2(0, 1);
            bar.anchorMax = new Vector2(1, 1);
            bar.pivot = new Vector2(0.5f, 1);
            bar.offsetMin = new Vector2(12, -12 - UiKit.MinTarget);
            bar.offsetMax = new Vector2(-12, -12);
            s.topBar = bar;
            s.back = UiKit.Button(bar, "Back", Loc.Get("common.back"), new Vector2(104, UiKit.MinTarget), th.smallSize);
            UiKit.Place((RectTransform)s.back.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, ((RectTransform)s.back.transform).sizeDelta);

            var actBox = UiKit.Box(bar, "ActPanel", th.buttonBoxScale);
            UiKit.Place(actBox.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-40, 0), new Vector2(360, 44));
            actBox.raycastTarget = false;
            s.act = UiKit.Label(actBox.transform, "Act", "ACT I - THE HOUSE WAKES", th.smallSize, th.gold, TextAlignmentOptions.Center, false);
            UiKit.Stretch(s.act.rectTransform, 8, 4, 8, 4);

            s.talk = UiKit.Button(bar, "Talk", Loc.Get("map.talk"), new Vector2(96, UiKit.MinTarget), th.smallSize);
            UiKit.Place((RectTransform)s.talk.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), Vector2.zero, ((RectTransform)s.talk.transform).sizeDelta);
            s.difficulty = UiKit.Button(bar, "Difficulty", "STANDARD", new Vector2(148, UiKit.MinTarget), th.smallSize);
            UiKit.Place((RectTransform)s.difficulty.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-108, 0), ((RectTransform)s.difficulty.transform).sizeDelta);

            // bottom info panel
            var info = UiKit.Box(root, "InfoPanel");
            s.infoPanel = info.rectTransform;
            s.infoPanel.anchorMin = new Vector2(0, 0);
            s.infoPanel.anchorMax = new Vector2(1, 0);
            s.infoPanel.pivot = new Vector2(0.5f, 0);
            s.infoPanel.offsetMin = new Vector2(12, 8);
            s.infoPanel.offsetMax = new Vector2(-12, 8 + InfoHeight);
            info.raycastTarget = true;
            var texts = UiKit.VStack(info.transform, "Texts", 6, null, TextAnchor.MiddleLeft);
            UiKit.Stretch((RectTransform)texts.transform, 22, 10, 190, 10);
            s.levelName = UiKit.Text(texts.transform, "Name", "1. FIESTA NIGHT", th.bodySize + 2, th.goldBright, TextAlignmentOptions.Left);
            s.levelName.textWrappingMode = TextWrappingModes.NoWrap;
            s.levelSub = UiKit.Text(texts.transform, "Sub", "ACT I - READY", th.tinySize, th.textDim, TextAlignmentOptions.Left);
            s.levelSub.textWrappingMode = TextWrappingModes.NoWrap;
            s.enter = UiKit.Button(info.transform, "Enter", Loc.Get("map.enter"), new Vector2(156, UiKit.MinTarget), th.bodySize);
            UiKit.Place((RectTransform)s.enter.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-16, 0), ((RectTransform)s.enter.transform).sizeDelta);

            s.firstSelected = null; // arrows walk the map; Up reaches the top bar
            return s;
        }

        public override bool OnBack()
        {
            BackRequested?.Invoke();
            return true;
        }
    }
}
