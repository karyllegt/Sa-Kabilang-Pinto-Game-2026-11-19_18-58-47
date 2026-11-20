using System.Collections.Generic;
using Skp.Core;
using Skp.Dimensions;
using Skp.Player;
using UnityEngine;

namespace Skp.Levels
{
    /// <summary>
    /// Completability bot. Breadth-first search over the places Tala can stand (ledge, x to half a tile,
    /// dimension), moving between them with a fixed set of macro-moves run through the real
    /// <see cref="PlayerSim"/> physics: runs and walk-offs, jumps with short and long run-ups, short and
    /// full holds, with or without steering, and using doors. Damage is real (knockback changes the path)
    /// but Tala cannot die; hits are counted. Items and creatures are left out of the search (the letter is
    /// detected by touch; creatures move and are covered by the playtests).
    /// </summary>
    public class LevelBot
    {
        public struct Result
        {
            public bool ClueReachable, ExitReachable, ExitReachableAfterClue;
            public bool[] LanternReachable;
            public int StatesVisited, MacrosRun;
            /// <summary>Fewest hits to touch the letter; fewest hits for the whole run spawn to letter to exit.</summary>
            public int HitsOnCluePath, HitsOnExitPath;
            public bool Completable => ClueReachable && ExitReachableAfterClue && System.Array.TrueForAll(LanternReachable, b => b);
            public override string ToString() =>
                $"clue {ClueReachable} exit {ExitReachable} exitAfterClue {ExitReachableAfterClue} lanterns {string.Join(",", System.Array.ConvertAll(LanternReachable, b => b ? "1" : "0"))} states {StatesVisited} macros {MacrosRun} hits to letter {HitsOnCluePath}, letter to exit total {HitsOnExitPath}";
        }

        struct Node
        {
            public Vector2 Pos;
            public Dim Dim;
            public int Parent;
            public int Hits;
        }

        readonly PlayerWorld _world;
        readonly PlayerSim _sim;
        readonly Dictionary<Ledge, int> _ledgeIndex = new Dictionary<Ledge, int>();
        readonly List<Rect> _clueRects = new List<Rect>();
        readonly List<Dim> _clueMasks = new List<Dim>();
        Rect _exit;
        bool _hasExit;
        public int MaxStates = 6000;

        static readonly int[] RunUps = { 0, 6, 14 };
        static readonly int[] Holds = { 0, 5, 40 };

        public LevelBot(LevelLayout layout, PlayerTuning tuning, DifficultyMode difficulty)
        {
            _world = LevelWorldFactory.Build(layout);
            for (int i = 0; i < _world.Ledges.Count; i++) _ledgeIndex[_world.Ledges[i]] = i;
            foreach (var it in _world.Items)
                if (it.Kind == ItemKind.Clue) { _clueRects.Add(it.Rect); _clueMasks.Add(it.Mask); }
            foreach (var d in _world.Doors) if (d.Kind == DoorKind.Exit) { _exit = d.Rect; _hasExit = true; }
            _world.Items.Clear();
            _world.Enemies.Clear();
            _sim = new PlayerSim(tuning, difficulty, _world);
        }

        public Result Run()
        {
            var r = new Result { LanternReachable = new bool[_world.Lanterns.Count] };
            var start = new Node { Pos = _sim.Position, Dim = Dim.Ordinary, Parent = -1 };
            var (clueNode, exitFromStart, hitsClue) = Search(start, true, ref r);
            r.ExitReachable = exitFromStart;
            if (_clueRects.Count == 0) { r.ClueReachable = true; r.ExitReachableAfterClue = exitFromStart; return r; }
            r.ClueReachable = clueNode.HasValue;
            r.HitsOnCluePath = hitsClue;
            if (clueNode.HasValue)
            {
                var dummy = new Result { LanternReachable = new bool[_world.Lanterns.Count] };
                var (_, exitAfter, hitsExit) = Search(clueNode.Value, false, ref dummy);
                r.ExitReachableAfterClue = exitAfter;
                r.HitsOnExitPath = hitsExit;
                r.MacrosRun += dummy.MacrosRun;
                r.StatesVisited += dummy.StatesVisited;
            }
            return r;
        }

        (Node? clue, bool exit, int hits) Search(Node start, bool lookForClue, ref Result r)
        {
            // Cheapest-first on hits taken (a bucket queue: hits are small integers), so the reported
            // letter and exit paths take as few hits as the move set allows.
            var nodes = new List<Node> { start };
            var best = new Dictionary<long, int> { [Key(start, -1)] = 0 };
            var buckets = new List<List<int>> { new List<int> { 0 } };
            var done = new HashSet<int>();
            Node? clue = null;
            bool exit = false;
            int exitHits = int.MaxValue, clueHits = int.MaxValue;
            for (int b = 0; b < buckets.Count && nodes.Count < MaxStates; b++)
            {
                // once the letter (if needed) and the exit are found at this cost or less, nothing cheaper remains
                if (exit && exitHits <= b && (!lookForClue || (clue.HasValue && clueHits <= b))
                    && System.Array.TrueForAll(r.LanternReachable, x => x)) break;
                for (int qi = 0; qi < buckets[b].Count; qi++)
                {
                    int head = buckets[b][qi];
                    if (!done.Add(head)) continue;
                    var n = nodes[head];
                    foreach (var macro in Macros(n))
                    {
                        r.MacrosRun++;
                        var o = Simulate(n, macro, out bool touchedClue, out bool touchedExit, out int hits, r.LanternReachable, out int ledge);
                        int total = n.Hits + hits;
                        if (touchedExit && total < exitHits) { exit = true; exitHits = total; }
                        if (lookForClue && touchedClue && total < clueHits) { clue = new Node { Pos = o.Pos, Dim = o.Dim, Parent = head, Hits = total }; clueHits = total; }
                        if (ledge < 0) continue;
                        long k = Key(o, ledge);
                        if (best.TryGetValue(k, out int prev) && prev <= total) continue;
                        best[k] = total;
                        o.Parent = head; o.Hits = total;
                        nodes.Add(o);
                        while (buckets.Count <= total) buckets.Add(new List<int>());
                        buckets[total].Add(nodes.Count - 1);
                    }
                }
            }
            r.StatesVisited += nodes.Count;
            return (clue, exit, lookForClue ? (clue.HasValue ? clueHits : 0) : (exit ? exitHits : 0));
        }

