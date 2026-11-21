using UnityEngine;

namespace Skp.Player
{
    /// <summary>Every source of damage. Each one goes through <see cref="PlayerSim.TryHurt"/>, which asks <see cref="PlayerSim.CanBeHurt"/>.</summary>
    public enum DamageSource { Spike, Root, Thorns, Enemy, Shadow, Water, Boss, Bell, Other }

    /// <summary>What a Liwanag Bolt hit.</summary>
    public enum BoltTarget { Ledge, Anino, Creature, Root, Thorns, Shadow, Boss, Expired }

    /// <summary>A Liwanag Bolt in flight (prototype G.bolts). Position is the point of light, units; velocity in units per second.</summary>
    public class Bolt
    {
        public Vector2 Position;
        public Vector2 Velocity;
        /// <summary>Ticks left.</summary>
        public int Life;
    }
}
