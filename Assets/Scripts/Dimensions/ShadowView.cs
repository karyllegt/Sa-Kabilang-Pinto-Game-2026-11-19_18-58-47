using Skp.Player;
using UnityEngine;

namespace Skp.Dimensions
{
    /// <summary>
    /// Draws the Anino shadow like the prototype: spr_anino frames alternating every 8 ticks at
    /// 65 percent opacity, mirrored by the recorded facing; a faint teal ghost while stunned;
    /// nothing outside the Spirit dimension or before the trail is long enough.
    /// </summary>
    public class ShadowView : MonoBehaviour
    {
        public PlayerController player;
        public SpriteRenderer body;
        public Sprite[] frames = new Sprite[2];

        void LateUpdate()
        {
            var sim = player != null ? player.Sim : null;
            bool show = sim != null && sim.Dimension == Dim.Spirit && sim.Shadow.Active && sim.Life == PlayerLife.Playing;
            body.enabled = show;
            if (!show) return;

            var s = sim.Shadow.Current;
            bool stunned = sim.Shadow.StunTicks > 0;
            if (frames != null && frames.Length > 0 && frames[0] != null)
                body.sprite = frames[(int)((sim.Tick >> 3) & 1) % frames.Length];
            body.flipX = s.Facing < 0;
            body.color = stunned ? new Color(0.25f, 0.88f, 0.82f, 0.25f) : new Color(1f, 1f, 1f, 0.65f);
            float x = s.Position.x + sim.Size.x * 0.5f;
            transform.position = new Vector3(Mathf.Round(x * 16f) / 16f, Mathf.Round(s.Position.y * 16f) / 16f, transform.position.z);
        }
    }
}
