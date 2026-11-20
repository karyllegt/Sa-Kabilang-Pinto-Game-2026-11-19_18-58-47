using Skp.Dimensions;
using Skp.Player;
using UnityEngine;

namespace Skp.Levels
{
    /// <summary>
    /// Converts a <see cref="LevelLayout"/> (prototype pixels, y down) into a <see cref="PlayerWorld"/>
    /// (Unity units, y up). The ground floor's top is y = 0; each room is 22 tiles tall.
    /// </summary>
    public static class LevelWorldFactory
    {
        const float T = 16f;

        public static PlayerWorld Build(LevelLayout L)
        {
            var w = new PlayerWorld(new Rect(0, (float)((L.Y0 - L.H) / T), (float)(L.W / T), (float)(L.H / T)))
            {
                NeedClue = L.NeedClue,
            };
            foreach (var p in L.Plats)
            {
                var led = new Ledge(R(L, p.x, p.y, p.w, p.h), (Dim)p.m, KindOf(p.kind));
                if (led.Kind == LedgeKind.Moving)
                {
                    led.BaseX = (float)(p.bx / T);
                    led.Amplitude = (float)(p.amp / T);
                    led.Speed = (float)p.sp;
                    led.Phase = (float)p.ph;
                }
                w.Ledges.Add(led);
            }
            foreach (var h in L.Haz)
            {
                w.Hazards.Add(new Hazard(R(L, h.x, h.y, h.w, h.h), (Dim)h.m,
                    h.kind == "root" ? HazardKind.Root : h.kind == "thorns" ? HazardKind.Thorns : HazardKind.Spike)
                { Phase = (int)h.ph, Hanging = h.up, Gauntlet = h.gaunt });
            }
            foreach (var d in L.Doors)
            {
                w.Doors.Add(new Door
                {
                    Rect = R(L, d.x, d.y, d.w, d.h),
                    Mask = (Dim)d.m,
                    Target = (Dim)d.dim,
                    Kind = d.kind == "exit" ? DoorKind.Exit : d.kind == "home" ? DoorKind.Home : DoorKind.Enter,
                    To = d.toX.HasValue ? new Vector2((float)(d.toX.Value / T), Y(L, d.toY.Value)) : (Vector2?)null,
                    Hanging = d.up,
                    Room = d.room,
                });
            }
            foreach (var it in L.Items)
            {
                w.Items.Add(new Item
                {
                    // prototype pickup box: {x-2, y-2, 14, 14}
                    Rect = R(L, it.x - 2, it.y - 2, 14, 14),
                    Mask = (Dim)it.m,
                    Kind = it.kind == "shard" ? ItemKind.Shard : it.kind == "clue" ? ItemKind.Clue : ItemKind.Flame,
                });
            }
            foreach (var e in L.Enemies)
            {
                var r = R(L, e.x, e.y, e.w, e.h);
                w.Enemies.Add(new Enemy
                {
                    Kind = e.t == "tik" ? EnemyKind.Tikbalang : e.t == "manan" ? EnemyKind.Manananggal : EnemyKind.Anino,
                    Position = r.position,
                    Size = r.size,
                    Mask = (Dim)e.m,
                    Hp = e.hp,
                    Speed = (float)e.spd,
                    Dir = e.dir == 0 ? 1 : e.dir,
                    MinX = (float)(e.min / T),
                    MaxX = (float)(e.max / T),
                    Home = new Vector2((float)(e.cx / T), Y(L, e.cy)),
                    Phase = (float)e.ph,
                });
            }
            foreach (var l in L.Lanterns) w.Lanterns.Add(new Vector2((float)(l.x / T), Y(L, l.y)));
            return w;
        }

        /// <summary>Prototype y (px, down) to Unity y (units, up), ground floor top = 0.</summary>
        public static float Y(LevelLayout L, double yPx) => (float)((L.Y0 - yPx) / T);

        /// <summary>Prototype rectangle (top-left, y down, px) to a Unity rect (bottom-left, y up, units).</summary>
        public static Rect R(LevelLayout L, double x, double y, double w, double h) =>
            new Rect((float)(x / T), (float)((L.Y0 - (y + h)) / T), (float)(w / T), (float)(h / T));

        /// <summary>Bottom of room r (its start floor), in units.</summary>
        public static float RoomBottom(int r) => r * LevelLayout.RoomTiles;

        static LedgeKind KindOf(string k) => k switch
        {
            "floor" => LedgeKind.Floor,
            "spirit" => LedgeKind.Spirit,
            "inv" => LedgeKind.Inverted,
            "crumble" => LedgeKind.Crumble,
            "moving" => LedgeKind.Moving,
            _ => LedgeKind.Wood,
        };
    }
}
