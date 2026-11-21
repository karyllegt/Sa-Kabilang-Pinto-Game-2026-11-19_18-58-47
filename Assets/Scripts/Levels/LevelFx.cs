using Skp.Core;
using UnityEngine;

namespace Skp.Levels
{
    /// <summary>
    /// Pixel particle bursts for flame stars, fragments, the veil, bolt impacts, burnt thorns and roots, and the
    /// letter. One fixed-size ParticleSystem per kind, built once; <see cref="Emit"/> uses EmitParams, so a burst
    /// allocates nothing during play.
    /// </summary>
    public class LevelFx : MonoBehaviour
    {
        public enum Kind { Flame, Fragment, Veil, Bolt, Burnt, Ember, Letter }
        const int Kinds = 7;

        FeelTuning _t;
        readonly ParticleSystem[] _ps = new ParticleSystem[Kinds];
        readonly FxBurst[] _burst = new FxBurst[Kinds];

        /// <summary>Particles emitted per kind since the level began (tests).</summary>
        public int[] Emitted { get; } = new int[Kinds];

        public static LevelFx Create(Transform parent, FeelTuning t)
        {
            var go = new GameObject("Level Fx");
            go.transform.SetParent(parent, false);
            var fx = go.AddComponent<LevelFx>();
            fx.Build(t);
            return fx;
        }

        void Build(FeelTuning t)
        {
            _t = t != null ? t : ScriptableObject.CreateInstance<FeelTuning>();
            _burst[(int)Kind.Flame] = _t.flame;
            _burst[(int)Kind.Fragment] = _t.fragment;
            _burst[(int)Kind.Veil] = _t.veil;
            _burst[(int)Kind.Bolt] = _t.bolt;
            _burst[(int)Kind.Burnt] = _t.burnt;
            _burst[(int)Kind.Ember] = _t.ember;
            _burst[(int)Kind.Letter] = _t.letterGlow;
            for (int i = 0; i < Kinds; i++) _ps[i] = MakeSystem(((Kind)i).ToString(), _burst[i]);
        }

        ParticleSystem MakeSystem(string name, FxBurst b)
        {
            var go = new GameObject("Fx " + name);
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 1f;
            main.maxParticles = Mathf.Max(8, _t.maxParticles);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = Mathf.Max(0.05f, b.life);
            main.startSpeed = 0f;
            main.startColor = b.color;
            float px = b.sprite != null ? b.sprite.rect.width / Mathf.Max(1f, b.sprite.pixelsPerUnit) : 2f / 16f;
            main.startSize = px;
            main.gravityModifier = -b.rise / 9.81f;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            var em = ps.emission; em.enabled = false;
            var shape = ps.shape; shape.enabled = false;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.55f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var limit = ps.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.dampen = 0.08f;
            limit.limit = b.speed;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.sortingOrder = 30;
            if (_t.particleMaterial != null) r.sharedMaterial = _t.particleMaterial;
            if (b.sprite != null)
            {
                var sheet = ps.textureSheetAnimation;
                sheet.enabled = true;
                sheet.mode = ParticleSystemAnimationMode.Sprites;
                sheet.SetSprite(0, b.sprite);
            }
            ps.Play();
            return ps;
        }

        /// <summary>A burst of the tuned count at a point (world units).</summary>
        public void Emit(Kind k, Vector2 at) => Emit(k, at, _burst[(int)k].count);

        public void Emit(Kind k, Vector2 at, int count)
        {
            var ps = _ps[(int)k];
            if (ps == null || count <= 0) return;
            var b = _burst[(int)k];
            var ep = new ParticleSystem.EmitParams { applyShapeToPosition = false };
            for (int i = 0; i < count; i++)
            {
                float a = Random.value * Mathf.PI * 2f;
                float s = b.speed * (0.35f + 0.65f * Random.value);
                ep.position = new Vector3(at.x, at.y, 0f);
                ep.velocity = new Vector3(Mathf.Cos(a) * s, Mathf.Sin(a) * s, 0f);
                ep.startLifetime = b.life * (0.6f + 0.4f * Random.value);
                ps.Emit(ep, 1);
            }
            Emitted[(int)k] += count;
        }

        /// <summary>A ring of particles (the veil coming up).</summary>
        public void Ring(Kind k, Vector2 at, float radius, int count)
        {
            var ps = _ps[(int)k];
            if (ps == null || count <= 0) return;
            var b = _burst[(int)k];
            var ep = new ParticleSystem.EmitParams { applyShapeToPosition = false };
            for (int i = 0; i < count; i++)
            {
                float a = i * Mathf.PI * 2f / count;
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                ep.position = new Vector3(at.x + dir.x * radius, at.y + dir.y * radius, 0f);
                ep.velocity = new Vector3(dir.x * b.speed * 0.4f, dir.y * b.speed * 0.4f, 0f);
                ep.startLifetime = b.life;
                ps.Emit(ep, 1);
            }
            Emitted[(int)k] += count;
        }

        public int Alive(Kind k) => _ps[(int)k] != null ? _ps[(int)k].particleCount : 0;

        public void Clear()
        {
            foreach (var ps in _ps) if (ps != null) ps.Clear(true);
        }
    }
}
