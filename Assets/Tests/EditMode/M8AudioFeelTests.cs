using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Skp.Audio;
using Skp.Core;
using Skp.Dimensions;
using Skp.Levels;
using Skp.Player;
using Skp.Save;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace Skp.Tests
{
    /// <summary>M8 audio and game feel, without play mode: assets, the bank, headroom, seamless loops, the dimension
    /// sound, and shake, hit-stop and vibration honouring the settings.</summary>
    public class M8AudioFeelTests
    {
        static AudioTuning Tuning => AssetDatabase.LoadAssetAtPath<AudioTuning>(SkpAudioSetup.TuningPath);
        static FeelTuning Feel => AssetDatabase.LoadAssetAtPath<FeelTuning>(SkpAudioSetup.FeelPath);

        string _dir;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "skp_m8_" + Guid.NewGuid().ToString("N"));
            CameraFollow.ShakeEnabledOverride = null;
            Haptics.EnabledOverride = null;
            Haptics.ResetCounts();
            Sfx.ResetCounts();
        }

        [TearDown]
        public void TearDown()
        {
            CameraFollow.ShakeEnabledOverride = null;
            Haptics.EnabledOverride = null;
            foreach (var go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include)) if (go != null && go.name.StartsWith("m8test")) UnityEngine.Object.DestroyImmediate(go);
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        // ------------------------------------------------------------ assets

        [Test]
        public void Mixer_HasMusicAndSfxGroups_WithGameAndUiUnderSfx_AndTheExposedParameters()
        {
            var m = AssetDatabase.LoadAssetAtPath<AudioMixer>(SkpAudioSetup.MixerPath);
            Assert.IsNotNull(m, "mixer asset");
            // FindMatchingGroups matches by path prefix, so look for the exact group name
            foreach (var p in new[] { "Master/Music", "Master/SFX", "Master/SFX/Game", "Master/SFX/UI" })
                Assert.IsTrue(m.FindMatchingGroups(p).Any(g => g.name == p.Substring(p.LastIndexOf('/') + 1)), "group " + p);
            Assert.AreEqual(3, m.FindMatchingGroups("Master/SFX").Length, "SFX holds Game and UI");
            var t = Tuning;
            Assert.AreSame(m, t.mixer);
            Assert.AreEqual("Music", t.musicGroup.name);
            Assert.AreEqual("SFX", t.sfxGroup.name);
            Assert.AreEqual("Game", t.gameGroup.name);
            Assert.AreEqual("UI", t.uiGroup.name);
            var exposed = ((Array)m.GetType().GetProperty("exposedParameters", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(m))
                .Cast<object>().Select(e => (string)e.GetType().GetField("name").GetValue(e)).ToArray();
            CollectionAssert.IsSubsetOf(SkpAudioSetup.Exposed, exposed);
        }

        [Test]
        public void Music_OneLoopPerActPlusMenuAndBoss_AllStreamedVorbis_AndTheProvidedOggsUntouched()
        {
            var t = Tuning;
            foreach (MusicTrack tr in new[] { MusicTrack.Menu, MusicTrack.Act1, MusicTrack.Act2, MusicTrack.Act3, MusicTrack.Boss })
            {
                var c = t.Track(tr);
                Assert.IsNotNull(c, tr + " clip");
                string path = AssetDatabase.GetAssetPath(c);
                StringAssert.StartsWith(SkpAudioSetup.LoopDir, path, tr + " uses the seamless loop");
                var ai = (AudioImporter)AssetImporter.GetAtPath(path);
                Assert.AreEqual(AudioClipLoadType.Streaming, ai.defaultSampleSettings.loadType, tr + " streamed");
                Assert.AreEqual(AudioCompressionFormat.Vorbis, ai.defaultSampleSettings.compressionFormat, tr + " ships as Vorbis (OGG)");
                Assert.Greater(c.length, 10f, tr + " is a real track");
            }
            foreach (var tr in SkpAudioSetup.Tracks) Assert.IsTrue(File.Exists($"Assets/Audio/Music/mus_{tr}.ogg"), "provided ogg kept: " + tr);
        }

        static readonly string[] Required =
        {
            "jump", "skill_dj", "land", "dash", "ward", "skill_veil", "skill_veil_end", "skill_bolt", "skill_bolt3", "bolt_hit",
            "burn", "hurt", "die", "lantern", "flame", "shard", "letter", "skill_learned", "skill_known", "door", "locked",
            "select", "bell", "toll", "parry", "kill", "stun", "crumble", "splash", "win",
        };

        [Test]
        public void SfxBank_HasEveryCueTheGameUses()
        {
            var bank = AssetDatabase.LoadAssetAtPath<SfxBank>(SkpSkillSetup.BankPath);
            foreach (var cue in Required) Assert.IsTrue(bank.TryGet(cue, out _), "bank has " + cue);
            // and every cue name the gameplay code sends is one of them
            foreach (var cue in new[] { GameCues.Jump, GameCues.Land, GameCues.Dash, GameCues.Ward, GameCues.Hurt, GameCues.Die, GameCues.Lantern, GameCues.Flame,
                                        GameCues.Fragment, GameCues.Letter, GameCues.Door, GameCues.Locked, GameCues.Kill, GameCues.Stun, GameCues.Crumble,
                                        SkillCues.VeilEnd, SkillCues.BoltHit, SkillCues.Burn })
                Assert.IsTrue(bank.TryGet(cue, out _), "bank has gameplay cue " + cue);
        }

        [Test]
        public void NoClipping_EveryEffectPeaksUnder0_71_TheMusicUnder0_86_AndTheGroupsLeaveHeadroom()
        {
            var bank = AssetDatabase.LoadAssetAtPath<SfxBank>(SkpSkillSetup.BankPath);
            foreach (var e in bank.entries)
            {
                float peak = SkpAudioSetup.Peak(e.clip) * e.volume;
                Assert.LessOrEqual(peak, SkpAudioSetup.SfxPeak + 0.01f, e.cue + " peak after its volume");
            }
            foreach (var tr in SkpAudioSetup.Tracks)
            {
                var wav = ReadWav(SkpAudioSetup.LoopPath(tr), out _);
                Assert.LessOrEqual(wav.Max(Mathf.Abs), SkpAudioSetup.MusicPeak + 0.01f, tr + " loop peak");
            }
            var t = Tuning;
            Assert.LessOrEqual(t.musicDb, -6f, "music sits under the effects");
            Assert.LessOrEqual(t.sfxDb, 0f, "effects never boosted");
            Assert.LessOrEqual(t.gameVoices + t.uiVoices, 12, "a fixed, small voice pool");
            Assert.Greater(t.sameCueInterval, 0f, "a repeated cue cannot stack");
        }

        [Test]
        public void MusicLoops_AreSeamless_NoClickAtTheSeam_AndTheSameLoudnessEitherSide()
        {
            foreach (var tr in SkpAudioSetup.Tracks)
            {
                var d = ReadWav(SkpAudioSetup.LoopPath(tr), out int rate);
                // the jump from the last sample to the first is no bigger than the track's ordinary sample-to-sample steps
                float seam = Mathf.Abs(d[d.Length - 1] - d[0]);
                var steps = new float[d.Length - 1];
                for (int i = 1; i < d.Length; i++) steps[i - 1] = Mathf.Abs(d[i] - d[i - 1]);
                Array.Sort(steps);
                float p999 = steps[(int)(steps.Length * 0.999f)];
                Assert.LessOrEqual(seam, Mathf.Max(0.02f, p999), $"{tr}: seam step {seam:0.000} vs 99.9th percentile step {p999:0.000}");
                int w = rate / 4;
                float head = Rms(d, 0, w), tail = Rms(d, d.Length - w, w);
                Assert.Less(Mathf.Abs(20f * Mathf.Log10((head + 1e-4f) / (tail + 1e-4f))), 6f, $"{tr}: loudness either side of the seam ({head:0.000} vs {tail:0.000})");
            }
        }

        [Test]
        public void CrossfadeLoop_JoinsTailIntoHead_ContinuousForAnySignal()
        {
            int n = 9000;
            var src = new float[n];
            for (int i = 0; i < n; i++) src[i] = Mathf.Sin(i * 0.0137f) * 0.5f + Mathf.Sin(i * 0.311f) * 0.2f;
            var o = SkpAudioSetup.CrossfadeLoop(src, 2000);
            Assert.AreEqual(n - 2000, o.Length);
            // the loop point: last output sample is src[n-2001], the next is o[0] ~ src[n-2000] (fully the tail at the start)
            Assert.Less(Mathf.Abs(o[0] - src[o.Length]), 0.01f, "the head starts as the tail's continuation");
            Assert.Less(Mathf.Abs(o[1999] - src[1999]), 0.01f, "and ends the crossfade as the head");
        }

        static float Rms(float[] d, int start, int len)
        {
            double s = 0;
            for (int i = start; i < start + len; i++) s += d[i] * d[i];
            return (float)Math.Sqrt(s / len);
        }

        static float[] ReadWav(string path, out int rate)
        {
            var b = File.ReadAllBytes(path);
            rate = BitConverter.ToInt32(b, 24);
            int dataLen = BitConverter.ToInt32(b, 40);
            var o = new float[dataLen / 2];
            for (int i = 0; i < o.Length; i++) o[i] = BitConverter.ToInt16(b, 44 + i * 2) / 32768f;
            return o;
        }

        // ------------------------------------------------------------ dimensions

        [Test]
        public void DimensionSound_SpiritIsMuffled_InvertedIsPitchedDown_OrdinaryIsOpen()
        {
            var t = Tuning;
            Music.DimensionSound(Dim.Ordinary, t, out float oc, out float op);
            Music.DimensionSound(Dim.Spirit, t, out float sc, out float sp);
            Music.DimensionSound(Dim.Inverted, t, out float ic, out float ip);
            Music.DimensionSound(Dim.None, t, out float nc, out float np);
            Assert.AreEqual(t.openCutoffHz, oc); Assert.AreEqual(1f, op);
            Assert.Less(sc, 2000f, "Spirit muffles"); Assert.AreEqual(1f, sp, "Spirit keeps the tempo");
            Assert.Less(ip, 0.95f, "Inverted pitches down"); Assert.Greater(ip, 0.75f, "not sluggish");
            Assert.Greater(ic, sc, "Inverted less muffled than Spirit");
            Assert.AreEqual(oc, nc); Assert.AreEqual(op, np);
            Assert.That(t.dimensionGlideSeconds, Is.InRange(0.2f, 1f), "a glide, not a cut, and not slow");
        }

        // ------------------------------------------------------------ shake, hit-stop, vibration

        CameraFollow Camera()
        {
            var go = new GameObject("m8test cam");
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 270f / 32f;
            var f = go.AddComponent<CameraFollow>();
            f.cam = cam;
            f.feel = Feel;
            f.bounds = new Rect(-100, -100, 200, 200);
            return f;
        }

        [Test]
        public void ScreenShake_MovesThenSettles_PixelSnapped_AndTheFollowIsUntouched()
        {
            CameraFollow.ShakeEnabledOverride = true;
            var f = Camera();
            f.SnapTo(Vector2.zero, 1);
            var b = f.Base;
            f.AddTrauma(0.8f);
            float most = 0;
            for (int i = 0; i < 6; i++) { f.StepShake(1f / 60f); most = Mathf.Max(most, f.ShakeOffset.magnitude); }
            Assert.Greater(most, 0f, "the view shakes");
            Assert.LessOrEqual(most, Feel.maxShake * 1.5f, "within the tuned size");
            Assert.AreEqual(Mathf.Round(f.ShakeOffset.x * 16f), f.ShakeOffset.x * 16f, 1e-4, "whole pixels");
            Assert.AreEqual(b, f.Base, "the follow position is not moved by the shake");
            for (int i = 0; i < 120; i++) f.StepShake(1f / 60f);
            Assert.AreEqual(0f, f.Trauma);
            Assert.AreEqual(Vector2.zero, f.ShakeOffset, "settles");
            Assert.AreEqual((Vector2)b, (Vector2)f.transform.position, "back on the follow position");
        }

        [Test]
        public void ScreenShake_Off_NothingMoves()
        {
            CameraFollow.ShakeEnabledOverride = false;
            var f = Camera();
            f.SnapTo(Vector2.zero, 1);
            f.AddTrauma(1f);
            for (int i = 0; i < 10; i++) f.StepShake(1f / 60f);
            Assert.AreEqual(0f, f.Trauma);
            Assert.AreEqual(Vector2.zero, f.ShakeOffset);
        }

        [Test]
        public void ScreenShake_TurnedOffMidShake_StopsAtOnce()
        {
            CameraFollow.ShakeEnabledOverride = true;
            var f = Camera();
            f.AddTrauma(1f);
            f.StepShake(1f / 60f);
            CameraFollow.ShakeEnabledOverride = false;
            f.StepShake(1f / 60f);
            Assert.AreEqual(Vector2.zero, f.ShakeOffset);
        }

        [Test]
        public void Vibration_RespectsTheSetting()
        {
            Haptics.EnabledOverride = false;
            Haptics.Pulse(60, 0.5f);
            Assert.AreEqual(0, Haptics.Pulses);
            Assert.AreEqual(1, Haptics.Suppressed);
            Haptics.EnabledOverride = true;
            Haptics.Pulse(60, 0.5f);
            Assert.AreEqual(1, Haptics.Pulses);
            Assert.AreEqual(60, Haptics.LastMs);
            Haptics.Pulse(0, 0f);
            Assert.AreEqual(1, Haptics.Pulses, "an empty kick is not a pulse");
        }

        LevelSession Session(int lv)
        {
            var save = new SaveService(_dir);
            save.Load();
            save.Data.unlocked = 9;
            var cfg = AssetDatabase.LoadAssetAtPath<LevelConfig>($"Assets/Data/Levels/LevelConfig_{lv + 1:00}.asset");
            return new LevelSession(cfg.spec, new LevelRequest { Level = lv, Difficulty = "standard" },
                AssetDatabase.LoadAssetAtPath<PlayerTuning>("Assets/Data/PlayerTuning.asset"),
                AssetDatabase.LoadAssetAtPath<DifficultyTable>("Assets/Data/DifficultyTable.asset"),
                AssetDatabase.LoadAssetAtPath<DifficultyCurve>("Assets/Data/DifficultyCurve.asset"), save);
        }

        (LevelSession, PlayerController, CameraFollow, LevelFx) Rig(int lv)
        {
            var s = Session(lv);
            var cam = Camera();
            var pgo = new GameObject("m8test player");
            var pc = pgo.AddComponent<PlayerController>();
            pc.cameraFollow = cam;
            var fx = LevelFx.Create(new GameObject("m8test fx").transform, Feel);
            return (s, pc, cam, fx);
        }

        [Test]
        public void Hurt_Shakes_HitStops_Vibrates_AndSounds_WhenTheSettingsAllowIt()
        {
            CameraFollow.ShakeEnabledOverride = true;
            Haptics.EnabledOverride = true;
            var (s, pc, cam, fx) = Rig(0);
            new LevelFeel(s, pc, fx, Feel);
            new GameCues(s.Sim);
            s.Sim.Invulnerable = 0;
            Assert.IsTrue(s.Sim.TryHurt(1, null));
            Assert.Greater(cam.Trauma, 0f, "shake");
            Assert.Greater(pc.HitStopLeft, 0f, "hit-stop");
            Assert.AreEqual(1, Haptics.Pulses, "vibration");
            Assert.AreEqual(1, Sfx.Count(GameCues.Hurt), "the hurt sound");
        }

        [Test]
        public void Hurt_WithShakeAndVibrationOff_NeitherHappens_ButHitStopAndSoundStill()
        {
            CameraFollow.ShakeEnabledOverride = false;
            Haptics.EnabledOverride = false;
            var (s, pc, cam, fx) = Rig(0);
            new LevelFeel(s, pc, fx, Feel);
            new GameCues(s.Sim);
            s.Sim.Invulnerable = 0;
            s.Sim.TryHurt(1, null);
            Assert.AreEqual(0f, cam.Trauma, "no shake");
            Assert.AreEqual(0, Haptics.Pulses, "no vibration");
            Assert.AreEqual(1, Haptics.Suppressed);
            Assert.Greater(pc.HitStopLeft, 0f, "hit-stop is not a setting");
            Assert.AreEqual(1, Sfx.Count(GameCues.Hurt));
        }

        [Test]
        public void HitStop_IsShort_AndNeverStacks()
        {
            var go = new GameObject("m8test hitstop");
            var pc = go.AddComponent<PlayerController>();
            pc.HitStop(0.07f);
            pc.HitStop(0.05f);
            Assert.AreEqual(0.07f, pc.HitStopLeft, 1e-5);
            pc.HitStop(5f);
            Assert.LessOrEqual(pc.HitStopLeft, 0.25f, "capped");
            foreach (var k in new[] { Feel.hurt, Feel.die, Feel.kill, Feel.parry, Feel.bossDefeated }) Assert.LessOrEqual(k.hitStop, 0.25f);
        }

        [Test]
        public void Particles_FlameFragmentVeilBoltAndBurntThorns_Emit()
        {
            var (s, pc, cam, fx) = Rig(0);
            new LevelFeel(s, pc, fx, Feel);
            var flame = s.World.Items.First(i => i.Kind == ItemKind.Flame);
            s.Sim.SetDimension(flame.Mask.HasFlag(Dim.Ordinary) ? Dim.Ordinary : Dim.Spirit);
            s.Sim.Position = new Vector2(flame.Rect.center.x - s.Sim.Size.x / 2, flame.Rect.y);
            s.Sim.Step(default);
            Assert.Greater(fx.Emitted[(int)LevelFx.Kind.Flame], 0, "flame burst");
            fx.Emit(LevelFx.Kind.Fragment, Vector2.zero);
            fx.Ring(LevelFx.Kind.Veil, Vector2.zero, 1f, 8);
            fx.Emit(LevelFx.Kind.Bolt, Vector2.zero);
            fx.Emit(LevelFx.Kind.Burnt, Vector2.zero);
            foreach (var k in new[] { LevelFx.Kind.Fragment, LevelFx.Kind.Veil, LevelFx.Kind.Bolt, LevelFx.Kind.Burnt }) Assert.Greater(fx.Emitted[(int)k], 0, k + " burst");
            foreach (var b in new[] { Feel.flame, Feel.fragment, Feel.veil, Feel.bolt, Feel.burnt }) Assert.IsNotNull(b.sprite, "particle sprite");
            Assert.IsNotNull(Feel.particleMaterial);
        }

        [Test]
        public void Credits_ListTheAudioAndItsLicences()
        {
            string all = Skp.UI.StringDefaults.En["credits.body"];
            StringAssert.Contains("Press Start 2P", all);
            StringAssert.Contains("SIL Open Font License", all);
            StringAssert.Contains("Music", all);
            StringAssert.Contains("Sound effects", all);
            StringAssert.Contains("original", all);
        }
    }
}
