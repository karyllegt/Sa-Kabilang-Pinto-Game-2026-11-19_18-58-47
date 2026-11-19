using System;
using System.Collections.Generic;
using UnityEngine;

namespace SaKabilangPinto.UI
{
    /// <summary>
    /// The core levels in play order. Each entry pairs a story id (from levels.json)
    /// with the scene that plays it. A level is unlocked when the one before it is finished.
    /// </summary>
    public class LevelList : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            [Tooltip("Must match an \"id\" in Assets/Story/levels.json.")]
            public string id;

            [Tooltip("Scene name to load. The scene must be in Build Settings.")]
            public string sceneName;
        }

        public List<Entry> levels = new List<Entry>();

        public int Count => levels.Count;

        public Entry Get(int index)
        {
            return index >= 0 && index < levels.Count ? levels[index] : null;
        }

        public int IndexOf(string id)
        {
            if (string.IsNullOrEmpty(id)) return -1;
            return levels.FindIndex(e => e.id == id);
        }

        public bool IsUnlocked(int index)
        {
            if (index < 0 || index >= levels.Count) return false;
            return index == 0 || LevelProgress.IsFinished(levels[index - 1].id);
        }
    }
}
