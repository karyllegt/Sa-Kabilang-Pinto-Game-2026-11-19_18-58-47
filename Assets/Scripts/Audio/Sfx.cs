using System;
using System.Collections.Generic;
using UnityEngine;

namespace Skp.Audio
{
    /// <summary>
    /// The one way gameplay asks for a sound: a cue name (the <see cref="SfxBank"/> maps names to clips).
    /// <see cref="SfxPlayer"/> plays them; tests read <see cref="LastCue"/> and <see cref="Count"/>.
    /// M8 adds the mixer, music and per-dimension filters on top of this.
    /// </summary>
    public static class Sfx
    {
        public static event Action<string> Played;
        public static string LastCue { get; private set; }
        static readonly Dictionary<string, int> _counts = new Dictionary<string, int>();

        public static void Cue(string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            LastCue = name;
            _counts.TryGetValue(name, out int n);
            _counts[name] = n + 1;
            Played?.Invoke(name);
        }

        public static int Count(string name) => _counts.TryGetValue(name, out int n) ? n : 0;

        public static void ResetCounts() { _counts.Clear(); LastCue = null; }
    }
}
