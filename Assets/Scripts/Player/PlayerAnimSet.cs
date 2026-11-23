using System;
using UnityEngine;

namespace Skp.Player
{
    /// <summary>
    /// R3: one character's frames and timing per animation (data, not code). Ticks per frame are 60 Hz simulation ticks,
    /// so the animation never drifts from the game (idle 6 fps = 10 ticks, run 10 fps = 6 ticks). Built by
    /// <c>SkpTalaAnimSetup</c> from the text-grid PNGs in Assets/Art/Game/Characters.
    /// </summary>
    [CreateAssetMenu(menuName = "Sa Kabilang Pinto/Player Anim Set")]
    public class PlayerAnimSet : ScriptableObject
    {
        [Serializable]
        public class Clip
        {
            public TalaAnim anim;
            public Sprite[] frames = Array.Empty<Sprite>();
            [Min(1)] public int ticksPerFrame = 6;
            public bool loop = true;
        }

        public Clip[] clips = Array.Empty<Clip>();
        Clip[] _byAnim;

        /// <summary>Number of <see cref="TalaAnim"/> values, read once (Enum.GetValues allocates; this runs every frame).</summary>
        static readonly int AnimCount = Enum.GetValues(typeof(TalaAnim)).Length;

        public Clip Get(TalaAnim a)
        {
            if (_byAnim == null)
            {
                _byAnim = new Clip[AnimCount];
                foreach (var c in clips) if (c != null && c.frames != null && c.frames.Length > 0) _byAnim[(int)c.anim] = c;
            }
            return _byAnim[(int)a];
        }

        /// <summary>The sprite for a state that has run <paramref name="ticks"/> ticks (null when the set lacks it).</summary>
        public Sprite Frame(TalaAnim a, int ticks)
        {
            var c = Get(a);
            if (c == null) return null;
            int i = ticks / Mathf.Max(1, c.ticksPerFrame);
            i = c.loop ? i % c.frames.Length : Mathf.Min(i, c.frames.Length - 1);
            return c.frames[i];
        }

        public void InvalidateCache() => _byAnim = null;
        void OnValidate() => InvalidateCache();
    }
}
