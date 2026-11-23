using System.Collections.Generic;
using Skp.Dimensions;
using Skp.Player;
using UnityEngine;

namespace Skp.Levels
{
    /// <summary>
    /// Draws a generated level from its <see cref="PlayerWorld"/>: one prefab instance (from the <see cref="LevelPrefabCatalog"/>)
    /// per ledge, hazard, door, item, creature, lantern, bell and the water, built once; LateUpdate only moves, sizes, toggles
    /// or switches their state (no per-frame allocation). Every prefab carries a <see cref="DimensionMember"/> for the
    /// dimension styling; the view sets its mask from the level data.
    /// </summary>
    public class LevelView : MonoBehaviour
    {
        [Tooltip("Backdrops and other level art the runner still reads.")]
        public LevelArt art;
        [Tooltip("The prefabs every level object is made from (R1). Falls back to Resources/" + LevelPrefabCatalog.ResourcePath + ".")]
        public LevelPrefabCatalog catalog;
        [Tooltip("1 to 3: picks the act's ledge art and decoration (set by the runner before Build).")]
        public int act = 1;

        /// <summary>World units per pixel of the imported white pixel (16 PPU): scale it by pixels.</summary>
        const float PxPerUnit = 16f;
        /// <summary>How much nearer the camera a Home door sits than the Enter door it shares a spot with.</summary>
        public const float HomeDoorDepth = 0.01f;

        PlayerWorld _world;
        PlayerSim _sim;
        readonly List<(Ledge ledge, Transform t, SpriteRenderer sr, float baseX)> _ledges = new List<(Ledge, Transform, SpriteRenderer, float)>();
        readonly List<(Hazard h, LevelPiece p)> _hazards = new List<(Hazard, LevelPiece)>();
        readonly List<(Item it, Transform t, Vector2 basePos, float seed)> _items = new List<(Item, Transform, Vector2, float)>();
        readonly List<(Enemy e, Transform t, LevelPiece p)> _enemies = new List<(Enemy, Transform, LevelPiece)>();
        readonly List<SpriteRenderer> _lanterns = new List<SpriteRenderer>();
        LevelPiece _exit;
        Transform _root;
        SpriteRenderer _water, _waterTop;
        readonly List<(BellDrop b, SpriteRenderer bell, SpriteRenderer mark)> _bells = new List<(BellDrop, SpriteRenderer, SpriteRenderer)>();
        int _doorCount;

        public int ObjectCount => _root == null ? 0 : _root.childCount;
        public Transform Root => _root;

        public LevelPrefabCatalog Catalog => catalog != null ? catalog : (catalog = LevelPrefabCatalog.Load());

        public void Build(PlayerWorld world, PlayerSim sim)
        {
            _world = world;
            _sim = sim;
            if (_root != null) { if (Application.isPlaying) Destroy(_root.gameObject); else DestroyImmediate(_root.gameObject); }
            _root = new GameObject("Level Objects").transform; // an empty folder; every drawn object below is a prefab instance
            _root.SetParent(transform, false);
            _ledges.Clear(); _hazards.Clear(); _items.Clear(); _enemies.Clear(); _lanterns.Clear(); _bells.Clear();
            _exit = null; _water = _waterTop = null;

            foreach (var l in world.Ledges) AddLedge(l);
            foreach (var h in world.Hazards) AddHazard(h);
            foreach (var d in world.Doors) AddDoor(d);
            foreach (var it in world.Items) AddItem(it);
            foreach (var e in world.Enemies) AddEnemy(e);
            for (int i = 0; i < world.Lanterns.Count; i++) AddLantern(world.Lanterns[i]);
            _doorCount = world.Doors.Count;
            foreach (var b in world.Bells) AddBell(b);
            if (world.Flood != null) AddWater();
            AddDecor();
        }

        // ------------------------------------------------------------ builders

        LevelPiece Spawn(LevelPiece prefab, Vector2 pos, Dim mask, Dim ghostTint = Dim.Ordinary)
        {
            var p = LevelPiece.Spawn(prefab, _root, new Vector3(pos.x, pos.y, 0));
            if (p.dimension != null) { p.dimension.mask = mask; p.dimension.ghostTint = ghostTint; }
            return p;
        }

        /// <summary>The black water: a dark body under a rippling surface strip (prototype drawWorld).</summary>
        void AddWater()
        {
            var c = Catalog;
            _water = Spawn(c.waterBody, Vector2.zero, Dim.All).main;
            if (c.waterSurface != null && c.waterSurface.main.sprite != null)
            {
                _waterTop = Spawn(c.waterSurface, Vector2.zero, Dim.All).main;
                _waterTop.size = new Vector2(_world.Bounds.width, _waterTop.sprite.bounds.size.y);
            }
        }

