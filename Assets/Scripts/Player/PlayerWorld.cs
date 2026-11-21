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

    public enum LedgeKind { Wood, Floor, Spirit, Inverted, Crumble, Moving }

    /// <summary>
    /// A one-way ledge. Crumbling ledges start a 42-tick timer when stood on, vanish for 220 ticks,
    /// then return. Moving ledges swing around <see cref="BaseX"/> (x = base + sin(t * speed + phase) * amplitude)
    /// and carry whoever stands on them.
    /// </summary>
    public class Ledge
    {
        public Rect Rect;
        public Dim Mask = Dim.All;
        public LedgeKind Kind;

        // crumble state, in ticks
        public int CrumbleTimer;
        public int DeadTicks;
        public bool IsGone => DeadTicks > 0;

        // moving ledge, in units; Speed and Phase are per tick (prototype values)
        public float BaseX, Amplitude, Speed, Phase, DeltaX;

        public Ledge() { }
        public Ledge(Rect r, Dim mask, LedgeKind kind = LedgeKind.Wood) { Rect = r; Mask = mask; Kind = kind; }

        public static implicit operator Ledge(Rect r) => new Ledge(r, Dim.All);
        public static implicit operator Ledge(MaskedRect m) => new Ledge(m.Rect, m.Mask);
    }

    /// <summary>
    /// Spike, Root and Thorns as in the prototype. Curtain (M7): violet hanging thorns over a veil gauntlet that
    /// make jumping over it impossible; never burns, only the Lambong Veil lets Tala pass.
    /// </summary>
    public enum HazardKind { Spike, Root, Thorns, Curtain }

    /// <summary>The rising black water (prototype L.water). Y is the surface, in units; it rises Speed units per tick.</summary>
    public class Flood
    {
        public float Y, Speed;
        /// <summary>Water starts this far below the lantern Tala begins or respawns at.</summary>
        public const float BelowLantern = 10f;
    }

    /// <summary>
    /// Act III falling bell (M7 bell mechanic): a red mark shows for <see cref="WarnTicks"/>, then a bell drops from
    /// <see cref="TopY"/> onto the ledge at <see cref="GroundY"/>, then the cycle repeats every <see cref="Period"/> ticks.
    /// </summary>
    public class BellDrop
    {
        public float X, TopY, GroundY;
        public int Period = 240, Phase, WarnTicks = 60;
        public Dim Mask = Dim.Ordinary;
        /// <summary>Bottom of the falling bell (units) while dropping.</summary>
        public float Y;
        public float Vy;
        public bool Falling, Warning;
        public static readonly Vector2 Size = new Vector2(14f / 16f, 16f / 16f);
        public Rect HitRect => new Rect(X - Size.x * 0.5f, Y, Size.x, Size.y);
    }

    /// <summary>The Bellkeeper arena (prototype L.bossArena), in units: the arena floor top and the top of the arena.</summary>
    public class BossArena
    {
        public float FloorY, TopY;
    }

    /// <summary>
    /// Spikes always hurt (1 px inset). Roots strike only in the last 40 of every 150 ticks
    /// (warning before that). Thorn walls block until burnt (Liwanag Bolt, M6).
    /// </summary>
    public class Hazard
    {
        public Rect Rect;
        public Dim Mask = Dim.All;
        public HazardKind Kind;
        /// <summary>Root strike phase offset, in ticks.</summary>
        public int Phase;
        /// <summary>Ticks left of being burnt (roots, thorns).</summary>
        public int BurnTicks;
        /// <summary>Hangs downward (Inverted route).</summary>
        public bool Hanging;
        /// <summary>Part of a veil spike gauntlet (M7).</summary>
        public bool Gauntlet;

        public Hazard() { }
        public Hazard(Rect r, Dim mask, HazardKind kind = HazardKind.Spike) { Rect = r; Mask = mask; Kind = kind; }

        public static implicit operator Hazard(Rect r) => new Hazard(r, Dim.All);
        public static implicit operator Hazard(MaskedRect m) => new Hazard(m.Rect, m.Mask);

        /// <summary>Prototype: roots strike when (t + phase) % 150 >= 110.</summary>
        public bool RootStriking(long tick) => (tick + Phase) % 150 >= 110;
    }

    public enum ItemKind { Flame, Shard, Clue }

    public class Item
    {
        /// <summary>Pickup area (the prototype's 14 x 14 px box around the sprite).</summary>
        public Rect Rect;
        public Dim Mask = Dim.All;
        public ItemKind Kind;
        public bool Got;
    }

    public enum EnemyKind { Anino, Tikbalang, Manananggal }

    public enum FlightState { Hover, Dive, Back }

    /// <summary>A folklore creature. Position is the bottom-left of its body, in units.</summary>
    public class Enemy
    {
        public EnemyKind Kind;
        public Vector2 Position;
        public Vector2 Size;
        public Dim Mask = Dim.Ordinary;
        public int Hp;
        /// <summary>Patrol speed, prototype px per frame before difficulty and level multipliers.</summary>
        public float Speed;
        public int Dir = 1;
        /// <summary>Patrol limits (units, left edge minimum and right edge maximum).</summary>
        public float MinX, MaxX;
        public int Stun;
        public bool Dead;
        public bool Charging;
        // manananggal flight: Home is the top-left of its body (prototype cx, cy)
        public Vector2 Home;
        public float Phase;
        public FlightState Flight;
        public int Cool, DiveTicks;
        public Vector2 DiveTarget;

        /// <summary>The prototype's contact box: 2 px in from the sides and top, 1 px up from the bottom.</summary>
        public Rect ContactRect => new Rect(Position.x + 2f / 16f, Position.y + 1f / 16f, Size.x - 4f / 16f, Size.y - 3f / 16f);
        public Vector2 Center => Position + Size * 0.5f;
    }

    public enum DoorKind
    {
        /// <summary>Into another dimension (Spirit or Inverted).</summary>
        Enter,
        /// <summary>Back to the Waking World.</summary>
        Home,
        /// <summary>The level's exit.</summary>
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
        public int Room;
    }

    /// <summary>
    /// Everything the player interacts with, in Unity units (y up). Every ledge is one-way, as in the
    /// prototype. Ledges, hazards, doors, items and enemies carry a dimension mask.
    /// </summary>
    public class PlayerWorld
    {
        /// <summary>Level area. The player is clamped horizontally; the camera is clamped to all of it.</summary>
        public Rect Bounds;
        public readonly List<Ledge> Ledges = new List<Ledge>();
        public readonly List<Hazard> Hazards = new List<Hazard>();
        /// <summary>Lantern checkpoints: x = center, y = the surface they stand on. Index 0 is the spawn.</summary>
        public readonly List<Vector2> Lanterns = new List<Vector2>();
        public readonly List<Door> Doors = new List<Door>();
        public readonly List<Item> Items = new List<Item>();
        public readonly List<Enemy> Enemies = new List<Enemy>();
        /// <summary>The level needs its letter before the exit opens.</summary>
        public bool NeedClue;
        /// <summary>The rising water (level 5, Endless), or null.</summary>
        public Flood Flood;
        public readonly List<BellDrop> Bells = new List<BellDrop>();
        /// <summary>The Bellkeeper's arena (level 9), or null.</summary>
        public BossArena Arena;

        public PlayerWorld(Rect bounds) { Bounds = bounds; }
    }
}
