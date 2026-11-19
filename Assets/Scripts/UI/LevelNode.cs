using UnityEngine;
using UnityEngine.UI;

namespace SaKabilangPinto.UI
{
    /// <summary>One level button on the Level Select screen.</summary>
    public class LevelNode : MonoBehaviour
    {
        [Tooltip("Position of this level in the Level List (0 = first level).")]
        [SerializeField] int levelIndex;
        [SerializeField] Button button;
        [Tooltip("Shown while the level is locked (e.g. a lock icon).")]
        [SerializeField] GameObject lockedMark;

        public int LevelIndex => levelIndex;
        public Button Button => button;

        public void SetLocked(bool locked)
        {
            button.interactable = !locked;
            if (lockedMark != null) lockedMark.SetActive(locked);
        }
    }
}
