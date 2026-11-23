using System.Collections;
using System.IO;
using NUnit.Framework;
using Skp.Core;
using Skp.Enemies;
using Skp.Levels;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Skp.Tests
{
    /// <summary>
    /// Every level, the Bellkeeper and map retain the reviewed presentation snapshot. The intentional art overhaul
    /// uses tests/Screenshots/presentation/baseline; historical pre-overhaul R1 snapshots are preserved separately.
    /// </summary>
    public class R1PrefabBaselineTests
    {
        static IEnumerator WaitFor(System.Func<bool> cond, float timeout = 20f)
        {
            float t = 0;
            while (!cond() && t < timeout) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(cond(), "timed out");
        }

        static void UseTempSave()
        {
            var dir = Path.Combine(Path.GetTempPath(), "skp_r1_" + System.Guid.NewGuid().ToString("N"));
            var save = new Skp.Save.SaveService(dir);
            save.Load();
            save.Data.unlocked = 9;
            save.Data.metLola = true;
            save.Data.lolaSeen = 9;
            GameRoot.Instance.UseSave(save);
        }

        static void Check(string name, System.Collections.Generic.List<string> lines)
        {
            var diff = R1Capture.CompareOrWrite(name, lines, out bool wrote);
            if (wrote) { Debug.Log($"[R1] baseline written: {name} ({lines.Count} lines)"); return; }
            Assert.IsEmpty(diff, name + " differs from the reviewed presentation baseline:\n" + string.Join("\n", diff.GetRange(0, Mathf.Min(diff.Count, 30))));
        }

        [UnityTest]
        public IEnumerator Levels1To9_And_TheBellkeeper_MatchPresentationBaseline()
        {
            UseTempSave();
            GameSession.Pending = new LevelRequest { Level = 0, Difficulty = "standard" };
            var old = Object.FindAnyObjectByType<LevelRunner>();
            SceneManager.LoadScene(SceneNames.Level);
            LevelRunner runner = null;
            yield return WaitFor(() => (runner = Object.FindAnyObjectByType<LevelRunner>()) != null && runner != old && runner.Session != null);
            runner.ui.enabled = false; // no intro, no unfreezing: the world stays at tick 0
            runner.ui.dialog.gameObject.SetActive(false);

            for (int lv = 0; lv < 9; lv++)
            {
                runner.Begin(new LevelRequest { Level = lv, Difficulty = "standard" });
                runner.player.Frozen = true;
                yield return null;
                yield return null;
                Assert.AreEqual(0, runner.Session.Sim.Tick, "the world is captured before it moves");
                var lines = R1Capture.Lines(runner.view.transform);
                Assert.Greater(lines.Count, 50);
                Check($"level_{lv + 1}", lines);
                // a Home door shares its spot with the room's Enter door: it always draws on top (nearer the camera)
                foreach (Transform t in runner.view.Root)
                {
                    if (!t.name.StartsWith("Door_")) continue;
                    float want = t.name.StartsWith("Door_Home") ? -LevelView.HomeDoorDepth : 0f;
                    Assert.AreEqual(want, t.position.z, 1e-5f, t.name + " depth");
                }
            }

            // the Bellkeeper: toll (ring) and bells (warning marks and falling bells), stepped like the M7 screenshots
            foreach (var want in new[] { BossState.Toll, BossState.Bells, BossState.Wave })
            {
                runner.Begin(new LevelRequest { Level = 8, Difficulty = "standard" });
                runner.player.Frozen = true;
                var s = runner.Session;
                var lantern = s.World.Lanterns[s.Layout.Rooms];
                s.Sim.Invulnerable = 1000000;
                s.Sim.Position = new Vector2(lantern.x - 0.3f, lantern.y);
                s.Sim.Step(default);
                for (int i = 0; i < 4000 && s.Boss.State != want; i++) { s.Sim.Position = new Vector2(lantern.x - 3, lantern.y); s.Sim.Step(default); }
                for (int i = 0; i < 40; i++) { s.Sim.Position = new Vector2(lantern.x - 3, lantern.y); s.Sim.Step(default); }
                Assert.AreEqual(want, s.Boss.State);
                yield return null;
                yield return null;
                // the warning marks pulse with the frame count: compare everything but their colour
                var lines = R1Capture.Lines(runner.BossView.transform).ConvertAll(l => l.StartsWith("sr|WHITE|") ? MaskColour(l) : l);
                Check("boss_" + want.ToString().ToLowerInvariant(), lines);
            }
        }

        static string MaskColour(string l)
        {
            var p = l.Split('|');
            p[5] = "*";
            return string.Join("|", p);
        }

        [UnityTest]
        public IEnumerator TheLevelMap_MatchesPresentationBaseline()
        {
            UseTempSave();
            GameRoot.Instance.Save.Data.unlocked = 5;
            GameRoot.Instance.Save.Data.lolaSeen = 4; // Lola has news: her "!" is drawn
            var old = Object.FindAnyObjectByType<Skp.UI.MapController>();
            SceneManager.LoadScene(SceneNames.Map);
            Skp.UI.MapController map = null;
            yield return WaitFor(() => (map = Object.FindAnyObjectByType<Skp.UI.MapController>()) != null && map != old && map.Model != null);
            yield return null;
            map.enabled = false; // stop the walk, the bob and the corner pulse
            typeof(Skp.UI.MapController).GetField("_t", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(map, 0f);
            map.Present(GameRoot.Instance.Save.Data, 3);
            yield return null;
            var lines = R1Capture.Lines(map.transform);
            Assert.Greater(lines.Count, 40);
            Check("map", lines);
        }
    }
}
