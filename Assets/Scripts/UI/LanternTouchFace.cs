using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Skp.UI
{
    // Visual feedback for the existing Input System buttons. Pointer input still goes
    // directly to OnScreenButton, including taps while a cooldown is running.
    public sealed class LanternTouchFace : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        Image _face, _icon;
        TMP_Text _label;
        UiTheme _theme;
        Color _accent;
        bool _available = true, _pressed;

        public void Initialize(Image face, Image icon, TMP_Text label, UiTheme theme, Color accent)
        {
            _face = face;
            _icon = icon;
            _label = label;
            _theme = theme;
            _accent = accent;
            Paint();
        }

        public void SetAvailable(bool available)
        {
            if (_available == available) return;
            _available = available;
            Paint();
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left) return;
            _pressed = true;
            Paint();
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left) return;
            _pressed = false;
            Paint();
        }

        void OnDisable()
        {
            _pressed = false;
            Paint();
        }

        void Paint()
        {
            if (_face == null) return;
            _face.color = !_available ? _theme.disabledFace : _pressed ? _theme.pressedFace : Color.white;
            _icon.color = _available ? _accent : _theme.textDisabled;
            if (_label != null) _label.color = _available ? _theme.text : _theme.textDisabled;
        }
    }
}
