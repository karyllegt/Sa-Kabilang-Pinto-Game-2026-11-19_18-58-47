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
using Skp.Skills;
using Skp.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Skp.Tests
{
    /// <summary>
    /// M7 in the real Level scene: a scripted run through levels 1 to 9 (each letter read and its skill learned, each
    /// exit taken, the Bellkeeper beaten with a parry and the last door), ending on the Ending screen with the game
    /// saved as cleared. Any error log fails it (the test framework fails on LogError). Hints show at most once a level.
    /// </summary>
    public class FullRunTests
    {
        string _dir;
        SaveService _save;
        GameRoot Root => GameRoot.Instance;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "skp_full_run_" + System.Guid.NewGuid().ToString("N"));
            _save = new SaveService(_dir);
            _save.Load();
            _save.Data.metLola = true;
            _save.Data.diff = "story";
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        static IEnumerator WaitFor(System.Func<bool> cond, float timeout = 15f, string what = "condition")
        {
            float t = 0;
            while (!cond() && t < timeout) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(cond(), "timed out waiting for " + what);
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

        IEnumerator Play(int lv)
        {
            var old = Object.FindAnyObjectByType<LevelRunner>();
            GameSession.Pending = new LevelRequest { Level = lv, Difficulty = "story" };
            Assert.IsTrue(Root.Loader.Load(SceneNames.Level), "load level " + (lv + 1));
            LevelRunner runner = null;
            yield return WaitFor(() => (runner = Object.FindAnyObjectByType<LevelRunner>()) != null && runner != old && runner.Session != null && !Root.Loader.IsBusy, 15, "level " + (lv + 1));
            yield return WaitFor(() => runner.ui.dialog.IsOpen, 5, "intro");
            yield return Dismiss(runner.ui.dialog);
            yield return WaitFor(() => runner.ui.Playing && !UiGate.Blocked, 5, "play");
        }

        [UnityTest]
        [Timeout(600000)]
        public IEnumerator ScriptedRun_Levels1To9_LettersSkillsExitsBossEnding_NoErrors()
        {
            if (Root == null) { SceneManager.LoadScene(SceneNames.Boot); yield return WaitFor(() => Root != null, 5, "GameRoot"); }
            Root.UseSave(_save);
            yield return WaitFor(() => !Root.Loader.IsBusy, 10, "idle loader");
            var hints = new Dictionary<string, int>();
            for (int lv = 0; lv < 9; lv++)
            {
                yield return Play(lv);
                var runner = Object.FindAnyObjectByType<LevelRunner>();
                var s = runner.Session;
                var sim = s.Sim;
                var ui = runner.ui;
                hints.Clear();
                ui.toast.Shown += text => { hints.TryGetValue(text, out int n); hints[text] = n + 1; };
                sim.Invulnerable = 1000000;
                // let the level run a little: creatures, water, bells and hints all tick
                float t0 = Time.realtimeSinceStartup;
                while (Time.realtimeSinceStartup - t0 < 1.5f) yield return null;

                if (lv < 8)
                {
                    var clue = s.World.Items.First(i => i.Kind == ItemKind.Clue && i.Mask == Skp.Dimensions.Dim.Ordinary);
                    sim.SetDimension(Skp.Dimensions.Dim.Ordinary);
                    sim.Position = new Vector2(clue.Rect.center.x - sim.Size.x / 2, clue.Rect.y);
                    yield return WaitFor(() => ui.dialog.IsOpen, 3, $"L{lv + 1} letter");
                    yield return new WaitForSecondsRealtime(LevelUi.SkillHoldSeconds);
                    yield return Dismiss(ui.dialog);
                    Assert.IsTrue(sim.Has(SkillIds.All[lv]), $"L{lv + 1} taught {SkillIds.All[lv]}");
                }
                else
                {
                    // the Bellkeeper: light the arena lantern, then parry its toll from under it
                    var boss = s.Boss;
                    var lantern = s.World.Lanterns[s.Layout.Rooms];
                    sim.Position = new Vector2(lantern.x - sim.Size.x / 2, lantern.y);
                    yield return WaitFor(() => boss.Active, 3, "the Bellkeeper wakes");
                    yield return WaitFor(() => ui.hud.bossBar.gameObject.activeSelf, 2, "the boss bar");
                    boss.Hp = Bellkeeper.ParryDamage; // one parry ends it here; the full bar is fought in M7Tests
                    runner.player.InputOverride = () => new PlayerInputFrame { WardPressed = boss.State == BossState.Toll && boss.TollTicks >= 64 && sim.WardCooldown == 0 };
                    float t1 = Time.realtimeSinceStartup;
                    while (!boss.Dead && Time.realtimeSinceStartup - t1 < 60f)
                    {
                        sim.Position = new Vector2(boss.X - sim.Size.x / 2, boss.FloorY);
                        yield return null;
                    }
                    Assert.IsTrue(boss.Dead, "the Bellkeeper falls");
                    runner.player.InputOverride = () => default;
                    yield return WaitFor(() => boss.ExitDoor.HasValue, 5, "the last door");
                    Assert.IsFalse(ui.hud.bossBar.gameObject.activeSelf, "the bar is gone");
                }

                var exit = s.World.Doors.First(d => d.Kind == DoorKind.Exit);
                sim.SetDimension(Skp.Dimensions.Dim.Ordinary);
                sim.Position = new Vector2(exit.Rect.center.x - sim.Size.x / 2, exit.Rect.y);
                bool pressed = false;
                runner.player.InputOverride = () => { var p = !pressed; pressed = true; return new PlayerInputFrame { InteractPressed = p }; };
                yield return WaitFor(() => s.Completed, 5, $"L{lv + 1} exit");
                runner.player.InputOverride = null;
                foreach (var kv in hints.Where(h => h.Key.StartsWith("Some ledges") || h.Key.StartsWith("Spiked") || h.Key.StartsWith("Thorn walls") || h.Key.StartsWith("Wide gaps") || h.Key.StartsWith("Bells fall")))
                    Assert.AreEqual(1, kv.Value, $"L{lv + 1}: hint shown once: {kv.Key}");
                yield return null;
                if (lv < 8) Assert.IsTrue(ui.clear.IsOpen, $"L{lv + 1} Level Clear");
                else
                {
                    Assert.IsTrue(ui.ending.IsOpen, "the Ending");
                    Assert.IsTrue(_save.Data.cleared, "cleared is saved");
                }
            }
            CollectionAssert.AreEquivalent(SkillIds.All, _save.Data.skills, "all eight skills learned");
        }
    }
}
