using System;
using Skp.Dimensions;
using Skp.Levels;
using Skp.Preview;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Skp.Core
{
    [CreateAssetMenu(menuName = "Sa Kabilang Pinto/Campaign Art")]
    public sealed class CampaignArt : ScriptableObject
    {
        public const string ResourcePath = "Presentation/CampaignArt";
        public const string AssetPath = "Assets/Resources/Presentation/CampaignArt.asset";
        public SmoothUiArt ui;
        public FeelTuning feel;
        public Sprite glideWings, dash, waking, spirit, inverted;
    }

    /// <summary>Applies the selected art to production scenes while retaining their legacy assets.</summary>
    public sealed class CampaignPresentation
    {
        readonly GameRoot _root;
        readonly CampaignArt _art;

        public CampaignPresentation(GameRoot root)
        {
            _root = root;
#if UNITY_EDITOR
            if (UnityEditor.SessionState.GetBool(SmoothActIPreview.SessionKey, false)) return;
#endif
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-legacy-art") >= 0) return;
            var selection = Resources.Load<CampaignArtSelection>(CampaignArtSelection.ResourcePath);
            if (selection == null || !selection.useSmoothArt) return;
            _art = Resources.Load<CampaignArt>(CampaignArt.ResourcePath);
            if (_art == null || _art.ui == null) return;
            SceneManager.sceneLoaded += Apply;
        }

        public static bool IsCampaignScene(Scene scene, string name) => scene.path == $"Assets/Scenes/{name}.unity";

        void Apply(Scene scene, LoadSceneMode _)
        {
            bool title = IsCampaignScene(scene, SceneNames.Title);
            bool map = IsCampaignScene(scene, SceneNames.Map);
            bool level = IsCampaignScene(scene, SceneNames.Level);
            if (!title && !map && !level)
            {
                _art.ui.Deactivate();
                _root.Wipe.SmoothEdge = false;
                return;
            }

            _art.ui.Activate();
            _root.Wipe.SmoothEdge = true;
            var camera = Camera.main;
            if (camera == null) return;
            _art.ui.Apply(scene, 0, camera.aspect, _root.Settings.Data.highContrast, configureLevelArt: false);
            if (title) camera.gameObject.AddComponent<CampaignTitleBackdrop>().Initialize(_art.ui, camera);
        }

        public void ConfigureLevel(LevelRunner runner, int act)
        {
            if (_art == null || _art.ui == null ||
                !IsCampaignScene(runner.gameObject.scene, SceneNames.Level)) return;
            _art.ui.ApplyLevelArt(runner, act);
            runner.feel = _art.feel;
            runner.player.view.glideWings.sprite = _art.glideWings;
            var art = runner.GetComponent<CampaignLevelBackdrop>();
            if (art == null) art = runner.gameObject.AddComponent<CampaignLevelBackdrop>();
            art.Initialize(_art.ui, runner, _root.Settings, _art);
        }

        public void Dispose()
        {
            SceneManager.sceneLoaded -= Apply;
            if (_art != null && _art.ui != null) _art.ui.Deactivate();
        }
    }
}
