using System.Collections.Generic;
using UnityEngine;

namespace Skp.Player
{
    /// <summary>
    /// What the player collides with, in Unity units (y up). Every ledge is one-way, as in the
    /// prototype: you land on its top from above and pass through it from below.
    /// Dimension masks, moving and crumbling ledges arrive in M3/M4.
    /// </summary>
    public class PlayerWorld
    {
        /// <summary>Level area. The player is clamped horizontally; the camera is clamped to all of it.</summary>
        public Rect Bounds;
        public readonly List<Rect> Ledges = new List<Rect>();
        /// <summary>Damaging areas (spikes). Each hit deals 1 and knocks back away from its center.</summary>
        public readonly List<Rect> Hazards = new List<Rect>();
        /// <summary>Lantern checkpoints: x = center, y = the surface they stand on. Index 0 is the spawn.</summary>
        public readonly List<Vector2> Lanterns = new List<Vector2>();

        public PlayerWorld(Rect bounds) { Bounds = bounds; }
    }
}
