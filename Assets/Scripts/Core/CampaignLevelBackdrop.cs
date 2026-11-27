using Skp.Dimensions;
using Skp.Levels;
using Skp.Player;
using Skp.Preview;
using Skp.Save;
using UnityEngine;

namespace Skp.Core
{
    public sealed class CampaignLevelBackdrop : MonoBehaviour
    {
        SmoothUiArt _art;
        LevelRunner _runner;
        SettingsStore _settings;
        SpriteRenderer _scenery;
        SmoothActionEffects _effects;
        PlayerSim _sim;

        public void Initialize(SmoothUiArt art, LevelRunner runner, SettingsStore settings, CampaignArt profile)
        {
            if (_runner != null) return;
            _art = art;
            _runner = runner;
            _settings = settings;
            _settings.Changed += OnSettingsChanged;
            _effects = new SmoothActionEffects(transform, runner.player.view.body,
                profile.dash, profile.waking, profile.spirit, profile.inverted,
                () => _settings.Data.reducedFlash);
        }

        void OnSettingsChanged(GameSettings settings) => _art.SetHudContrast(_runner, settings.highContrast);

        public void Bind(PlayerSim sim)
        {
            if (_sim != null) _sim.DoorUsed -= OnDoorUsed;
            _sim = sim;
            _effects.Reset();
            if (_sim != null) _sim.DoorUsed += OnDoorUsed;
        }

        void OnDoorUsed(Door door) => _effects.ShowTransition(_sim, door.Target);

        void LateUpdate()
        {
            if (_sim == null) return;
            var backdrop = _runner.backdrop;
            var camera = _runner.player.cameraFollow.cam;
            DrawScenery(_art, _sim.Dimension, backdrop, camera, ref _scenery);
            _effects.Draw(_sim);
        }

        public static void DrawScenery(SmoothUiArt art, Dim dimension, SpriteRenderer backdrop, Camera camera,
            ref SpriteRenderer scenery)
        {
            float height = camera.orthographicSize * 2f;
            var view = new Vector2(height * camera.aspect, height);
            const float sourceAspect = 16f / 9f;
            float width = Mathf.Max(view.x, height * sourceAspect);
            if (backdrop.drawMode != SpriteDrawMode.Sliced) backdrop.drawMode = SpriteDrawMode.Sliced;
            var backdropSize = new Vector2(width, width / sourceAspect);
            if (backdrop.size != backdropSize) backdrop.size = backdropSize;
            var sprite = art.Scenery(dimension);
            if (sprite != null && scenery == null)
            {
                scenery = new GameObject("Smooth dimension scenery").AddComponent<SpriteRenderer>();
                scenery.transform.SetParent(backdrop.transform, false);
                scenery.sharedMaterial = backdrop.sharedMaterial;
                scenery.sortingLayerID = backdrop.sortingLayerID;
                scenery.sortingOrder = backdrop.sortingOrder + 2;
                scenery.drawMode = SpriteDrawMode.Sliced;
            }
            if (scenery == null) return;
            if (scenery.sprite != sprite) scenery.sprite = sprite;
            if (scenery.enabled != (sprite != null)) scenery.enabled = sprite != null;
            if (sprite == null) return;
            scenery.color = dimension == Dim.Spirit
                ? new Color(0.50f, 0.82f, 0.76f, 0.34f)
                : new Color(0.72f, 0.54f, 0.86f, 0.34f);
            if (scenery.size != view) scenery.size = view;
        }

        void OnDestroy()
        {
            if (_sim != null) _sim.DoorUsed -= OnDoorUsed;
            if (_settings != null) _settings.Changed -= OnSettingsChanged;
        }
    }
}
