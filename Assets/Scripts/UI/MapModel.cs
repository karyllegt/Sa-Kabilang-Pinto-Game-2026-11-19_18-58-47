using System.Collections.Generic;
using Skp.Save;
using UnityEngine;

namespace Skp.UI
{
    public enum NodeState { Locked, Ready, Cleared }

    /// <summary>
    /// The level map without any visuals (ported from the prototype's openMap / mapGoTo / stepMap):
    /// nine nodes in map pixels (1440x270, y down), the selected node, the Tala token walking the path
    /// node by node, and a "peek" at a locked node. Pure C#, so tests drive it directly.
    /// </summary>
    public class MapModel
    {
        public const int MapWidth = 1440, MapHeight = 270, ViewWidth = 480;
        /// <summary>Token speed: 2.3 px per 60 Hz frame.</summary>
        public const float WalkSpeed = 2.3f * 60f;

        public static readonly Vector2[] Nodes =
        {
            new Vector2(60, 196), new Vector2(190, 150), new Vector2(330, 198), new Vector2(540, 156), new Vector2(670, 206),
            new Vector2(810, 152), new Vector2(1020, 196), new Vector2(1160, 150), new Vector2(1320, 182),
        };

        public static readonly Vector2 Lola = new Vector2(18, 182);

        readonly SaveData _save;
        readonly List<int> _queue = new List<int>();

        public int Selected { get; private set; }
        public int At { get; private set; }
        /// <summary>A locked node the player tapped (info panel shows it); -1 = none.</summary>
        public int Peek { get; private set; } = -1;
        public Vector2 Token { get; private set; }
        public bool Walking => _queue.Count > 0;
        public int Facing { get; private set; } = 1;
        public int MaxReachable => Mathf.Min(Nodes.Length - 1, _save.unlocked - 1);
        public IReadOnlyList<int> Queue => _queue;

        public MapModel(SaveData save, int from = -1)
        {
            _save = save;
            int max = MaxReachable;
            if (from >= 0 && from < Nodes.Length)
            {
                At = Selected = from;
                int nx = Mathf.Min(max, from + 1);
                if (nx != from) { Selected = nx; _queue.Add(nx); }
            }
            else At = Selected = max;
            Token = Nodes[At];
        }

        public NodeState State(int i) => ProgressRules.IsNodeCleared(_save, i) ? NodeState.Cleared
                                      : ProgressRules.IsNodeUnlocked(_save, i) ? NodeState.Ready : NodeState.Locked;

        /// <summary>The node the info panel describes: a peeked locked node, else the selected one.</summary>
        public int InfoNode => Peek >= 0 ? Peek : Selected;

        /// <summary>Walk toward node i (clamped to the reachable range). Returns false if nothing changed.</summary>
        public bool Select(int i)
        {
            i = Mathf.Clamp(i, 0, MaxReachable);
            Peek = -1;
            if (i == Selected && At == Selected && _queue.Count == 0) return false;
            var q = new List<int>();
            int n = At;
            if (_queue.Count > 0) { q.Add(_queue[0]); n = _queue[0]; }
            while (n != i) { n += i > n ? 1 : -1; q.Add(n); }
            _queue.Clear();
            _queue.AddRange(q);
            Selected = i;
            return true;
        }

        public void PeekLocked(int i) => Peek = i;

        /// <summary>True when the token stands on the selected node and it is unlocked.</summary>
        public bool CanEnter => _queue.Count == 0 && At == Selected && State(Selected) != NodeState.Locked;

        /// <summary>Advances the token by dt seconds.</summary>
        public void Step(float dt)
        {
            float budget = WalkSpeed * dt;
            while (_queue.Count > 0 && budget > 0)
            {
                var target = Nodes[_queue[0]];
                var d = target - Token;
                float len = d.magnitude;
                if (Mathf.Abs(d.x) > 0.4f) Facing = d.x < 0 ? -1 : 1;
                if (len <= budget)
                {
                    Token = target;
                    At = _queue[0];
                    _queue.RemoveAt(0);
                    budget -= len;
                }
                else
                {
                    Token += d / len * budget;
                    budget = 0;
                }
            }
        }

        /// <summary>Nearest node to a map-pixel point within 26 px (tap target), or -1.</summary>
        public static int NodeAt(Vector2 mapPx)
        {
            int best = -1;
            float bd = 26f;
            for (int i = 0; i < Nodes.Length; i++)
            {
                float d = Vector2.Distance(mapPx, Nodes[i] + new Vector2(0, -6));
                if (d < bd) { bd = d; best = i; }
            }
            return best;
        }

        public static bool LolaAt(Vector2 mapPx) => Vector2.Distance(mapPx, Lola + new Vector2(7, 10)) < 16f;

        /// <summary>Lola has something new to say (the "!" marker).</summary>
        public bool LolaHasNews => !_save.metLola || _save.lolaSeen < _save.unlocked;

        /// <summary>Lola's lines for this point of the story (prototype talkLola): the first meeting once, then by progress.</summary>
        public static string[] LolaKeys(SaveData s)
        {
            if (!s.metLola) return new[] { "lola.first.1", "lola.first.2", "lola.first.3", "lola.first.4", "lola.first.5" };
            if (s.cleared) return new[] { "lola.cleared" };
            int u = s.unlocked;
            if (u <= 3) return new[] { "lola.act1" };
            if (u <= 5) return new[] { "lola.act2a" };
            if (u <= 8) return new[] { "lola.act2b" };
            return new[] { "lola.act3" };
        }

        public static int ActOf(int node) => node / 3;

        /// <summary>Map pixels (y down) to world units (y up), 16 px per unit, map origin at world (0,0) bottom-left.</summary>
        public static Vector2 ToWorld(Vector2 px) => new Vector2(px.x / 16f, (MapHeight - px.y) / 16f);
        public static Vector2 ToMap(Vector2 world) => new Vector2(world.x * 16f, MapHeight - world.y * 16f);
    }
}
