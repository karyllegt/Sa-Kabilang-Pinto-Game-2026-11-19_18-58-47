using System;
using UnityEngine;

namespace Skp.Player
{
    /// <summary>R3: Tala's animation states (art bible section 5 and the R3 frame table).</summary>
    public enum TalaAnim { Idle, Run, JumpRise, Apex, Fall, Land, Dash, Ward, Hurt, Death, Door, Glide, Bolt }

    /// <summary>
    /// R3: maps <see cref="PlayerSim"/> state to an animation, one step per simulation tick (so hit-stop, which runs no
    /// ticks, pauses it). The simulation never reads it back, so the game stays deterministic. One-shot animations
    /// (land, hurt, door, bolt cast) start from the simulation's events and run their length unless something more
    /// important takes over. Allocates only when bound.
    /// </summary>
    public class PlayerAnimator
    {
        /// <summary>Length in ticks of each one-shot (frames x ticks per frame in the default sets).</summary>
        public const int LandTicks = 8, HurtTicks = 16, DoorTicks = 24, BoltTicks = 10;
        /// <summary>Below this vertical speed (px per tick) a jump reads as its apex.</summary>
        public const float ApexSpeed = 0.8f;
        const float RunSpeed = 0.4f; // px per tick, as PlayerView's run test

        public TalaAnim State { get; private set; } = TalaAnim.Idle;
        public int StateTicks { get; private set; }

        TalaAnim _shot;
        int _shotLeft;
        PlayerSim _sim;
        readonly Action _onLanded, _onRespawned;
        readonly Action<int> _onHurt, _onBolt;
        readonly Action<Door> _onDoor;

        public PlayerAnimator()
        {
            _onLanded = () => Shot(TalaAnim.Land, LandTicks);
            _onHurt = _ => Shot(TalaAnim.Hurt, HurtTicks);
            _onDoor = _ => Shot(TalaAnim.Door, DoorTicks);
            _onBolt = _ => Shot(TalaAnim.Bolt, BoltTicks);
            _onRespawned = () => { _shotLeft = 0; Set(TalaAnim.Idle); };
        }

        public void Bind(PlayerSim sim)
        {
            if (_sim == sim) return;
            Unbind();
            _sim = sim;
            if (sim == null) return;
            sim.Landed += _onLanded;
            sim.Hurt += _onHurt;
            sim.DoorUsed += _onDoor;
            sim.BoltFired += _onBolt;
            sim.Respawned += _onRespawned;
            _shotLeft = 0;
            Set(TalaAnim.Idle);
        }

        public void Unbind()
        {
            if (_sim == null) return;
            _sim.Landed -= _onLanded;
            _sim.Hurt -= _onHurt;
            _sim.DoorUsed -= _onDoor;
            _sim.BoltFired -= _onBolt;
            _sim.Respawned -= _onRespawned;
            _sim = null;
        }

        void Shot(TalaAnim a, int ticks)
        {
            _shot = a;
            _shotLeft = ticks;
        }

        void Set(TalaAnim a)
        {
            if (a == State) return;
            State = a;
            StateTicks = 0;
        }

        /// <summary>Call once after every simulation tick.</summary>
        public void Tick(PlayerSim sim)
        {
            var want = Pick(sim, _shotLeft > 0 ? _shot : (TalaAnim?)null);
            if (_shotLeft > 0) _shotLeft--;
            if (want == State) StateTicks++;
            else Set(want);
        }

        /// <summary>
        /// The state rules, highest first: death, hurt, dash, ward, door and bolt cast, glide, air (rise, apex, fall),
        /// landing, run, idle. <paramref name="shot"/> is the one-shot still playing, if any.
        /// </summary>
        public static TalaAnim Pick(PlayerSim sim, TalaAnim? shot)
        {
            if (sim.Life != PlayerLife.Playing) return TalaAnim.Death;
            if (shot == TalaAnim.Hurt) return TalaAnim.Hurt;
            if (sim.DashTicks > 0) return TalaAnim.Dash;
            if (sim.WardTicks > 0) return TalaAnim.Ward;
            if (shot == TalaAnim.Door || shot == TalaAnim.Bolt) return shot.Value;
            if (!sim.Grounded)
            {
                if (sim.Gliding) return TalaAnim.Glide;
                float up = sim.Velocity.y * sim.GravitySign * PlayerSim.Dt * PlayerTuning.PxPerUnit;
                if (Mathf.Abs(up) < ApexSpeed) return TalaAnim.Apex;
                return up > 0 ? TalaAnim.JumpRise : TalaAnim.Fall;
            }
            if (shot == TalaAnim.Land) return TalaAnim.Land;
            return Mathf.Abs(sim.Velocity.x) * PlayerSim.Dt * PlayerTuning.PxPerUnit > RunSpeed ? TalaAnim.Run : TalaAnim.Idle;
        }
    }
}
