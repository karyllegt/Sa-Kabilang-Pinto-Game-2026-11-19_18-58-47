using System;
using System.IO;
using Skp.Core;
using Skp.Levels;
using Skp.Save;
using UnityEngine;

namespace Skp.Preview
{
    /// <summary>Starts the isolated art preview at Level 1's first checkpoint.</summary>
    [DefaultExecutionOrder(-900)]
    public sealed class SmoothActIPreview : MonoBehaviour
    {
        public const string ScenePath = "Assets/Art/Smooth/ActI/Act I Smooth Preview.unity";
        public const string SessionKey = "Skp.SmoothActIPreview";

        [SerializeField] LevelRunner level;
        [SerializeField] Sprite fullHeart;
        [SerializeField] bool useBoy;

        public void Configure(LevelRunner runner, Sprite heart)
        {
            level = runner;
            fullHeart = heart;
        }

        public static string NewSaveDirectory() =>
            Path.Combine(Application.temporaryCachePath, "SmoothActIPreview", Guid.NewGuid().ToString("N"));

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void IsolateEditorSave()
        {
            if (UnityEditor.SessionState.GetBool(SessionKey, false))
                GameRoot.SaveDirectoryOverride = NewSaveDirectory();
        }
#endif

        public static GameRoot UsePreviewSave()
        {
            var root = GameRoot.Instance;
            if (string.IsNullOrEmpty(GameRoot.SaveDirectoryOverride))
                GameRoot.SaveDirectoryOverride = NewSaveDirectory();
            if (GameRoot.SaveDirectoryOverride != root.Save.Directory)
            {
                var save = new SaveService(GameRoot.SaveDirectoryOverride);
                save.Load();
                root.UseSave(save);
            }
            return root;
        }

        void Start()
        {
            var root = UsePreviewSave();
            root.Save.Data.@char = useBoy ? "boy" : "girl";
            GameSession.Pending = FirstCheckpoint();
        }

        public static LevelRequest FirstCheckpoint() => new LevelRequest
        {
            Level = 0, Difficulty = "standard",
            Resume = new ResumeState { active = true, cp = 1, vit = 99, flame = 100, lives = -1 }
        };

        void LateUpdate() => ShowHearts(level);

        public void ShowHearts(LevelRunner runner)
        {
            if (runner.ui == null || runner.Session == null) return;
            SetHearts(runner.ui.hud, runner.Session.Sim.Vitality);
        }

        public void SetHearts(Skp.UI.HudView hud, int vitality)
        {
            for (int i = 0; i < hud.hearts.Length; i++)
            {
                var image = hud.hearts[i];
                if (image == null) continue;
                if (i < vitality && image.overrideSprite != fullHeart) image.overrideSprite = fullHeart;
                else if (i >= vitality && image.overrideSprite == fullHeart) image.overrideSprite = null;
            }
        }
    }
}

