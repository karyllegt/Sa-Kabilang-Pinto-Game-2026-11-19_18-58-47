using System.Collections.Generic;
using Skp.Core;
using Skp.Levels;
using Skp.Player;
using Skp.Save;
using UnityEngine;

namespace Skp.UI
{
    /// <summary>
    /// The Level scene's UI: before-stage dialog then a circle wipe into play, HUD, toasts, the letter
    /// dialog, pause (with settings), Level Clear, Game Over and the Ending. The player simulation is
    /// frozen whenever a box is open or a wipe runs, so input pressed in a dialog never leaks into play.
    /// Back pauses during play and goes up one level everywhere else.
    /// </summary>
    public class LevelUi : MonoBehaviour
    {
        public HudView hud;
        public Toast toast;
        public DialogBox dialog;
        public PauseScreen pause;
        public SettingsScreen settings;
        public LevelClearScreen clear;
        public GameOverScreen gameOver;
        public EndingScreen ending;
        public TouchControls touch;

        LevelRunner _runner;
        LevelSession _session;
        bool _playing, _wired;

        public bool Paused { get; private set; }
        public bool Playing => _playing;
        public LevelSession Session => _session;

        GameRoot Root => GameRoot.Instance;
        bool PlayerIsBoy => Root != null && Root.Save.Data.@char == "boy";

        /// <summary>Called by the runner once the session exists.</summary>
        public void Bind(LevelRunner runner, LevelSession session, bool showIntro)
        {
            _runner = runner;
            _session = session;
            _playing = false;
            Wire();
            hud.SetLevel(session);
            hud.Show(session);
            hud.SetBoss(false, 1);

            session.Message += m => toast.Show(m);
            session.LevelCompleted += ShowCompleted;
            session.GameOver += ShowGameOver;
            session.Sim.ClueFound += ShowLetter;
            session.Sim.LanternLit += k => { if (k > 0) toast.Show(Loc.Get("toast.lantern"), 1.8f); };
            session.Sim.ItemCollected += it => { if (it.Kind == ItemKind.Shard) toast.Show(Loc.Get("toast.fragment"), 2f); };

            if (showIntro) ShowIntro();
            else BeginPlay(false);
        }

        void Wire()
        {
            if (_wired) return;
            _wired = true;
            UiNavigator.Fallback = () => { if (_playing && !Paused) { Pause(); return true; } return false; };
            hud.pause.Clicked += () => { if (_playing) Pause(); };
            pause.resume.Clicked += Resume;
            pause.settings.Clicked += () =>
            {
                pause.Close();
                settings.Bind(Root.Settings, () => { settings.Close(); pause.Open(); });
                settings.Open();
            };
            pause.toLantern.Clicked += () => { _session.Sim.Respawn(); Resume(); };
            pause.restart.Clicked += () => { Unpause(); Load(new LevelRequest { Level = _session.Level, Difficulty = _session.DifficultyKey, Loop = _session.Loop }, SceneNames.Level); };
            pause.quit.Clicked += () =>
            {
                if (_session.Sim.Checkpoint > 0) _session.SaveCheckpoint();
                Unpause();
                Root.Loader.Load(SceneNames.Title);
            };
            clear.next.Clicked += () => Load(GameSession.FromSave(Root.Save.Data), SceneNames.Level);
            clear.replay.Clicked += () => Load(new LevelRequest { Level = _session.Level, Difficulty = _session.DifficultyKey, Loop = _session.Spec.endless ? 0 : _session.Loop }, SceneNames.Level);
            clear.map.Clicked += () =>
            {
                if (_session.Spec.endless) { Root.Loader.Load(SceneNames.Title); return; }
                GameSession.MapFrom = _session.Level;
                Root.Loader.Load(SceneNames.Map);
            };
            gameOver.retry.Clicked += () => Load(new LevelRequest { Level = _session.Level, Difficulty = _session.DifficultyKey, Loop = _session.Spec.endless ? 0 : _session.Loop }, SceneNames.Level);
            gameOver.map.Clicked += () => Root.Loader.Load(_session.Spec.endless ? SceneNames.Title : SceneNames.Map);
            ending.menu.Clicked += () => Root.Loader.Load(SceneNames.Title);
            if (Root != null && Root.Settings != null) Root.Settings.Changed += ApplySettings;
            ApplySettings(Root != null && Root.Settings != null ? Root.Settings.Data : new GameSettings());
        }

        void OnDestroy()
        {
            if (Application.isPlaying) Time.timeScale = 1f;
            if (Root != null && Root.Settings != null) Root.Settings.Changed -= ApplySettings;
            UiNavigator.Fallback = null;
        }

