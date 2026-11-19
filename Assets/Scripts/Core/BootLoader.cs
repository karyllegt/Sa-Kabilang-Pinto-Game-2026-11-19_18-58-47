using UnityEngine;

namespace Skp.Core
{
    /// <summary>Lives in the Boot scene: once GameRoot is up, go to the Title screen.</summary>
    public class BootLoader : MonoBehaviour
    {
        void Start()
        {
            Application.targetFrameRate = 60;
            GameRoot.Instance.Loader.Load(SceneNames.Title);
        }
    }
}
