using UnityEngine;

namespace Skp.Dimensions
{
    /// <summary>
    /// The Anino shadow in the Spirit dimension: replays the player's recorded position, facing
    /// and animation from exactly <see cref="Delay"/> ticks ago. Prototype: G.hist keeps the last
    /// N+1 samples and the chaser sits on the oldest; it appears once N+1 samples exist.
    /// </summary>
    public class ShadowChaser
    {
        public struct Sample
        {
            public Vector2 Position; // bottom-left of the 10 x 14 px body
            public int Facing;
            public float Anim;
        }

        Sample[] _ring = new Sample[1];
        int _head, _count;

        /// <summary>How many ticks the shadow lags behind.</summary>
        public int Delay { get; private set; }
        /// <summary>Ticks of stun left (ward 190, bolt 140). A stunned shadow cannot hurt.</summary>
        public int StunTicks { get; private set; }

        public ShadowChaser(int delayTicks) => SetDelay(delayTicks);

        public void SetDelay(int delayTicks)
        {
            Delay = Mathf.Max(1, delayTicks);
            _ring = new Sample[Delay + 1];
            Clear();
        }

        /// <summary>Forget the trail (dimension change, door, death).</summary>
        public void Clear() { _head = 0; _count = 0; }

        public void ClearStun() => StunTicks = 0;

        /// <summary>Stuns for <paramref name="ticks"/> unless already stunned (prototype only stuns when chStun is 0).</summary>
        public bool Stun(int ticks)
        {
            if (!Active || StunTicks > 0) return false;
            StunTicks = ticks;
            return true;
        }

        /// <summary>Called once per tick in every dimension.</summary>
        public void TickStun() { if (StunTicks > 0) StunTicks--; }

        public void Record(Vector2 position, int facing, float anim)
        {
            _ring[_head] = new Sample { Position = position, Facing = facing, Anim = anim };
            _head = (_head + 1) % _ring.Length;
            if (_count < _ring.Length) _count++;
        }

        /// <summary>True once the shadow has a sample from <see cref="Delay"/> ticks ago.</summary>
        public bool Active => _count > Delay;

        /// <summary>The oldest sample, i.e. where the player was <see cref="Delay"/> ticks ago.</summary>
        public Sample Current => _ring[_head % _ring.Length];

        public bool CanHurt => Active && StunTicks == 0;
    }
}
