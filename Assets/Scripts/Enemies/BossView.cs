using System.Collections.Generic;
using Skp.Levels;
using UnityEngine;

namespace Skp.Enemies
{
    /// <summary>
    /// Draws the Bellkeeper like the prototype's drawBoss(): the body (stunned and tolling states), the closing golden
    /// toll ring, shockwaves along the arena floor, red warning lines and the falling bells. Every piece is a prefab from the
    /// <see cref="LevelPrefabCatalog"/> (R1); shockwaves, marks and bells are pooled and pre-warmed when bound, so nothing
    /// is created or allocated per frame.
    /// </summary>
    public class BossView : MonoBehaviour
    {
        const float Px = 1f / 16f;
        const int RingPoints = 48;
        /// <summary>Pool sizes made up front (more are added only if a fight ever needs them).</summary>
        const int PrewarmWaves = 4, PrewarmMarks = 6, PrewarmBells = 6;

        [System.NonSerialized] public Bellkeeper boss;
        LevelPrefabCatalog _catalog;
        LevelPiece _body;
        LineRenderer _ring;
        readonly List<SpriteRenderer> _waves = new List<SpriteRenderer>(), _marks = new List<SpriteRenderer>(), _bells = new List<SpriteRenderer>();
        long _t;

        public SpriteRenderer Body => _body != null ? _body.main : null;
        public bool RingVisible => _ring != null && _ring.enabled;

        public void Bind(Bellkeeper b, LevelPrefabCatalog catalog)
        {
            boss = b;
            _catalog = catalog != null ? catalog : LevelPrefabCatalog.Load();
            _body = LevelPiece.Spawn(_catalog.bossBody, transform, transform.position);
            _ring = LevelPiece.Spawn(_catalog.tollRing, transform, transform.position).line;
            _ring.positionCount = RingPoints;
            _ring.enabled = false;
            for (int i = 0; i < PrewarmWaves; i++) Pooled(_waves, i, _catalog.shockwave).enabled = false;
            for (int i = 0; i < PrewarmMarks; i++) Pooled(_marks, i, _catalog.bossMark).enabled = false;
            for (int i = 0; i < PrewarmBells; i++) Pooled(_bells, i, _catalog.bossBell).enabled = false;
            Render();
        }

        SpriteRenderer Pooled(List<SpriteRenderer> pool, int i, LevelPiece prefab)
        {
            while (pool.Count <= i) pool.Add(LevelPiece.Spawn(prefab, transform, transform.position).main);
            var r = pool[i];
            r.enabled = true;
            return r;
        }

        static float Snap(float v) => Mathf.Round(v * 16f) / 16f;

        void LateUpdate() => Render();

        public void Render()
        {
            if (boss == null || _body == null) return;
            _t++;
            bool show = boss.Active && !boss.Gone;
            var body = _body.main;
            body.enabled = show;
            if (show)
            {
                _body.SetState(boss.State == BossState.Stun ? 1 : boss.State == BossState.Toll ? 2 : 0); // normal, stunned, tolling
                float h = body.sprite != null ? body.sprite.bounds.size.y : Bellkeeper.BodySize.y;
                float jitter = boss.Dead ? ((boss.DeadTicks % 4) < 2 ? Px : -Px) : 0;
                body.transform.position = new Vector3(Snap(boss.X) + jitter, Snap(boss.TopY - h), 0);
                var c = Color.white;
                if (boss.Dead) c.a = Mathf.Max(0, 1f - boss.DeadTicks / 180f);
                else if (boss.FlashTicks > 0 && boss.FlashTicks % 2 == 1) c.a = 0.5f;
                body.color = c;
            }

            _ring.enabled = show && boss.State == BossState.Toll;
            if (_ring.enabled)
            {
                float k = Mathf.Min(1f, boss.TollTicks / (float)Bellkeeper.TollTicksToClose);
                Color col = k > 0.8f ? Color.white : (Color)new Color32(0xff, 0xd3, 0x6a, 0xff);
                _ring.startColor = _ring.endColor = col;
                float r = boss.RingRadius;
                var o = boss.Core;
                for (int i = 0; i < RingPoints; i++)
                {
                    float a = i * Mathf.PI * 2f / RingPoints;
                    _ring.SetPosition(i, new Vector3(o.x + Mathf.Cos(a) * r, o.y + Mathf.Sin(a) * r, 0));
                }
            }

            int n = 0;
            for (int i = 0; i < boss.Waves.Count; i++)
            {
                var w = Pooled(_waves, n++, _catalog.shockwave);
                w.transform.position = new Vector3(Snap(boss.Waves[i]), boss.FloorY + 6 * Px, 0);
                w.flipX = boss.WaveDirs[i] < 0;
            }
            for (int i = n; i < _waves.Count; i++) _waves[i].enabled = false;

            n = 0;
            float pulse = 0.4f + 0.4f * Mathf.Sin(_t * 0.4f);
            foreach (var m in boss.Marks)
            {
                var r = Pooled(_marks, n++, _catalog.bossMark);
                r.color = new Color(1f, 0.35f, 0.29f, pulse);
                r.transform.position = new Vector3(Snap(m.x), boss.FloorY + 115 * Px, 0);
            }
            for (int i = n; i < _marks.Count; i++) _marks[i].enabled = false;

            n = 0;
            foreach (var q in boss.Bells)
            {
                var r = Pooled(_bells, n++, _catalog.bossBell);
                r.transform.position = new Vector3(Snap(q.x), Snap(q.y - 8 * Px), 0);
            }
            for (int i = n; i < _bells.Count; i++) _bells[i].enabled = false;
        }
    }
}
