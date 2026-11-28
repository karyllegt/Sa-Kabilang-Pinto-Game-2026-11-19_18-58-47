using System.Collections.Generic;
using UnityEngine;

namespace Skp.Player
{
    /// <summary>
    /// Draws the player from the simulation, like the prototype's drawPlayer(): frame 0 idle,
    /// 1 and 2 run, 3 air; mirrored by facing, flipped in Inverted gravity, blinking while
    /// invulnerable, translucent while dashing, hidden while dead. Positions are interpolated
    /// between ticks and snapped to the 16 px grid.
    /// </summary>
    public class PlayerView : MonoBehaviour
    {
        public SpriteRenderer body;
        [Tooltip("spr_pl_girl_0..3")] public Sprite[] girl = new Sprite[4];
        [Tooltip("spr_pl_boy_0..3")] public Sprite[] boy = new Sprite[4];
        public bool useBoy;
        [Tooltip("R3: the girl's animation set (empty = the 4 prototype frames above).")] public PlayerAnimSet girlAnims;
        [Tooltip("R3: the boy's animation set (empty = the 4 prototype frames above).")] public PlayerAnimSet boyAnims;

        /// <summary>R3: picks the animation from the simulation, one step per tick (the controller calls <see cref="Tick"/>).</summary>
        public PlayerAnimator Animator { get; } = new PlayerAnimator();
        public PlayerAnimSet ActiveAnims => useBoy ? boyAnims : girlAnims;

        public void Bind(PlayerSim sim) => Animator.Bind(sim);
        public void Tick(PlayerSim sim) => Animator.Tick(sim);
        [Tooltip("Optional ring shown while the ward pulse is active.")] public SpriteRenderer wardRing;
        [Tooltip("Purple Lambong Veil ring (flickers in its last second).")] public SpriteRenderer veilRing;
        [Tooltip("Pakpak Glide wings, shown while gliding.")] public SpriteRenderer glideWings;
        [Tooltip("R1: the Bolt prefab (one copy per bolt in flight, pooled).")] public Skp.Levels.LevelPiece boltPrefab;
        /// <summary>Bolts made up front when a level starts (Triple Bolt fires three; more are added only if ever needed).</summary>
        public const int PrewarmBolts = 6;

        readonly List<SpriteRenderer> _bolts = new List<SpriteRenderer>();
        /// <summary>Bolt sprites currently drawn (tests).</summary>
        public int VisibleBolts { get; private set; }

        readonly SpriteRenderer[] _rim = new SpriteRenderer[4];

        public void UseLanternRim(SpriteRenderer rim)
        {
            if (body == null || rim == null || _rim[0] != null) return;
            var offsets = new[] { Vector2.left, Vector2.right, Vector2.up, Vector2.down };
            // A narrow rim follows the animated silhouette and the existing feet anchor.
            body.transform.localScale *= 1.08f;
            for (int i = 0; i < _rim.Length; i++)
            {
                var edge = Instantiate(rim, body.transform, false);
                edge.transform.localPosition = offsets[i] * 0.035f;
                edge.sharedMaterial = body.sharedMaterial;
                edge.sortingLayerID = body.sortingLayerID;
                edge.sortingOrder = body.sortingOrder - 1;
                _rim[i] = edge;
            }
        }

        public int CurrentFrame { get; private set; }

