using System;
using System.Collections.Generic;
using Skp.Player;
using Skp.Skills;
using UnityEngine;

namespace Skp.Enemies
{
    public enum BossState { Asleep, Intro, Idle, Wave, Bells, Toll, Stun, Dead }

    /// <summary>
    /// The Bellkeeper (prototype activateBoss, updBoss, bossDie, updBossDead), in Unity units with y up. It hovers over
    /// the arena and cycles wave, bells, toll. The toll is a closing golden ring: WARD as it closes (within 230 px)
    /// to parry it for 4 damage and a stun; otherwise it hurts. While stunned, a fresh WARD within 64 px or a Liwanag
    /// Bolt deals 1. Every hit on Tala goes through <see cref="PlayerSim.TryHurt"/>, so the veil blocks the shockwave,
    /// the bells, the toll and contact. Two seconds after it falls, the last door opens.
    /// </summary>
    public class Bellkeeper : IBossFight
    {
        const float Px = 1f / 16f;

        public readonly int MaxHp;
        public int Hp;
        public BossState State { get; private set; } = BossState.Asleep;
        /// <summary>Center x and top of the body (units).</summary>
        public float X, TopY;
        public int T, Cycle, TollTicks, StunTicks, DeadTicks, FlashTicks;
        public readonly float FloorY;
        public bool Active => State != BossState.Asleep;
        public bool Dead => State == BossState.Dead;
        public bool Gone => Dead && DeadTicks > 180;
        public Door? ExitDoor { get; private set; }

        public readonly List<float> Waves = new List<float>();
        public readonly List<int> WaveDirs = new List<int>();
        public readonly List<(float x, int t)> Marks = new List<(float, int)>();
        /// <summary>Falling bells: center x, top y (units), fall speed (units per tick).</summary>
        public readonly List<Vector3> Bells = new List<Vector3>();

        public const int TollTicksToClose = 72, ParryDamage = 4, StunTime = 210;
        public static readonly Vector2 BodySize = new Vector2(40f / 16f, 50f / 16f);

        /// <summary>A line for a toast (string table key).</summary>
        public event Action<string> Message;
        public event Action Parried, Struck, Defeated, ShockwaveLaunched, TollStarted;
        /// <summary>The toll closed without a parry (it rings out and hurts unless veiled).</summary>
        public event Action TollHit;
        public event Action<Door> ExitOpened;

        readonly System.Random _rng = new System.Random(9);
        readonly float _levelWidth;
        bool _wardHit;

        public Bellkeeper(BossArena arena, int hp, float levelWidth)
        {
            FloorY = arena.FloorY;
            MaxHp = Hp = Mathf.Max(1, hp);
            _levelWidth = levelWidth;
        }

        public Vector2 Core => new Vector2(X, TopY - 25 * Px);
        public Rect ContactRect => new Rect(X - 18 * Px, TopY - 44 * Px, 36 * Px, 40 * Px);
        public float Fraction => Mathf.Clamp01(Hp / (float)MaxHp);
        /// <summary>The toll ring's radius in units (shrinks from 154 to 14 px).</summary>
        public float RingRadius => (140f * (1f - Mathf.Min(1f, TollTicks / (float)TollTicksToClose)) + 14f) * Px;
        float Phase => 1f + (1f - Hp / (float)MaxHp) * 0.6f;
        float HoverTop(long tick) => FloorY + 190 * Px - Mathf.Sin(tick * 0.04f) * 3 * Px;

        /// <summary>Wakes it at the arena (prototype activateBoss).</summary>
        public void Activate()
        {
            if (Active) return;
            X = 256 * Px;
            TopY = FloorY + 190 * Px;
            State = BossState.Intro;
            T = 0;
        }

