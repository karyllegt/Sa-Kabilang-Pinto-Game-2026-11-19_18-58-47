using System;
using Skp.Save;
using Skp.Player;
using Skp.Skills;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Skp.UI
{
    /// <summary>
    /// On-screen buttons (Input System OnScreenButtons driving a virtual gamepad, so they feed the same
    /// Gameplay actions as keyboard and gamepad). Thumb layout (docs/04 known layout issue 1): left thumb
    /// moves; right thumb has JUMP in the corner with DASH and WARD beside it on the bottom row; the second row
    /// holds BOLT (above DASH) and VEIL (above WARD) once learned, and DOOR (above JUMP) only when Tala stands at
    /// a door, so the play area stays clear. Two rows at most. The whole
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
        [Tooltip("Skill buttons, shown once the skill is learned.")]
        public GameObject veil, bolt;
        public bool forceShow;

        LanternTouchFace _dashFace, _wardFace, _veilFace, _boltFace;

        internal void CacheLanternFaces()
        {
            foreach (var slot in slots)
            {
                if (slot.rect == null || !slot.rect.TryGetComponent(out LanternTouchFace face)) continue;
                switch (slot.rect.name)
                {
                    case "Dash": _dashFace = face; break;
                    case "Ward": _wardFace = face; break;
                    case "Veil": _veilFace = face; break;
                    case "Bolt": _boltFace = face; break;
                }
            }
        }

        internal void PaintLanternAvailability(PlayerSim sim)
        {
            if (_dashFace == null) return;
            var tuning = sim.SkillTuning ?? SkillTuning.Default;
            int dashCost = sim.Has(SkillIds.Swift) ? tuning.swiftDashFlameCost : sim.Tuning.dashFlameCost;
            int boltCost = sim.Has(SkillIds.TripleBolt) ? tuning.tripleBoltFlameCost : tuning.boltFlameCost;
            _dashFace.SetAvailable(sim.DashTicks == 0 && sim.DashCooldown == 0 && sim.Flame >= dashCost);
            _wardFace?.SetAvailable(sim.WardCooldown == 0);
            _veilFace?.SetAvailable(sim.VeilCooldown == 0);
            _boltFace?.SetAvailable(sim.BoltCooldown == 0 && sim.Flame >= boltCost);
        }

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

        /// <summary>Shows VEIL and BOLT only once they are learned.</summary>
        public void SetSkills(bool hasVeil, bool hasBolt)
        {
            if (veil != null && veil.activeSelf != hasVeil) veil.SetActive(hasVeil);
            if (bolt != null && bolt.activeSelf != hasBolt) bolt.SetActive(hasBolt);
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
