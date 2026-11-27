using Skp.Core;
using Skp.Dimensions;
using Skp.Levels;
using Skp.Player;
using Skp.UI;
using UnityEngine;

namespace Skp.Preview
{
    public sealed class SmoothInteractionPreview : MonoBehaviour
    {
        public static readonly string[] ScenePaths =
        {
            "Assets/Art/Smooth/Interactions/Act I Interactions.unity",
            "Assets/Art/Smooth/Interactions/Act II Interactions.unity",
            "Assets/Art/Smooth/Interactions/Act III Interactions.unity",
        };

        [SerializeField] LevelRunner level;
        [SerializeField] FeelTuning smoothFeel;
        [SerializeField] Sprite letterPortrait;
        [SerializeField] Sprite ward;
        [SerializeField] Sprite veil;
        [SerializeField] Sprite dash;
        [SerializeField] Sprite wakingTransition;
        [SerializeField] Sprite spiritTransition;
        [SerializeField] Sprite invertedTransition;

        SmoothActionEffects _effects;
        PlayerSim _sim;
        DialogBox _dialog;

        void Awake()
        {
            ApplyVisuals();
            _dialog = level.ui.dialog;
            if (_dialog != null) _dialog.LineShown += ShowLetter;
        }

        public void ApplyVisuals()
        {
            var view = level.player.view;
            level.feel = smoothFeel;
            view.boltPrefab = level.view.Catalog.bolt;
            view.wardRing.sprite = ward;
            view.veilRing.sprite = veil;
        }

        void LateUpdate()
        {
            var sim = level.Session?.Sim;
            if (sim != _sim) Bind(sim);
            if (sim != null) Draw(sim);
        }

        void Bind(PlayerSim sim)
        {
            if (_sim != null) _sim.DoorUsed -= ShowTransition;
            _sim = sim;
            if (_sim != null) _sim.DoorUsed += ShowTransition;
            _effects?.Reset();
        }

        void OnDestroy()
        {
            Bind(null);
            if (_dialog != null) _dialog.LineShown -= ShowLetter;
        }

        void ShowLetter(int _)
        {
            var line = _dialog.CurrentLine;
            if (line.Who == Speaker.Clue && line.Portrait == null)
                _dialog.portrait.sprite = letterPortrait;
        }

        void ShowTransition(Door door) => ShowTransition(_sim, door.Target);

        public void ShowTransition(PlayerSim sim, Dim destination) => Effects.ShowTransition(sim, destination);

        public void Draw(PlayerSim sim) => Effects.Draw(sim);

        SmoothActionEffects Effects => _effects ??= new SmoothActionEffects(transform, level.player.view.body, dash,
            wakingTransition, spiritTransition, invertedTransition,
            () => GameRoot.Instance?.Settings?.Data.reducedFlash ?? false);
    }
}
