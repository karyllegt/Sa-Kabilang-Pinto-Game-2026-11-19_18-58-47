using System;
using Skp.Save;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Skp.UI
{
    /// <summary>
    /// On-screen buttons (Input System OnScreenButtons driving a virtual gamepad, so they feed the same
    /// Gameplay actions as keyboard and gamepad). Thumb layout (docs/04 known layout issue 1): left thumb
    /// moves; right thumb has JUMP in the corner with DASH beside it and WARD above DASH; DOOR appears above
    /// JUMP only when Tala stands at a door, so the play area stays clear. Two rows at most. The whole
    /// layout mirrors for left-handed players. Shown only during play, per the Touch controls setting.
    /// </summary>
    public class TouchControls : MonoBehaviour
    {
        [Serializable]
        public struct Slot
        {
            public RectTransform rect;
            public Vector2 anchor, position;
        }

        [Tooltip("Root holding the buttons.")]
        public GameObject buttons;
        public Slot[] slots = new Slot[0];
        [Tooltip("The contextual door button.")]
        public GameObject door;
        public bool forceShow;

        TouchMode _mode = TouchMode.Auto;
        bool _left;
        bool _playing = true;

        public static bool TouchAvailable => Touchscreen.current != null;

        public bool Wanted => forceShow || _mode == TouchMode.On || (_mode == TouchMode.Auto && TouchAvailable);
        public bool LeftHanded => _left;

        void OnEnable() => Refresh();

        public void Apply(GameSettings s)
        {
            _mode = s.touch;
            _left = s.leftHanded;
            Layout();
            Refresh();
        }

        /// <summary>Places every button for the current hand: mirrored anchors and offsets for left-handed.</summary>
        public void Layout()
        {
            foreach (var s in slots)
            {
                if (s.rect == null) continue;
                var a = _left ? new Vector2(1 - s.anchor.x, s.anchor.y) : s.anchor;
                s.rect.anchorMin = s.rect.anchorMax = a;
                s.rect.anchoredPosition = _left ? new Vector2(-s.position.x, s.position.y) : s.position;
            }
        }

        public void SetPlaying(bool playing, bool nearDoor)
        {
            if (door != null && door.activeSelf != nearDoor) door.SetActive(nearDoor);
            if (_playing == playing) return;
            _playing = playing;
            Refresh();
        }

        public void Refresh()
        {
            if (buttons != null) buttons.SetActive(Wanted && _playing);
        }
    }
}
