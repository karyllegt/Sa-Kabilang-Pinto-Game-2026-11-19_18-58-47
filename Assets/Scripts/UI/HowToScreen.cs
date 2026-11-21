using TMPro;
using UnityEngine;

namespace Skp.UI
{
    /// <summary>How to Play, in four short pages (the last lists the skills) so the body text stays at 14 px on every screen.</summary>
    public class HowToScreen : UiScreen
    {
        public const int Pages = 4;
        public PixelButton back, prev, next;
        public TextMeshProUGUI body, counter;

        public int Page { get; private set; }
        bool _wired;

        public static HowToScreen Build(Transform parent)
        {
            var th = UiKit.Theme;
            var root = ScreenParts.FullScreen(parent, "HowToScreen");
            var s = root.gameObject.AddComponent<HowToScreen>();
            s.back = ScreenParts.BackButton(root);
            ScreenParts.Heading(root, Loc.Get("how.title"));

            var col = ScreenParts.Column(root, 12, 80);
            var card = UiKit.Box(col.transform, "Card");
            var v = card.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            v.padding = UiKit.Pad(22, 18);
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandHeight = false;
            UiKit.Size(card, 760, 276);
            s.body = UiKit.Text(card.transform, "Body", Loc.Get("how.1"), th.bodySize, th.text, TextAlignmentOptions.TopLeft);
            UiKit.Size(s.body, -1, -1, 1, 1);

            var nav = UiKit.HStack(col.transform, "Pager", 24);
            s.prev = UiKit.Button(nav.transform, "Prev", Loc.Get("common.prev"), new Vector2(120, UiKit.MinTarget), th.smallSize);
            s.counter = UiKit.Text(nav.transform, "Counter", "1 / 3", th.smallSize, th.textDim, TextAlignmentOptions.Center, 80);
            s.next = UiKit.Button(nav.transform, "Next", Loc.Get("common.next"), new Vector2(120, UiKit.MinTarget), th.smallSize);
            s.firstSelected = s.next;
            root.gameObject.SetActive(false);
            return s;
        }

        public void ShowPage(int page)
        {
            if (!_wired)
            {
                _wired = true;
                prev.Clicked += () => ShowPage(Page - 1);
                next.Clicked += () => ShowPage(Page + 1);
            }
            Page = Mathf.Clamp(page, 0, Pages - 1);
            body.text = Loc.Get("how." + (Page + 1));
            counter.text = Loc.Format("how.page", Page + 1, Pages);
            prev.Interactable = Page > 0;
            next.Interactable = Page < Pages - 1;
        }

        public override void Open()
        {
            base.Open();
            ShowPage(0);
        }

        public override bool OnBack() { back.Press(); return true; }
    }
}
