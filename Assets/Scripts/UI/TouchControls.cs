using UnityEngine;
using UnityEngine.InputSystem;

namespace Skp.UI
{
    /// <summary>
    /// Shows the on-screen buttons only on touch devices (or when forced for testing).
    /// The buttons are Input System OnScreenButtons that drive a virtual gamepad, so they feed
    /// the same Gameplay actions as keyboard and gamepad. Final thumb layout and the
    /// left-handed mirror come in M5/M6 (docs/04 known layout issue 1).
    /// </summary>
    public class TouchControls : MonoBehaviour
    {
        [Tooltip("Root holding the buttons; hidden when no touchscreen is present.")]
        public GameObject buttons;
        public bool forceShow;

        public static bool TouchAvailable => Touchscreen.current != null;

        void OnEnable() => Refresh();

        public void Refresh()
        {
            if (buttons != null) buttons.SetActive(forceShow || TouchAvailable);
        }
    }
}