        void AddBell(BellDrop b)
        {
            var c = Catalog;
            var bell = Spawn(c.fallingBell, new Vector2(b.X, b.TopY), b.Mask).main;
            var mark = Spawn(c.bellMark, new Vector2(b.X, (b.TopY + b.GroundY) * 0.5f), b.Mask).main;
            mark.transform.localScale = new Vector3(2f, (b.TopY - b.GroundY) * PxPerUnit, 1);
            _bells.Add((b, bell, mark));
        }

        /// <summary>Act II hanging balete roots under room floors, Act III candles on them (decoration only, fixed spots).</summary>
        void AddDecor()
        {
            var prefab = Catalog.decorByAct[Mathf.Clamp(act, 1, 3) - 1];
            if (prefab == null || prefab.main == null || prefab.main.sprite == null) return;
            var s = prefab.main.sprite;
            int n = 0;
            foreach (var l in _world.Ledges)
            {
                if (l.Kind != LedgeKind.Floor || l.Rect.height > 1.5f) continue;
                foreach (float fx in new[] { 3.5f, 28.5f })
                {
                    float x = l.Rect.x + fx + (n++ % 3);
                    var pos = act == 2 ? new Vector2(x, l.Rect.yMin - s.bounds.size.y * 0.5f) : new Vector2(x, l.Rect.yMax + s.bounds.size.y * 0.5f);
                    Spawn(prefab, pos, Dim.All);
                }
            }
        }

        void AddLedge(Ledge l)
        {
            var r = l.Rect;
            bool ground = l.Kind == LedgeKind.Floor && r.height > 1.5f;
            // Ledge art is one tile tall: aligned to the top surface (to the underside for Inverted-only ledges).
            float h = ground ? r.height : 1f;
            bool hangs = l.Mask == Dim.Inverted;
            float cy = ground ? r.center.y : hangs ? r.yMin + h * 0.5f : r.yMax - h * 0.5f;
            var sr = Spawn(Catalog.Ledge(l.Kind, act, ground), new Vector2(r.center.x, cy), l.Mask).main;
            sr.size = new Vector2(r.width, h);
            sr.flipY = hangs;
            _ledges.Add((l, sr.transform, sr, sr.transform.position.x - r.x));
        }

        void AddHazard(Hazard h)
        {
            var c = Catalog;
            var r = h.Rect;
            var prefab = h.Kind switch
            {
                HazardKind.Thorns => c.thorns,
                HazardKind.Curtain => c.curtain,
                HazardKind.Root => c.root,
                _ => h.Gauntlet ? c.spikeGauntlet : c.spike,
            };
            var p = Spawn(prefab, r.center, h.Mask);
            if (h.Kind == HazardKind.Spike || h.Kind == HazardKind.Curtain)
            {
                p.main.size = r.size;
                p.main.flipY = h.Hanging;
            }
            _hazards.Add((h, p));
        }

        void AddDoor(Door d)
        {
            var c = Catalog;
            var prefab = d.Kind == DoorKind.Exit ? c.exit
                       : d.Kind == DoorKind.Home ? c.doorHome
                       : d.Target == Dim.Spirit ? c.doorSpirit : c.doorInverted;
            var p = Spawn(prefab, d.Rect.center, d.Mask, d.Kind == DoorKind.Enter ? d.Target : Dim.Ordinary);
            p.main.flipY = d.Hanging;
            // A room's Home door shares its spot and sorting order with the Enter door. Equal sprites sort by depth, so the Home
            // door sits a hair nearer the camera: its ghost always draws over the Enter door, as the prototype draws them.
            if (d.Kind == DoorKind.Home) p.transform.position += new Vector3(0, 0, -HomeDoorDepth);
            if (d.Kind == DoorKind.Exit) _exit = p;
        }

        void AddItem(Item it)
        {
            var c = Catalog;
            var prefab = it.Kind == ItemKind.Shard ? c.fragment : it.Kind == ItemKind.Clue ? c.letter : c.flame;
            var p = Spawn(prefab, it.Rect.center, it.Mask);
            _items.Add((it, p.transform, it.Rect.center, it.Rect.x * 7f));
        }

        void AddEnemy(Enemy e)
        {
            var p = Spawn(Catalog.Creature(e.Kind), new Vector2(e.Center.x, e.Position.y), e.Mask);
            _enemies.Add((e, p.transform, p));
        }

        /// <summary>The lantern prefab: a dark post and the flame on top; the flame dims until the lantern is reached.</summary>
        void AddLantern(Vector2 at) => _lanterns.Add(Spawn(Catalog.lantern, at, Dim.All).main);

        // ------------------------------------------------------------ per frame

