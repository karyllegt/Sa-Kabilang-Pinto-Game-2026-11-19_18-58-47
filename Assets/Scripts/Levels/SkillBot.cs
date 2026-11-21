using System;
using System.Collections.Generic;
using Skp.Core;
using Skp.Dimensions;
using Skp.Player;
using Skp.Skills;
using UnityEngine;

namespace Skp.Levels
{
    /// <summary>
    /// Skill-aware reachability bot (docs/02 Feature 4 tests). Search over the places Tala can stand in one dimension,
    /// cheapest first on hits taken, moving with macro-moves run through the real <see cref="PlayerSim"/>: walks, jumps
    /// (run-ups, holds, steering), and, for the skills it is given, a second jump in mid-air, dashes in the air, holding
    /// jump to glide, raising the veil (its time and cooldown carry from move to move) and firing a bolt first. Doors
    /// are not used; creatures, water and bells are left out (they move on their own clocks). Tala cannot die; hits count.
    /// </summary>
    public class SkillBot
    {
        public struct Outcome
        {
            public bool Reached;
            /// <summary>Fewest hits on the way to the goal (int.MaxValue if not reached).</summary>
            public int Hits;
            public int States, Macros;
            public bool Clean => Reached && Hits == 0;
            public override string ToString() => Reached ? $"reached with {Hits} hits ({States} states)" : $"not reached ({States} states)";
        }

        struct Node { public Vector2 Pos; public int Hits, VeilLeft, VeilCd, DashCd, Parent; public string How; }

        /// <summary>The moves of the last path found (debugging).</summary>
        public readonly List<string> LastPath = new List<string>();

        struct Macro
        {
            public int Dir, RunUp, Hold, Steer, DjAt, DashAt;
            public bool Veil, Bolt;
        }

        readonly PlayerWorld _world;
        readonly PlayerSim _sim;
        readonly ISkillSet _skills;
        readonly Dim _dim;
        readonly Dictionary<Ledge, int> _ledgeIndex = new Dictionary<Ledge, int>();
        readonly List<Macro> _macros = new List<Macro>();
        public int MaxStates = 500;
        readonly bool _canDash, _useVeil, _useBolt;
        /// <summary>States outside this area are not expanded (keeps a search inside one room).</summary>
        public Rect Region = new Rect(-1e5f, -1e5f, 2e5f, 2e5f);

        /// <param name="useVeil">Raise the veil in moves (the veil gauntlet's own test); off elsewhere to keep searches small.</param>
        /// <param name="useBolt">Fire a bolt before moves (the thorn wall's own test).</param>
        public SkillBot(PlayerWorld world, PlayerTuning tuning, DifficultyMode difficulty, ISkillSet skills, Dim dimension = Dim.Ordinary,
                        bool canDash = true, bool useVeil = true, bool useBolt = true)
        {
            _useVeil = useVeil;
            _useBolt = useBolt;
            _world = world;
            _world.Items.Clear();
            _world.Enemies.Clear();
            _world.Bells.Clear();
            _world.Flood = null;
            _skills = skills ?? SkillSet.None;
            _dim = dimension;
            _canDash = canDash;
            for (int i = 0; i < _world.Ledges.Count; i++) _ledgeIndex[_world.Ledges[i]] = i;
            _sim = new PlayerSim(tuning, difficulty, _world) { Skills = _skills, SkillTuning = SkillLibrary.Tuning };
            BuildMacros();
        }

        bool Has(string id) => _skills.Has(id);

