using System;
using Skp.Player;
using UnityEngine;

namespace Skp.Dimensions
{
    /// <summary>
    /// Scene-level presentation of the current dimension: restyles every <see cref="DimensionMember"/>,
    /// sets the full-screen tint behind the world, and raises <see cref="Changed"/> so audio (M8: Spirit
    /// low-pass, Inverted pitch-down) and the HUD label can follow.
    /// </summary>
    public class DimensionDirector : MonoBehaviour
    {
        public PlayerController player;
        [Tooltip("Full-screen sprite drawn just above the backdrop and below the world.")]
        public SpriteRenderer screenTint;

        /// <summary>Raised whenever the dimension changes (including back to Ordinary on respawn).</summary>
        public static event Action<Dim> Changed;

        public Dim Current { get; private set; } = Dim.None;

        DimensionMember[] _members = new DimensionMember[0];

        void Start()
        {
            _members = FindObjectsByType<DimensionMember>(FindObjectsInactive.Include);
            if (screenTint != null && screenTint.sprite == null) screenTint.sprite = WhiteSprite();
        }

        /// <summary>Re-scan the scene (after the level generator adds objects).</summary>
        public void Refresh()
        {
            _members = FindObjectsByType<DimensionMember>(FindObjectsInactive.Include);
            Apply(Current == Dim.None ? Dim.Ordinary : Current, force: true);
        }

        void LateUpdate()
        {
            if (player == null || player.Sim == null) return;
            Apply(player.Sim.Dimension, force: false);
        }

        public void Apply(Dim d, bool force)
        {
            if (!force && d == Current) return;
            Current = d;
            foreach (var m in _members) if (m != null) m.Apply(d);
            if (screenTint != null) screenTint.color = DimensionRules.ScreenTint(d);
            Changed?.Invoke(d);
        }

        static Sprite _white;
        public static Sprite WhiteSprite()
        {
            if (_white != null) return _white;
            var t = new Texture2D(1, 1) { filterMode = FilterMode.Point };
            t.SetPixel(0, 0, Color.white);
            t.Apply();
            _white = Sprite.Create(t, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            return _white;
        }
    }
}
