using System.Collections.Generic;
using Skp.Dimensions;
using Skp.Player;
using UnityEngine;

namespace Skp.Levels
{
    /// <summary>
    /// Draws a generated level from its <see cref="PlayerWorld"/>: one sprite object per ledge, hazard,
    /// door, item, creature and lantern, built once; LateUpdate only moves or toggles them (no per-frame
    /// allocation). Every object gets a <see cref="DimensionMember"/> so the dimension styling applies.
    /// </summary>
    public class LevelView : MonoBehaviour
    {
        public LevelArt art;

        PlayerWorld _world;
        PlayerSim _sim;
        readonly List<(Ledge ledge, Transform t, SpriteRenderer sr, float baseX)> _ledges = new List<(Ledge, Transform, SpriteRenderer, float)>();
        readonly List<(Hazard h, SpriteRenderer sr)> _hazards = new List<(Hazard, SpriteRenderer)>();
        readonly List<(Item it, Transform t, SpriteRenderer sr, Vector2 basePos, float seed)> _items = new List<(Item, Transform, SpriteRenderer, Vector2, float)>();
        readonly List<(Enemy e, Transform t, SpriteRenderer sr)> _enemies = new List<(Enemy, Transform, SpriteRenderer)>();
        readonly List<SpriteRenderer> _lanterns = new List<SpriteRenderer>();
        SpriteRenderer _exit;
        Transform _root;
        static Sprite _white;

        public int ObjectCount => _root == null ? 0 : _root.childCount;

        public void Build(PlayerWorld world, PlayerSim sim)
        {
            _world = world;
            _sim = sim;
            if (_root != null) { if (Application.isPlaying) Destroy(_root.gameObject); else DestroyImmediate(_root.gameObject); }
            _root = new GameObject("Level Objects").transform;
            _root.SetParent(transform, false);
            _ledges.Clear(); _hazards.Clear(); _items.Clear(); _enemies.Clear(); _lanterns.Clear();

            foreach (var l in world.Ledges) AddLedge(l);
            foreach (var h in world.Hazards) AddHazard(h);
            foreach (var d in world.Doors) AddDoor(d);
            foreach (var it in world.Items) AddItem(it);
            foreach (var e in world.Enemies) AddEnemy(e);
            for (int i = 0; i < world.Lanterns.Count; i++) AddLantern(world.Lanterns[i]);
        }

        // ------------------------------------------------------------ builders

