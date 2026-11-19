using System.Collections;
using UnityEngine;

namespace Skp.Core
{
    /// <summary>
    /// Hook for the circle wipe between screens. M1 uses <see cref="InstantWipe"/>;
    /// the real circle wipe (about 0.35 s close, 0.35 s open) replaces it in M5.
    /// </summary>
    public interface IScreenWipe
    {
        /// <summary>Close the wipe around a viewport point (0..1).</summary>
        IEnumerator Close(Vector2 viewportFocus);
        /// <summary>Open the wipe around a viewport point (0..1).</summary>
        IEnumerator Open(Vector2 viewportFocus);
    }

    /// <summary>No visual transition; used until M5.</summary>
    public class InstantWipe : IScreenWipe
    {
        public IEnumerator Close(Vector2 viewportFocus) { yield break; }
        public IEnumerator Open(Vector2 viewportFocus) { yield break; }
    }
}
