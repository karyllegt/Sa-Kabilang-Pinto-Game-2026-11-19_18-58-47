using UnityEngine;

namespace Skp.Player
{
    /// <summary>A boss stepped by the player simulation (the Bellkeeper, Skp.Enemies). Its damage goes through <see cref="PlayerSim.TryHurt"/>.</summary>
    public interface IBossFight
    {
        /// <summary>Called at the end of every simulation tick.</summary>
        void Step(PlayerSim sim);
        /// <summary>Tala died and came back: attacks in flight are cleared, health is kept.</summary>
        void OnRespawn(PlayerSim sim);
        /// <summary>A Liwanag Bolt at <paramref name="point"/> (units) struck it; true stops the bolt.</summary>
        bool BoltStrikes(PlayerSim sim, Vector2 point);
    }
}
