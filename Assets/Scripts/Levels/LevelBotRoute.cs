using System.Collections.Generic;
using Skp.Core;
using Skp.Dimensions;
using Skp.Player;
using UnityEngine;

namespace Skp.Levels
{
    /// <summary>
    /// The bot's route as real input: for one room, the macro-moves from its lantern to the letter (if the room has it)
    /// and then on to the next lantern or the exit, each expanded into the exact 60 Hz input frames the search ran
    /// through <see cref="PlayerSim"/>. Used to record the game playing itself (the progress video); a replay feeds
    /// these frames to the live player and starts each step from the place the bot planned it.
    /// </summary>
    public partial class LevelBot
    {
        public struct RouteStep
        {
            public Vector2 Start;
            public Dim Dim;
            public PlayerInputFrame[] Inputs;
        }

        enum Goal { Clue, NextLantern, Exit }

        List<PlayerInputFrame> _record;

        void StepRec(PlayerInputFrame f)
        {
            _record?.Add(f);
            _sim.Step(f);
        }

        /// <summary>The route through one room, or null when the bot finds none.</summary>
        public static List<RouteStep> PlanRoom(LevelLayout layout, PlayerTuning tuning, DifficultyMode difficulty, Skp.Skills.ISkillSet skills, int room)
        {
            var bot = new LevelBot(layout, tuning, difficulty, skills, room);
            var steps = new List<RouteStep>();
            var cur = new Node { Pos = bot._sim.Position, Dim = Dim.Ordinary, Parent = -1 };
            if (bot._clueRects.Count > 0)
            {
                if (!bot.FindPath(cur, Goal.Clue, steps, out cur)) return null;
            }
            return bot.FindPath(cur, bot._needExit ? Goal.Exit : Goal.NextLantern, steps, out _) ? steps : null;
        }

        bool FindPath(Node start, Goal goal, List<RouteStep> steps, out Node end)
        {
            end = start;
            start.Parent = -1; start.Hits = 0;
            var nodes = new List<Node> { start };
            var via = new List<Macro> { default };
            var best = new Dictionary<long, int> { [Key(start, -1)] = 0 };
            var buckets = new List<List<int>> { new List<int> { 0 } };
            var done = new HashSet<int>();
            var lanterns = new bool[_world.Lanterns.Count];
            int target = Mathf.Min(_room + 1, lanterns.Length - 1);
            for (int b = 0; b < buckets.Count && nodes.Count < MaxStates; b++)
                for (int qi = 0; qi < buckets[b].Count; qi++)
                {
                    int head = buckets[b][qi];
                    if (!done.Add(head)) continue;
                    var n = nodes[head];
                    foreach (var macro in Macros(n))
                    {
                        System.Array.Clear(lanterns, 0, lanterns.Length);
                        var o = Simulate(n, macro, out bool clue, out bool exit, out int hits, lanterns, out int ledge);
                        bool reached = goal == Goal.Clue ? clue && ledge >= 0 : goal == Goal.Exit ? exit : lanterns[target];
                        if (reached)
                        {
                            // the chain of macros from the start to this one, each expanded to its input frames
                            var chain = new List<(int from, Macro m)> { (head, macro) };
                            for (int x = head; nodes[x].Parent >= 0; x = nodes[x].Parent) chain.Add((nodes[x].Parent, via[x]));
                            chain.Reverse();
                            foreach (var (from, m) in chain) steps.Add(Expand(nodes[from], m));
                            end = ledge >= 0 ? o : new Node { Pos = _sim.Position, Dim = _sim.Dimension };
                            end.Parent = -1; end.Hits = 0;
                            return true;
                        }
                        if (ledge < 0) continue;
                        int total = n.Hits + hits;
                        long k = Key(o, ledge);
                        if (best.TryGetValue(k, out int prev) && prev <= total) continue;
                        best[k] = total;
                        o.Parent = head; o.Hits = total;
                        nodes.Add(o);
                        via.Add(macro);
                        while (buckets.Count <= total) buckets.Add(new List<int>());
                        buckets[total].Add(nodes.Count - 1);
                    }
                }
            return false;
        }

        RouteStep Expand(Node from, Macro m)
        {
            _record = new List<PlayerInputFrame>();
            try
            {
                Simulate(from, m, out _, out _, out _, new bool[_world.Lanterns.Count], out _);
                return new RouteStep { Start = from.Pos, Dim = from.Dim, Inputs = _record.ToArray() };
            }
            finally { _record = null; }
        }
    }
}