        void ApplySettings(GameSettings s)
        {
            if (touch != null) touch.Apply(s);
        }

        void Load(LevelRequest req, string scene)
        {
            if (Root.Loader.IsBusy) return;
            GameSession.Pending = req;
            Root.Loader.Load(scene);
        }

        // ------------------------------------------------------------------ flow

        public void ShowIntro()
        {
            var spec = _session.Spec;
            var line = new DialogLine
            {
                Who = Speaker.Player,
                Sub = spec.endless ? Loc.Format("level.endless.sub", _session.Loop + 1) : LevelCatalog.ActNames[Mathf.Clamp(spec.act - 1, 0, 2)],
                Name = _session.Level >= 0 ? Loc.Format("level.node", _session.Level + 1, spec.name.ToUpperInvariant()) : spec.name.ToUpperInvariant(),
                Text = spec.endless ? Loc.Get("level.endless.intro") : spec.intro,
                Button = Loc.Get("common.begin"),
            };
            dialog.Show(new[] { line }, () => BeginPlay(true), PlayerIsBoy);
        }

        void BeginPlay(bool wipe)
        {
            void Go()
            {
                _playing = true;
                if (_session.Sim.Checkpoint == 0 && !string.IsNullOrEmpty(_session.Spec.hint)) toast.Show(_session.Spec.hint, 4.2f);
            }
            var w = Root != null ? Root.Wipe : null;
            if (wipe && w != null && Application.isPlaying) w.Run(Go, PlayerViewport());
            else Go();
        }

        Vector2 PlayerViewport()
        {
            var cam = Camera.main;
            if (cam == null) return new Vector2(0.5f, 0.5f);
            return cam.WorldToViewportPoint(_session.Sim.Center);
        }

        public void ShowLetter()
        {
            var spec = _session.Spec;
            if (string.IsNullOrEmpty(spec.clue)) return;
            var line = new DialogLine
            {
                Who = Speaker.Clue,
                Sub = Loc.Format("clue.sub", spec.name.ToUpperInvariant()),
                Name = Loc.Get("clue.name"),
                Text = spec.clue,
                Button = Loc.Get("common.continue"),
            };
            dialog.Show(new[] { line }, null, PlayerIsBoy);
        }

        public void Pause()
        {
            if (!_playing || Paused || UiGate.Blocked || UiNavigator.Top != null || _session.Completed || _session.Over) return;
            Paused = true;
            if (Application.isPlaying) Time.timeScale = 0f;
            toast.Hide();
            pause.Open();
        }

        public void Resume()
        {
            pause.Close();
            Unpause();
        }

        void Unpause()
        {
            Paused = false;
            if (Application.isPlaying) Time.timeScale = 1f;
        }

        /// <summary>Level Clear box, or the Ending after the final level (called by the session; public for the audit).</summary>
        public void ShowCompleted()
        {
            _playing = false;
            toast.Hide();
            var s = _session;
            var sim = s.Sim;
            int secs = Mathf.RoundToInt(sim.Tick / (float)PlayerSim.TickRate);
            if (!s.Spec.endless && s.Level >= ProgressRules.FinalLevelIndex)
            {
                ending.stats.text = Loc.Format("ending.stats", secs, sim.Score, sim.Shards);
                ending.Open();
                return;
            }
            if (s.Spec.endless)
                clear.Fill(Loc.Format("clear.loop", s.Loop + 1), Loc.Get("clear.loop.sub"), Loc.Format("clear.seconds", secs), sim.Score.ToString(), "-", "",
                           Loc.Get("clear.nextloop"), Loc.Get("clear.menu"));
            else
                clear.Fill(Loc.Format("clear.title", s.Level + 1), s.Spec.name, Loc.Format("clear.seconds", secs), sim.Score.ToString(), sim.Shards + "/3",
                           s.Spec.clue, Loc.Get("clear.next"), Loc.Get("clear.map"));
            clear.Open();
        }

        public void ShowGameOver()
        {
            _playing = false;
            toast.Hide();
            var sim = _session.Sim;
            gameOver.stats.text = Loc.Format("over.stats", _session.Level + 1, sim.Score, sim.Shards);
            gameOver.Open();
        }

        void Update()
        {
            if (_session == null || _runner == null) return;
            bool frozen = !_playing || Paused || UiNavigator.Top != null || UiGate.Blocked || _session.Completed || _session.Over;
            _runner.player.Frozen = frozen;
            hud.Show(_session);
            if (touch != null) touch.SetPlaying(!frozen, _session.Sim.NearDoor >= 0);
        }
    }
}
