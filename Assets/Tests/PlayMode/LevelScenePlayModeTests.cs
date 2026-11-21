using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Skp.Core;
using Skp.Levels;
using Skp.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Skp.Tests
{
    /// <summary>
    /// M4 in the real Level scene: level 1 loads and draws, the intro dialog is dismissed, Tala plays with scripted input, picks up the
    /// letter (and reads it), uses the exit, the save unlocks level 2, and Level Clear > Map returns to the Map. Any error log fails
    /// the test (Unity Test Framework default).
    /// </summary>
    public class LevelScenePlayModeTests
    {
        static IEnumerator WaitFor(System.Func<bool> cond, float timeout = 15f)
        {
            float t = 0;
            while (!cond() && t < timeout) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(cond(), "timed out");
        }

        static IEnumerator Dismiss(Skp.UI.DialogBox d)
        {
            for (int i = 0; i < 40 && d.IsOpen; i++)
            {
                yield return WaitFor(() => !Skp.UI.UiGate.Blocked);
                d.Advance();
                yield return null;
            }
            Assert.IsFalse(d.IsOpen);
        }

        [UnityTest]
        public IEnumerator Level1_LoadsPlaysAndCompletes_ThroughTheRealScene()
        {
            // keep the developer's real save untouched: point GameRoot at a temp save
            var root0 = GameRoot.Instance;
            var dir = Path.Combine(Path.GetTempPath(), "skp_scene_test_" + System.Guid.NewGuid().ToString("N"));
            var save = new Skp.Save.SaveService(dir);
            save.Load();
            root0.UseSave(save);

            GameSession.Pending = new LevelRequest { Level = 0, Difficulty = "standard" };
            SceneManager.LoadScene(SceneNames.Level);
            LevelRunner runner = null;
            yield return WaitFor(() => (runner = Object.FindAnyObjectByType<LevelRunner>()) != null && runner.Session != null);

            var session = runner.Session;
            var view = runner.view;
            Assert.AreEqual("Fiesta Night", session.Spec.name);
            Assert.Greater(view.ObjectCount, 100, "the level is drawn");

            // M5: the before-stage dialog comes first, then a circle wipe into play
            yield return Dismiss(runner.ui.dialog);
            yield return WaitFor(() => runner.ui.Playing && !Skp.UI.UiGate.Blocked);

            // play: run right and jump for a while
            var pc = runner.player;
            int f = 0;
            pc.InputOverride = () => { f++; return new PlayerInputFrame { MoveX = (f / 90) % 2 == 0 ? 1 : -1, JumpPressed = f % 40 == 0, JumpHeld = f % 40 < 20 }; };
            yield return new WaitForSeconds(3f);
            Assert.Greater(session.Sim.Tick, 100, "the simulation runs");
            pc.InputOverride = () => default;

            // take the letter, then stand at the exit and press interact
            var clue = session.World.Items.First(i => i.Kind == ItemKind.Clue && i.Mask == Skp.Dimensions.Dim.Ordinary);
            session.Sim.Invulnerable = 100000;
            session.Sim.Position = new Vector2(clue.Rect.center.x - session.Sim.Size.x / 2, clue.Rect.y);
            yield return WaitFor(() => session.Sim.HasClue, 3f);
            yield return WaitFor(() => runner.ui.dialog.IsOpen, 2f); // the letter
            yield return Dismiss(runner.ui.dialog);
            var exit = session.World.Doors.First(d => d.Kind == DoorKind.Exit);
            session.Sim.SetDimension(Skp.Dimensions.Dim.Ordinary);
            session.Sim.Position = new Vector2(exit.Rect.center.x - session.Sim.Size.x / 2, exit.Rect.y);
            bool pressed = false;
            pc.InputOverride = () => { var p = !pressed; pressed = true; return new PlayerInputFrame { InteractPressed = p }; };
            yield return WaitFor(() => session.Completed, 3f);
            Assert.AreEqual(2, save.Data.unlocked);
            yield return null;
            Assert.IsTrue(runner.ui.clear.IsOpen, "Level Clear box");
            runner.ui.clear.map.Press();
            yield return WaitFor(() => SceneManager.GetActiveScene().name == SceneNames.Map, 6f);

            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }
}
