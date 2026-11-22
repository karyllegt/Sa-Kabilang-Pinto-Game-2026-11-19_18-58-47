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
        Skp.Audio.SkillCues _cues;
        Skp.Audio.GameCues _gameCues;
        LevelFeel _feel;
        LevelFx _fx;
        public FeelTuning feel;
        public LevelFeel Feel => _feel;
        public LevelFx Fx => _fx;
        Skp.Enemies.BossView _bossView;
        public Skp.Enemies.BossView BossView => _bossView;

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
            if (_cues != null) player.Ticked -= _cues.Tick;
            _cues = new Skp.Audio.SkillCues(Session.Sim);
            player.Ticked += _cues.Tick;
            if (_gameCues != null) player.Ticked -= _gameCues.Tick;
            _gameCues = new Skp.Audio.GameCues(Session.Sim);
            player.Ticked += _gameCues.Tick;
            if (feel == null) feel = Resources.Load<FeelTuning>(FeelTuning.ResourcePath);
            var catalog = view.Catalog;
            if (_fx == null) _fx = LevelFx.Create(transform, feel, catalog != null ? catalog.fx : null);
            else _fx.Clear();
            if (_feel != null) player.Ticked -= _feel.Tick;
            _feel = new LevelFeel(Session, player, _fx, feel);
            player.Ticked += _feel.Tick;
            player.Ticked -= MusicTick;
            player.Ticked += MusicTick;
            Skp.Audio.Music.SetDimension(Session.Sim.Dimension);
            Session.Sim.DimensionChanged += d => Skp.Audio.Music.SetDimension(d);
            Skp.Audio.Music.Play(Skp.Audio.Music.ForAct(spec.act));
            view.act = Mathf.Clamp(spec.act, 1, 3);
            view.Build(Session.World, Session.Sim);
            if (_bossView != null) { if (Application.isPlaying) Destroy(_bossView.gameObject); else DestroyImmediate(_bossView.gameObject); }
            if (Session.Boss != null)
            {
                _bossView = Instantiate(catalog.bossView);
                _bossView.Bind(Session.Boss, catalog);
            }
            if (player.view != null) player.view.PrewarmBoltPool();
            if (player.cameraFollow != null) player.cameraFollow.LockTopY = null;
            if (backdrop != null && view.art != null && view.art.backdrops != null && view.art.backdrops.Length >= 3)
                backdrop.sprite = view.art.backdrops[Mathf.Clamp(spec.act - 1, 0, 2)];
            if (director != null) director.Refresh();
            // Continue from a lantern past the first resumes straight into play; a fresh start shows the intro.
            bool resumed = req.Resume != null && req.Resume.active && req.Resume.cp > 0;
            if (ui != null) ui.Bind(this, Session, !resumed);
            DebugOverlay.ExtraLines -= DebugLine;
            DebugOverlay.ExtraLines += DebugLine;
        }

        void OnDestroy()
        {
            DebugOverlay.ExtraLines -= DebugLine;
            Skp.Audio.Music.SetDimension(Skp.Dimensions.Dim.Ordinary);
        }

        /// <summary>Per tick: the music follows the dimension, and the boss theme takes over once the Bellkeeper wakes.</summary>
        void MusicTick(PlayerSim sim)
        {
            if (Session == null || sim != Session.Sim) return;
            Skp.Audio.Music.SetDimension(sim.Dimension);
            var boss = Session.Boss;
            if (boss != null && boss.State != Skp.Enemies.BossState.Asleep && boss.State != Skp.Enemies.BossState.Dead)
                Skp.Audio.Music.Play(Skp.Audio.MusicTrack.Boss);
        }

        /// <summary>Development overlay: level and room, the room's hazard and enemy multipliers, and its hazard and enemy counts.</summary>
        public string DebugLine()
        {
            if (Session == null) return "";
            var L = Session.Layout;
            int room = Mathf.Clamp(Session.Sim.Checkpoint, 0, Mathf.Max(0, L.Rooms - 1));
            var (hz, en) = room < L.RoomMults.Count ? L.RoomMults[room] : (0d, 0d);
            float y0 = LevelWorldFactory.RoomBottom(room), y1 = y0 + LevelLayout.RoomTiles;
            int haz = 0, ens = 0;
            foreach (var h in Session.World.Hazards) if (h.Rect.y >= y0 && h.Rect.y < y1) haz++;
            foreach (var e in Session.World.Enemies) if (e.Position.y >= y0 && e.Position.y < y1) ens++;
            return $"RAMP level {Session.Level} room {room + 1}/{L.Rooms} hazard x{hz:0.00} enemy x{en:0.00} | room hazards {haz} enemies {ens} bells {Session.World.Bells.Count} | speed x{Session.Sim.EnemySpeedMult:0.00} chase {Session.Sim.Shadow.Delay}";
        }
    }
}