        void BuildMacros()
        {
            bool dj = Has(SkillIds.DoubleJump), glide = Has(SkillIds.Glide), veil = _useVeil && Has(SkillIds.Veil), bolt = _useBolt && Has(SkillIds.Bolt);
            int longHold = glide ? 200 : 40;
            void Add(Macro m)
            {
                if (!_canDash && m.DashAt != -1) return;
                _macros.Add(m);
            }
            foreach (int dir in new[] { -1, 1 })
            {
                Add(new Macro { Dir = dir, Steer = dir, DjAt = -1, DashAt = -1 }); // walk / walk off
                foreach (int run in new[] { 0, 14 })
                    foreach (int hold in new[] { 5, longHold })
                    {
                        Add(new Macro { Dir = dir, RunUp = run, Hold = hold, Steer = dir, DjAt = -1, DashAt = -1 });
                        Add(new Macro { Dir = dir, RunUp = run, Hold = hold, Steer = 0, DjAt = -1, DashAt = -1 });
                    }
                // the dash is part of the base kit: from standing, or in the air
                Add(new Macro { Dir = dir, Steer = dir, DjAt = -1, DashAt = -2 });
                foreach (int run in new[] { 0, 14 })
                    foreach (int at in new[] { 0, 12, 24 })
                        Add(new Macro { Dir = dir, RunUp = run, Hold = longHold, Steer = dir, DjAt = -1, DashAt = at });
                if (dj)
                {
                    foreach (int run in new[] { 0, 14 })
                        foreach (int at in new[] { 8, 12, 20 })
                        {
                            Add(new Macro { Dir = dir, RunUp = run, Hold = longHold, Steer = dir, DjAt = at, DashAt = -1 });
                            Add(new Macro { Dir = dir, RunUp = run, Hold = longHold, Steer = 0, DjAt = at, DashAt = -1 });
                        }
                    foreach (int run in new[] { 0, 14 })
                        foreach (int at in new[] { 8, 16 })
                            foreach (int after in new[] { 6, 12 })
                                Add(new Macro { Dir = dir, RunUp = run, Hold = longHold, Steer = dir, DjAt = at, DashAt = at + after });
                }
                if (veil)
                {
                    Add(new Macro { Dir = dir, Steer = dir, DjAt = -1, DashAt = -1, Veil = true });
                    foreach (int run in new[] { 0, 14 }) Add(new Macro { Dir = dir, RunUp = run, Hold = longHold, Steer = dir, DjAt = -1, DashAt = -1, Veil = true });
                }
                if (bolt)
                {
                    Add(new Macro { Dir = dir, Steer = dir, DjAt = -1, DashAt = -1, Bolt = true });
                    foreach (int run in new[] { 0, 6, 14 })
                        foreach (int hold in new[] { 5, longHold })
                        {
                            Add(new Macro { Dir = dir, RunUp = run, Hold = hold, Steer = dir, DjAt = -1, DashAt = -1, Bolt = true });
                            Add(new Macro { Dir = dir, RunUp = run, Hold = hold, Steer = 0, DjAt = -1, DashAt = -1, Bolt = true });
                        }
                    if (dj) Add(new Macro { Dir = dir, RunUp = 14, Hold = longHold, Steer = dir, DjAt = 16, DashAt = -1, Bolt = true });
                }
            }
            foreach (int steer in new[] { -1, 0, 1 })
            {
                Add(new Macro { Dir = 0, Hold = longHold, Steer = steer, DjAt = -1, DashAt = -1 });
                if (dj) Add(new Macro { Dir = 0, Hold = longHold, Steer = steer, DjAt = 16, DashAt = -1 });
            }
        }

        /// <summary>Fewest-hits search from standing at <paramref name="start"/> (feet) until landing on a ledge that satisfies <paramref name="goal"/>.</summary>
        public Outcome Reach(Vector2 start, Func<Ledge, bool> goal)
        {
            var o = new Outcome { Hits = int.MaxValue };
            var nodes = new List<Node> { new Node { Pos = start, Parent = -1 } };
            LastPath.Clear();
            var best = new Dictionary<long, int>();
            var buckets = new List<List<int>> { new List<int> { 0 } };
            var done = new HashSet<int>();
            for (int b = 0; b < buckets.Count && nodes.Count < MaxStates; b++)
            {
                if (o.Reached && o.Hits <= b) break;
                for (int qi = 0; qi < buckets[b].Count && nodes.Count < MaxStates; qi++)
                {
                    int head = buckets[b][qi];
                    if (!done.Add(head)) continue;
                    var n = nodes[head];
                    foreach (var m in _macros)
                    {
                        if (m.Veil && n.VeilCd > 0) continue;
                        if (m.DashAt != -1 && n.DashCd > 0 && m.DashAt < n.DashCd) continue; // the dash is not back yet
                        o.Macros++;
                        var next = Simulate(n, m, out int hits, out var ledge);
                        if (ledge == null) continue;
                        int total = n.Hits + hits;
                        if (goal(ledge))
                        {
                            if (total < o.Hits)
                            {
                                o.Hits = total; o.Reached = true;
                                LastPath.Clear();
                                LastPath.Add(Describe(m) + $" -> lands {next.Pos}");
                                for (int pi = head; pi > 0; pi = nodes[pi].Parent) LastPath.Insert(0, nodes[pi].How + $" -> {nodes[pi].Pos}");
                            }
                            if (total == 0) { o.States = nodes.Count; return o; } // nothing beats a clean crossing
                            continue;
                        }
                        if (!Region.Contains(next.Pos)) continue;
                        long k = Key(next, _ledgeIndex[ledge]);
                        if (best.TryGetValue(k, out int prev) && prev <= total) continue;
                        best[k] = total;
                        next.Hits = total;
                        next.Parent = head;
                        next.How = Describe(m);
                        nodes.Add(next);
                        while (buckets.Count <= total) buckets.Add(new List<int>());
                        buckets[total].Add(nodes.Count - 1);
                    }
                }
            }
            o.States = nodes.Count;
            return o;
        }

