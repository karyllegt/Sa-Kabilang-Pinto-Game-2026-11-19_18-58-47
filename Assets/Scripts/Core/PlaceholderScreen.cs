using UnityEngine;

namespace Skp.Core
{
    /// <summary>
    /// M1 stand-in for the Title, Map and Level screens so the scene flow can be walked
    /// end to end: Jump or Interact goes forward, Pause goes back. Replaced by the real
    /// screen controllers in M4 and M5.
    /// </summary>
    public class PlaceholderScreen : MonoBehaviour
    {
        public string nextScene;
        public string backScene;

        void Update()
        {
            var root = GameRoot.Instance;
            if (root == null || root.Input == null || root.Loader.IsBusy) return;
            if (!string.IsNullOrEmpty(nextScene) && (root.Input.JumpPressed || root.Input.InteractPressed))
                root.Loader.Load(nextScene);
            else if (!string.IsNullOrEmpty(backScene) && root.Input.PausePressed)
                root.Loader.Load(backScene);
        }
    }
}
