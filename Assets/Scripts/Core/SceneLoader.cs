using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Skp.Core
{
    /// <summary>
    /// Loads one of the game scenes behind a wipe. Input is blocked (<see cref="IsBusy"/>)
    /// from the moment a load starts until the wipe has opened again, and a second load
    /// request during a transition is ignored, so double taps never load twice.
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        public IScreenWipe Wipe { get; set; } = new InstantWipe();

        /// <summary>True during a transition. Gameplay and UI must ignore input while true.</summary>
        public bool IsBusy { get; private set; }

        public event Action<string> SceneLoaded;

        public bool Load(string sceneName) => Load(sceneName, new Vector2(0.5f, 0.5f));

        /// <returns>False if a transition is already running (the request is dropped).</returns>
        public bool Load(string sceneName, Vector2 viewportFocus)
        {
            if (IsBusy) return false;
            if (Array.IndexOf(SceneNames.All, sceneName) < 0)
                throw new ArgumentException($"Unknown scene '{sceneName}'", nameof(sceneName));
            StartCoroutine(Run(sceneName, viewportFocus));
            return true;
        }

        IEnumerator Run(string sceneName, Vector2 focus)
        {
            IsBusy = true;
            try
            {
                yield return Wipe.Close(focus);
                var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
                while (op != null && !op.isDone) yield return null;
                SceneLoaded?.Invoke(sceneName);
                yield return Wipe.Open(focus);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
