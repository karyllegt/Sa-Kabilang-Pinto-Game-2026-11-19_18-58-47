using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Skp.Core
{
    /// <summary>
    /// The only gameplay input entry point. Reads the "Gameplay" map of the project-wide
    /// actions (Assets/Settings/Input/SkpControls.inputactions): keyboard, gamepad and the
    /// on-screen touch buttons all feed the same actions. No UnityEngine.Input calls anywhere.
    /// While <see cref="Blocked"/> is true (wipes, dialogs), every query returns "not pressed".
    /// </summary>
    public class InputReader
    {
        public const string GameplayMap = "Gameplay";
        public const string DebugMap = "Debug";
        public const string UiMap = "UI";
        public static readonly string[] GameplayActions = { "Move", "Jump", "Dash", "Ward", "Veil", "Bolt", "Interact", "Pause" };

        readonly InputActionMap _map;
        readonly InputAction _move, _jump, _dash, _ward, _veil, _bolt, _interact, _pause, _debugToggle;
        readonly InputAction _uiNavigate, _uiSubmit, _uiCancel;

        /// <summary>External blocks (e.g. the scene loader while a wipe runs).</summary>
        public Func<bool> BlockedWhen { get; set; }
        public bool Blocked => BlockedWhen != null && BlockedWhen();

        public InputActionAsset Asset { get; }

        public InputReader(InputActionAsset asset)
        {
            Asset = asset ?? throw new ArgumentNullException(nameof(asset));
            _map = asset.FindActionMap(GameplayMap, true);
            _move = _map.FindAction("Move", true);
            _jump = _map.FindAction("Jump", true);
            _dash = _map.FindAction("Dash", true);
            _ward = _map.FindAction("Ward", true);
            _veil = _map.FindAction("Veil", true);
            _bolt = _map.FindAction("Bolt", true);
            _interact = _map.FindAction("Interact", true);
            _pause = _map.FindAction("Pause", true);
            _debugToggle = asset.FindActionMap(DebugMap, false)?.FindAction("Toggle", false);
            var ui = asset.FindActionMap(UiMap, false);
            _uiNavigate = ui?.FindAction("Navigate", false);
            _uiSubmit = ui?.FindAction("Submit", false);
            _uiCancel = ui?.FindAction("Cancel", false);
        }

        public void Enable()
        {
            _map.Enable();
            _debugToggle?.actionMap.Enable();
            _uiSubmit?.actionMap.Enable();
        }

        public void Disable()
        {
            _map.Disable();
            _debugToggle?.actionMap.Disable();
        }

        /// <summary>-1..1 horizontal movement.</summary>
        public float MoveX => Blocked ? 0f : Mathf.Clamp(_move.ReadValue<float>(), -1f, 1f);

        public bool JumpHeld => !Blocked && _jump.IsPressed();
        public bool JumpPressed => !Blocked && _jump.WasPressedThisFrame();
        public bool DashPressed => !Blocked && _dash.WasPressedThisFrame();
        public bool WardPressed => !Blocked && _ward.WasPressedThisFrame();
        public bool VeilPressed => !Blocked && _veil.WasPressedThisFrame();
        public bool BoltPressed => !Blocked && _bolt.WasPressedThisFrame();
        public bool InteractPressed => !Blocked && _interact.WasPressedThisFrame();
        public bool PausePressed => !Blocked && _pause.WasPressedThisFrame();

        /// <summary>Menu navigation (UI map): Submit, Cancel/Back (Escape, Android back, gamepad B), and the navigate vector.</summary>
        public bool SubmitPressed => !Blocked && _uiSubmit != null && _uiSubmit.WasPressedThisFrame();
        public bool CancelPressed => !Blocked && _uiCancel != null && _uiCancel.WasPressedThisFrame();
        public Vector2 Navigate => Blocked || _uiNavigate == null ? Vector2.zero : _uiNavigate.ReadValue<Vector2>();
        /// <summary>-1/1 on the frame a horizontal navigate starts (map node stepping).</summary>
        public int NavigateXPressed
        {
            get
            {
                if (Blocked || _uiNavigate == null || !_uiNavigate.WasPressedThisFrame()) return 0;
                float x = _uiNavigate.ReadValue<Vector2>().x;
                return x > 0.5f ? 1 : x < -0.5f ? -1 : 0;
            }
        }

        /// <summary>Development-only overlay toggle (F1 / gamepad Select). Not blocked by wipes.</summary>
        public bool DebugTogglePressed => _debugToggle != null && _debugToggle.WasPressedThisFrame();
    }
}
