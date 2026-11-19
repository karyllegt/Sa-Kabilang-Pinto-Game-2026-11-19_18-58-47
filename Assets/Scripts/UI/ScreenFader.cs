using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SaKabilangPinto.UI
{
    /// <summary>
    /// Screen wipe between scenes: fades to the cover image, loads the scene, fades back.
    /// A simple fade for now. Lives across scene loads.
    /// </summary>
    public class ScreenFader : MonoBehaviour
    {
        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] float fadeSeconds = 0.35f;

        static ScreenFader instance;
        bool busy;

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }

        /// <summary>Wipe to a scene. Uses the existing fader, or creates one from the prefab.</summary>
        public static void LoadScene(ScreenFader prefab, string sceneName)
        {
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError("Scene \"" + sceneName + "\" is not in Build Settings (File > Build Profiles > Scene List).");
                return;
            }

            if (instance == null)
            {
                if (prefab == null)
                {
                    Time.timeScale = 1f;
                    SceneManager.LoadScene(sceneName);
                    return;
                }
                Instantiate(prefab);
            }
            if (instance.busy) return;
            instance.StartCoroutine(instance.Wipe(sceneName));
        }

        IEnumerator Wipe(string sceneName)
        {
            busy = true;
            canvasGroup.blocksRaycasts = true;
            yield return Fade(0f, 1f);

            // Unpause before loading so the new scene can pause itself again (e.g. IntroBox).
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync(sceneName);

            yield return Fade(1f, 0f);
            canvasGroup.blocksRaycasts = false;
            busy = false;
        }

        IEnumerator Fade(float from, float to)
        {
            float t = 0f;
            while (t < fadeSeconds)
            {
                t += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(from, to, t / fadeSeconds);
                yield return null;
            }
            canvasGroup.alpha = to;
        }
    }
}
