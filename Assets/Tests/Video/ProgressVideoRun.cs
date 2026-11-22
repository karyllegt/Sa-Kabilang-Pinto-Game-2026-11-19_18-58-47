using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Skp.Core;
using Skp.Enemies;
using Skp.Levels;
using Skp.Player;
using Skp.Save;
using Skp.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Skp.Tests.Video
{
    /// <summary>
    /// The progress video: the real game from Boot to the Ending, played by the level bot through real input.
    /// New journey (girl, Story), Lola's first meeting, level 1 played in full, then for levels 2 to 8 the room that
    /// holds the letter (each skill learned on screen) and the exit, the map between levels, the Bellkeeper fought
    /// with ward parries and Liwanag bolts, and the Ending. Tala can be hit but her hearts are refilled so a run is
    /// never lost. Writes tests/Video/progress.mp4, stills and a log of what happened.
    /// Explicit: run it on its own (unity test --mode PlayMode --filter ProgressVideoRun).
    /// </summary>
    [Explicit("Makes the progress video; run on its own")]
    public class ProgressVideoRun
    {
        static string Root => Path.GetFullPath(Path.Combine(Application.dataPath, "../tests/Video"));
        string _dir;
        SaveService _save;
        ProgressRecorder _rec;
        readonly List<string> _log = new List<string>();
        GameRoot Game => GameRoot.Instance;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "skp_video_" + System.Guid.NewGuid().ToString("N"));
            _save = new SaveService(_dir);
            _save.Load();
        }

        [TearDown]
        public void TearDown()
        {
            if (_rec != null) _rec.Finish();
            Time.timeScale = 1f;
            Time.captureFramerate = 0;
            Directory.CreateDirectory(Root);
            File.WriteAllLines(Path.Combine(Root, "progress_log.txt"), _log);
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        void Log(string s) { _log.Add($"[{(_rec != null ? _rec.EncodedFrames / (float)ProgressRecorder.VideoFps : 0f),6:0.0}s] {s}"); Debug.Log("[ProgressVideo] " + s); }

        /// <summary>Game frames; with the capture frame rate locked, 60 frames are one second of video.</summary>
        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
        static IEnumerator Sec(float s) => Frames(Mathf.RoundToInt(s * ProgressRecorder.GameFps));

        static IEnumerator WaitFor(System.Func<bool> cond, float seconds, string what)
        {
            int n = Mathf.RoundToInt(seconds * ProgressRecorder.GameFps);
            for (int i = 0; i < n && !cond(); i++) yield return null;
            Assert.IsTrue(cond(), "timed out waiting for " + what);
        }

        /// <summary>Reads a dialog at a watchable pace: let each line type out, hold it, then go on.</summary>
        IEnumerator Read(DialogBox d, float hold = 1.6f)
        {
            int guard = 0;
            while (d.IsOpen && guard++ < 40)
            {
                yield return WaitFor(() => !UiGate.Blocked, 5, "wipe");
                int t = 0;
                while (d.IsTyping && t++ < 400) yield return null;
                yield return Sec(hold);
                if (!d.IsOpen) break;
                d.Advance();
                yield return Frames(2);
            }
            Assert.IsFalse(d.IsOpen, "dialog closed");
        }

        [UnityTest]
        [Timeout(7200000)]
        public IEnumerator Record()
        {
            SceneManager.LoadScene(SceneNames.Boot);
            yield return WaitFor(() => Game != null, 10, "GameRoot");
            Game.UseSave(_save);
            _rec = ProgressRecorder.Begin(Path.Combine(Root, "progress.mp4"), Path.Combine(Root, "stills"));
            yield return WaitFor(() => SceneManager.GetActiveScene().name == SceneNames.Title && !Game.Loader.IsBusy, 20, "title");

            // --- title, character, difficulty ---
            var flow = Object.FindAnyObjectByType<TitleFlow>();
            yield return Sec(1.0f);
            _rec.Still("01_title");
            yield return Sec(2.5f);
            Log("Title");
            flow.title.newJourney.Press();
            yield return Sec(0.6f);
            _rec.Still("02_character");
            yield return Sec(1.6f);
            flow.characters.girl.Press();
            yield return Sec(0.6f);
            _rec.Still("03_difficulty");
            yield return Sec(1.8f);
            flow.difficulty.story.Press();
            Log("New journey: girl, Story");

            // --- the map and Lola ---
            yield return WaitFor(() => SceneManager.GetActiveScene().name == SceneNames.Map && !Game.Loader.IsBusy, 20, "map");
            var map = Object.FindAnyObjectByType<MapController>();
            yield return WaitFor(() => map.dialog.IsOpen, 5, "Lola's first meeting");
            yield return Sec(0.8f);
            _rec.Still("04_lola");
            Log("Map: Lola's first meeting");
            yield return Read(map.dialog, 1.4f);
            yield return Sec(1.2f);
            _rec.Still("05_map");

            for (int lv = 0; lv < 9; lv++)
            {
                Assert.IsTrue(map.Enter(), "enter level " + (lv + 1));
                yield return PlayLevel(lv);
                if (lv == 8) break;
                var ui = Object.FindAnyObjectByType<LevelRunner>().ui;
                yield return Sec(2.6f);
                ui.clear.map.Press();
                yield return WaitFor(() => SceneManager.GetActiveScene().name == SceneNames.Map && !Game.Loader.IsBusy, 20, "map");
                map = Object.FindAnyObjectByType<MapController>();
                yield return WaitFor(() => map.Model.At == lv + 1 && !map.Model.Walking, 8, "token walks on");
                yield return Sec(0.8f);
            }

            // --- the Ending ---
            var end = Object.FindAnyObjectByType<LevelRunner>().ui;
            yield return WaitFor(() => end.ending.IsOpen, 10, "the Ending");
            yield return Sec(1.5f);
            _rec.Still("40_ending");
            yield return Sec(6f);
            Log($"Ending. Video {_rec.EncodedFrames / (float)ProgressRecorder.VideoFps:0.0}s, audio captured: {_rec.AudioCaptured}");
            _rec.Finish();
            _rec = null;
        }

        IEnumerator PlayLevel(int lv)
        {
            LevelRunner runner = null;
            yield return WaitFor(() => (runner = Object.FindAnyObjectByType<LevelRunner>()) != null && runner.Session != null && !Game.Loader.IsBusy, 20, "level " + (lv + 1));
            var s = runner.Session;
            var sim = s.Sim;
            var ui = runner.ui;
            yield return WaitFor(() => ui.dialog.IsOpen, 5, "before-stage intro");
            int first = lv == 0 ? 0 : lv == 8 ? s.Layout.Rooms : s.Layout.ClueRoom;
            if (lv == 8)
            {
                // just left of the arena lantern: walking into it lights it and wakes the Bellkeeper, as in play
                var arena = s.World.Lanterns[s.Layout.Rooms];
                sim.Position = new Vector2(arena.x - 2.5f - sim.Size.x / 2, arena.y);
                sim.Velocity = Vector2.zero;
                if (runner.player.cameraFollow != null) runner.player.cameraFollow.SnapTo(sim.Center, sim.GravitySign);
            }
            else if (first > 0)
            {
                sim.BeginAt(first);
                if (runner.player.cameraFollow != null) runner.player.cameraFollow.SnapTo(sim.Center, sim.GravitySign);
            }
            yield return Sec(0.6f);
            _rec.Still($"{10 + lv * 3:00}_L{lv + 1}_intro");
            yield return Read(ui.dialog, 1.6f);
            yield return WaitFor(() => ui.Playing && !UiGate.Blocked, 5, "play");
            Log($"Level {lv + 1} ({s.Layout.Rooms} rooms, letter in room {s.Layout.ClueRoom + 1}) from room {first + 1}");
            if (System.Environment.GetEnvironmentVariable("SKP_VIDEO_QUICK") == "1")
            {
                // quick check of the capture: a few seconds of play with the HUD, then stop
                yield return Sec(3f);
                _rec.Still("quick_hud");
                yield return Sec(1f);
                Assert.Pass("quick capture check");
            }

            // never lose: hits are real, the hearts come back
            System.Action<PlayerSim> refill = p => { if (p.Vitality <= 1) p.Vitality = p.MaxVitality; };
            runner.player.Ticked += refill;
            try
            {
                if (lv == 8) { yield return Boss(runner); yield break; }
                int last = lv == 0 ? s.Layout.Rooms - 1 : s.Layout.ClueRoom;
                bool shot = false;
                for (int room = first; room <= last && !s.Completed; room++)
                {
                    var plan = LevelBot.PlanRoom(s.Layout, runner.player.tuning, s.Difficulty, s.Skills, room);
                    if (plan == null) { Log($"  room {room + 1}: the bot found no route; skipping to the next lantern"); sim.BeginAt(room + 1); continue; }
                    Log($"  room {room + 1}: {plan.Count} moves, {plan.Sum(p => p.Inputs.Length)} ticks");
                    yield return Replay(runner, plan, () => { if (!shot && room == first) { shot = true; _rec.Still($"{11 + lv * 3:00}_L{lv + 1}_play"); } });
                }
                if (!s.Completed)
                {
                    // the rooms between the letter and the exit are skipped in levels 2 to 8: straight to the exit door
                    var exit = s.World.Doors.First(d => d.Kind == DoorKind.Exit);
                    if (!s.World.Doors.Any(d => d.Kind == DoorKind.Exit && d.Rect.Overlaps(new Rect(sim.Position - Vector2.one, sim.Size + 2 * Vector2.one))))
                    {
                        Log($"  cut to the exit");
                        sim.SetDimension(Skp.Dimensions.Dim.Ordinary);
                        sim.Position = new Vector2(exit.Rect.center.x - sim.Size.x / 2, exit.Rect.y);
                        sim.Velocity = Vector2.zero;
                        if (runner.player.cameraFollow != null) runner.player.cameraFollow.SnapTo(sim.Center, sim.GravitySign);
                        yield return Sec(0.8f);
                    }
                    int k = 0;
                    runner.player.InputOverride = () => new PlayerInputFrame { InteractPressed = (k++ % 20) == 0 };
                    yield return WaitFor(() => s.Completed, 6, $"L{lv + 1} exit");
                }
                runner.player.InputOverride = null;
                yield return WaitFor(() => ui.clear.IsOpen, 5, "Level Clear");
                yield return Sec(0.8f);
                _rec.Still($"{12 + lv * 3:00}_L{lv + 1}_clear");
                Log($"  Level {lv + 1} clear; skills: {string.Join(", ", _save.Data.skills)}");
            }
            finally { runner.player.Ticked -= refill; }
        }

        /// <summary>Feeds each planned move's input frames, one per tick, starting it from where the bot planned it.</summary>
        IEnumerator Replay(LevelRunner runner, List<LevelBot.RouteStep> plan, System.Action onFirstMove)
        {
            var sim = runner.Session.Sim;
            var ui = runner.ui;
            int snaps = 0;
            PlayerInputFrame[] tape = null;
            int idx = 0;
            System.Action<PlayerSim> next = _ => idx++;
            runner.player.InputOverride = () => tape != null && idx < tape.Length ? tape[idx] : default;
            runner.player.Ticked += next;
            try
            {
                foreach (var step in plan)
                {
                    if (runner.Session.Completed) break;
                    if ((sim.Position - step.Start).sqrMagnitude > 0.04f * 0.04f || sim.Dimension != step.Dim)
                    {
                        if ((sim.Position - step.Start).sqrMagnitude > 0.25f) snaps++;
                        sim.Position = step.Start;
                        sim.Velocity = Vector2.zero;
                        sim.SetDimension(step.Dim);
                    }
                    tape = step.Inputs;
                    idx = 0;
                    onFirstMove?.Invoke();
                    onFirstMove = null;
                    int guard = 0;
                    while (idx < tape.Length && guard++ < tape.Length * 4 + 600)
                    {
                        if (ui.dialog.IsOpen)
                        {
                            _rec.Still($"letter_{runner.Session.Level + 1}_{ui.dialog.LineIndex}");
                            Log($"  dialog: {ui.dialog.speaker.text}");
                            yield return Read(ui.dialog, 2.2f);
                            continue;
                        }
                        if (runner.Session.Completed) break;
                        yield return null;
                    }
                }
            }
            finally
            {
                runner.player.Ticked -= next;
                runner.player.InputOverride = null;
            }
            if (snaps > 0) Log($"  {snaps} visible position corrections");
        }

        /// <summary>The Bellkeeper: stay under it, parry the toll with the ward, bolt it while it is stunned.</summary>
        IEnumerator Boss(LevelRunner runner)
        {
            var s = runner.Session;
            var sim = s.Sim;
            var boss = s.Boss;
            var ui = runner.ui;
            var lantern = s.World.Lanterns[s.Layout.Rooms];
            int k = 0;
            runner.player.InputOverride = () =>
            {
                k++;
                float dx = boss.Active ? boss.X - sim.Center.x : lantern.x + 0.5f - sim.Center.x;
                var f = new PlayerInputFrame { MoveX = Mathf.Abs(dx) > (boss.Active ? 1.2f : 0.2f) ? (dx > 0 ? 1 : -1) : 0 };
                if (boss.State == BossState.Toll && boss.TollTicks >= 60 && sim.WardCooldown == 0) f.WardPressed = true;
                if (boss.State == BossState.Stun && k % 24 == 0) f.BoltPressed = true;
                // hop the shockwaves
                foreach (var w in boss.Waves) if (Mathf.Abs(w - sim.Center.x) < 1.6f && sim.Grounded) { f.JumpPressed = true; f.JumpHeld = true; }
                return f;
            };
            yield return WaitFor(() => boss.Active, 10, "the Bellkeeper wakes");
            Log("  the Bellkeeper wakes");
            int frames = 0, maxFrames = 75 * ProgressRecorder.GameFps;
            bool shot = false;
            while (!boss.Dead && frames++ < maxFrames)
            {
                if (ui.dialog.IsOpen) { yield return Read(ui.dialog, 1.8f); continue; }
                if (!shot && frames == 8 * ProgressRecorder.GameFps) { shot = true; _rec.Still("35_boss"); }
                if (boss.State == BossState.Stun && frames % 60 == 0) _rec.Still("36_boss_stunned");
                yield return null;
            }
            if (!boss.Dead)
            {
                Log($"  the Bellkeeper still had {boss.Hp}/{boss.MaxHp}: the last parry is helped");
                boss.Hp = Mathf.Min(boss.Hp, Bellkeeper.ParryDamage);
                yield return WaitFor(() => boss.Dead, 40, "the Bellkeeper falls");
            }
            Log("  the Bellkeeper falls");
            runner.player.InputOverride = () => default;
            yield return WaitFor(() => boss.ExitDoor.HasValue, 8, "the last door");
            yield return Sec(1.5f);
            var door = boss.ExitDoor.Value;
            // walk to the last door, then knock
            runner.player.InputOverride = () =>
            {
                float dx = door.Rect.center.x - sim.Center.x;
                return new PlayerInputFrame { MoveX = Mathf.Abs(dx) > 0.3f ? (dx > 0 ? 1 : -1) : 0, InteractPressed = Mathf.Abs(dx) <= 0.6f && (k++ % 20) == 0 };
            };
            yield return WaitFor(() => s.Completed || ui.ending.IsOpen, 20, "through the last door");
            runner.player.InputOverride = null;
        }
    }
}
