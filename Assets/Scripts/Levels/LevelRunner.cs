using Skp.Core;
using Skp.Dimensions;
using Skp.Player;
using UnityEngine;

namespace Skp.Levels
{
    /// <summary>
    /// The Level scene's director: takes the pending <see cref="LevelRequest"/> (or the save's resume point),
    /// builds a <see cref="LevelSession"/>, hands its player to the <see cref="PlayerController"/>, draws the
    /// level, and hands the session to the Level UI (intro dialog, HUD, letter, pause, Level Clear, Game Over).
    /// </summary>
    public class LevelRunner : MonoBehaviour
    {
        public LevelConfig[] levels = new LevelConfig[9];
        public PlayerTuning tuning;
        public DifficultyTable difficulties;
        public DifficultyCurve curve;
        public PlayerController player;
        public LevelView view;
        public DimensionDirector director;
        public SpriteRenderer backdrop;
        public Skp.UI.LevelUi ui;

        public LevelSession Session { get; private set; }
        public LevelRequest Request { get; private set; }

        void Start()
        {
            var root = GameRoot.Instance;
            var req = GameSession.Take(root.Save.Data);
            Begin(req);
        }

        public void Begin(LevelRequest req)
        {
            var root = GameRoot.Instance;
            Request = req;
            var spec = req.Level >= 0 ? levels[Mathf.Clamp(req.Level, 0, levels.Length - 1)].spec : LevelCatalog.Endless(req.Loop);
            Session = new LevelSession(spec, req, tuning, difficulties, curve, root.Save);

            player.Attach(Session.Sim);
            view.Build(Session.World, Session.Sim);
            if (backdrop != null && view.art != null && view.art.backdrops != null && view.art.backdrops.Length >= 3)
                backdrop.sprite = view.art.backdrops[Mathf.Clamp(spec.act - 1, 0, 2)];
            if (director != null) director.Refresh();
            // Continue from a lantern past the first resumes straight into play; a fresh start shows the intro.
            bool resumed = req.Resume != null && req.Resume.active && req.Resume.cp > 0;
            if (ui != null) ui.Bind(this, Session, !resumed);
        }
    }
}
