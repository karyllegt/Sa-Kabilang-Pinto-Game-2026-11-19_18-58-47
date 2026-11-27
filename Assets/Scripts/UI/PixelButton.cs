using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Skp.UI
{
    /// <summary>
    /// The one button style: a PixelBox frame, a highlight fill and a gold arrow shown while selected
    /// (keyboard, gamepad or hover), and a greyed label when disabled. Clicks are dropped while
    /// <see cref="UiGate.Blocked"/> (wipes), so double taps and key spam never fire twice.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class PixelButton : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerDownHandler, IPointerUpHandler
    {
        public Button button;
        public Image frame;
        public Image highlight;
        public TextMeshProUGUI label;
        [Tooltip("Optional gold arrow (menu rows).")]
        public TextMeshProUGUI arrow;
        [Tooltip("Sound cue name for M8 audio (select, locked, door).")]
        public string cue = "select";

        public event Action Clicked;
        /// <summary>Raised when a disabled button is pressed (e.g. a locked map node plays the locked cue).</summary>
        public event Action ClickedWhileDisabled;

        bool _interactable = true;
        bool _wired;
        bool _pressed;
        UiTheme _lanternTheme;
        Color _restingFace;
        internal Image selectionMark;

        public void UseLanternFace(UiTheme theme)
        {
            _lanternTheme = theme;
            _restingFace = frame != null ? frame.color : Color.white;
            Refresh();
        }

        public bool Interactable
        {
            get => _interactable;
            set { _interactable = value; Refresh(); }
        }

        public bool Highlighted { get; private set; }

        public string Text
        {
            get => label != null ? label.text : "";
            set { if (label != null) label.text = value; }
        }

        void Awake() => Wire();

        void Wire()
        {
            if (_wired) return;
            _wired = true;
            if (button == null) button = GetComponent<Button>();
            button.onClick.AddListener(Press);
            Refresh();
        }

        /// <summary>What a tap, click or Submit does. Public so tests and keyboard shortcuts use the same path.</summary>
        public void Press()
        {
            if (UiGate.Blocked) return;
            if (!_interactable) { UiAudio.Cue("locked"); ClickedWhileDisabled?.Invoke(); return; }
            UiAudio.Cue(cue);
            Clicked?.Invoke();
        }

        public void SetHighlighted(bool on)
        {
            Highlighted = on;
            if (highlight != null) highlight.enabled = on;
            if (arrow != null) arrow.enabled = on;
            if (_lanternTheme != null) Refresh();
        }

        void Refresh()
        {
            var th = UiTheme.Load();
            if (label != null) label.color = _interactable ? th.text : th.textDisabled;
            if (_lanternTheme != null)
            {
                th = _lanternTheme;
                if (label != null) label.color = _interactable ? th.text : th.textDisabled;
                if (frame != null) frame.color = _restingFace * (!_interactable ? th.disabledFace :
                    _pressed ? th.pressedFace : Color.white);
                if (highlight != null)
                {
                    highlight.enabled = _interactable && (Highlighted || _pressed);
                    highlight.color = _pressed ? new Color32(0x65, 0x42, 0x32, 0xff) : th.highlight;
                }
                if (arrow != null) arrow.enabled = Highlighted && _interactable;
                if (selectionMark != null) selectionMark.enabled = Highlighted && _interactable;
            }
            // disabled buttons stay selectable for navigation (Continue is greyed, not hidden)
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (_lanternTheme == null || e.button != PointerEventData.InputButton.Left) return;
            _pressed = true;
            Refresh();
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (!_pressed || e.button != PointerEventData.InputButton.Left) return;
            _pressed = false;
            Refresh();
        }

        void OnDisable()
        {
            if (!_pressed) return;
            _pressed = false;
            Refresh();
        }

        public void OnSelect(BaseEventData e) => SetHighlighted(true);
        public void OnDeselect(BaseEventData e) => SetHighlighted(false);

        public void OnPointerEnter(PointerEventData e)
        {
            if (_interactable && EventSystem.current != null && !UiGate.Blocked)
                EventSystem.current.SetSelectedGameObject(gameObject);
        }
    }

    /// <summary>Global UI input gate: true while a wipe or scene load runs.</summary>
    public static class UiGate
    {
        public static Func<bool> BlockedWhen;
        public static bool Blocked => BlockedWhen != null && BlockedWhen();
    }

    /// <summary>Sound cue hook; M8 plugs the audio service in. Counts cues so tests can check them.</summary>
    public static class UiAudio
    {
        public static Action<string> Play;
        public static string LastCue { get; private set; }
        public static int CueCount { get; private set; }

        public static void Cue(string name)
        {
            LastCue = name;
            CueCount++;
            Play?.Invoke(name);
        }
    }
}
