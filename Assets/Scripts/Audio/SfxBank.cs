using System;
using UnityEngine;

namespace Skp.Audio
{
    /// <summary>Cue name to clip.</summary>
    [CreateAssetMenu(menuName = "Sa Kabilang Pinto/Sfx Bank", fileName = "SfxBank")]
    public class SfxBank : ScriptableObject
    {
        public const string ResourcePath = "Audio/SfxBank";

        [Serializable]
        public struct Entry
        {
            public string cue;
            public AudioClip clip;
            [Range(0, 1)] public float volume;
        }

        public Entry[] entries = new Entry[0];

        public bool TryGet(string cue, out Entry e)
        {
            foreach (var x in entries) if (x.cue == cue && x.clip != null) { e = x; return true; }
            e = default;
            return false;
        }
    }
}
