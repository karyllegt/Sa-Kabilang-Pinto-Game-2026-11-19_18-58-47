using System;
using NUnit.Framework;
using Skp.Audio;
using Skp.Core;
using Skp.Levels;
using Skp.Player;
using Skp.UI;
using UnityEditor;
using UnityEngine;

namespace Skp.Tests
{
    public class PresentationUpgradeTests
    {
        [Test]
        public void ActivePlayerArt_CoversEveryState_WithoutChangingWorldHeightOrShotTiming()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SkpTestRoomBuilder.PlayerPrefabPath);
            var view = prefab.GetComponentInChildren<PlayerView>(true);
            foreach (var set in new[] { view.girlAnims, view.boyAnims })
            {
                foreach (TalaAnim state in Enum.GetValues(typeof(TalaAnim)))
                {
                    var clip = set.Get(state);
                    Assert.NotNull(clip, state.ToString());
                    Assert.IsNotEmpty(clip.frames, state.ToString());
                    foreach (var frame in clip.frames)
                    {
                        Assert.NotNull(frame);
                        Assert.AreEqual(18f / 16, frame.bounds.size.y, 0.001f, frame.name);
                        var pixels = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                        string texturePath = AssetDatabase.GetAssetPath(frame.texture);
                        pixels.LoadImage(System.IO.File.ReadAllBytes(texturePath));
                        var colours = pixels.GetPixels32();
                        int footRow = 0;
                        while (footRow < frame.rect.height)
                        {
                            bool found = false;
                            for (int x = 0; x < frame.rect.width; x++) if (colours[((int)frame.rect.y + footRow) * pixels.width + (int)frame.rect.x + x].a >= 192) { found = true; break; }
                            if (found) break;
                            footRow++;
                        }
                        Assert.AreEqual(footRow, frame.pivot.y, 0.001f, frame.name + " visible feet align to origin");
                        UnityEngine.Object.DestroyImmediate(pixels);
                        var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
                        Assert.AreEqual(FilterMode.Point, importer.filterMode);
                        Assert.IsFalse(importer.mipmapEnabled);
                    }
                }
                int Length(TalaAnim state) => set.Get(state).frames.Length * set.Get(state).ticksPerFrame;
                Assert.AreEqual(PlayerAnimator.LandTicks, Length(TalaAnim.Land));
                Assert.AreEqual(PlayerAnimator.HurtTicks, Length(TalaAnim.Hurt));
                Assert.AreEqual(PlayerAnimator.DoorTicks, Length(TalaAnim.Door));
                Assert.AreEqual(PlayerAnimator.BoltTicks, Length(TalaAnim.Bolt));
            }
        }

        [Test]
        public void FoleySources_AreWiredToPhysicalActions_AndMusicUsesLicensedTracks()
        {
            var bank = Resources.Load<SfxBank>(SfxBank.ResourcePath);
            foreach (var cue in new[] { "jump", "land", "letter", "door", "locked", "crumble", "step_left", "step_right", "dash", "select" })
            {
                Assert.IsTrue(bank.TryGet(cue, out var entry), cue);
                StringAssert.StartsWith(SkpPresentationUpgrade.Foley, AssetDatabase.GetAssetPath(entry.clip), cue);
                Assert.Greater(entry.volume, 0);
            }
            var tuning = Resources.Load<AudioTuning>(AudioTuning.ResourcePath);
            foreach (var clip in new[] { tuning.menu, tuning.act1, tuning.act2, tuning.act3, tuning.boss })
            {
                Assert.NotNull(clip);
                string path = AssetDatabase.GetAssetPath(clip);
                StringAssert.StartsWith(SkpPresentationUpgrade.MusicDir, path);
                var importer = (AudioImporter)AssetImporter.GetAtPath(path);
                Assert.AreEqual(AudioClipLoadType.Streaming, importer.defaultSampleSettings.loadType);
                Assert.IsFalse(importer.forceToMono, "preserve recorded music stereo");
            }
        }

        [Test]
        public void FootstepsRequireGroundedTravel_AndStopInAirDashAndAtRest()
        {
            var tuning = ScriptableObject.CreateInstance<PlayerTuning>();
            var difficulty = ScriptableObject.CreateInstance<DifficultyTable>();
            try
            {
                var sim = new PlayerSim(tuning, difficulty.Get("standard"), new PlayerWorld(new Rect(0, 0, 32, 32)));
                var cues = new GameCues(sim);
                Sfx.ResetCounts();
                sim.Grounded = true; sim.Velocity = Vector2.zero;
                for (int i = 0; i < 120; i++) cues.Tick(sim);
                Assert.AreEqual(0, Sfx.Count("step_left") + Sfx.Count("step_right"));
                sim.Velocity = new Vector2(7.3f, 0);
                for (int i = 0; i < 120; i++) cues.Tick(sim);
                int traveled = Sfx.Count("step_left") + Sfx.Count("step_right");
                Assert.Greater(traveled, 1);
                sim.Grounded = false;
                for (int i = 0; i < 60; i++) cues.Tick(sim);
                sim.Grounded = true; sim.DashTicks = 10;
                for (int i = 0; i < 60; i++) cues.Tick(sim);
                Assert.AreEqual(traveled, Sfx.Count("step_left") + Sfx.Count("step_right"));
            }
            finally { UnityEngine.Object.DestroyImmediate(tuning); UnityEngine.Object.DestroyImmediate(difficulty); Sfx.ResetCounts(); }
        }

        [Test]
        public void EditingExistingString_InvalidatesTheLookupCache()
        {
            var table = ScriptableObject.CreateInstance<StringTable>();
            try { table.Set("test", "old"); Assert.AreEqual("old", table.Get("test")); table.Set("test", "new"); Assert.AreEqual("new", table.Get("test")); }
            finally { UnityEngine.Object.DestroyImmediate(table); }
        }
    }
}
