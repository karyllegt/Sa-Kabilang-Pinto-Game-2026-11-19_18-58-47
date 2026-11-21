using UnityEngine;

namespace Skp.Core
{
    /// <summary>On the GameRoot: ends gamepad rumble on time and stops it when the game is paused or closed.</summary>
    public class FeelService : MonoBehaviour
    {
        void Update() => Haptics.Tick();
        void OnDisable() => Haptics.Stop();
    }
}
