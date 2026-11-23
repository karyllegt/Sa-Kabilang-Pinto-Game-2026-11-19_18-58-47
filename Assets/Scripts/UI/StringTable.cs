using System;
using System.Collections.Generic;
using UnityEngine;

namespace Skp.UI
{
    /// <summary>
    /// Every UI string, keyed, in one asset (Assets/Resources/UI/Strings_en.asset), ready for a
    /// second language. Screens ask <see cref="Loc"/> for text; nothing user-facing is hard-coded in
    /// screen code. Level names, intros and letters live in the LevelConfig assets.
    /// </summary>
    [CreateAssetMenu(menuName = "Sa Kabilang Pinto/String Table", fileName = "Strings_en")]
    public class StringTable : ScriptableObject
    {
        public const string ResourcePath = "UI/Strings_en";

        [Serializable]
        public struct Entry
        {
            public string key;
            [TextArea] public string text;
        }

        public string language = "en";
        public List<Entry> entries = new List<Entry>();

        Dictionary<string, string> _map;

        public void Set(string key, string text)
        {
            int index = entries.FindIndex(e => e.key == key);
            var entry = new Entry { key = key, text = text };
            if (index >= 0) entries[index] = entry; else entries.Add(entry);
            _map = null;
        }

        public string Get(string key)
        {
            if (_map == null || _map.Count != entries.Count)
            {
                _map = new Dictionary<string, string>();
                foreach (var e in entries) _map[e.key] = e.text;
            }
            return _map.TryGetValue(key, out var t) ? t : null;
        }

        /// <summary>Replaces the entries with the built-in English defaults (used by the setup).</summary>
        public void ResetToDefaults()
        {
            entries.Clear();
            foreach (var kv in StringDefaults.En) entries.Add(new Entry { key = kv.Key, text = kv.Value });
            _map = null;
        }
    }

    /// <summary>Text lookup. Missing keys show as [key] so they are easy to spot (and a test fails on them).</summary>
    public static class Loc
    {
        static StringTable _table;

        public static StringTable Table
        {
            get
            {
                if (_table == null) _table = Resources.Load<StringTable>(StringTable.ResourcePath);
                return _table;
            }
            set => _table = value;
        }

        public static string Get(string key)
        {
            var t = Table != null ? Table.Get(key) : null;
            if (t == null && !StringDefaults.En.TryGetValue(key, out t)) t = "[" + key + "]";
            return t;
        }

        public static string Format(string key, params object[] args) => string.Format(Get(key), args);

        public static bool Has(string key) => (Table != null && Table.Get(key) != null) || StringDefaults.En.ContainsKey(key);
    }
}
