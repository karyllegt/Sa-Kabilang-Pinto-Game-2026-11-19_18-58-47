using System;
using Skp.Dimensions;
using Skp.Player;
using UnityEngine;

namespace Skp.Preview
{
    /// <summary>Cosmetic dash trails and destination flares driven by existing simulation events.</summary>
    public sealed class SmoothActionEffects
    {
        readonly Transform _parent;
        readonly SpriteRenderer _body;
        readonly Sprite _dash, _waking, _spirit, _inverted;
        readonly Func<bool> _reducedFlash;
        SpriteRenderer _dashTrail, _transition;
        long _transitionTick;
        Vector2 _transitionOrigin;
        const int TransitionTicks = 14;

        public SmoothActionEffects(Transform parent, SpriteRenderer body, Sprite dash, Sprite waking,
            Sprite spirit, Sprite inverted, Func<bool> reducedFlash)
        {
            _parent = parent;
            _body = body;
            _dash = dash;
            _waking = waking;
            _spirit = spirit;
            _inverted = inverted;
            _reducedFlash = reducedFlash;
        }

        public void Reset()
        {
            if (_dashTrail != null) _dashTrail.enabled = false;
            if (_transition != null) _transition.enabled = false;
        }

        public void ShowTransition(PlayerSim sim, Dim destination)
        {
            if (_reducedFlash()) return;
            if (_transition == null) _transition = CreateRenderer("Smooth dimension transition");
            _transition.sprite = destination == Dim.Spirit ? _spirit :
                destination == Dim.Inverted ? _inverted : _waking;
            _transitionTick = sim.Tick;
            _transitionOrigin = sim.Center;
            _transition.enabled = true;
        }

        public void Draw(PlayerSim sim)
        {
            if (sim.DashTicks > 0 && sim.Life == PlayerLife.Playing)
            {
                if (_dashTrail == null)
                {
                    _dashTrail = CreateRenderer("Smooth dash trail");
                    _dashTrail.sprite = _dash;
                }
                int direction = sim.Velocity.x < 0f ? -1 : 1;
                float ratio = Mathf.Clamp01(sim.DashTicks / (float)PlayerTuning.Ticks(sim.Tuning.dashDuration));
                _dashTrail.flipX = direction < 0;
                _dashTrail.transform.position = sim.Center - new Vector2(direction * 0.85f, 0f);
                _dashTrail.color = new Color(1f, 1f, 1f, ratio * (_reducedFlash() ? 0.25f : 0.65f));
                _dashTrail.enabled = true;
            }
            else if (_dashTrail != null) _dashTrail.enabled = false;

            if (_transition == null || !_transition.enabled) return;
            float progress = (sim.Tick - _transitionTick) / (float)TransitionTicks;
            _transition.enabled = progress >= 0f && progress < 1f && sim.Life == PlayerLife.Playing && !_reducedFlash();
            _transition.transform.position = _transitionOrigin;
            _transition.transform.localScale = Vector3.one * Mathf.Lerp(0.82f, 1.4f, progress);
            _transition.color = new Color(1f, 1f, 1f, (1f - progress) * 0.72f);
        }

        SpriteRenderer CreateRenderer(string objectName)
        {
            var renderer = new GameObject(objectName).AddComponent<SpriteRenderer>();
            renderer.transform.SetParent(_parent, false);
            renderer.sharedMaterial = _body.sharedMaterial;
            renderer.sortingLayerID = _body.sortingLayerID;
            renderer.sortingOrder = _body.sortingOrder - 1;
            return renderer;
        }
    }
}
