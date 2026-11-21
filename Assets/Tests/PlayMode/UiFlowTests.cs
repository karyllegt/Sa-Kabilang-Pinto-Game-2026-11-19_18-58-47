using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Skp.Core;
using Skp.Levels;
using Skp.Player;
using Skp.Save;
using Skp.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Skp.Tests
{
    /// <summary>
    /// M5 flows in the real scenes: Boot to Title to Map (Lola's first meeting once), entering a level (intro,
    /// wipe, play), Level Clear and the token walking on, double Enter, locked nodes, Back/Escape everywhere,
    /// wipes blocking input, pause freezing play, and Game Over (Retry, Map). Any error log fails a test.
    /// </summary>
    public class UiFlowTests
    {
        string _dir;
        SaveService _save;
        Keyboard _kb;
        InputSettings.BackgroundBehavior _bg;
        InputSettings.EditorInputBehaviorInPlayMode _editorInput;

        GameRoot Root => GameRoot.Instance;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "skp_ui_flow_" + System.Guid.NewGuid().ToString("N"));
            _save = new SaveService(_dir);
            _save.Load();
            // batch-mode editors have no focused window: let the simulated keyboard through anyway
            _bg = InputSystem.settings.backgroundBehavior;
            _editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            if (_kb != null) { InputSystem.RemoveDevice(_kb); _kb = null; }
            InputSystem.settings.backgroundBehavior = _bg;
            InputSystem.settings.editorInputBehaviorInPlayMode = _editorInput;
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

        IEnumerator Boot(string scene)
        {
            if (Root == null) { SceneManager.LoadScene(SceneNames.Boot); yield return WaitFor(() => Root != null, 5, "GameRoot"); }
            Root.UseSave(_save);
            yield return WaitFor(() => !Root.Loader.IsBusy, 10, "idle loader");
            SceneManager.LoadScene(scene);
            yield return WaitFor(() => SceneManager.GetActiveScene().name == scene, 10, scene);
            yield return null;
        }

        IEnumerator Load(string scene)
        {
            Assert.IsTrue(Root.Loader.Load(scene), "load " + scene);
            yield return WaitFor(() => SceneManager.GetActiveScene().name == scene && !Root.Loader.IsBusy, 10, scene);
            yield return null;
        }

        static IEnumerator Dismiss(DialogBox d)
        {
            float t0 = Time.realtimeSinceStartup;
            while (d.IsOpen && Time.realtimeSinceStartup - t0 < 10f) // the skill box holds taps for a moment
            {
                yield return WaitFor(() => !UiGate.Blocked, 5, "wipe");
                d.Advance();
                yield return null;
            }
            Assert.IsFalse(d.IsOpen, "dialog closed");
        }

        IEnumerator StartLevel(int level, string diff = "standard")
        {
            _save.Data.metLola = true;
            GameSession.Pending = new LevelRequest { Level = level, Difficulty = diff };
            yield return Load(SceneNames.Level);
            var runner = Object.FindAnyObjectByType<LevelRunner>();
            Assert.IsNotNull(runner.Session);
            Assert.IsTrue(runner.ui.dialog.IsOpen, "before-stage dialog");
            yield return Dismiss(runner.ui.dialog);
            yield return WaitFor(() => runner.ui.Playing && !UiGate.Blocked, 5, "play after the wipe");
        }

        IEnumerator PressKey(Key key)
        {
            if (_kb == null) _kb = InputSystem.AddDevice<Keyboard>();
            InputSystem.QueueStateEvent(_kb, new KeyboardState(key));
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(_kb, new KeyboardState());
            yield return null;
            yield return null;
        }

        // ---------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator NewJourney_ReachesMap_OnNodeOne_AndLolaMeetsTalaExactlyOnce()
        {
            yield return Boot(SceneNames.Boot);
            yield return WaitFor(() => SceneManager.GetActiveScene().name == SceneNames.Title && !Root.Loader.IsBusy, 10, "title");
            var flow = Object.FindAnyObjectByType<TitleFlow>();
            Assert.IsTrue(flow.title.IsOpen);
            Assert.IsFalse(flow.title.continueButton.Interactable, "Continue greyed without a save");
            Assert.IsTrue(flow.title.continueButton.gameObject.activeInHierarchy, "greyed, not hidden");

            flow.title.newJourney.Press();
            Assert.IsTrue(flow.characters.IsOpen);
            flow.characters.boy.Press();
            Assert.AreEqual("boy", _save.Data.@char);
            Assert.IsTrue(flow.difficulty.IsOpen);
            Assert.IsFalse(flow.difficulty.endless.Interactable, "Endless locked before the story is cleared");
            flow.difficulty.standard.Press();
            yield return WaitFor(() => SceneManager.GetActiveScene().name == SceneNames.Map && !Root.Loader.IsBusy, 10, "map");

            var map = Object.FindAnyObjectByType<MapController>();
            Assert.AreEqual(0, map.Model.Selected, "node 1 selected");
            Assert.AreEqual("standard", _save.Data.diff);
            yield return WaitFor(() => map.dialog.IsOpen, 3, "Lola's first meeting");
            Assert.AreEqual(1, map.LolaTalks);
            Assert.AreEqual(5, map.dialog.LineCount);
            yield return Dismiss(map.dialog);
            Assert.IsTrue(_save.Data.metLola);
            yield return Seconds(1.5f);
            Assert.AreEqual(1, map.LolaTalks, "shown once");

            yield return Load(SceneNames.Map);
            map = Object.FindAnyObjectByType<MapController>();
            yield return Seconds(1.5f);
            Assert.AreEqual(0, map.LolaTalks, "not again on the next visit");
            Assert.IsFalse(map.dialog.IsOpen);
        }

        [UnityTest]
        public IEnumerator EnterLevel_Intro_Wipe_Play_Clear_ThenTheTokenWalksOn()
        {
            _save.Data.metLola = true;
            yield return Boot(SceneNames.Map);
            var map = Object.FindAnyObjectByType<MapController>();
            Assert.IsTrue(map.Enter());
            yield return WaitFor(() => SceneManager.GetActiveScene().name == SceneNames.Level && !Root.Loader.IsBusy, 10, "level");
            var runner = Object.FindAnyObjectByType<LevelRunner>();
            var ui = runner.ui;
            var sim = runner.Session.Sim;
            Assert.IsTrue(ui.dialog.IsOpen, "before-stage dialog first");
            StringAssert.Contains("FIESTA NIGHT", ui.dialog.speaker.text);
            long t0 = sim.Tick;
            yield return Seconds(0.5f);
            Assert.AreEqual(t0, sim.Tick, "play waits for the dialog");
            ui.dialog.Advance(); // completes the typewriter
            Assert.IsTrue(ui.dialog.IsOpen);
            ui.dialog.Advance(); // BEGIN
            Assert.IsTrue(Root.Wipe.Busy, "circle wipe into the level");
            Assert.IsTrue(Root.Input.Blocked, "input blocked during the wipe");
            yield return WaitFor(() => ui.Playing && !UiGate.Blocked, 5, "play");
            yield return Seconds(0.4f);
            Assert.Greater(sim.Tick, t0, "simulation runs");

            // the letter, its dialog, then the exit
            sim.Invulnerable = 100000;
            var clue = runner.Session.World.Items.First(i => i.Kind == ItemKind.Clue && i.Mask == Skp.Dimensions.Dim.Ordinary);
            sim.Position = new Vector2(clue.Rect.center.x - sim.Size.x / 2, clue.Rect.y);
            yield return WaitFor(() => sim.HasClue, 3, "letter");
            yield return WaitFor(() => ui.dialog.IsOpen, 2, "letter dialog");
            StringAssert.Contains("Come home", ui.dialog.body.text);
            long t1 = sim.Tick;
            yield return Seconds(0.3f);
            Assert.AreEqual(t1, sim.Tick, "frozen while reading");
            yield return Dismiss(ui.dialog);
            var exit = runner.Session.World.Doors.First(d => d.Kind == DoorKind.Exit);
            sim.SetDimension(Skp.Dimensions.Dim.Ordinary);
            sim.Position = new Vector2(exit.Rect.center.x - sim.Size.x / 2, exit.Rect.y);
            bool pressed = false;
            runner.player.InputOverride = () => { var p = !pressed; pressed = true; return new PlayerInputFrame { InteractPressed = p }; };
            yield return WaitFor(() => runner.Session.Completed, 3, "level complete");
            yield return null;
            Assert.IsTrue(ui.clear.IsOpen, "Level Clear box");
            Assert.AreEqual("LEVEL 1 CLEAR", ui.clear.title.text);
            Assert.AreEqual(Mathf.RoundToInt(sim.Tick / 60f) + "s", ui.clear.time.text);
            Assert.AreEqual(sim.Score.ToString(), ui.clear.score.text);
            Assert.AreEqual(sim.Shards + "/3", ui.clear.fragments.text);
            StringAssert.Contains("midnight bell", ui.clear.clue.text);
            Assert.AreEqual(2, _save.Data.unlocked, "next node unlocked");

            ui.clear.map.Press();
            yield return WaitFor(() => SceneManager.GetActiveScene().name == SceneNames.Map && !Root.Loader.IsBusy, 10, "map");
            map = Object.FindAnyObjectByType<MapController>();
            Assert.AreEqual(1, map.Model.Selected, "the new node is selected");
            yield return WaitFor(() => map.Model.At == 1 && !map.Model.Walking, 5, "token walks to node 2");
        }

        [UnityTest]
        public IEnumerator DoubleEnter_StartsTheLevelOnce()
        {
            _save.Data.metLola = true;
            yield return Boot(SceneNames.Map);
            var map = Object.FindAnyObjectByType<MapController>();
            int loads = 0;
            System.Action<string> count = n => { if (n == SceneNames.Level) loads++; };
            Root.Loader.SceneLoaded += count;
            try
            {
                map.screen.enter.Press();
                map.screen.enter.Press();
                Assert.IsFalse(map.Enter(), "third press ignored");
                yield return null;
                map.screen.enter.Press();
                yield return WaitFor(() => SceneManager.GetActiveScene().name == SceneNames.Level && !Root.Loader.IsBusy, 10, "level");
                yield return Seconds(0.8f);
                Assert.AreEqual(1, loads, "one level load");
            }
            finally { Root.Loader.SceneLoaded -= count; }
        }

        [UnityTest]
        public IEnumerator LockedNode_CannotBeEntered_AndPlaysTheLockedCue()
        {
            _save.Data.metLola = true;
            yield return Boot(SceneNames.Map);
            var map = Object.FindAnyObjectByType<MapController>();
            map.TapNode(4);
            Assert.AreEqual("locked", UiAudio.LastCue);
            Assert.AreEqual(4, map.Model.InfoNode);
            StringAssert.Contains("LOCKED: clear level 4 first", map.screen.levelSub.text);
            Assert.IsFalse(map.screen.enter.Interactable, "ENTER disabled");
            int before = UiAudio.CueCount;
            map.screen.enter.Press();
            Assert.AreEqual("locked", UiAudio.LastCue);
            Assert.AreEqual(before + 1, UiAudio.CueCount);
            map.Step(5);
            Assert.AreEqual(0, map.Model.Selected, "cannot walk into locked nodes");
            yield return Seconds(0.8f);
            Assert.AreEqual(SceneNames.Map, SceneManager.GetActiveScene().name);
            Assert.IsFalse(Root.Loader.IsBusy);
        }

        [UnityTest]
        public IEnumerator BackAndEscape_GoUpOneLevel_PauseInPlay_NeverQuit()
        {
            yield return Boot(SceneNames.Title);
            var flow = Object.FindAnyObjectByType<TitleFlow>();
            flow.title.settings.Press();
            Assert.IsTrue(flow.settings.IsOpen);
            yield return PressKey(Key.Escape);
            Assert.IsTrue(flow.title.IsOpen, "Escape: settings -> title");
            flow.title.howToPlay.Press();
            yield return PressKey(Key.Escape);
            Assert.IsTrue(flow.title.IsOpen, "Escape: how to play -> title");
            yield return PressKey(Key.Escape);
            Assert.IsTrue(flow.title.IsOpen, "Escape on the title does nothing");
            Assert.AreEqual(SceneNames.Title, SceneManager.GetActiveScene().name);

            _save.Data.metLola = true;
            yield return Load(SceneNames.Map);
            yield return PressKey(Key.Escape);
            yield return WaitFor(() => SceneManager.GetActiveScene().name == SceneNames.Title && !Root.Loader.IsBusy, 10, "map -> title");

            yield return StartLevel(0);
            var ui = Object.FindAnyObjectByType<LevelRunner>().ui;
            yield return PressKey(Key.Escape);
            Assert.IsTrue(ui.Paused && ui.pause.IsOpen, "Escape pauses play");
            Assert.AreEqual(0f, Time.timeScale);
            ui.pause.settings.Press();
            Assert.IsTrue(ui.settings.IsOpen);
            yield return PressKey(Key.Escape);
            Assert.IsTrue(ui.pause.IsOpen, "Escape: settings -> pause");
            yield return PressKey(Key.Escape);
            Assert.IsFalse(ui.Paused, "Escape resumes");
            Assert.AreEqual(1f, Time.timeScale);
            Assert.AreEqual(SceneNames.Level, SceneManager.GetActiveScene().name);
        }

        [UnityTest]
        public IEnumerator Wipes_BlockTapsKeysAndBack()
        {
            yield return Boot(SceneNames.Title);
            var flow = Object.FindAnyObjectByType<TitleFlow>();
            int clicks = 0;
            flow.title.credits.Clicked += () => clicks++;
            Assert.IsTrue(Root.Wipe.Run(null, new Vector2(0.5f, 0.5f)));
            Assert.IsTrue(UiGate.Blocked);
            Assert.IsTrue(Root.Input.Blocked);
            flow.title.credits.Press();
            Assert.AreEqual(0, clicks, "tap during a wipe ignored");
            Assert.IsFalse(UiNavigator.Back(), "Back during a wipe ignored");
            Assert.IsFalse(Root.Wipe.Run(null, Vector2.zero), "no second wipe on top");
            yield return WaitFor(() => !UiGate.Blocked, 3, "wipe end");
            flow.title.credits.Press();
            Assert.AreEqual(1, clicks);
            Assert.IsTrue(flow.credits.IsOpen);

            Assert.IsTrue(Root.Loader.Load(SceneNames.Map));
            Assert.IsTrue(UiGate.Blocked, "scene loads block too");
            Assert.IsFalse(Root.Loader.Load(SceneNames.Level));
            yield return WaitFor(() => !Root.Loader.IsBusy, 10, "load");
            Assert.AreEqual(SceneNames.Map, SceneManager.GetActiveScene().name);
        }

        [UnityTest]
        public IEnumerator Pause_FreezesPlay_AndIsIgnoredDuringDialogsAndWipes()
        {
            yield return Boot(SceneNames.Title);
            yield return StartLevel(1);
            var runner = Object.FindAnyObjectByType<LevelRunner>();
            var ui = runner.ui;
            var sim = runner.Session.Sim;
            sim.Invulnerable = 100000;
            yield return Seconds(0.3f);
            ui.Pause();
            Assert.IsTrue(ui.Paused);
            long t0 = sim.Tick;
            var shadow = sim.Shadow.Active;
            yield return Seconds(0.6f);
            Assert.AreEqual(t0, sim.Tick, "simulation (player, Anino, creatures, timers) frozen");
            ui.Resume();
            yield return Seconds(0.4f);
            Assert.Greater(sim.Tick, t0);

            ui.ShowLetter();
            ui.Pause();
            Assert.IsFalse(ui.Paused, "no pause over a dialog");
            yield return Dismiss(ui.dialog);
            Root.Wipe.Run(null, new Vector2(0.5f, 0.5f));
            ui.Pause();
            Assert.IsFalse(ui.Paused, "no pause during a wipe");
            yield return WaitFor(() => !UiGate.Blocked, 3, "wipe");
        }

        [UnityTest]
        public IEnumerator GameOver_RetryRestartsFresh_AndMapReturns()
        {
            yield return Boot(SceneNames.Title);
            yield return StartLevel(0);
            var runner = Object.FindAnyObjectByType<LevelRunner>();
            var sim = runner.Session.Sim;
            sim.Lives = 0;
            sim.Die();
            yield return WaitFor(() => runner.ui.gameOver.IsOpen, 6, "game over");
            Assert.IsFalse(_save.Data.HasContinue, "out of lives clears Continue");

            runner.ui.gameOver.retry.Press();
            yield return WaitFor(() => SceneManager.GetActiveScene().name == SceneNames.Level && !Root.Loader.IsBusy && Object.FindAnyObjectByType<LevelRunner>() != runner, 10, "retry");
            runner = Object.FindAnyObjectByType<LevelRunner>();
            Assert.AreEqual(0, runner.Session.Level);
            Assert.AreEqual(0, runner.Session.Sim.Checkpoint, "restarts at the first lantern");
            Assert.AreEqual(4, runner.Session.Sim.Vitality, "Standard vitality");
            Assert.AreEqual(5, runner.Session.Sim.Lives, "Standard lives");
            Assert.IsTrue(runner.ui.dialog.IsOpen, "the level intro again");
            yield return Dismiss(runner.ui.dialog);
            yield return WaitFor(() => runner.ui.Playing && !UiGate.Blocked, 5, "play");

            runner.Session.Sim.Lives = 0;
            runner.Session.Sim.Die();
            yield return WaitFor(() => runner.ui.gameOver.IsOpen, 6, "game over");
            runner.ui.gameOver.map.Press();
            yield return WaitFor(() => SceneManager.GetActiveScene().name == SceneNames.Map && !Root.Loader.IsBusy, 10, "map");
        }
    }
}
