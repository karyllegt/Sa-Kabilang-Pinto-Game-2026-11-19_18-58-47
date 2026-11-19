using System.Collections;
using NUnit.Framework;
using Skp.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Skp.Tests
{
    /// <summary>M1 smoke test: GameRoot boots, and the loader walks Boot -> Title -> Map -> Level -> Map.</summary>
    public class SceneFlowTests
    {
        static IEnumerator WaitFor(System.Func<bool> cond, float timeout = 10f)
        {
            float t = 0;
            while (!cond() && t < timeout) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(cond(), "timed out");
        }

        [UnityTest]
        public IEnumerator Boot_ReachesTitle_ThenLoaderWalksEveryScene()
        {
            SceneManager.LoadScene(SceneNames.Boot);
            yield return WaitFor(() => SceneManager.GetActiveScene().name == SceneNames.Title && !GameRoot.Instance.Loader.IsBusy);

            var root = GameRoot.Instance;
            Assert.IsNotNull(root, "GameRoot created before the first scene");
            Assert.IsNotNull(root.Input, "input reader bound to the project-wide actions");
            Assert.IsNotNull(root.Save.Data);

            foreach (var next in new[] { SceneNames.Map, SceneNames.Level, SceneNames.Map })
            {
                Assert.IsTrue(root.Loader.Load(next));
                yield return WaitFor(() => SceneManager.GetActiveScene().name == next && !root.Loader.IsBusy);
            }
        }

        [UnityTest]
        public IEnumerator SecondLoad_DuringTransition_IsIgnored_AndInputIsBlocked()
        {
            SceneManager.LoadScene(SceneNames.Title);
            yield return WaitFor(() => SceneManager.GetActiveScene().name == SceneNames.Title && !GameRoot.Instance.Loader.IsBusy);
            var root = GameRoot.Instance;

            Assert.IsTrue(root.Loader.Load(SceneNames.Map));
            Assert.IsTrue(root.Loader.IsBusy);
            Assert.IsTrue(root.Input.Blocked, "input blocked during a transition");
            Assert.IsFalse(root.Loader.Load(SceneNames.Level), "double request dropped");
            yield return WaitFor(() => !root.Loader.IsBusy);
            Assert.AreEqual(SceneNames.Map, SceneManager.GetActiveScene().name);
            Assert.IsFalse(root.Input.Blocked);
        }
    }
}
