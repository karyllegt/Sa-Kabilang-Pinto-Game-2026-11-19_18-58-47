using Skp.Save;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Skp.Core
{
    /// <summary>
    /// The one persistent object. Created before the first scene loads (so any scene can be
    /// played directly in the Editor) and kept across scene loads. Owns the save, input,
    /// scene loader and development overlay.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class GameRoot : MonoBehaviour
    {
        public static GameRoot Instance { get; private set; }

        public SaveService Save { get; private set; }
        public InputReader Input { get; private set; }
        public SceneLoader Loader { get; private set; }
        public SettingsStore Settings { get; private set; }
        /// <summary>The circle wipe used by the loader and by in-scene transitions.</summary>
        public Skp.UI.CircleWipe Wipe { get; private set; }

        /// <summary>Set by tests to keep the real save untouched (must be set before first access).</summary>
        public static string SaveDirectoryOverride;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Bootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject("GameRoot");
            DontDestroyOnLoad(go);
            go.AddComponent<GameRoot>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            Save = SaveDirectoryOverride != null ? new SaveService(SaveDirectoryOverride) : new SaveService();
            Save.Load();
            Settings = new SettingsStore(Save.Directory);
            Settings.Load();

            Loader = gameObject.AddComponent<SceneLoader>();
            Wipe = Skp.UI.CircleWipe.Create(transform);
            Loader.Wipe = Wipe;
            Skp.UI.UiGate.BlockedWhen = () => Loader.IsBusy || Wipe.Busy;
            Loader.SceneLoaded += _ => Time.timeScale = 1f;

            var asset = InputSystem.actions;
            if (asset == null || asset.FindActionMap(InputReader.GameplayMap) == null)
            {
                Debug.LogError("[GameRoot] Project-wide input actions are missing the Gameplay map. " +
                               "Expected Assets/Settings/Input/SkpControls.inputactions (Project Settings > Input System Package).");
            }
            else
            {
                Input = new InputReader(asset) { BlockedWhen = () => Loader.IsBusy || Wipe.Busy };
                Input.Enable();
            }

            if (Debug.isDebugBuild) gameObject.AddComponent<DebugOverlay>();
        }

        /// <summary>Swap the save service (tests point it at a temp folder so the real save is never touched).</summary>
        public void UseSave(SaveService save)
        {
            Save = save;
            Settings = new SettingsStore(save.Directory);
            Settings.Load();
        }

        void OnApplicationPause(bool paused)
        {
            // Android can kill a backgrounded app without warning: persist on the way out.
            if (paused) Save?.Save();
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Input?.Disable();
                Instance = null;
            }
        }
    }
}
