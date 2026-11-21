using System.Collections.Generic;
using System.Linq;
using Skp.Core;
using Skp.Dimensions;
using Skp.Player;
using Skp.Skills;
using UnityEngine;

namespace Skp.Levels
{
    /// <summary>
    /// docs/02 Feature 4 obstacle proof, on the ordinary route. Each obstacle is tried on its own: the ledge Tala comes
    /// from, the ledge it guards, the ledge that is the way on, and their hazards. With the skill the bot reaches the way
    /// on without a hit; without that skill (the other owned skills kept) it cannot. Separately, the whole room is searched
    /// without the skill to count how often the rest of the ordinary route offers another way up (reported, not required).
    /// A triple-bolt pack counts as crossed when one fan strikes two or more of it and a single bolt one.
    /// </summary>
    public static class ObstacleCheck
    {
        public struct Case
        {
            public string Type, Difficulty;
            public int Level, Room;
            public bool Crossed, Blocked;
            /// <summary>Without the skill, the rest of the room's ordinary route still leads past it (informational).</summary>
            public bool RoomBypass;
            public Vector2 At;
            public string Detail;
            public bool Ok => Crossed && Blocked;
            public override string ToString() => $"{Type} L{Level + 1} {Difficulty} room {Room + 1} at ({At.x:0.0}, {At.y:0.0}): crossed {Crossed}, blocked without {Blocked}, room bypass {RoomBypass} {Detail}";
        }

        /// <summary>Skills owned when level <paramref name="lv"/> starts in a normal playthrough (every earlier letter read).</summary>
        public static string[] OwnedBefore(int lv) => SkillIds.All.Take(Mathf.Clamp(lv, 0, SkillIds.All.Length)).ToArray();

        public static List<Case> Run(LevelLayout L, PlayerTuning tuning, DifficultyMode d, string[] owned)
        {
            var cases = new List<Case>();
            foreach (var o in L.Obstacles)
            {
                if (o.type == "pack") { cases.Add(Pack(L, o, tuning, d, owned)); continue; }
                var with = new SkillSet(owned);
                var without = new SkillSet(owned.Where(s => s != o.skill && !(o.skill == SkillIds.Veil && s == SkillIds.GreaterVeil)).ToArray());
                bool dash = o.type == "dash";
                var a = Attempt(L, o, tuning, d, with, true, true);
                var b = Attempt(L, o, tuning, d, without, !dash, true);
                var room = Attempt(L, o, tuning, d, without, !dash, false);
                var from = LevelWorldFactory.Build(L).Ledges[L.Plats.IndexOf(o.From)];
                cases.Add(new Case
                {
                    Type = o.type, Difficulty = d.SaveKey, Level = L.LevelIndex, Room = o.room,
                    Crossed = a.Clean, Blocked = !b.Clean, RoomBypass = room.Clean, At = from.Rect.center,
                    Detail = $"(with: {a}; without: {b})",
                });
            }
            return cases;
        }

        static SkillBot.Outcome Attempt(LevelLayout L, LObstacle o, PlayerTuning tuning, DifficultyMode d, ISkillSet skills, bool canDash, bool local)
        {
            var world = LevelWorldFactory.Build(L);
            var from = world.Ledges[L.Plats.IndexOf(o.From)];
            var to = world.Ledges[L.Plats.IndexOf(o.To)];
            var over = o.Over != null ? world.Ledges[L.Plats.IndexOf(o.Over)] : null;
            float roomBottom = LevelWorldFactory.RoomBottom(o.room);
            float roomTop = roomBottom + LevelLayout.RoomTiles;
            if (local)
            {
                // the obstacle on its own: its ledges and the hazards on them (spikes, roots, curtain, thorns)
                var keep = new HashSet<Ledge> { from, to };
                if (over != null) keep.Add(over);
                world.Ledges.RemoveAll(l => !keep.Contains(l));
                // its own hazard (with a gauntlet's curtains) and whatever sits on its ledges; other obstacles have their own checks
                var own = o.Hazard != null ? world.Hazards[L.Haz.IndexOf(o.Hazard)] : null;
                world.Hazards.RemoveAll(h => h != own
                    && !(h.Kind == HazardKind.Curtain && over != null && h.Rect.xMax > over.Rect.xMin && h.Rect.xMin < over.Rect.xMax && Mathf.Abs(h.Rect.yMin - over.Rect.yMax) < 3f || h.Kind == HazardKind.Curtain && over != null && Mathf.Abs(h.Rect.yMax - over.Rect.yMin) < 0.1f)
                    && !keep.Any(l => h.Rect.xMax > l.Rect.xMin && h.Rect.xMin < l.Rect.xMax && Mathf.Abs(h.Rect.yMin - l.Rect.yMax) < 0.6f));
            }
            else
            {
                // only this room (and a little around it): searches step a small world
                float lo = roomBottom - 2f, hi = roomTop + 3f;
                world.Ledges.RemoveAll(l => l.Rect.yMax < lo || l.Rect.yMin > hi);
                world.Hazards.RemoveAll(h => h.Rect.yMax < lo || h.Rect.yMin > hi);
            }
            world.Doors.Clear();
            var bot = new SkillBot(world, tuning, d, skills, Dim.Ordinary, canDash, useVeil: o.type == "veil", useBolt: o.type == "bolt")
            {
                Region = new Rect(-1, roomBottom - 1f, world.Bounds.width + 2, LevelLayout.RoomTiles + 2f),
            };
            float goalTop = to.Rect.yMax - 0.01f;
            var start = SafeSpot(world, from, tuning.hitbox);
            // the way on, or (searching the room) any ordinary ledge as high, up to the room's top floor with its lantern
            return bot.Reach(start, l => l == to || !local && (l.Mask & Dim.Ordinary) != 0 && l.Rect.yMax >= goalTop && l.Rect.yMax <= roomTop + 0.6f);
        }

