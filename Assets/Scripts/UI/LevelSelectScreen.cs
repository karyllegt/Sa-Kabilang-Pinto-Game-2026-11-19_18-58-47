using UnityEngine;

namespace SaKabilangPinto.UI
{
    /// <summary>Level Select: one node per core level, locked until the previous level is finished.</summary>
    public class LevelSelectScreen : MonoBehaviour
    {
        [SerializeField] LevelList levelList;
        [SerializeField] LevelNode[] nodes;
        [SerializeField] ScreenFader faderPrefab;

        void Awake()
        {
            UIEventSystem.Ensure();
            Time.timeScale = 1f;
            foreach (var node in nodes)
            {
                int index = node.LevelIndex;
                node.Button.onClick.AddListener(() => OnNodeTapped(index));
            }
        }

        void OnEnable()
        {
            Refresh();
        }

        public void Refresh()
        {
            foreach (var node in nodes)
            {
                node.SetLocked(!levelList.IsUnlocked(node.LevelIndex));
            }
        }

        void OnNodeTapped(int index)
        {
            if (!levelList.IsUnlocked(index)) return;
            // Stop double taps while the wipe runs.
            foreach (var node in nodes) node.Button.interactable = false;
            StageSession.StartLevel(levelList, index, faderPrefab);
        }
    }
}
