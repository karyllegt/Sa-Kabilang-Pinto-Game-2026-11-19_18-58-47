using System.Collections;
using Skp.Core;
using Skp.Dimensions;
using Skp.Player;
using TMPro;
using UnityEngine;

namespace Skp.Levels
{
    /// <summary>
    /// The Level scene's director: takes the pending <see cref="LevelRequest"/> (or the save's resume point),
    /// builds a <see cref="LevelSession"/>, hands its player to the <see cref="PlayerController"/>, draws the
    /// level, and returns to the Map when the level is completed or lost. The M5 UI replaces the placeholder
    /// status line with the real HUD, intro dialog, Level Clear box and Game Over screen.
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
        [Tooltip("Placeholder status line until the M5 HUD.")]
        public TextMeshProUGUI status;

        public LevelSession Session { get; private set; }
        string _message;
        float _messageUntil;

        void Start()
        {
            var root = GameRoot.Instance;
            var req = GameSession.Take(root.Save.Data);
            Begin(req);
        }

        public void Begin(LevelRequest req)
        {
            var root = GameRoot.Instance;
            var spec = req.Level >= 0 ? levels[Mathf.Clamp(req.Level, 0, levels.Length - 1)].spec : LevelCatalog.Endless(req.Loop);
            Session = new LevelSession(spec, req, tuning, difficulties, curve, root.Save);
            Session.Message += m => { _message = m; _messageUntil = Time.time + 2.6f; };
            Session.LevelCompleted += () => StartCoroutine(BackToMap("LEVEL CLEAR"));
            Session.GameOver += () => StartCoroutine(BackToMap("THE NIGHT TAKES YOU"));
            Session.Sim.ClueFound += () => { _message = "A LETTER, FOLDED SMALL: " + spec.clue; _messageUntil = Time.time + 4f; };
            Session.Sim.LanternLit += _ => { _message = "LANTERN LIT - progress saved"; _messageUntil = Time.time + 1.8f; };

            player.Attach(Session.Sim);
            view.Build(Session.World, Session.Sim);
            if (backdrop != null && view.art != null && view.art.backdrops != null && view.art.backdrops.Length >= 3)
                backdrop.sprite = view.art.backdrops[Mathf.Clamp(spec.act - 1, 0, 2)];
            if (director != null) director.Refresh();
            _message = spec.hint;
            _messageUntil = Time.time + 4.2f;
        }

        IEnumerator BackToMap(string why)
        {
            _message = why;
            _messageUntil = Time.time + 10f;
            yield return new WaitForSeconds(1.5f);
            GameRoot.Instance.Loader.Load(SceneNames.Map);
        }

        void Update()
        {
            var root = GameRoot.Instance;
            if (Session == null || root == null) return;
            if (!Session.Completed && !Session.Over && root.Input != null && root.Input.PausePressed && !root.Loader.IsBusy)
                root.Loader.Load(SceneNames.Map); // M5 replaces this with the pause screen
            if (status != null)
            {
                var s = Session.Sim;
                string lives = s.InfiniteLives ? "inf" : s.Lives.ToString();
                string msg = Time.time < _messageUntil ? "\n" + _message : "";
                status.SetText($"{Session.Level + 1}. {Session.Spec.name.ToUpperInvariant()}   VIT {s.Vitality}/{s.MaxVitality}   FLAME {s.Flame}   LIVES {lives}   SCORE {s.Score}   FRAGMENTS {s.Shards}/3   {(s.HasClue ? "LETTER FOUND" : "")}{msg}");
            }
        }
    }
}