        public void Render(PlayerSim sim, Vector2 previousPosition, float alpha)
        {
            if (body == null) return;
            // R3: the death animation is drawn when there is one; without it Tala is hidden while dead, as before
            bool dead = sim.Life != PlayerLife.Playing && (ActiveAnims == null || ActiveAnims.Get(TalaAnim.Death) == null);
            bool blinkOff = sim.Invulnerable > 0 && ((sim.Tick >> 2) & 1) == 1 && sim.DashTicks == 0;
            body.enabled = !dead && !blinkOff;

            int f = 0;
            if (!sim.Grounded) f = 3;
            else if (Mathf.Abs(sim.Velocity.x) * PlayerSim.Dt * PlayerTuning.PxPerUnit > 0.4f) f = 1 + ((int)sim.Anim % 2);
            CurrentFrame = f;
            var anims = ActiveAnims;
            var animSprite = anims != null ? anims.Frame(Animator.State, Animator.StateTicks) : null;
            if (animSprite != null) body.sprite = animSprite;
            else
            {
                var set = useBoy ? boy : girl;
                if (set != null && f < set.Length && set[f] != null) body.sprite = set[f];
            }

            body.flipX = sim.Facing < 0;
            body.flipY = sim.GravitySign < 0;
            var c = body.color; c.a = sim.DashTicks > 0 ? 0.7f : 1f; body.color = c;

            if (_rim[0] != null)
            {
                var tint = sim.Dimension == Skp.Dimensions.Dim.Spirit ?
                    new Color(0.66f, 0.98f, 0.9f, c.a * 0.53f) : new Color(1f, 0.82f, 0.53f, c.a * 0.57f);
                foreach (var edge in _rim)
                {
                    edge.sprite = body.sprite;
                    edge.flipX = body.flipX;
                    edge.flipY = body.flipY;
                    edge.enabled = body.enabled;
                    edge.color = tint;
                }
            }

            // Sprite pivot is bottom-center; in Inverted gravity the feet are at the hitbox top.
            Vector2 pos = Vector2.Lerp(previousPosition, sim.Position, Mathf.Clamp01(alpha));
            float x = pos.x + sim.Size.x * 0.5f;
            float y = sim.GravitySign > 0 ? pos.y : pos.y + sim.Size.y;
            transform.position = new Vector3(Snap(x), Snap(y), transform.position.z);

            RenderSkills(sim, x, pos);

            if (wardRing != null)
            {
                float wt = sim.WardTicks / Mathf.Max(1f, PlayerTuning.Ticks(sim.Tuning.wardDuration));
                wardRing.enabled = sim.WardTicks > 0;
                if (wardRing.enabled)
                {
                    float r = (8f + (1f - wt) * 24f * 1.6f) / PlayerTuning.PxPerUnit; // prototype: 8 + (24 - wardT) * 1.6 px
                    wardRing.transform.position = new Vector3(Snap(x), Snap(pos.y + sim.Size.y * 0.5f), transform.position.z);
                    wardRing.transform.localScale = Vector3.one * (r * 2f);
                    var wc = wardRing.color; wc.a = wt; wardRing.color = wc;
                }
            }
        }

        void RenderSkills(PlayerSim sim, float x, Vector2 pos)
        {
            float cy = pos.y + sim.Size.y * 0.5f;
            if (veilRing != null)
            {
                int flickerTicks = PlayerTuning.Ticks(sim.SkillTuning != null ? sim.SkillTuning.veilFlickerTime : 1f);
                bool flickerOff = sim.VeilTicks < flickerTicks && ((sim.Tick >> 2) & 1) == 1;
                veilRing.enabled = sim.Life == PlayerLife.Playing && sim.VeilTicks > 0 && !flickerOff;
                if (veilRing.enabled)
                {
                    veilRing.transform.position = new Vector3(Snap(x), Snap(cy), transform.position.z - 0.01f);
                    veilRing.transform.localScale = Vector3.one * (1f + Mathf.Sin(sim.Tick * 0.2f) * 1.5f / 13f); // prototype: radius 13 + sin(t * 0.2) * 1.5 px
                }
            }
            if (glideWings != null)
            {
                glideWings.enabled = sim.Gliding && sim.Life == PlayerLife.Playing;
                if (glideWings.enabled)
                {
                    glideWings.flipY = sim.GravitySign < 0;
                    float wy = sim.GravitySign > 0 ? pos.y + sim.Size.y - 3f / 16f : pos.y + 3f / 16f;
                    glideWings.transform.position = new Vector3(Snap(x), Snap(wy), transform.position.z + 0.01f);
                }
            }
            int n = 0;
            if (boltPrefab != null)
            {
                foreach (var b in sim.Bolts)
                {
                    if (n >= _bolts.Count) AddBolt();
                    var r = _bolts[n++];
                    r.enabled = true;
                    r.flipX = b.Velocity.x < 0;
                    r.transform.position = new Vector3(Snap(b.Position.x), Snap(b.Position.y), transform.position.z);
                }
            }
            for (int i = n; i < _bolts.Count; i++) _bolts[i].enabled = false;
            VisibleBolts = n;
        }

        /// <summary>Makes the pooled bolts before play, so firing never creates objects (the level runner calls it).</summary>
        public void PrewarmBoltPool()
        {
            while (boltPrefab != null && _bolts.Count < PrewarmBolts) AddBolt().enabled = false;
        }

        SpriteRenderer AddBolt()
        {
            var sr = Object.Instantiate(boltPrefab, transform.parent, false).main;
            sr.sortingOrder = (body != null ? body.sortingOrder : 10) + 1;
            _bolts.Add(sr);
            return sr;
        }

        static float Snap(float v) => Mathf.Round(v * PlayerTuning.PxPerUnit) / PlayerTuning.PxPerUnit;
    }
}