        static long Key(Node n, int ledge) => ((long)(ledge + 1) << 24) ^ ((long)Mathf.RoundToInt(n.Pos.x * 2f) << 4) ^ (long)n.Dim;

        struct Macro { public int Dir, RunUp, Hold, Steer; public bool Door; }

        IEnumerable<Macro> Macros(Node n)
        {
            yield return new Macro { Door = true };
            for (int dir = -1; dir <= 1; dir++)
                foreach (var run in RunUps)
                    foreach (var hold in Holds)
                    {
                        // no jump = a plain walk (or walk-off) in one direction, without a separate run-up
                        if (hold == 0 && (dir == 0 || run > 0)) continue;
                        if (dir == 0)
                        {
                            if (run > 0) continue;
                            yield return new Macro { Dir = 0, Hold = hold, Steer = -1 };
                            yield return new Macro { Dir = 0, Hold = hold, Steer = 1 };
                            yield return new Macro { Dir = 0, Hold = hold, Steer = 0 };
                        }
                        else
                        {
                            yield return new Macro { Dir = dir, RunUp = run, Hold = hold, Steer = dir };
                            if (hold > 0) yield return new Macro { Dir = dir, RunUp = run, Hold = hold, Steer = 0 };
                        }
                    }
        }

        void Reset(Node n)
        {
            _sim.Respawn(); // clears timers, dimension, shadow; then place exactly
            _sim.Position = n.Pos;
            _sim.Velocity = Vector2.zero;
            _sim.SetDimension(n.Dim);
            _sim.Shadow.Clear();
            _sim.Grounded = true;
            _sim.Invulnerable = 0;
            _sim.MaxVitality = _sim.Vitality = 999;
            _sim.Flame = 100;
            foreach (var l in _world.Ledges) { l.CrumbleTimer = 0; l.DeadTicks = 0; }
        }

        Node Simulate(Node n, Macro m, out bool clue, out bool exit, out int hits, bool[] lanterns, out int ledge)
        {
            Reset(n);
            clue = false; exit = false; hits = 0; ledge = -1;
            int hitCount = 0;
            System.Action<int> onHurt = _ => hitCount++;
            System.Action<int> onLantern = k => { if (k < lanterns.Length) lanterns[k] = true; };
            _sim.Hurt += onHurt;
            _sim.LanternLit += onLantern;
            _sim.Checkpoint = -1; // every lantern touched counts
            try
            {
                if (m.Door)
                {
                    _sim.Step(new PlayerInputFrame());
                    if (_sim.NearDoor < 0) return n;
                    _sim.Step(new PlayerInputFrame { InteractPressed = true });
                    Settle(ref clue, ref exit, 120);
                }
                else
                {
                    bool left = false;
                    int t = 0;
                    for (; t < m.RunUp; t++) { Tick(new PlayerInputFrame { MoveX = m.Dir }, ref clue, ref exit); if (!_sim.Grounded) { left = true; break; } }
                    if (m.Hold > 0 && !left)
                    {
                        for (int h = 0; h < m.Hold && t < 200; h++, t++)
                            Tick(new PlayerInputFrame { MoveX = m.Steer, JumpPressed = h == 0, JumpHeld = true }, ref clue, ref exit);
                    }
                    else if (m.Hold == 0)
                    {
                        for (; t < 90 && _sim.Grounded; t++) Tick(new PlayerInputFrame { MoveX = m.Dir }, ref clue, ref exit);
                    }
                    for (; t < 220 && !_sim.Grounded; t++) Tick(new PlayerInputFrame { MoveX = m.Hold == 0 ? m.Dir : m.Steer }, ref clue, ref exit);
                    // settle a couple of ticks so landing is final
                    for (int s = 0; s < 2; s++) Tick(new PlayerInputFrame(), ref clue, ref exit);
                }
                hits = hitCount;
                if (!_sim.Grounded || _sim.Standing == null || !_ledgeIndex.TryGetValue(_sim.Standing, out ledge)) { ledge = -1; return n; }
                return new Node { Pos = _sim.Position, Dim = _sim.Dimension };
            }
            finally
            {
                _sim.Hurt -= onHurt;
                _sim.LanternLit -= onLantern;
            }
        }

        void Settle(ref bool clue, ref bool exit, int max)
        {
            for (int t = 0; t < max; t++)
            {
                Tick(new PlayerInputFrame(), ref clue, ref exit);
                if (_sim.Grounded && t > 2) break;
            }
        }

        void Tick(PlayerInputFrame f, ref bool clue, ref bool exit)
        {
            _sim.Step(f);
            var hb = _sim.Hitbox;
            for (int i = 0; i < _clueRects.Count; i++)
                if (!clue && DimensionRules.Active(_clueMasks[i], _sim.Dimension) && hb.Overlaps(_clueRects[i])) clue = true;
            if (_hasExit && !exit)
            {
                const float m4 = 4f / 16f;
                var reach = new Rect(hb.x - m4, hb.y - m4, hb.width + 2 * m4, hb.height + 2 * m4);
                if (reach.Overlaps(_exit)) exit = true;
            }
        }
    }
}
