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
        [Tooltip("Optional ring shown while the ward pulse is active.")] public SpriteRenderer wardRing;

        public int CurrentFrame { get; private set; }

        public void Render(PlayerSim sim, Vector2 previousPosition, float alpha)
        {
            if (body == null) return;
            bool dead = sim.Life != PlayerLife.Playing;
            bool blinkOff = sim.Invulnerable > 0 && ((sim.Tick >> 2) & 1) == 1 && sim.DashTicks == 0;
            body.enabled = !dead && !blinkOff;

            int f = 0;
            if (!sim.Grounded) f = 3;
            else if (Mathf.Abs(sim.Velocity.x) * PlayerSim.Dt * PlayerTuning.PxPerUnit > 0.4f) f = 1 + ((int)sim.Anim % 2);
            CurrentFrame = f;
            var set = useBoy ? boy : girl;
            if (set != null && f < set.Length && set[f] != null) body.sprite = set[f];

            body.flipX = sim.Facing < 0;
            body.flipY = sim.GravitySign < 0;
            var c = body.color; c.a = sim.DashTicks > 0 ? 0.7f : 1f; body.color = c;

            // Sprite pivot is bottom-center; in Inverted gravity the feet are at the hitbox top.
            Vector2 pos = Vector2.Lerp(previousPosition, sim.Position, Mathf.Clamp01(alpha));
            float x = pos.x + sim.Size.x * 0.5f;
            float y = sim.GravitySign > 0 ? pos.y : pos.y + sim.Size.y;
            transform.position = new Vector3(Snap(x), Snap(y), transform.position.z);

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

        static float Snap(float v) => Mathf.Round(v * PlayerTuning.PxPerUnit) / PlayerTuning.PxPerUnit;
    }
}
