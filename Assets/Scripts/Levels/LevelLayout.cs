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
}
