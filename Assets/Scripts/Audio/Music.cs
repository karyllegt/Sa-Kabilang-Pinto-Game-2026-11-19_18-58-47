using System;
using Skp.Dimensions;
using UnityEngine;

namespace Skp.Audio
{
    public enum MusicTrack { None, Menu, Act1, Act2, Act3, Boss }

    /// <summary>
    /// The one way the game asks for music: a track, the current dimension and the pause duck.
    /// <see cref="AudioService"/> plays it; tests read the state here.
    /// </summary>
    public static class Music
    {
        public static MusicTrack Current { get; private set; }
        public static Dim Dimension { get; private set; } = Dim.Ordinary;
        public static bool Ducked { get; private set; }

        public static event Action Changed;

        public static void Play(MusicTrack t)
        {
            if (t == Current) return;
            Current = t;
            Changed?.Invoke();
        }

        public static void SetDimension(Dim d)
        {
            if (d == Dim.None) d = Dim.Ordinary;
            if (d == Dimension) return;
            Dimension = d;
            Changed?.Invoke();
        }

        public static void Duck(bool on)
        {
            if (on == Ducked) return;
            Ducked = on;
            Changed?.Invoke();
        }

        public static MusicTrack ForAct(int act) => act <= 1 ? MusicTrack.Act1 : act == 2 ? MusicTrack.Act2 : MusicTrack.Act3;

        /// <summary>
        /// How each dimension colours the music: the low-pass cutoff (Hz) and the pitch (1 = normal).
        /// The service glides between these over <see cref="AudioTuning.dimensionGlideSeconds"/>.
        /// </summary>
        public static void DimensionSound(Dim d, AudioTuning t, out float cutoffHz, out float pitch)
        {
            switch (d)
            {
                case Dim.Spirit: // muffled, as if heard through the door; tempo unchanged
                    cutoffHz = t.spiritCutoffHz; pitch = 1f; break;
                case Dim.Inverted: // pitched down, with a light muffle so both other worlds feel related
                    cutoffHz = Mathf.Min(t.openCutoffHz, t.spiritCutoffHz * 4f); pitch = t.invertedPitch; break;
                default:
                    cutoffHz = t.openCutoffHz; pitch = 1f; break;
            }
        }

        /// <summary>Back to the menu defaults (tests and scene changes).</summary>
        public static void ResetState()
        {
            Current = MusicTrack.None;
            Dimension = Dim.Ordinary;
            Ducked = false;
            Changed?.Invoke();
        }
    }
}