        static string Describe(Macro m) => $"dir {m.Dir} run {m.RunUp} hold {m.Hold} steer {m.Steer} dj {m.DjAt} dash {m.DashAt}{(m.Veil ? " veil" : "")}{(m.Bolt ? " bolt" : "")}";

        static long Key(Node n, int ledge) =>
            ((long)(ledge + 1) << 28) ^ ((long)Mathf.RoundToInt(n.Pos.x * 2f) << 12) ^ ((long)((n.VeilLeft + 29) / 30) << 6) ^ (long)Mathf.Min(63, n.VeilCd / 60) ^ ((long)Mathf.Min(7, n.DashCd / 6) << 40);

        Node Simulate(Node n, Macro m, out int hits, out Ledge ledge)
        {
            _sim.Respawn();
            _sim.SetDimension(_dim);
            _sim.Shadow.Clear();
            _sim.Position = n.Pos;
            _sim.Velocity = Vector2.zero;
            _sim.Grounded = true;
            _sim.Invulnerable = 0;
            _sim.MaxVitality = _sim.Vitality = 999;
            _sim.Flame = 100;
            _sim.VeilTicks = n.VeilLeft;
            _sim.VeilCooldown = n.VeilCd;
            _sim.DashCooldown = n.DashCd; // cooldowns carry from move to move, as in play
            if (m.Dir != 0) _sim.Facing = m.Dir;
            foreach (var l in _world.Ledges) { l.CrumbleTimer = 0; l.DeadTicks = 0; }
            foreach (var h in _world.Hazards) h.BurnTicks = 0;
            int count = 0;
            Action<int> onHurt = _ => count++;
            _sim.Hurt += onHurt;
            try
            {
                int t = 0;
                PlayerInputFrame F(int move) => new PlayerInputFrame { MoveX = move, VeilPressed = m.Veil && t == 0, BoltPressed = m.Bolt && t == 0 };
                if (m.Bolt) { _sim.Step(F(0)); t++; for (int i = 0; i < 30 && _sim.Bolts.Count > 0; i++, t++) _sim.Step(default); } // until the bolt lands (a wall up to 8 tiles off)
                if (m.DashAt == -2)
                {
                    var f = F(m.Dir); f.DashPressed = true; _sim.Step(f); t++;
                    for (; t < 60 && _sim.Grounded; t++) _sim.Step(F(m.Dir));
                }
                else if (m.Hold == 0)
                {
                    for (; t < 90 && _sim.Grounded; t++) _sim.Step(F(m.Dir));
                }
                else
                {
                    bool left = false;
                    for (int r = 0; r < m.RunUp; r++, t++) { _sim.Step(F(m.Dir)); if (!_sim.Grounded) { left = true; break; } }
                    if (!left)
                        for (int h = 0; h < m.Hold && t < 260; h++, t++)
                        {
                            var f = F(m.Steer);
                            f.JumpPressed = h == 0 || h == m.DjAt;
                            f.JumpHeld = true;
                            f.DashPressed = h == m.DashAt;
                            _sim.Step(f);
                            if (h > 2 && _sim.Grounded) break;
                        }
                }
                // a move ends only once Tala stands again and any dash is over
                for (; t < 300 && (!_sim.Grounded || _sim.DashTicks > 0); t++) _sim.Step(F(m.Hold == 0 ? m.Dir : m.Steer));
                for (int s = 0; s < 2; s++) _sim.Step(default);
                hits = count;
                ledge = _sim.Grounded ? _sim.Standing : null;
                if (ledge != null && !_ledgeIndex.ContainsKey(ledge)) ledge = null;
                return new Node { Pos = _sim.Position, VeilLeft = _sim.VeilTicks, VeilCd = _sim.VeilCooldown, DashCd = _sim.DashCooldown };
            }
            finally { _sim.Hurt -= onHurt; }
        }
    }
}
