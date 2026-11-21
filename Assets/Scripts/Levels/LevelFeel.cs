using Skp.Core;
using Skp.Enemies;
using Skp.Player;
using UnityEngine;

namespace Skp.Levels
{
    /// <summary>
    /// Game feel for one level session: screen shake (Screen shake setting), hit-stop, vibration and rumble
    /// (Vibration setting), and the particle bursts. Numbers from <see cref="FeelTuning"/>. Subscribed once,
    /// nothing allocated per tick.
    /// </summary>
    public class LevelFeel
    {
        readonly FeelTuning _t;
        readonly PlayerController _player;
        readonly CameraFollow _cam;
        readonly LevelFx _fx;
        readonly PlayerSim _sim;
        int _veilTick;

        public LevelFx Fx => _fx;

        public LevelFeel(LevelSession session, PlayerController player, LevelFx fx, FeelTuning tuning)
        {
            _t = tuning != null ? tuning : ScriptableObject.CreateInstance<FeelTuning>();
            _player = player;
            _cam = player != null ? player.cameraFollow : null;
            _fx = fx;
            var sim = _sim = session.Sim;

            sim.Hurt += _ => Kick(_t.hurt);
            sim.Died += () => Kick(_t.die);
            sim.EnemyKilled += e => { Kick(_t.kill); Burst(LevelFx.Kind.Bolt, e.Center); };
            sim.Warded += (_, _) => Kick(_t.ward);
            sim.ItemCollected += it =>
            {
                if (it.Kind == ItemKind.Flame) Burst(LevelFx.Kind.Flame, it.Rect.center);
                else if (it.Kind == ItemKind.Shard) Burst(LevelFx.Kind.Fragment, it.Rect.center);
            };
            sim.ClueFound += () => { Kick(_t.letter); Burst(LevelFx.Kind.Letter, sim.Center); };
            sim.VeilStarted += () => { if (_fx != null) _fx.Ring(LevelFx.Kind.Veil, sim.Center, 13f / 16f, _t.veil.count); };
            sim.BoltHit += (p, target) =>
            {
                switch (target)
                {
                    case BoltTarget.Root:
                    case BoltTarget.Thorns:
                        Kick(_t.burn);
                        Burst(LevelFx.Kind.Burnt, p);
                        Burst(LevelFx.Kind.Ember, p);
                        break;
                    case BoltTarget.Expired:
                        break;
                    default:
                        Kick(_t.boltHit);
                        Burst(LevelFx.Kind.Bolt, p);
                        break;
                }
            };
            sim.LedgeCrumbled += _ => Kick(_t.crumble);
            sim.BellLanded += b =>
            {
                if (Mathf.Abs(b.X - sim.Center.x) < _t.bellShakeRange && Mathf.Abs(b.GroundY - sim.Center.y) < _t.bellShakeRange) Kick(_t.bell);
            };
            sim.Respawned += () => { if (_cam != null) _cam.ClearShake(); };

            var boss = session.Boss;
            if (boss != null)
            {
                boss.Parried += () => { Kick(_t.parry); Burst(LevelFx.Kind.Letter, boss.Core); };
                boss.TollHit += () => Kick(_t.toll);
                boss.Struck += () => { Kick(_t.bossStruck); Burst(LevelFx.Kind.Bolt, boss.Core); };
                boss.Defeated += () => { Kick(_t.bossDefeated); Burst(LevelFx.Kind.Ember, boss.Core, 40); };
                boss.ShockwaveLaunched += () => Kick(_t.bell);
            }
        }

        void Kick(FeelKick k)
        {
            if (_cam != null) _cam.AddTrauma(k.shake);
            if (_player != null && k.hitStop > 0) _player.HitStop(k.hitStop);
            Haptics.Pulse(k.vibrateMs, k.rumble);
        }

        void Burst(LevelFx.Kind kind, Vector2 at) { if (_fx != null) _fx.Emit(kind, at); }
        void Burst(LevelFx.Kind kind, Vector2 at, int n) { if (_fx != null) _fx.Emit(kind, at, n); }

        /// <summary>Per tick: violet motes drift off Tala while the veil lasts.</summary>
        public void Tick(PlayerSim sim)
        {
            if (_fx == null || sim.VeilTicks <= 0) { _veilTick = 0; return; }
            if (++_veilTick % Mathf.Max(1, _t.veilTrickleTicks) == 0) _fx.Emit(LevelFx.Kind.Veil, sim.Center, 1);
        }
    }
}
