using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Skp.Audio;
using Skp.Core;
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
    /// M6 in the real Level scene: the letter pauses play and shows two boxes (letter, then NEW SKILL), the skill is
    /// learned once and saved at once, rapid taps cannot skip the skill box, a replay says SKILL KNOWN, the HUD chip
    /// and the VEIL/BOLT touch buttons appear once learned (mirrored left-handed), the Level Clear box names the new
    /// skill, and the veil ring and bolts are drawn with their sounds. Any error log fails a test.
    /// </summary>
    public class SkillFlowTests
    {
        string _dir;
        SaveService _save;

        GameRoot Root => GameRoot.Instance;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "skp_skill_flow_" + System.Guid.NewGuid().ToString("N"));
            _save = new SaveService(_dir);
            _save.Load();
            _save.Data.metLola = true;
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

        static IEnumerator Seconds(float s)
        {
            float t = 0;
            while (t < s) { t += Time.unscaledDeltaTime; yield return null; }
        }

        IEnumerator StartLevel(int level)
        {
            if (Root == null) { SceneManager.LoadScene(SceneNames.Boot); yield return WaitFor(() => Root != null, 5, "GameRoot"); }
            Root.UseSave(_save);
            yield return WaitFor(() => !Root.Loader.IsBusy, 10, "idle loader");
            GameSession.Pending = new LevelRequest { Level = level, Difficulty = "standard" };
            var old = Object.FindAnyObjectByType<LevelRunner>(); // the last test may have ended in the Level scene
            SceneManager.LoadScene(SceneNames.Level);
            LevelRunner runner = null;
            yield return WaitFor(() => (runner = Object.FindAnyObjectByType<LevelRunner>()) != null && runner != old && runner.Session != null, 10, "level");
            yield return WaitFor(() => runner.ui.dialog.IsOpen, 5, "intro");
            yield return Dismiss(runner.ui.dialog);
            yield return WaitFor(() => runner.ui.Playing && !UiGate.Blocked, 5, "play");
        }

        static LevelRunner Runner => Object.FindAnyObjectByType<LevelRunner>();

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

        static IEnumerator TakeLetter(LevelRunner runner)
        {
            var sim = runner.Session.Sim;
            sim.Invulnerable = 100000;
            var clue = runner.Session.World.Items.First(i => i.Kind == ItemKind.Clue && i.Mask == Skp.Dimensions.Dim.Ordinary);
            sim.Position = new Vector2(clue.Rect.center.x - sim.Size.x / 2, clue.Rect.y);
            yield return WaitFor(() => sim.HasClue, 3, "letter");
            yield return WaitFor(() => runner.ui.dialog.IsOpen, 2, "letter dialog");
        }

        [UnityTest]
        public IEnumerator Letter_PausesPlay_TwoBoxes_LearnsOnceAndSavesAtOnce_RapidTapsCannotSkip_ReplaySaysKnown()
        {
            yield return StartLevel(0);
            var runner = Runner;
            var ui = runner.ui;
            var sim = runner.Session.Sim;
            Assert.IsFalse(sim.Has(SkillIds.DoubleJump));
            Assert.IsFalse(ui.hud.chips[0].gameObject.activeInHierarchy, "no chip before the letter");
            Sfx.ResetCounts();

            yield return TakeLetter(runner);
            var d = ui.dialog;
            Assert.AreEqual(2, d.LineCount, "letter, then the skill");
            Assert.AreEqual(Speaker.Clue, d.CurrentLine.Who);
            StringAssert.Contains("A LETTER, FOLDED SMALL", d.speaker.text);
            // learned and saved before the boxes are even read
            var disk = new SaveService(_dir);
            disk.Load();
            CollectionAssert.Contains(disk.Data.skills, SkillIds.DoubleJump, "saved immediately");
            long frozenAt = sim.Tick;
            yield return Seconds(0.3f);
            Assert.AreEqual(frozenAt, sim.Tick, "gameplay paused during the letter");

            // rapid taps: the typewriter completes, the skill box appears, and then holds
            for (int i = 0; i < 30; i++) d.Advance();
            Assert.IsTrue(d.IsOpen, "rapid taps did not skip the skill box");
            Assert.AreEqual(1, d.LineIndex);
            Assert.AreEqual(Speaker.Skill, d.CurrentLine.Who);
            StringAssert.StartsWith("NEW SKILL", d.sub.text);
            StringAssert.Contains("DOUBLE JUMP (Hakbang ng Hangin)", d.speaker.text);
            StringAssert.Contains("mid-air", d.body.text);
            Assert.AreSame(SkillLibrary.Find(SkillIds.DoubleJump).icon, d.portrait.sprite, "the skill's icon in the portrait frame");
            Assert.AreEqual(1, Sfx.Count("skill_learned"), "the fanfare");
            yield return Seconds(0.3f);
            Assert.AreEqual(frozenAt, sim.Tick, "still paused on the skill box");
            for (int i = 0; i < 10; i++) { d.Advance(); yield return null; }
            yield return Seconds(LevelUi.SkillHoldSeconds);
            yield return Dismiss(d);
            yield return Seconds(0.2f);
            Assert.Greater(sim.Tick, frozenAt, "play resumes");
            Assert.IsTrue(sim.Has(SkillIds.DoubleJump));
            Assert.IsTrue(ui.hud.chips[0].gameObject.activeInHierarchy, "the 2J badge shows");
            Assert.AreEqual(1, _save.Data.skills.Length);

            // finish: the Level Clear box names the new skill
            var exit = runner.Session.World.Doors.First(x => x.Kind == DoorKind.Exit);
            sim.SetDimension(Skp.Dimensions.Dim.Ordinary);
            sim.Position = new Vector2(exit.Rect.center.x - sim.Size.x / 2, exit.Rect.y);
            bool pressed = false;
            runner.player.InputOverride = () => { var p = !pressed; pressed = true; return new PlayerInputFrame { InteractPressed = p }; };
            yield return WaitFor(() => runner.Session.Completed, 3, "level clear");
            yield return null;
            Assert.IsTrue(ui.clear.skillGroup.activeInHierarchy);
            StringAssert.Contains("NEW SKILL: DOUBLE JUMP", ui.clear.skillText.text);

            // replay the level: the letter says SKILL KNOWN and nothing is learned twice
            ui.clear.replay.Press();
            yield return WaitFor(() => Runner != null && Runner != runner && Runner.Session != null, 10, "replay");
            runner = Runner;
            yield return WaitFor(() => runner.ui.dialog.IsOpen, 5, "intro");
            yield return Dismiss(runner.ui.dialog);
            yield return WaitFor(() => runner.ui.Playing && !UiGate.Blocked, 5, "play");
            Sfx.ResetCounts();
            yield return TakeLetter(runner);
            runner.ui.dialog.CompleteLine();
            runner.ui.dialog.Advance();
            Assert.AreEqual(1, runner.ui.dialog.LineIndex);
            StringAssert.StartsWith("SKILL KNOWN", runner.ui.dialog.sub.text);
            Assert.AreEqual(1, Sfx.Count("skill_known"));
            Assert.AreEqual(0, Sfx.Count("skill_learned"));
            Assert.IsNull(runner.Session.LearnedThisRun);
            Assert.AreEqual(1, _save.Data.skills.Count(x => x == SkillIds.DoubleJump));
            yield return Seconds(LevelUi.SkillHoldSeconds);
            yield return Dismiss(runner.ui.dialog);
        }

        [UnityTest]
        public IEnumerator VeilAndBolt_ButtonsAndChipsAppearOnceLearned_MirroredLeftHanded_RingAndBoltsDrawn()
        {
            yield return StartLevel(2);
            var runner = Runner;
            var ui = runner.ui;
            var touch = ui.touch;
            touch.forceShow = true;
            ui.ApplySettings(new GameSettings { touch = TouchMode.On });
            yield return null;
            Assert.IsTrue(ui.hud.TallChips, "touch layout: tall chips");
            Assert.IsFalse(touch.veil.activeSelf, "no VEIL before it is learned");
            Assert.IsFalse(touch.bolt.activeSelf, "no BOLT before it is learned");

            Assert.IsTrue(runner.Session.Skills.Learn(SkillIds.Veil));
            yield return null;
            yield return null;
            Assert.IsTrue(touch.veil.activeSelf, "VEIL appears");
            Assert.IsFalse(touch.bolt.activeSelf);
            var veilChip = ui.hud.chips[System.Array.IndexOf(SkillIds.All, SkillIds.Veil)];
            Assert.IsTrue(veilChip.gameObject.activeInHierarchy, "the veil chip");
            Assert.IsTrue(veilChip.IsReady, "gold outline when ready");
            Assert.GreaterOrEqual(((RectTransform)veilChip.transform).rect.height, UiKit.MinTarget - 0.5f, "48 dp tall in the touch layout");

            runner.Session.Skills.Learn(SkillIds.Bolt);
            yield return null;
            yield return null;
            Assert.IsTrue(touch.bolt.activeSelf, "BOLT appears");
            var veilRt = (RectTransform)touch.veil.transform;
            Assert.AreEqual(1f, veilRt.anchorMin.x, 1e-4f, "right thumb");
            touch.Apply(new GameSettings { touch = TouchMode.On, leftHanded = true });
            Assert.AreEqual(0f, veilRt.anchorMin.x, 1e-4f, "mirrored for left-handed players");
            Assert.Greater(veilRt.anchoredPosition.x, 0);

            // use them through the real controller: the ring shows, a bolt is drawn, each has its own sound
            Sfx.ResetCounts();
            var sim = runner.Session.Sim;
            sim.Invulnerable = 0;
            int f = 0;
            runner.player.InputOverride = () => { f++; return new PlayerInputFrame { VeilPressed = f == 1, BoltPressed = f == 3 }; };
            var view = runner.player.view;
            int drawn = 0;
            float t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < 1f) { drawn = Mathf.Max(drawn, view.VisibleBolts); yield return null; }
            Assert.IsTrue(sim.Veiled);
            Assert.IsTrue(view.veilRing.enabled, "the veil ring");
            Assert.GreaterOrEqual(drawn, 1, "the bolt is drawn");
            Assert.AreEqual(1, Sfx.Count("skill_veil"));
            Assert.AreEqual(1, Sfx.Count("skill_bolt"));
            Assert.IsTrue(veilChip.IsOn, "bright border while active");
            runner.player.InputOverride = () => default;
            touch.Apply(new GameSettings());
        }
    }
}
