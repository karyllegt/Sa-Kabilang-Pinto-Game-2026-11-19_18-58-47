using UnityEngine;

namespace SaKabilangPinto.UI
{
    /// <summary>Saves which levels are finished, using PlayerPrefs.</summary>
    public static class LevelProgress
    {
        const string KeyPrefix = "skp_level_finished_";

        public static bool IsFinished(string levelId)
        {
            return PlayerPrefs.GetInt(KeyPrefix + levelId, 0) == 1;
        }

        public static void MarkFinished(string levelId)
        {
            if (string.IsNullOrEmpty(levelId)) return;
            PlayerPrefs.SetInt(KeyPrefix + levelId, 1);
            PlayerPrefs.Save();
        }
    }
}
