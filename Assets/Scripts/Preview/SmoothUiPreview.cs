using System.Linq;
using Skp.Core;
using Skp.Levels;
using Skp.Save;
using Skp.Skills;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Skp.Preview
{
    /// <summary>Applies the selected look to the normal scene flow for one temporary play session.</summary>
    [DefaultExecutionOrder(-950)]
    public sealed class SmoothUiPreview : MonoBehaviour
    {
        public const string ScenePath = "Assets/Art/Smooth/UI/Smooth UI Journey.unity";
        public const string ArtPath = "Assets/Art/Smooth/UI/SmoothUiArt.asset";
        [SerializeField] SmoothUiArt art;
        LevelRunner _level;
        SettingsStore _settings;
        SpriteRenderer _scenery;
        float _aspect;

        void Awake()
        {
            art.Activate();
            var root = SmoothActIPreview.UsePreviewSave();
            _settings = root.Settings;
            _settings.Changed += OnSettingsChanged;
            // Stage a checkpoint to expose HUD icons and all normal menu actions at once.
            var save = root.Save.Data;
            save.unlocked = 5;
            save.skills = SkillIds.All.Take(4).ToArray();
            save.metLola = true;
            save.lolaSeen = 4;
            save.cont = new ResumeState { active = true, lv = 4, cp = 1, vit = 3, flame = 100, lives = 3 };
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += ApplyLook;
        }

        void ApplyLook(Scene scene, LoadSceneMode _)
        {
            _level = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                _level = root.GetComponentInChildren<LevelRunner>(true);
                if (_level != null) break;
            }
            var game = GameRoot.Instance;
            var request = GameSession.Pending ?? GameSession.FromSave(game.Save.Data);
            int index = request.Level >= 0 ? request.Level : (request.Loop % 3) * 3;
            _aspect = Camera.main != null ? Camera.main.aspect : 16f / 9f;
            art.Apply(scene, index, _aspect, game.Settings.Data.highContrast);
        }

        void OnSettingsChanged(GameSettings settings) => art.SetHudContrast(_level, settings.highContrast);

        void LateUpdate()
        {
            var camera = Camera.main;
            if (camera == null) return;
            if (!Mathf.Approximately(_aspect, camera.aspect))
            {
                _aspect = camera.aspect;
                art.FitTitle(SceneManager.GetActiveScene(), _aspect);
            }
            if (_level == null || _level.Session == null) return;
            CampaignLevelBackdrop.DrawScenery(art, _level.Session.Sim.Dimension, _level.backdrop, camera, ref _scenery);
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= ApplyLook;
            if (_settings != null) _settings.Changed -= OnSettingsChanged;
            art.Deactivate();
        }
    }
}
