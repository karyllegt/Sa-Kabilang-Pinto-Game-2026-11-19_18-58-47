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
        public Material silhouetteMaterial;
        public Sprite eyeSprite;
        SpriteRenderer _leftEye, _rightEye;
        Vector2 _previous;

        void Awake()
        {
            if (silhouetteMaterial == null || eyeSprite == null) return;
            body.sharedMaterial = silhouetteMaterial;
            _leftEye = Eye("Left eye"); _rightEye = Eye("Right eye");
        }

        SpriteRenderer Eye(string name)
        {
            var eye = new GameObject(name).AddComponent<SpriteRenderer>();
            eye.transform.SetParent(transform, false);
            eye.sprite = eyeSprite; eye.sortingOrder = body.sortingOrder + 1;
            eye.transform.localScale = new Vector3(0.72f, 0.56f, 1);
            eye.color = new Color(0.85f, 0.95f, 1, 1); eye.enabled = false;
            return eye;
        }

        void LateUpdate()
        {
            var sim = player != null ? player.Sim : null;
            bool show = sim != null && sim.Dimension == Dim.Spirit && sim.Shadow.Active && sim.Life == PlayerLife.Playing;
            body.enabled = show;
            if (_leftEye != null) { _leftEye.enabled = show; _rightEye.enabled = show; }
            if (!show) return;

            var s = sim.Shadow.Current;
            bool stunned = sim.Shadow.StunTicks > 0;
            if (silhouetteMaterial != null && player.view != null && player.view.ActiveAnims != null)
            {
                bool moving = (s.Position - _previous).sqrMagnitude > 0.0001f;
                body.sprite = player.view.ActiveAnims.Frame(moving ? TalaAnim.Run : TalaAnim.Idle, (int)(sim.Tick % int.MaxValue));
                _previous = s.Position;
            }
            else if (frames != null && frames.Length > 0 && frames[0] != null)
                body.sprite = frames[(int)((sim.Tick >> 3) & 1) % frames.Length];
            body.flipX = s.Facing < 0;
            body.color = stunned ? new Color(0.25f, 0.88f, 0.82f, 0.25f) : silhouetteMaterial != null ? new Color(0.04f, 0.025f, 0.07f, 0.9f) : new Color(1f, 1f, 1f, 0.65f);
            if (_leftEye != null)
            {
                _leftEye.transform.localPosition = new Vector3(0, 0.7f, 0);
                _rightEye.transform.localPosition = new Vector3(s.Facing * 0.06f, 0.7f, 0);
            }
            float x = s.Position.x + sim.Size.x * 0.5f;
            transform.position = new Vector3(Mathf.Round(x * 16f) / 16f, Mathf.Round(s.Position.y * 16f) / 16f, transform.position.z);
        }
    }
}
