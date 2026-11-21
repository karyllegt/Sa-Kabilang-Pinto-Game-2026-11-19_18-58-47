using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Skp.Audio;
using Skp.Core;
using Skp.Dimensions;
using Skp.Levels;
using Skp.Player;
using Skp.Save;
using Skp.UI;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Skp.Tests
{
    /// <summary>M8 in the running game: the Settings toggles mute the right mixer groups, pause ducks and resumes,
    /// the music follows the dimension, shake and vibration honour the settings, and gameplay allocates nothing per frame.</summary>
    public class AudioFeelPlayModeTests
    {
        string _dir;
        SaveService _save;
        GameRoot Root => GameRoot.Instance;
        AudioService Service => AudioService.Instance;
        AudioTuning T => Service.Tuning;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "skp_m8_play_" + System.Guid.NewGuid().ToString("N"));
            _save = new SaveService(_dir);
            _save.Load();
            _save.Data.metLola = true;
            _save.Data.diff = "standard";
            Haptics.EnabledOverride = null;
            CameraFollow.ShakeEnabledOverride = null;
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            Music.Duck(false);
            if (Root != null && Root.Settings != null)
            {
                var d = Root.Settings.Data;
                d.music = d.sfx = d.vibration = d.shake = true;
            }
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        static IEnumerator WaitFor(System.Func<bool> cond, float timeout = 15f, string what = "condition")
        {
            float t = 0;
            while (!cond() && t < timeout) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(cond(), "timed out waiting for " + what);
        }

        static IEnumerator Seconds(float s)
        {
            float t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < s) yield return null;
        }

        static IEnumerator Dismiss(DialogBox d)
        {
            float t0 = Time.realtimeSinceStartup;
            while (d.IsOpen && Time.realtimeSinceStartup - t0 < 10f)
            {
                yield return WaitFor(() => !UiGate.Blocked, 5, "wipe");
                d.Advance();
                yield return null;
            }
            Assert.IsFalse(d.IsOpen, "dialog closed");
        }

        IEnumerator Boot()
        {
            if (Root == null) { SceneManager.LoadScene(SceneNames.Boot); yield return WaitFor(() => Root != null, 5, "GameRoot"); }
            Root.UseSave(_save);
            yield return WaitFor(() => !Root.Loader.IsBusy, 10, "idle loader");
        }

        IEnumerator StartLevel(int level)
        {
            yield return Boot();
            GameSession.Pending = new LevelRequest { Level = level, Difficulty = "standard" };
            var old = Object.FindAnyObjectByType<LevelRunner>();
            SceneManager.LoadScene(SceneNames.Level);
            LevelRunner runner = null;
            yield return WaitFor(() => (runner = Object.FindAnyObjectByType<LevelRunner>()) != null && runner != old && runner.Session != null, 10, "level");
            yield return WaitFor(() => runner.ui.dialog.IsOpen, 5, "intro");
            yield return Dismiss(runner.ui.dialog);
            yield return WaitFor(() => runner.ui.Playing && !UiGate.Blocked, 5, "play");
        }

        static LevelRunner Runner => Object.FindAnyObjectByType<LevelRunner>();

        float Mixer(string p) { Assert.IsTrue(T.mixer.GetFloat(p, out float v), "mixer exposes " + p); return v; }

        void SetSettings(System.Action<GameSettings> change)
        {
            change(Root.Settings.Data);
            Root.Settings.Save(); // raises Changed
        }

        // ------------------------------------------------------------ mixer and settings

        [UnityTest]
        public IEnumerator SettingsToggles_MuteTheMusicGroupAndTheSfxGroup_Separately()
        {
            yield return Boot();
            Assert.IsNotNull(Service, "audio service on the GameRoot");
            Assert.IsNotNull(T.mixer, "mixer assigned");

            SetSettings(d => { d.music = true; d.sfx = true; });
            Service.Settle();
            Assert.AreEqual(T.musicDb, Mixer(AudioTuning.MusicVolume), 0.01f);
            Assert.AreEqual(T.sfxDb, Mixer(AudioTuning.SfxVolume), 0.01f);

            SetSettings(d => d.music = false);
            Service.Settle();
            Assert.AreEqual(T.muteDb, Mixer(AudioTuning.MusicVolume), 0.01f, "music off mutes Music");
            Assert.AreEqual(T.sfxDb, Mixer(AudioTuning.SfxVolume), 0.01f, "and leaves SFX");

            SetSettings(d => { d.music = true; d.sfx = false; });
            Service.Settle();
            Assert.AreEqual(T.musicDb, Mixer(AudioTuning.MusicVolume), 0.01f, "music back");
            Assert.AreEqual(T.muteDb, Mixer(AudioTuning.SfxVolume), 0.01f, "sound effects off mutes SFX (game and menu sounds)");

            SetSettings(d => d.sfx = true);
            yield return Seconds(T.levelFadeSeconds + 0.2f);
            Assert.AreEqual(T.sfxDb, Mixer(AudioTuning.SfxVolume), 0.05f, "the level glides back without Settle");

            // every voice is routed into the mixer: game sounds to Game, menu sounds to UI, music to Music
            var sources = Root.GetComponents<AudioSource>();
            Assert.IsTrue(sources.All(s => s.outputAudioMixerGroup != null), "no voice bypasses the mixer");
            Assert.AreEqual(2, sources.Count(s => s.outputAudioMixerGroup == T.musicGroup));
            Assert.AreEqual(T.gameVoices, sources.Count(s => s.outputAudioMixerGroup == T.gameGroup));
            Assert.AreEqual(T.uiVoices, sources.Count(s => s.outputAudioMixerGroup == T.uiGroup));
        }

        [UnityTest]
        public IEnumerator Menus_PlayTheMenuTheme_Levels_PlayTheirAct()
        {
            yield return Boot();
            Root.Loader.Load(SceneNames.Title);
            yield return WaitFor(() => SceneManager.GetActiveScene().name == SceneNames.Title && !Root.Loader.IsBusy, 10, "title");
            Assert.AreEqual(MusicTrack.Menu, Music.Current);
            yield return WaitFor(() => Service.PlayingClip == T.menu, 3, "menu clip");
            yield return StartLevel(3); // act II
            Assert.AreEqual(MusicTrack.Act2, Music.Current);
            yield return Seconds(T.trackCrossfadeSeconds + 0.2f);
            Assert.AreSame(T.act2, Service.PlayingClip, "the act II loop");
            Assert.IsTrue(Root.GetComponents<AudioSource>().Where(s => s.outputAudioMixerGroup == T.musicGroup).All(s => s.loop), "music loops");
        }

        [UnityTest]
        public IEnumerator Pause_DucksTheMusicAndSilencesGameSounds_Resume_BringsThemBack()
        {
            yield return StartLevel(0);
            var ui = Runner.ui;
            Service.Settle();
            float m0 = Mixer(AudioTuning.MusicVolume);
            Assert.AreEqual(T.musicDb, m0, 0.01f);
            Assert.AreEqual(0f, Mixer(AudioTuning.GameVolume), 0.01f);

            ui.Pause();
            Assert.IsTrue(Music.Ducked);
            yield return Seconds(1.0f); // timeScale is 0: the fade runs on unscaled time
            Assert.AreEqual(T.musicDb + T.pauseMusicDuckDb, Mixer(AudioTuning.MusicVolume), 0.1f, "music ducked while paused");
            Assert.AreEqual(T.pauseGameDuckDb, Mixer(AudioTuning.GameVolume), 0.1f, "game sounds silenced");
            Assert.AreEqual(T.sfxDb, Mixer(AudioTuning.SfxVolume), 0.1f, "menu sounds still heard");
            Assert.AreEqual(MusicTrack.Act1, Music.Current, "same track, not stopped");

            ui.Resume();
            Assert.IsFalse(Music.Ducked);
            yield return Seconds(1.0f);
            Assert.AreEqual(T.musicDb, Mixer(AudioTuning.MusicVolume), 0.1f, "music back after resume");
            Assert.AreEqual(0f, Mixer(AudioTuning.GameVolume), 0.1f, "game sounds back");
        }

        [UnityTest]
        public IEnumerator Doors_GlideTheMusicIntoSpiritAndInverted_AndBackAtTheLantern()
        {
            yield return StartLevel(0);
            var sim = Runner.Session.Sim;
            Service.Settle();
            Assert.AreEqual(T.openCutoffHz, Mixer(AudioTuning.MusicCutoff), 1f);

            // the start lantern pulls Tala back to Ordinary on her next tick: hold the simulation still
            Runner.ui.enabled = false; // it unfreezes the player every frame
            Runner.player.Frozen = true;
            sim.SetDimension(Dim.Spirit);
            Assert.AreEqual(Dim.Spirit, Music.Dimension, "the runner follows the dimension at once");
            yield return null; yield return null;
            float early = Service.CutoffHz;
            Assert.Greater(early, T.spiritCutoffHz * 1.05f, "a glide, not a cut");
            yield return Seconds(T.dimensionGlideSeconds + 0.3f);
            Music.DimensionSound(Dim.Spirit, T, out float sc, out float sp);
            Assert.AreEqual(sc, Mixer(AudioTuning.MusicCutoff), sc * 0.02f, "Spirit muffles the music");
            Assert.AreEqual(sp, Mixer(AudioTuning.MusicPitch), 0.01f);

            sim.SetDimension(Dim.Inverted);
            yield return Seconds(T.dimensionGlideSeconds + 0.3f);
            Music.DimensionSound(Dim.Inverted, T, out float ic, out float ip);
            Assert.AreEqual(ip, Mixer(AudioTuning.MusicPitch), 0.01f, "Inverted pitches down");
            Assert.AreEqual(ic, Mixer(AudioTuning.MusicCutoff), ic * 0.02f);

            sim.SetDimension(Dim.Ordinary);
            yield return Seconds(T.dimensionGlideSeconds + 0.3f);
            Assert.AreEqual(1f, Mixer(AudioTuning.MusicPitch), 0.01f);
            Assert.AreEqual(T.openCutoffHz, Mixer(AudioTuning.MusicCutoff), T.openCutoffHz * 0.02f, "Ordinary is open again");
            Runner.ui.enabled = true;
        }

        // ------------------------------------------------------------ shake and vibration settings in play

        [UnityTest]
        public IEnumerator ShakeAndVibration_FollowTheSettings_InTheLevel()
        {
            yield return StartLevel(0);
            var r = Runner;
            var sim = r.Session.Sim;
            var cam = r.player.cameraFollow;

            SetSettings(d => { d.shake = false; d.vibration = false; });
            Haptics.ResetCounts();
            sim.Invulnerable = 0;
            Assert.IsTrue(sim.TryHurt(1, null));
            Assert.AreEqual(0f, cam.Trauma, "shake off: no trauma");
            yield return null;
            Assert.AreEqual(Vector2.zero, cam.ShakeOffset, "shake off: the view is still");
            Assert.AreEqual(0, Haptics.Pulses, "vibration off: no pulse");
            Assert.Greater(Haptics.Suppressed, 0);

            yield return Seconds(1.2f);
            SetSettings(d => { d.shake = true; d.vibration = true; });
            Haptics.ResetCounts();
            sim.Invulnerable = 0;
            Assert.IsTrue(sim.TryHurt(1, null));
            Assert.Greater(cam.Trauma, 0f, "shake on");
            Assert.AreEqual(1, Haptics.Pulses, "vibration on");
            float most = 0;
            for (int i = 0; i < 12; i++) { yield return null; most = Mathf.Max(most, cam.ShakeOffset.magnitude); }
            Assert.Greater(most, 0f, "the view shakes");
        }

        // ------------------------------------------------------------ garbage

        [UnityTest]
        [Timeout(120000)]
        public IEnumerator Gameplay_AllocatesNoGarbagePerFrame_AfterWarmUp()
        {
            yield return StartLevel(0);
            var r = Runner;
            var sim = r.Session.Sim;
            sim.Invulnerable = 1000000;
            int frame = 0;
            // run back and forth, jumping, dashing and warding: sounds, particles, shake and the HUD all work
            r.player.InputOverride = () =>
            {
                frame++;
                int phase = (frame / 90) % 2;
                return new PlayerInputFrame
                {
                    MoveX = phase == 0 ? 1 : -1,
                    JumpHeld = frame % 40 < 12,
                    JumpPressed = frame % 40 == 0,
                    DashPressed = frame % 120 == 60,
                    WardPressed = frame % 150 == 75,
                };
            };
            // Our code is run by hand (each game behaviour's Update and LateUpdate, in execution order) between
            // GC.GetAllocatedBytesForCurrentThread readings, so every byte is attributed to a component. The frame
            // recorder then shows the Editor's own floor (editor and test runner allocate every frame in play mode;
            // a player build has no such floor), and gameplay frames must not rise above it.
            var comps = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .Where(b => b.isActiveAndEnabled && b.GetType().Assembly.GetName().Name == "Skp.Runtime")
                .OrderBy(b => ((DefaultExecutionOrder)System.Attribute.GetCustomAttribute(b.GetType(), typeof(DefaultExecutionOrder)))?.order ?? 0)
                .ToArray();
            System.Action Hook(MonoBehaviour b, string m)
            {
                var mi = b.GetType().GetMethod(m, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic, null, System.Type.EmptyTypes, null);
                return mi == null ? null : (System.Action)System.Delegate.CreateDelegate(typeof(System.Action), b, mi);
            }
            var updates = comps.Select(b => (name: b.GetType().Name, up: Hook(b, "Update"), late: Hook(b, "LateUpdate"))).Where(x => x.up != null || x.late != null).ToArray();
            foreach (var b in comps) if (updates.Any(x => x.name == b.GetType().Name)) b.enabled = false;
            var bytes = new long[updates.Length];
            int dirtyFrames = 0;

            void RunFrame(bool measure)
            {
                bool dirtyNow = false;
                for (int pass = 0; pass < 2; pass++)
                    for (int i = 0; i < updates.Length; i++)
                    {
                        var a = pass == 0 ? updates[i].up : updates[i].late;
                        if (a == null) continue;
                        long b0 = System.GC.GetAllocatedBytesForCurrentThread();
                        a();
                        long d = System.GC.GetAllocatedBytesForCurrentThread() - b0;
                        if (measure && d > 0) { bytes[i] += d; dirtyNow = true; }
                    }
                if (dirtyNow) dirtyFrames++;
            }

            // warm-up: first sounds, pools, particle buffers, the HUD's first strings
            for (int i = 0; i < 300; i++) { yield return null; RunFrame(false); }

            using var rec = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
            Assert.IsTrue(rec.Valid, "the GC recorder is available");
            var play = new List<long>();
            for (int i = 0; i < 300; i++) { yield return null; RunFrame(true); if (i >= 2) play.Add(rec.LastValue); }

            // the Editor's floor: the same scene with the game held still (our behaviours not run)
            var idle = new List<long>();
            for (int i = 0; i < 120; i++) { yield return null; if (i >= 2) idle.Add(rec.LastValue); }
            foreach (var b in comps) if (b != null) b.enabled = true;
            r.player.InputOverride = null;

            play.Sort(); idle.Sort();
            long Median(List<long> l) => l[l.Count / 2];
            var report = string.Join(", ", updates.Select((x, i) => (x.name, b: bytes[i])).Where(x => x.b > 0).Select(x => $"{x.name} {x.b} B"));
            Debug.Log($"[GC] game code: {dirtyFrames} of 300 frames allocated; {(report.Length > 0 ? report : "nothing")}");
            Debug.Log($"[GC] frame recorder: gameplay median {Median(play)} B, editor idle median {Median(idle)} B");
            Assert.AreEqual(0, dirtyFrames, "gameplay code allocated in some frames: " + report);
            Assert.LessOrEqual(Median(play), Median(idle) + 64, "gameplay frames stay at the Editor's floor");
        }
    }
}
