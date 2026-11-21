using System.Collections.Generic;
using Skp.Core;
using Skp.Levels;
using Skp.Player;
using Skp.Save;
using Skp.Skills;
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

        readonly HashSet<string> _hinted = new HashSet<string>();
        LevelRunner _runner;
        LevelSession _session;
        bool _playing, _wired, _lineHooked;

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
            session.Sim.NotEnoughFlame += () => toast.Show(Loc.Get("toast.noflame"), 1.5f);
            _hinted.Clear();
            SyncLanes();

            if (!_lineHooked)
            {
                _lineHooked = true;
                dialog.LineShown += i => { if (dialog.CurrentLine.Who == Speaker.Skill) Skp.Audio.Sfx.Cue(_session != null && _session.LetterSkillWasNew ? "skill_learned" : "skill_known"); };
            }
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

        public void ApplySettings(GameSettings s)
        {
            if (touch != null) touch.Apply(s);
            // chips are 48 dp tall whenever the touch layout is in use
            hud.SetTallChips(touch == null || touch.Wanted);
            hud.FitLanes(true);
        }

        /// <summary>Keeps the toast lane under the objective line and clear of the skill chips.</summary>
        public void SyncLanes()
        {
            hud.FitLanes();
            toast.SetLane(hud.LaneBottom + 22f, hud.LaneWidth);
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

        /// <summary>Seconds the skill box ignores taps after it is fully shown, so rapid taps cannot skip it.</summary>
        public const float SkillHoldSeconds = 0.6f;

        /// <summary>
        /// The letter pickup: box 1 is the letter, box 2 is NEW SKILL (or SKILL KNOWN on a replay) with the skill's
        /// name, Filipino name, description and controls. Play stays frozen through both.
        /// </summary>
        public void ShowLetter()
        {
            var spec = _session.Spec;
            if (string.IsNullOrEmpty(spec.clue)) return;
            var lines = LetterLines(spec, _session.LetterSkill, _session.LetterSkillWasNew);
            dialog.Show(lines, null, PlayerIsBoy);
        }

        /// <summary>The letter dialog's lines (public for the audit and tests).</summary>
        public static List<DialogLine> LetterLines(LevelSpec spec, string skillId, bool fresh)
        {
            var lines = new List<DialogLine>
            {
                new DialogLine
                {
                    Who = Speaker.Clue,
                    Sub = Loc.Format("clue.sub", spec.name.ToUpperInvariant()),
                    Name = Loc.Get("clue.name"),
                    Text = spec.clue,
                    Button = Loc.Get("common.continue"),
                },
            };
            var def = string.IsNullOrEmpty(skillId) ? null : SkillLibrary.Find(skillId);
            if (def != null)
            {
                lines.Add(new DialogLine
                {
                    Who = Speaker.Skill,
                    Sub = Loc.Format(fresh ? "skill.new.sub" : "skill.known.sub", def.tag),
                    Name = def.FullName,
                    NameColor = def.color,
                    Text = def.description,
                    Portrait = def.icon,
                    Button = Loc.Get("common.continue"),
                    HoldSeconds = SkillHoldSeconds,
                });
            }
            return lines;
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
                           s.Spec.clue, Loc.Get("clear.next"), Loc.Get("clear.map"), SkillLibrary.Find(s.LearnedThisRun));
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
            SyncLanes();
            var sim = _session.Sim;
            if (touch != null)
            {
                touch.SetSkills(sim.Has(SkillIds.Veil), sim.Has(SkillIds.Bolt));
                touch.SetPlaying(!frozen, sim.NearDoor >= 0);
            }
            if (!frozen) Hints(sim);
        }

        /// <summary>Prototype contextual hints: the first time a level holds an obstacle built for an owned skill.</summary>
        void Hints(PlayerSim sim)
        {
            var f = _session.Layout.Features;
            void Hint(long tick, string feature, string skill, string key)
            {
                if (sim.Tick != tick || !f.Contains(feature) || !sim.Has(skill) || !_hinted.Add(key)) return;
                toast.Show(Loc.Get(key), 3.5f);
            }
            Hint(200, "dj", SkillIds.DoubleJump, "hint.dj");
            Hint(260, "gaunt", SkillIds.Veil, "hint.gaunt");
            Hint(320, "thorn", SkillIds.Bolt, "hint.thorn");
            Hint(380, "wide", SkillIds.Glide, "hint.wide");
        }
    }
}
