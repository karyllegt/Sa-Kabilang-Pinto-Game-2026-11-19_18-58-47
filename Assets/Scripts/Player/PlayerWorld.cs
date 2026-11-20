using System.Collections.Generic;
using Skp.Dimensions;
using UnityEngine;

namespace Skp.Player
{
    /// <summary>A rectangle that exists only in the dimensions of its mask.</summary>
    public struct MaskedRect
    {
        public Rect Rect;
        public Dim Mask;

        public MaskedRect(Rect rect, Dim mask) { Rect = rect; Mask = mask; }

        /// <summary>A plain Rect exists in every dimension.</summary>
        public static implicit operator MaskedRect(Rect r) => new MaskedRect(r, Dim.All);
    }

    public enum DoorKind
    {
        /// <summary>Into another dimension (Spirit or Inverted).</summary>
        Enter,
        /// <summary>Back to the Waking World.</summary>
        Home,
        /// <summary>The level's exit (M4).</summary>
        Exit,
    }

    public struct Door
    {
        public Rect Rect;
        /// <summary>The dimensions the door can be seen and used in.</summary>
        public Dim Mask;
        /// <summary>The dimension it leads to.</summary>
        public Dim Target;
        public DoorKind Kind;
        /// <summary>Optional arrival point (feet, center x); otherwise Tala stays where she is.</summary>
        public Vector2? To;
        /// <summary>Hangs from a ceiling (Inverted route); drawn upside down.</summary>
        public bool Hanging;
    }

    /// <summary>
    /// What the player collides with, in Unity units (y up). Every ledge is one-way, as in the
    /// prototype: you land on its top from above (on its underside in Inverted gravity) and pass
    /// through it otherwise. Ledges, hazards and doors carry a dimension mask.
    /// </summary>
    public class PlayerWorld
    {
        /// <summary>Level area. The player is clamped horizontally; the camera is clamped to all of it.</summary>
        public Rect Bounds;
        public readonly List<MaskedRect> Ledges = new List<MaskedRect>();
        /// <summary>Damaging areas (spikes). Each hit deals 1 and knocks back away from its center.</summary>
        public readonly List<MaskedRect> Hazards = new List<MaskedRect>();
        /// <summary>Lantern checkpoints: x = center, y = the surface they stand on. Index 0 is the spawn.</summary>
        public readonly List<Vector2> Lanterns = new List<Vector2>();
        public readonly List<Door> Doors = new List<Door>();

        public PlayerWorld(Rect bounds) { Bounds = bounds; }
    }
}
