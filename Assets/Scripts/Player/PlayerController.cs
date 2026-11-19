using System;
using System.Linq;
using Skp.Core;
using Skp.Levels;
using UnityEngine;

namespace Skp.Player
{
    /// <summary>
    /// Runs <see cref="PlayerSim"/> on its own fixed 60 Hz clock (independent of frame rate and of
    /// Project Settings > Time), feeds it latched input, then draws it and moves the camera.
    /// Pausing (Time.timeScale = 0) stops the clock.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        public PlayerTuning tuning;
        public DifficultyTable difficulties;
        [Tooltip("Empty = use the save file's difficulty (Standard if none).")]
        public string difficultyOverride = "";
        public LevelWorldAuthoring worldSource;
        public PlayerView view;
        public CameraFollow cameraFollow;

        public PlayerSim Sim { get; private set; }

        /// <summary>Tests replace live input with scripted input.</summary>
        public Func<PlayerInputFrame> InputOverride;

        /// <summary>Raised after every simulation tick.</summary>
        public event Action<PlayerSim> Ticked;

        readonly FixedTickClock _clock = new FixedTickClock();
        readonly PlayerInputLatch _latch = new PlayerInputLatch();
        Vector2 _prevPos;

        void Start()
        {
            if (Sim == null) Initialize(worldSource != null ? worldSource.Build() : null);
        }

        /// <summary>Builds the simulation for a world. Called by Start, the level loader or tests.</summary>
        public void Initialize(PlayerWorld world)
        {
            if (world == null) throw new InvalidOperationException("PlayerController needs a world (assign worldSource).");
            var table = difficulties != null ? difficulties : ScriptableObject.CreateInstance<DifficultyTable>();
            var save = GameRoot.Instance != null ? GameRoot.Instance.Save.Data : null;
            string key = !string.IsNullOrEmpty(difficultyOverride) ? difficultyOverride : save?.diff ?? "standard";
            Sim = new PlayerSim(tuning, table.Get(key), world);
            if (view != null) view.useBoy = save != null && save.@char == "boy";
            _prevPos = Sim.Position;
            _latch.Clear();
            if (cameraFollow != null) { cameraFollow.bounds = world.Bounds; cameraFollow.SnapTo(Sim.Center, Sim.GravitySign); }
            if (worldSource != null)
            {
                var lanterns = worldSource.GetComponentsInChildren<LanternMarker>().OrderBy(l => l.order).ToArray();
                for (int i = 0; i < lanterns.Length; i++) lanterns[i].SetLit(i == 0);
                Sim.LanternLit += k => { if (k < lanterns.Length) lanterns[k].SetLit(true); };
            }
            DebugOverlay.ExtraLines -= DebugLine;
            DebugOverlay.ExtraLines += DebugLine;
        }

        void OnDestroy() => DebugOverlay.ExtraLines -= DebugLine;

        void Update()
        {
            if (Sim == null) return;
            _latch.Collect(ReadInput());
            int n = _clock.Advance(Time.deltaTime);
            for (int i = 0; i < n; i++)
            {
                _prevPos = Sim.Position;
                Sim.Step(_latch.Consume());
                if (cameraFollow != null) cameraFollow.Tick(Sim.Center, Sim.GravitySign);
                Ticked?.Invoke(Sim);
            }
            if (view != null) view.Render(Sim, _prevPos, _clock.Alpha);
        }

        PlayerInputFrame ReadInput()
        {
            if (InputOverride != null) return InputOverride();
            var input = GameRoot.Instance != null ? GameRoot.Instance.Input : null;
            if (input == null) return default;
            return new PlayerInputFrame
            {
                MoveX = PlayerInputFrame.Digital(input.MoveX),
                JumpHeld = input.JumpHeld,
                JumpPressed = input.JumpPressed,
                DashPressed = input.DashPressed,
                WardPressed = input.WardPressed,
                VeilPressed = input.VeilPressed,
                BoltPressed = input.BoltPressed,
                InteractPressed = input.InteractPressed,
            };
        }

        string DebugLine()
        {
            if (Sim == null) return "";
            return $"PLAYER pos {Sim.Position.x * 16:0},{Sim.Position.y * 16:0}px vel {Sim.Velocity.x * 16 / 60:0.00},{Sim.Velocity.y * 16 / 60:0.00}px/f " +
                   $"{(Sim.Grounded ? "ground" : "air")} vit {Sim.Vitality}/{Sim.MaxVitality} flame {Sim.Flame} lives {(Sim.InfiniteLives ? "inf" : Sim.Lives.ToString())} cp {Sim.Checkpoint}";
        }
    }
}