        void LateUpdate()
        {
            if (_sim == null) return;
            long t = _sim.Tick;

            foreach (var (l, tr, sr, offset) in _ledges)
            {
                if (l.Kind == LedgeKind.Moving) tr.position = new Vector3(l.Rect.x + offset, tr.position.y, tr.position.z);
                else if (l.Kind == LedgeKind.Crumble)
                {
                    sr.enabled = !l.IsGone;
                    float shake = l.CrumbleTimer > 0 ? ((t % 2 == 0) ? 1f : -1f) / 16f : 0f;
                    tr.position = new Vector3(l.Rect.x + offset + shake, tr.position.y, tr.position.z);
                }
            }

            foreach (var (h, p) in _hazards)
            {
                if (h.Kind == HazardKind.Root)
                {
                    long c = h.BurnTicks > 0 ? 0 : (t + h.Phase) % 150;
                    p.SetState(c < 85 ? 0 : c < 110 ? 1 : 2); // dormant, warning, strike
                }
                else if (h.Kind == HazardKind.Thorns) p.SetState(h.BurnTicks > 0 ? 1 : 0);
            }

            foreach (var (it, tr, basePos, seed) in _items)
            {
                if (it.Got) { if (tr.gameObject.activeSelf) tr.gameObject.SetActive(false); continue; }
                float bob = Mathf.Round(Mathf.Sin(t * 0.07f + seed) * 2f) / 16f;
                tr.position = new Vector3(basePos.x, basePos.y + bob, tr.position.z);
            }

            foreach (var (e, tr, p) in _enemies)
            {
                if (e.Dead) { if (tr.gameObject.activeSelf) tr.gameObject.SetActive(false); continue; }
                var sr = p.main;
                if (p.StateCount > 1) p.SetState((int)((t / 15) % p.StateCount)); // 4 fps on the 60 Hz simulation clock
                bool facesLeft = e.Kind == EnemyKind.Manananggal ? _sim.Center.x < e.Center.x : e.Dir < 0;
                sr.flipX = facesLeft ^ p.artFacesLeft;
                if (e.Kind == EnemyKind.Anino && p.extra != null && p.extra.Length == 2)
                {
                    p.extra[0].transform.localPosition = new Vector3(0, 0.7f, 0);
                    p.extra[1].transform.localPosition = new Vector3(facesLeft ? -0.06f : 0.06f, 0.7f, 0);
                }
                tr.position = new Vector3(Mathf.Round(e.Center.x * 16f) / 16f, Mathf.Round(e.Position.y * 16f) / 16f, tr.position.z);
                var c = sr.color;
                c.a = e.Stun > 0 ? 0.6f + Mathf.Sin(t * 0.5f) * 0.2f : (DimensionRules.Active(e.Mask, _sim.Dimension) ? 1f : c.a);
                sr.color = c;
            }

            // doors added during play (the last door after the Bellkeeper falls)
            while (_doorCount < _world.Doors.Count) AddDoor(_world.Doors[_doorCount++]);
            if (_exit != null)
            {
                bool open = !_world.NeedClue || _sim.HasClue;
                _exit.SetState(open ? 1 : 0);
                // Detailed architecture stays readable; its illumination communicates the lock state.
                _exit.main.color = open ? Color.white : new Color(0.42f, 0.39f, 0.48f, 1f);
            }

            foreach (var (b, bell, mark) in _bells)
            {
                bool here = DimensionRules.Active(b.Mask, _sim.Dimension);
                bell.enabled = b.Falling && here;
                if (b.Falling) bell.transform.position = new Vector3(b.X, Mathf.Round((b.Y + BellDrop.Size.y * 0.5f) * 16f) / 16f, bell.transform.position.z);
                mark.enabled = here && b.Warning && ((t >> 3) & 1) == 0;
            }
            var fl = _world.Flood;
            if (fl != null && _water != null)
            {
                float bottom = _world.Bounds.yMin - 3f;
                float h = Mathf.Max(0.01f, fl.Y - bottom);
                _water.transform.position = new Vector3(_world.Bounds.center.x, bottom + h * 0.5f, 0);
                _water.transform.localScale = new Vector3(_world.Bounds.width * PxPerUnit, h * PxPerUnit, 1);
                if (_waterTop != null)
                {
                    float ripple = Mathf.Round(Mathf.Sin(t * 0.08f) * 1.5f) / 16f;
                    _waterTop.transform.position = new Vector3(_world.Bounds.center.x, fl.Y + ripple, 0);
                }
            }
            for (int i = 0; i < _lanterns.Count; i++)
            {
                var c = _lanterns[i].color;
                c.a = i <= _sim.Checkpoint ? 1f : 0.45f;
                _lanterns[i].color = c;
            }
        }
    }
}