        SpriteRenderer NewSprite(string name, Sprite s, Vector2 pos, int order, Dim mask, DimensionMember.Ghost ghost, Dim ghostTint = Dim.Ordinary)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root, false);
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.sortingOrder = order;
            var dm = go.AddComponent<DimensionMember>();
            dm.mask = mask; dm.ghost = ghost; dm.ghostTint = ghostTint; dm.renderers = new[] { sr };
            return sr;
        }

        void AddLedge(Ledge l)
        {
            var r = l.Rect;
            bool ground = l.Kind == LedgeKind.Floor && r.height > 1.5f;
            Sprite s = ground ? art.floorBlock : l.Kind switch
            {
                LedgeKind.Spirit => art.spirit,
                LedgeKind.Inverted => art.inverted,
                LedgeKind.Crumble => art.crumble,
                LedgeKind.Moving => art.moving,
                _ => art.wood,
            };
            // Ledge art is one tile tall: aligned to the top surface (to the underside for Inverted-only ledges).
            float h = ground ? r.height : 1f;
            bool hangs = l.Mask == Dim.Inverted;
            float cy = ground ? r.center.y : hangs ? r.yMin + h * 0.5f : r.yMax - h * 0.5f;
            var sr = NewSprite("ledge", s, new Vector2(r.center.x, cy), ground ? -1 : 0, l.Mask, DimensionMember.Ghost.Ledge);
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;
            sr.size = new Vector2(r.width, h);
            sr.flipY = hangs;
            _ledges.Add((l, sr.transform, sr, sr.transform.position.x - r.x));
        }

        void AddHazard(Hazard h)
        {
            var r = h.Rect;
            Sprite s = h.Kind switch
            {
                HazardKind.Thorns => art.thorns,
                HazardKind.Root => art.rootDormant,
                _ => h.Gauntlet ? art.spikeGauntlet : art.spike,
            };
            var sr = NewSprite(h.Kind.ToString(), s, r.center, 3, h.Mask, DimensionMember.Ghost.Hazard);
            if (h.Kind == HazardKind.Spike)
            {
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.tileMode = SpriteTileMode.Continuous;
                sr.size = r.size;
                sr.flipY = h.Hanging;
            }
            _hazards.Add((h, sr));
        }

        void AddDoor(Door d)
        {
            Sprite s = d.Kind == DoorKind.Exit ? art.exitLocked
                     : d.Kind == DoorKind.Home ? art.doorHome
                     : d.Target == Dim.Spirit ? art.doorSpirit : art.doorInvert;
            var sr = NewSprite(d.Kind + " door", s, d.Rect.center, 1, d.Mask,
                d.Kind == DoorKind.Exit ? DimensionMember.Ghost.AlwaysVisible : DimensionMember.Ghost.Door,
                d.Kind == DoorKind.Enter ? d.Target : Dim.Ordinary);
            sr.flipY = d.Hanging;
            if (d.Kind == DoorKind.Exit) _exit = sr;
        }

        void AddItem(Item it)
        {
            Sprite s = it.Kind == ItemKind.Shard ? art.shard : it.Kind == ItemKind.Clue ? art.clue : art.flame;
            var sr = NewSprite(it.Kind.ToString(), s, it.Rect.center, 4, it.Mask, DimensionMember.Ghost.Hidden);
            _items.Add((it, sr.transform, sr, it.Rect.center, it.Rect.x * 7f));
        }

        void AddEnemy(Enemy e)
        {
            var frames = Frames(e.Kind);
            var sr = NewSprite(e.Kind.ToString(), frames != null && frames.Length > 0 ? frames[0] : null,
                               new Vector2(e.Center.x, e.Position.y), 6, e.Mask, DimensionMember.Ghost.Hidden);
            _enemies.Add((e, sr.transform, sr));
        }

        void AddLantern(Vector2 at)
        {
            // Post (dark wood, 2 x 34 px) and flame, as the prototype draws lanterns until dedicated art exists.
            var post = NewSprite("lantern post", White(), at + new Vector2(0, 17f / 16f), 2, Dim.All, DimensionMember.Ghost.AlwaysVisible);
            post.color = new Color32(0x3a, 0x24, 0x10, 0xff);
            post.transform.localScale = new Vector3(2f / 16f, 34f / 16f, 1);
            var flame = NewSprite("lantern flame", art.flame, at + new Vector2(0, 2.1f), 5, Dim.All, DimensionMember.Ghost.AlwaysVisible);
            flame.transform.localScale = Vector3.one * 2f;
            _lanterns.Add(flame);
        }

        Sprite[] Frames(EnemyKind k) => k == EnemyKind.Tikbalang ? art.tikbalang : k == EnemyKind.Manananggal ? art.manananggal : art.anino;

        static Sprite White()
        {
            if (_white != null) return _white;
            var t = new Texture2D(1, 1) { filterMode = FilterMode.Point };
            t.SetPixel(0, 0, Color.white);
            t.Apply();
            _white = Sprite.Create(t, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            return _white;
        }

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

            foreach (var (h, sr) in _hazards)
            {
                if (h.Kind == HazardKind.Root)
                {
                    long c = h.BurnTicks > 0 ? 0 : (t + h.Phase) % 150;
                    sr.sprite = c < 85 ? art.rootDormant : c < 110 ? art.rootWarning : art.rootStrike;
                }
                else if (h.Kind == HazardKind.Thorns) sr.sprite = h.BurnTicks > 0 ? art.thornsBurnt : art.thorns;
            }

            foreach (var (it, tr, sr, basePos, seed) in _items)
            {
                if (it.Got) { if (tr.gameObject.activeSelf) tr.gameObject.SetActive(false); continue; }
                float bob = Mathf.Round(Mathf.Sin(t * 0.07f + seed) * 2f) / 16f;
                tr.position = new Vector3(basePos.x, basePos.y + bob, tr.position.z);
            }

            foreach (var (e, tr, sr) in _enemies)
            {
                if (e.Dead) { if (tr.gameObject.activeSelf) tr.gameObject.SetActive(false); continue; }
                var frames = Frames(e.Kind);
                if (frames != null && frames.Length > 1 && frames[0] != null) sr.sprite = frames[(int)((t >> 4) & 1)];
                sr.flipX = e.Dir < 0;
                tr.position = new Vector3(Mathf.Round(e.Center.x * 16f) / 16f, Mathf.Round(e.Position.y * 16f) / 16f, tr.position.z);
                var c = sr.color;
                c.a = e.Stun > 0 ? 0.6f + Mathf.Sin(t * 0.5f) * 0.2f : (DimensionRules.Active(e.Mask, _sim.Dimension) ? 1f : c.a);
                sr.color = c;
            }

            if (_exit != null) _exit.sprite = !_world.NeedClue || _sim.HasClue ? art.exitOpen : art.exitLocked;
            for (int i = 0; i < _lanterns.Count; i++)
            {
                var c = _lanterns[i].color;
                c.a = i <= _sim.Checkpoint ? 1f : 0.45f;
                _lanterns[i].color = c;
            }
        }
    }
}
