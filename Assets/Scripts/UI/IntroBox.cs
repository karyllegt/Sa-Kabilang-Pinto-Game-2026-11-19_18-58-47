using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SaKabilangPinto.UI
{
    /// <summary>
    /// Shown at the start of a level with the level name and intro text.
    /// The game is paused until the player taps; then play begins.
    /// </summary>
    public class IntroBox : MonoBehaviour
    {
        [SerializeField] TextAsset storyFile;
        [SerializeField] LevelList levelList;

        [SerializeField] GameObject panel;
        [SerializeField] Text levelNameText;
        [SerializeField] Text introText;
        [Tooltip("Full-screen button: tapping anywhere closes the box.")]
        [SerializeField] Button tapToClose;

        [Tooltip("Called when the box closes and play begins.")]
        public UnityEvent onClosed;

        public bool IsOpen { get; private set; }

        void Awake()
        {
            UIEventSystem.Ensure();
            tapToClose.onClick.AddListener(Close);
        }

        void Start()
        {
            Open();
        }

        public void Open()
        {
            int index = StageSession.CurrentIndex(levelList);
            var entry = levelList != null ? levelList.Get(index) : null;
            string id = entry != null ? entry.id : StageSession.CurrentLevelId;
            var story = StoryText.Get(storyFile, id);

            levelNameText.text = StoryText.Or(story != null ? story.name : null, "Level " + id);
            introText.text = StoryText.Or(story != null ? story.intro : null, "");

            panel.SetActive(true);
            IsOpen = true;
            Time.timeScale = 0f;
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            panel.SetActive(false);
            Time.timeScale = 1f;
            onClosed.Invoke();
        }
    }
}
