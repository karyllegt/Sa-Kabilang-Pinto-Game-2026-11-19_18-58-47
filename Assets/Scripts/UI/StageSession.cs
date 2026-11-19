using UnityEngine;
using UnityEngine.SceneManagement;

namespace SaKabilangPinto.UI
{
    /// <summary>Remembers which level is being played between scene loads.</summary>
    public static class StageSession
    {
        public static string CurrentLevelId { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay()
        {
            CurrentLevelId = null;
        }

        /// <summary>
        /// Index of the level being played. If the scene was opened directly in the Editor
        /// (not from Level Select), falls back to the first entry that uses this scene.
        /// </summary>
        public static int CurrentIndex(LevelList levelList)
        {
            if (levelList == null) return -1;
            int index = levelList.IndexOf(CurrentLevelId);
            if (index >= 0) return index;
            string sceneName = SceneManager.GetActiveScene().name;
            return levelList.levels.FindIndex(e => e.sceneName == sceneName);
        }

        public static void StartLevel(LevelList levelList, int index, ScreenFader faderPrefab)
        {
            var entry = levelList != null ? levelList.Get(index) : null;
            if (entry == null)
            {
                Debug.LogError("No level at index " + index + " in the Level List.");
                return;
            }
            CurrentLevelId = entry.id;
            ScreenFader.LoadScene(faderPrefab, entry.sceneName);
        }
    }
}