        /// <summary>Where Tala stands on the ledge to start: the hazard-free spot nearest its middle.</summary>
        static Vector2 SafeSpot(PlayerWorld w, Ledge l, Vector2 size)
        {
            Vector2 best = new Vector2(l.Rect.center.x - size.x * 0.5f, l.Rect.yMax);
            float bestD = float.MaxValue;
            for (float x = l.Rect.xMin; x <= l.Rect.xMax - size.x; x += 0.125f)
            {
                var body = new Rect(x, l.Rect.yMax, size.x, size.y);
                if (w.Hazards.Any(h => h.Rect.Overlaps(body))) continue;
                float d = Mathf.Abs(x + size.x * 0.5f - l.Rect.center.x);
                if (d < bestD) { bestD = d; best = new Vector2(x, l.Rect.yMax); }
            }
            return best;
        }

        /// <summary>Stand a tile and a half from the pack and fire once: a fan strikes two or more of it, a single bolt one.</summary>
        static Case Pack(LevelLayout L, LObstacle o, PlayerTuning tuning, DifficultyMode d, string[] owned)
        {
            int Kills(bool spread)
            {
                var world = LevelWorldFactory.Build(L);
                var ledge = world.Ledges[L.Plats.IndexOf(o.Over)];
                var pack = world.Enemies.Where(e => !e.Dead && Mathf.Abs(e.Position.y - ledge.Rect.yMax) < 0.1f && e.Position.x >= ledge.Rect.xMin - 0.1f && e.Position.x <= ledge.Rect.xMax).ToList();
                world.Enemies.Clear();
                world.Enemies.AddRange(pack);
                var skills = new SkillSet(owned.Where(s => spread || s != SkillIds.TripleBolt).Concat(spread ? new[] { SkillIds.TripleBolt } : new string[0]).ToArray());
                var sim = new PlayerSim(tuning, d, world) { Skills = skills, SkillTuning = SkillLibrary.Tuning };
                float px = pack.Count > 0 ? pack[0].Center.x : ledge.Rect.center.x;
                sim.Position = new Vector2(px - 1.5f - tuning.hitbox.x, ledge.Rect.yMax);
                sim.Facing = 1;
                sim.Invulnerable = 100000;
                int hit = 0;
                sim.BoltHit += (_, t) => { if (t == BoltTarget.Anino || t == BoltTarget.Creature) hit++; };
                sim.Step(new PlayerInputFrame { BoltPressed = true });
                for (int i = 0; i < 40; i++) sim.Step(default);
                return hit;
            }
            int fan = Kills(true), single = Kills(false);
            var at = LevelWorldFactory.Build(L).Ledges[L.Plats.IndexOf(o.Over)].Rect.center;
            return new Case
            {
                Type = "pack", Difficulty = d.SaveKey, Level = L.LevelIndex, Room = o.room, At = at,
                Crossed = fan >= 2, Blocked = single <= 1, Detail = $"(one fan strikes {fan}, one bolt strikes {single})",
            };
        }
    }
}
