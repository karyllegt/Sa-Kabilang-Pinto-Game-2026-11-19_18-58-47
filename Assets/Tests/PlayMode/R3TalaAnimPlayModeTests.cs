using System.Collections;
using System.IO;
using NUnit.Framework;
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
    /// R3 in the real Level scene: run, jump, dash, ward, glide, bolt, hurt and death each put Tala's animator in its
    /// state, and the body draws that animation's frame from the character's set (girl and boy).
    /// </summary>
    public class R3TalaAnimPlayModeTests
    {
        string _dir;
        SaveService _save;
        GameRoot Root => GameRoot.Instance;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "skp_r3_" + System.Guid.NewGuid().ToString("N"));
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

        static IEnumerator WaitFor(System.Func<bool> cond, float timeout, string what)
        {
            float t = 0;
            while (!cond() && t < timeout) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(cond(), "timed out waiting for " + what);
        }

        IEnumerator StartLevel(string ch)
        {
            _save.Data.@char = ch;
            if (Root == null) { SceneManager.LoadScene(SceneNames.Boot); yield return WaitFor(() => Root != null, 5, "GameRoot"); }
            Root.UseSave(_save);
            yield return WaitFor(() => !Root.Loader.IsBusy, 10, "idle loader");
            GameSession.Pending = new LevelRequest { Level = 0, Difficulty = "story" };
            Assert.IsTrue(Root.Loader.Load(SceneNames.Level));
            LevelRunner r = null;
            yield return WaitFor(() => (r = Object.FindAnyObjectByType<LevelRunner>()) != null && r.Session != null && !Root.Loader.IsBusy, 15, "level");
            yield return WaitFor(() => r.ui.dialog.IsOpen, 5, "intro");
            while (r.ui.dialog.IsOpen) { yield return WaitFor(() => !UiGate.Blocked, 5, "wipe"); r.ui.dialog.Advance(); yield return null; }
            yield return WaitFor(() => r.ui.Playing && !UiGate.Blocked, 5, "play");
        }

        IEnumerator Expect(PlayerView view, PlayerSim sim, TalaAnim want, System.Func<PlayerInputFrame> input, float seconds = 1f)
        {
            var pc = Object.FindAnyObjectByType<LevelRunner>().player;
            pc.InputOverride = input;
            float t = 0;
            while (view.Animator.State != want && t < seconds) { t += Time.deltaTime; yield return null; }
            pc.InputOverride = () => default;
            Assert.AreEqual(want, view.Animator.State, "animator state for " + want);
            yield return null;
            Assert.IsNotNull(view.ActiveAnims.Get(want), "frames for " + want);
            Assert.Contains(view.body.sprite, view.ActiveAnims.Get(view.Animator.State).frames, $"the body draws a {view.Animator.State} frame");
        }

        [UnityTest]
        [Timeout(300000)]
        public IEnumerator EveryAction_DrivesItsAnimation_BothCharacters()
        {
            foreach (var ch in new[] { "girl", "boy" })
            {
                yield return StartLevel(ch);
                var runner = Object.FindAnyObjectByType<LevelRunner>();
                var sim = runner.Session.Sim;
                var view = runner.player.view;
                Assert.AreEqual(ch == "boy", view.useBoy, "character");
                Assert.IsNotNull(view.ActiveAnims, "the set is wired");
                sim.Skills = SkillSet.AllSkills();
                sim.MaxVitality = sim.Vitality = 99;
                yield return new WaitForSeconds(0.3f);
                Assert.AreEqual(TalaAnim.Idle, view.Animator.State, ch + " idle");

                yield return Expect(view, sim, TalaAnim.Run, () => new PlayerInputFrame { MoveX = 1 });
                yield return Expect(view, sim, TalaAnim.JumpRise, () => new PlayerInputFrame { JumpPressed = sim.Grounded, JumpHeld = true });
                yield return Expect(view, sim, TalaAnim.Fall, () => default, 2f);
                yield return WaitFor(() => sim.Grounded, 3, "landed");
                yield return Expect(view, sim, TalaAnim.Dash, () => new PlayerInputFrame { MoveX = 1, DashPressed = true });
                yield return new WaitForSeconds(0.6f);
                yield return Expect(view, sim, TalaAnim.Ward, () => new PlayerInputFrame { WardPressed = true });
                yield return new WaitForSeconds(0.6f);
                sim.Flame = 100;
                yield return Expect(view, sim, TalaAnim.Bolt, () => new PlayerInputFrame { BoltPressed = true });
                yield return new WaitForSeconds(0.4f);
                int jumps = 0;
                yield return Expect(view, sim, TalaAnim.Glide, () => new PlayerInputFrame { JumpPressed = (jumps++ % 30) == 0, JumpHeld = true }, 3f);
                yield return WaitFor(() => sim.Grounded, 4, "landed after the glide");
                yield return new WaitForSeconds(0.3f);
                sim.Invulnerable = 0;
                sim.TryHurt(1, null);
                yield return Expect(view, sim, TalaAnim.Hurt, () => default, 0.5f);
                yield return new WaitForSeconds(0.6f);
                sim.Invulnerable = 0;
                sim.Vitality = 1;
                sim.TryHurt(1, null);
                yield return Expect(view, sim, TalaAnim.Death, () => default, 1f);
            }
        }
    }
}