        public void Step(PlayerSim sim)
        {
            if (!Active) return;
            if (State == BossState.Dead) { StepDead(sim); return; }
            T++;
            if (FlashTicks > 0) FlashTicks--;
            float ph = Phase;
            var p = sim.Center;
            switch (State)
            {
                case BossState.Intro:
                    TopY = Mathf.Lerp(TopY, HoverTop(sim.Tick), 0.05f);
                    if (T == 1) Message?.Invoke("boss.awakens");
                    if (T > 110) { State = BossState.Idle; T = 0; Cycle = 0; Message?.Invoke("boss.howto"); }
                    break;
                case BossState.Idle:
                    X += Mathf.Clamp((p.x - X) * 0.01f, -0.8f * Px, 0.8f * Px);
                    TopY = Mathf.Lerp(TopY, HoverTop(sim.Tick), 0.06f);
                    if (T > Mathf.Max(40f, 90f / ph))
                    {
                        T = 0;
                        State = (Cycle % 3) switch { 0 => BossState.Wave, 1 => BossState.Bells, _ => BossState.Toll };
                        Cycle++;
                        if (State == BossState.Toll) { TollTicks = 0; TollStarted?.Invoke(); }
                    }
                    break;
                case BossState.Wave:
                    TopY = Mathf.Lerp(TopY, HoverTop(sim.Tick) - 14 * Px, 0.08f);
                    if (T == 40 || T == 100) { Waves.Add(X); WaveDirs.Add(-1); Waves.Add(X); WaveDirs.Add(1); ShockwaveLaunched?.Invoke(); }
                    if (T > 150) { State = BossState.Idle; T = 0; }
                    break;
                case BossState.Bells:
                    if (T == 1)
                        for (int i = 0; i < 5; i++)
                        {
                            float off = (i - 2) * (30f + (float)_rng.NextDouble() * 30f) * Px;
                            Marks.Add((Mathf.Clamp(sim.Position.x + off, 24 * Px, _levelWidth - 24 * Px), 50 + i * 18));
                        }
                    if (T > 180 && Bells.Count == 0 && Marks.Count == 0) { State = BossState.Idle; T = 0; }
                    break;
                case BossState.Toll:
                    TollTicks++;
                    if (TollTicks == 1) Message?.Invoke("boss.toll");
                    if (TollTicks >= TollTicksToClose)
                    {
                        float d = Vector2.Distance(p, Core);
                        if (sim.WardTicks > 0 && d < 230 * Px)
                        {
                            Hp -= ParryDamage;
                            State = BossState.Stun; StunTicks = StunTime; T = 0;
                            sim.Score += 150;
                            Parried?.Invoke();
                            if (Hp <= 0) { Die(sim); return; }
                            Message?.Invoke("boss.parry");
                        }
                        else
                        {
                            State = BossState.Idle; T = 0;
                            TollHit?.Invoke();
                            sim.TryHurt(1, null, DamageSource.Boss);
                        }
                    }
                    break;
                case BossState.Stun:
                    StunTicks--;
                    TopY = Mathf.Lerp(TopY, FloorY + 62 * Px, 0.08f);
                    int full = PlayerTuning.Ticks(sim.Tuning.wardDuration);
                    if (sim.WardTicks == full) _wardHit = false; // a fresh ward this tick
                    if (sim.WardTicks > full - 4 && !_wardHit && Vector2.Distance(p, Core) < 64 * Px)
                    {
                        _wardHit = true;
                        Hit(sim, 1, 50);
                        if (Dead) return;
                    }
                    if (StunTicks <= 0) { State = BossState.Idle; T = 0; }
                    break;
            }

            // its attacks: shockwaves along the floor, marked bells, and contact
            float spd = 1.9f * sim.EnemySpeedMult * ph * Px;
            for (int i = Waves.Count - 1; i >= 0; i--)
            {
                Waves[i] += WaveDirs[i] * spd;
                if (Waves[i] < -10 * Px || Waves[i] > _levelWidth + 10 * Px) { Waves.RemoveAt(i); WaveDirs.RemoveAt(i); continue; }
                if (sim.Hitbox.Overlaps(new Rect(Waves[i] - 6 * Px, FloorY, 12 * Px, 12 * Px))) sim.TryHurt(1, Waves[i], DamageSource.Boss);
            }
            for (int i = Marks.Count - 1; i >= 0; i--)
            {
                var m = Marks[i];
                if (--m.t <= 0) { Marks.RemoveAt(i); Bells.Add(new Vector3(m.x, FloorY + 245 * Px, 0)); }
                else Marks[i] = m;
            }
            for (int i = Bells.Count - 1; i >= 0; i--)
            {
                var q = Bells[i];
                q.z = Mathf.Min(6.5f * Px, q.z + 0.3f * Px);
                q.y -= q.z;
                Bells[i] = q;
                if (sim.Hitbox.Overlaps(BellRect(q))) sim.TryHurt(1, q.x, DamageSource.Boss);
                if (q.y < FloorY + 14 * Px) Bells.RemoveAt(i);
            }
            if (State != BossState.Stun && sim.Hitbox.Overlaps(ContactRect)) sim.TryHurt(1, X, DamageSource.Boss);
        }

        public static Rect BellRect(Vector3 q) => new Rect(q.x - 7 * Px, q.y - 16 * Px, 14 * Px, 16 * Px);

        /// <summary>A bolt within 30 px of its core stops there; it hurts only while the Bellkeeper is stunned.</summary>
        public bool BoltStrikes(PlayerSim sim, Vector2 point)
        {
            if (!Active || Dead || State == BossState.Intro) return false;
            if (Vector2.Distance(point, Core) >= 30 * Px) return false;
            if (State == BossState.Stun)
            {
                var st = sim.SkillTuning ?? SkillTuning.Default;
                Hit(sim, st.bossBoltDamage, 40);
            }
            return true;
        }

        void Hit(PlayerSim sim, int damage, int score)
        {
            Hp -= damage;
            FlashTicks = 10;
            sim.Score += score;
            Struck?.Invoke();
            if (Hp <= 0) Die(sim);
        }

        void Die(PlayerSim sim)
        {
            Hp = 0;
            State = BossState.Dead;
            DeadTicks = 0;
            Waves.Clear(); WaveDirs.Clear(); Marks.Clear(); Bells.Clear();
            sim.Score += 500;
            Defeated?.Invoke();
        }

        void StepDead(PlayerSim sim)
        {
            DeadTicks++;
            TopY -= Mathf.Min(1.5f, DeadTicks * 0.02f) * Px;
            if (DeadTicks == 120 && ExitDoor == null)
            {
                var door = new Door { Rect = new Rect(24, FloorY, 20 * Px, 32 * Px), Mask = Skp.Dimensions.Dim.All, Target = Skp.Dimensions.Dim.Ordinary, Kind = DoorKind.Exit };
                ExitDoor = door;
                sim.World.Doors.Add(door);
                ExitOpened?.Invoke(door);
                Message?.Invoke("boss.lastdoor");
            }
        }

        /// <summary>Death in the arena clears its attacks; its health stays (prototype respawn).</summary>
        public void OnRespawn(PlayerSim sim)
        {
            if (!Active || Dead) return;
            Waves.Clear(); WaveDirs.Clear(); Marks.Clear(); Bells.Clear();
            TollTicks = 0; State = BossState.Idle; T = 0; StunTicks = 0;
        }
    }
}
