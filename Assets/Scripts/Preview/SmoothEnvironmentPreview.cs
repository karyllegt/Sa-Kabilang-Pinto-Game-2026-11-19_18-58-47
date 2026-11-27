using System.Linq;
using Skp.Core;
using Skp.Dimensions;
using Skp.Levels;
using Skp.Save;
using Skp.Skills;
using UnityEngine;

namespace Skp.Preview
{
    /// <summary>Shows the environment art in a copy of a standard campaign level.</summary>
    [DefaultExecutionOrder(-700)]
    public sealed class SmoothEnvironmentPreview : MonoBehaviour
    {
        public static readonly string[] ScenePaths =
        {
            "Assets/Art/Smooth/Environments/Act I House Towers.unity",
            "Assets/Art/Smooth/Environments/Act II Blackwater Well.unity",
            "Assets/Art/Smooth/Environments/Act III Bell Tower.unity",
        };

        [SerializeField] LevelRunner level;
        [SerializeField] int levelIndex;
        [SerializeField] Sprite spiritScenery;
        [SerializeField] Sprite invertedScenery;
        SpriteRenderer _scenery;
        Vector2 _lastViewSize;

        void Start()
        {
            GameSession.Pending = MakeRequest(SmoothActIPreview.UsePreviewSave().Save);
        }

        public LevelRequest MakeRequest(SaveService save)
        {
            save.Data.unlocked = levelIndex + 1;
            save.Data.skills = SkillIds.All.Take(levelIndex).ToArray();
            return new LevelRequest
            {
                Level = levelIndex, Difficulty = "standard",
                Resume = new ResumeState { active = true, lv = levelIndex, cp = 1, vit = 99, flame = 100, lives = -1 }
            };
        }

        void LateUpdate()
        {
            if (level.Session != null) DrawScenery(level.Session.Sim.Dimension);
        }

        public void DrawScenery(Dim dimension)
        {
            var backdrop = level.backdrop;
            var camera = level.player.cameraFollow.cam;
            if (backdrop == null || camera == null) return;

            // Cover the view at its current aspect without changing the playable framing.
            float height = camera.orthographicSize * 2f;
            var view = new Vector2(height * camera.aspect, height);
            if (_lastViewSize != view)
            {
                const float artworkAspect = 16f / 9f;
                float width = Mathf.Max(view.x, height * artworkAspect);
                backdrop.drawMode = SpriteDrawMode.Sliced;
                backdrop.size = new Vector2(width, width / artworkAspect);
                _lastViewSize = view;
            }

            var sprite = dimension == Dim.Spirit ? spiritScenery : dimension == Dim.Inverted ? invertedScenery : null;
            if (sprite == null)
            {
                if (_scenery != null) _scenery.enabled = false;
                return;
            }

            if (_scenery == null)
            {
                _scenery = new GameObject("Dimension Scenery").AddComponent<SpriteRenderer>();
                _scenery.transform.SetParent(backdrop.transform, false);
                _scenery.sharedMaterial = backdrop.sharedMaterial;
                _scenery.sortingLayerID = backdrop.sortingLayerID;
                _scenery.sortingOrder = backdrop.sortingOrder + 2;
                _scenery.drawMode = SpriteDrawMode.Sliced;
            }
            _scenery.sprite = sprite;
            _scenery.color = dimension == Dim.Spirit
                ? new Color(0.50f, 0.82f, 0.76f, 0.42f)
                : new Color(0.72f, 0.54f, 0.86f, 0.42f);
            _scenery.size = view;
            _scenery.enabled = true;
        }
    }
}

