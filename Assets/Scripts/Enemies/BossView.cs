using System.Collections.Generic;
using Skp.Levels;
using UnityEngine;

namespace Skp.Enemies
{
    /// <summary>
    /// Draws the Bellkeeper like the prototype's drawBoss(): the body (stunned and tolling frames), the closing golden
    /// toll ring, shockwaves along the arena floor, red warning lines and the falling bells. Built at runtime by the
    /// level runner on the boss level; nothing is allocated per frame after warm-up.
    /// </summary>
    public class BossView : MonoBehaviour
    {
        const float Px = 1f / 16f;
        const int RingPoints = 48;

        public LevelArt art;
        [System.NonSerialized] public Bellkeeper boss;
        SpriteRenderer _body;
        LineRenderer _ring;
        readonly List<SpriteRenderer> _waves = new List<SpriteRenderer>(), _marks = new List<SpriteRenderer>(), _bells = new List<SpriteRenderer>();
        static Sprite _white;
        long _t;

        public SpriteRenderer Body => _body;
        public bool RingVisible => _ring != null && _ring.enabled;

        public void Bind(Bellkeeper b, LevelArt levelArt)
        {
            boss = b;
            art = levelArt;
            _body = Make("Bellkeeper", art.boss, 8);
            _ring = new GameObject("Toll ring").AddComponent<LineRenderer>();
            _ring.transform.SetParent(transform, false);
            _ring.useWorldSpace = true;
            _ring.loop = true;
            _ring.positionCount = RingPoints;
            _ring.widthMultiplier = 3 * Px;
            _ring.sharedMaterial = _body.sharedMaterial; // the sprites' own material: drawn like the rest of the level
            _ring.sortingOrder = 12;
            _ring.enabled = false;
            Render();
        }

        SpriteRenderer Make(string name, Sprite s, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.sortingOrder = order;
            return sr;
        }

        static Sprite White()
        {
            if (_white != null) return _white;
            var t = new Texture2D(1, 1) { filterMode = FilterMode.Point };
            t.SetPixel(0, 0, Color.white);
            t.Apply();
            _white = Sprite.Create(t, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            return _white;
        }

        SpriteRenderer Pooled(List<SpriteRenderer> pool, int i, Sprite s, int order)
        {
            while (pool.Count <= i) pool.Add(Make("fx", s, order));
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
            _body.enabled = show;
            if (show)
            {
                _body.sprite = boss.State == BossState.Stun && art.bossStun != null ? art.bossStun
                             : boss.State == BossState.Toll && art.bossToll != null ? art.bossToll : art.boss;
                float h = _body.sprite != null ? _body.sprite.bounds.size.y : Bellkeeper.BodySize.y;
                float jitter = boss.Dead ? ((boss.DeadTicks % 4) < 2 ? Px : -Px) : 0;
                _body.transform.position = new Vector3(Snap(boss.X) + jitter, Snap(boss.TopY - h), 0);
                var c = Color.white;
                if (boss.Dead) c.a = Mathf.Max(0, 1f - boss.DeadTicks / 180f);
                else if (boss.FlashTicks > 0 && boss.FlashTicks % 2 == 1) c.a = 0.5f;
                _body.color = c;
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
                var w = Pooled(_waves, n++, art.shockwave != null ? art.shockwave : White(), 9);
                w.transform.position = new Vector3(Snap(boss.Waves[i]), boss.FloorY + 6 * Px, 0);
                if (art.shockwave == null) { w.color = new Color32(0xff, 0xd3, 0x6a, 0xff); w.transform.localScale = new Vector3(12 * Px, 12 * Px, 1); }
                w.flipX = boss.WaveDirs[i] < 0;
            }
            for (int i = n; i < _waves.Count; i++) _waves[i].enabled = false;

            n = 0;
            float pulse = 0.4f + 0.4f * Mathf.Sin(_t * 0.4f);
            foreach (var m in boss.Marks)
            {
                var r = Pooled(_marks, n++, White(), 2);
                r.color = new Color(1f, 0.35f, 0.29f, pulse);
                r.transform.localScale = new Vector3(2 * Px, 230 * Px, 1);
                r.transform.position = new Vector3(Snap(m.x), boss.FloorY + 115 * Px, 0);
            }
            for (int i = n; i < _marks.Count; i++) _marks[i].enabled = false;

            n = 0;
            foreach (var q in boss.Bells)
            {
                var r = Pooled(_bells, n++, art.bell, 9);
                r.transform.position = new Vector3(Snap(q.x), Snap(q.y - 8 * Px), 0);
            }
            for (int i = n; i < _bells.Count; i++) _bells[i].enabled = false;
        }
    }
}
