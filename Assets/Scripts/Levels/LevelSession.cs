using System;
using Skp.Core;
using Skp.Player;
using Skp.Save;
using UnityEngine;

namespace Skp.Levels
{
    /// <summary>
    /// One run of one level, without any scene: generates the layout, builds the world and the player,
    /// applies the difficulty ramp, saves a checkpoint at every lantern, keeps the exit locked until the
    /// letter is found, completes the level (score bonus, unlock, next-level resume point) and handles
    /// game over. The Level scene's <see cref="LevelRunner"/> drives it; tests drive it directly.
    /// </summary>
    public class LevelSession
    {
        public const string LockedMessage = "LOCKED - find the clue hidden in this tower first";

        public readonly LevelSpec Spec;
        public readonly int Level;
        public readonly int Loop;
        public readonly string DifficultyKey;
        public readonly DifficultyMode Difficulty;
        public readonly LevelLayout Layout;
        public readonly PlayerWorld World;
        public readonly PlayerSim Sim;
        readonly SaveService _save;

        public bool Completed { get; private set; }
        public bool Over { get; private set; }

        public event Action<string> Message;
        public event Action LockedExit;
        public event Action LevelCompleted;
        public event Action GameOver;

        public LevelSession(LevelSpec spec, LevelRequest req, PlayerTuning tuning, DifficultyTable table, DifficultyCurve curve, SaveService save)
        {
            Spec = spec;
            Level = req.Level;
            Loop = req.Loop;
            DifficultyKey = req.Difficulty;
            Difficulty = table.Get(req.Difficulty);
            _save = save;
            var data = save.Data;
            Layout = LevelGenerator.Build(spec, Level, Exact(Difficulty.hazardMult), Exact(Difficulty.enemyMult), Exact(Difficulty.floodMult),
                                          id => ProgressRules.HasSkill(data, id), Loop);
            World = LevelWorldFactory.Build(Layout);
            Sim = new PlayerSim(tuning, Difficulty, World);

            int ramp = Mathf.Max(0, Level);
            Sim.SetChaseDelay(curve != null ? curve.ChaseFrames(Difficulty.chaseFrames, ramp) : Difficulty.chaseFrames);
            Sim.EnemySpeedMult = Difficulty.speedMult * (1f + 0.02f * ramp);

            var resume = req.Resume;
            if (resume != null && resume.active)
            {
                Sim.BeginAt(resume.cp);
                Sim.Vitality = Mathf.Clamp(resume.vit, 1, Sim.MaxVitality);
                Sim.Flame = Mathf.Clamp(resume.flame, 0, Sim.MaxFlame);
                if (!Sim.InfiniteLives && resume.lives >= 0) Sim.Lives = resume.lives;
                Sim.Score = resume.score;
                Sim.Shards = resume.shards;
                if (resume.clue)
                {
                    Sim.HasClue = true;
                    foreach (var it in World.Items) if (it.Kind == ItemKind.Clue) it.Got = true;
                }
            }
            else Sim.BeginAt(0);

            Sim.LanternLit += _ => SaveCheckpoint();
            Sim.ExitRequested += _ => TryExit();
            Sim.OutOfLives += OnGameOver;
        }

        /// <summary>Recovers the exact decimal behind a float multiplier (0.6f -> 0.6) so generation matches the prototype's doubles.</summary>
        public static double Exact(float f) => Math.Round(f, 6);

        public void SaveCheckpoint()
        {
            _save.Data.cont = Snapshot(Level, Sim.Checkpoint, Sim.Vitality, Sim.Flame, Sim.HasClue, Sim.Shards);
            _save.Save();
        }

        ResumeState Snapshot(int lv, int cp, int vit, int flame, bool clue, int shards) => new ResumeState
        {
            active = true, lv = lv, dfKey = DifficultyKey, loop = Loop, cp = cp, vit = vit, flame = flame,
            lives = Sim.InfiniteLives ? -1 : Sim.Lives, score = Sim.Score, clue = clue, shards = shards,
        };

        public void TryExit()
        {
            if (Completed || Over) return;
            if (World.NeedClue && !Sim.HasClue)
            {
                LockedExit?.Invoke();
                Message?.Invoke(LockedMessage);
                return;
            }
            Complete();
        }

        void Complete()
        {
            Completed = true;
            Sim.Score += 500 + (Sim.InfiniteLives ? 0 : Mathf.Max(0, Sim.Lives) * 50);
            var data = _save.Data;
            if (Spec.endless)
            {
                if (Sim.Score > data.best) data.best = Sim.Score;
                data.cont = new ResumeState { active = true, lv = -1, dfKey = DifficultyKey, loop = Loop + 1, vit = Sim.Vitality, flame = Sim.Flame,
                                              lives = Sim.Lives, score = Sim.Score };
            }
            else
            {
                ProgressRules.CompleteLevel(data, Level);
                data.cont = Level >= ProgressRules.FinalLevelIndex
                    ? new ResumeState()
                    : Snapshot(Level + 1, 0, Sim.MaxVitality, 100, false, 0);
            }
            _save.Save();
            LevelCompleted?.Invoke();
        }

        void OnGameOver()
        {
            if (Over) return;
            Over = true;
            var data = _save.Data;
            if (Spec.endless && Sim.Score > data.best) data.best = Sim.Score;
            // Out of lives: the night starts over; there is nothing to continue.
            data.cont = new ResumeState();
            _save.Save();
            GameOver?.Invoke();
        }
    }
}
