using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Skp.UI
{
    public enum Speaker { Player, Lola, Clue, Anino, Boss, Skill }

    /// <summary>One page of a dialog.</summary>
    public struct DialogLine
    {
        public Speaker Who;
        public string Sub, Name, Text;
        /// <summary>Label of the button on the last line (BEGIN, CONTINUE); NEXT is used before that.</summary>
        public string Button;
        /// <summary>Overrides the speaker portrait (the skill card shows the skill icon).</summary>
        public Sprite Portrait;
        /// <summary>Once the whole line is visible, taps are ignored for this long so a fast tapper cannot skip it.</summary>
        public float HoldSeconds;
        /// <summary>Color of the speaker name (default gold).</summary>
        public Color? NameColor;
    }

    /// <summary>
    /// The portrait dialog box used for Lola, level intros, letters and creature lines: bottom of the
    /// screen, square portrait on the left with the NEXT/BEGIN button under it, location subtitle in teal,
    /// speaker in gold, body with a typewriter. Tap anywhere on the box (or Submit) first completes the
    /// line, then advances; Back does the same. Never reaches the top of the screen, so it cannot cover the
    /// pause button.
    /// </summary>
    public class DialogBox : UiScreen, IPointerClickHandler
    {
        public const float Height = 196f;
        public const float CharSeconds = 0.022f;

        public Image portrait;
        public TextMeshProUGUI sub, speaker, body;
        public PixelButton next;
        public RectTransform box;
        internal GameObject coveredCard;

        public bool IsTyping { get; private set; }
        public int LineIndex { get; private set; }
        public int LineCount => _lines.Count;
        public DialogLine CurrentLine => _lines.Count > 0 ? _lines[LineIndex] : default;

        readonly List<DialogLine> _lines = new List<DialogLine>();
        Action _done;
        float _typeT, _completedAt;
        bool _wired;
        bool _isPlayerBoy;

        /// <summary>Raised when the last line is dismissed (after the completion callback).</summary>
        public event Action Finished;
        /// <summary>Raised when a line starts showing (its index).</summary>
        public event Action<int> LineShown;

        public static DialogBox Build(Transform parent)
        {
            var th = UiKit.Theme;
            var root = UiKit.Rect(parent, "DialogBox");
            UiKit.Stretch(root);
            var d = root.gameObject.AddComponent<DialogBox>();

            var frame = UiKit.Box(root, "Box");
            d.box = frame.rectTransform;
            d.box.anchorMin = new Vector2(0, 0);
            d.box.anchorMax = new Vector2(1, 0);
            d.box.pivot = new Vector2(0.5f, 0);
            d.box.offsetMin = new Vector2(12, 10);
            d.box.offsetMax = new Vector2(-12, 10 + Height);
            frame.raycastTarget = true;

            var row = UiKit.HStack(frame.transform, "Row", 18, UiKit.Pad(18, 16), TextAnchor.UpperLeft);
            UiKit.Stretch((RectTransform)row.transform);
            row.childForceExpandHeight = false;

            var left = UiKit.VStack(row.transform, "Left", 8, null, TextAnchor.UpperCenter);
            UiKit.Size(left, 112, -1, 0);
            var pf = UiKit.Box(left.transform, "PortraitFrame", th.buttonBoxScale);
            UiKit.Size(pf, 112, 104);
            d.portrait = UiKit.Picture(pf.transform, "Portrait", th.portraitLola, new Vector2(88, 88));
            UiKit.Stretch(d.portrait.rectTransform, 8, 8, 8, 8);
            d.portrait.preserveAspect = true;
            d.next = UiKit.Button(left.transform, "Next", Loc.Get("common.next"), new Vector2(112, UiKit.MinTarget), th.smallSize);

            var col = UiKit.VStack(row.transform, "Text", 6, null, TextAnchor.UpperLeft);
            UiKit.Size(col, -1, -1, 1, 1);
            d.sub = UiKit.Text(col.transform, "Sub", "THE LANTERN PATH", th.tinySize, th.teal, TextAlignmentOptions.Left);
            d.speaker = UiKit.Text(col.transform, "Speaker", "LOLA", th.headingSize - 4, th.gold, TextAlignmentOptions.Left);
            d.body = UiKit.Text(col.transform, "Body", "", th.bodySize, th.text, TextAlignmentOptions.TopLeft);
            UiKit.Size(d.body, -1, -1, 1, 1);

            d.firstSelected = d.next;
            root.gameObject.SetActive(false);
            return d;
        }

        public override void Open()
        {
            if (coveredCard != null) coveredCard.SetActive(false);
            base.Open();
        }

        public override void Close()
        {
            base.Close();
            if (coveredCard != null) coveredCard.SetActive(true);
        }

        void Wire()
        {
            if (_wired || next == null) return;
            _wired = true;
            next.Clicked += Advance;
        }

        /// <summary>Shows the lines; <paramref name="done"/> runs when the last one is dismissed.</summary>
        public void Show(IList<DialogLine> lines, Action done, bool playerIsBoy = false)
        {
            Wire();
            _lines.Clear();
            _lines.AddRange(lines);
            _done = done;
            _isPlayerBoy = playerIsBoy;
            LineIndex = 0;
            Open();
            Paint();
        }

        void Paint()
        {
            var l = _lines[LineIndex];
            var th = UiKit.Theme;
            portrait.sprite = l.Portrait != null ? l.Portrait : PortraitFor(l.Who, th);
            sub.text = l.Sub ?? "";
            speaker.text = l.Name ?? "";
            speaker.color = l.NameColor ?? th.gold;
            body.text = l.Text ?? "";
            body.maxVisibleCharacters = 0;
            next.Text = LineIndex < _lines.Count - 1 ? Loc.Get("common.next") : (string.IsNullOrEmpty(l.Button) ? Loc.Get("common.close") : l.Button);
            _typeT = 0;
            IsTyping = true;
            LineShown?.Invoke(LineIndex);
        }

        Sprite PortraitFor(Speaker who, UiTheme th)
        {
            switch (who)
            {
                case Speaker.Player: return _isPlayerBoy ? th.portraitBoy : th.portraitGirl;
                case Speaker.Clue: return th.portraitClue;
                case Speaker.Anino: return th.portraitAnino;
                case Speaker.Boss: return th.portraitBoss;
                default: return th.portraitLola;
            }
        }

        void Update()
        {
            if (!IsTyping) return;
            _typeT += Time.unscaledDeltaTime;
            int n = Mathf.FloorToInt(_typeT / CharSeconds);
            int len = body.text.Length;
            if (n >= len) CompleteLine();
            else body.maxVisibleCharacters = n;
        }

        /// <summary>Shows the whole current line at once.</summary>
        public void CompleteLine()
        {
            if (IsTyping) _completedAt = Time.unscaledTime;
            IsTyping = false;
            if (body != null) body.maxVisibleCharacters = 99999;
        }

        /// <summary>Tap, Submit or Back: finish the typewriter first, then go to the next line or close.</summary>
        public void Advance()
        {
            if (!IsOpen || UiGate.Blocked) return;
            if (IsTyping) { CompleteLine(); return; }
            if (_lines.Count > 0 && Time.unscaledTime - _completedAt < _lines[LineIndex].HoldSeconds) return; // still holding this line
            if (LineIndex < _lines.Count - 1)
            {
                LineIndex++;
                Paint();
                return;
            }
            var done = _done;
            _done = null;
            Close();
            done?.Invoke();
            Finished?.Invoke();
        }

        public override bool OnBack() { Advance(); return true; }

        public void OnPointerClick(PointerEventData e) => Advance();
    }
}
