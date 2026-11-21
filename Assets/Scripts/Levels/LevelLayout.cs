using System.Collections.Generic;

namespace Skp.Levels
{
    /// <summary>
    /// A generated level in the prototype's own coordinates: pixels, y pointing DOWN, origin at the
    /// top of the tower (exactly what buildLevel() produces). Kept this way so it can be compared one to
    /// one with the prototype; <see cref="LevelWorldFactory"/> converts it to Unity units (y up).
    /// Masks use the prototype values: Ordinary 1, Spirit 2, Inverted 4.
    /// </summary>
    public class LevelLayout
    {
        public const int T = 16, LW = 32, RoomTiles = 22;

        public LevelSpec Spec;
        public int LevelIndex, Loop;
        public double W, H, Y0;
        public int Rooms, ClueRoom;
        public bool NeedClue;
        public readonly HashSet<string> Features = new HashSet<string>();

        public readonly List<LPlat> Plats = new List<LPlat>();
        public readonly List<LHaz> Haz = new List<LHaz>();
        public readonly List<LDoor> Doors = new List<LDoor>();
        public readonly List<LItem> Items = new List<LItem>();
        public readonly List<LEnemy> Enemies = new List<LEnemy>();
        public readonly List<LLantern> Lanterns = new List<LLantern>();
        public LWater Water;
        /// <summary>Act III falling bells (M7).</summary>
        public readonly List<LBell> Bells = new List<LBell>();
        /// <summary>Skill obstacles placed in this layout, for the bots and the statistics (M7).</summary>
        public readonly List<LObstacle> Obstacles = new List<LObstacle>();
        /// <summary>Hazard and enemy multipliers used for each room (debug overlay, tests).</summary>
        public readonly List<(double hazard, double enemy)> RoomMults = new List<(double, double)>();
        public LBossArena BossArena;
    }

    public class LPlat { public double x, y, w, h; public int m; public string kind; public double bx, amp, sp, ph; }
    public class LHaz { public double x, y, w, h; public int m; public string kind; public bool up, gaunt; public double ph; }
    public class LDoor { public double x, y, w, h; public int m, dim, room; public string kind; public bool up; public double? toX, toY; }
    public class LItem { public double x, y; public int m; public string kind; }
    public class LEnemy { public string t; public double x, y, w, h; public int m, hp, dir; public double spd, min, max, cx, cy, ph; }
    public class LLantern { public double x, y; public int room; }
    public class LWater { public double y, sp; }
    public class LBossArena { public double yf, top, triggerY; }
    /// <summary>A bell that drops on column x from top onto the ledge whose top is ground (px, y down).</summary>
    public class LBell { public double x, top, ground; public int period, phase; }
    /// <summary>
    /// One skill obstacle: its type (dj climb, veil gauntlet, bolt thorns, dash gap, glide gap, triple-bolt pack), the room,
    /// the ledge Tala comes from (From), the ledge the obstacle guards (Over: gauntlet or thorn ledge) and the ledge that
    /// is the way on (To).
    /// </summary>
    public class LObstacle { public string type, skill; public int room; public LPlat From, Over, To; public LHaz Hazard; }
}
