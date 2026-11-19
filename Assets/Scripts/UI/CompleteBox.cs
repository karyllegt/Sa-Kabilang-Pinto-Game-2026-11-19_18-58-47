using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SaKabilangPinto.UI
{
    /// <summary>
    /// Shown when a level ends: clue text, flames and fragments collected,
    /// and Next level / Replay / Level select. After the last level (the boss)
    /// the ending text is shown instead of Next level.
    /// Gameplay code calls Show(flames, fragments) when the level is finished.
    /// </summary>
    public class CompleteBox : MonoBehaviour
    {
        [SerializeField] TextAsset storyFile;
        [SerializeField] LevelList levelList;
        [SerializeField] ScreenFader faderPrefab;
        [SerializeField] string levelSelectScene = "LevelSelect";

        [SerializeField] GameObject panel;
        [SerializeField] Text clueText;
        [SerializeField] Text collectedText;
        [SerializeField] Text endingText;
        [SerializeField] Button nextButton;
        [SerializeField] Button replayButton;
        [SerializeField] Button levelSelectButton;

        int currentIndex = -1;

        void Awake()
        {
            UIEventSystem.Ensure();
            panel.SetActive(false);
            nextButton.onClick.AddListener(OnNext);
            replayButton.onClick.AddListener(OnReplay);
            levelSelectButton.onClick.AddListener(OnLevelSelect);
        }

        /// <summary>Call this when the player finishes the level.</summary>
        public void Show(int flames, int fragments)
        {
            currentIndex = StageSession.CurrentIndex(levelList);
            var entry = levelList != null ? levelList.Get(currentIndex) : null;
            string id = entry != null ? entry.id : StageSession.CurrentLevelId;

            // Finishing this level unlocks the next node on Level Select.
            if (entry != null) LevelProgress.MarkFinished(entry.id);

            var story = StoryText.Get(storyFile, id);
            clueText.text = StoryText.Or(story != null ? story.clue : null, "");
            collectedText.text = "Flames: " + flames + "    Fragments: " + fragments;

            bool hasNext = entry != null && currentIndex + 1 < levelList.Count;
            nextButton.gameObject.SetActive(hasNext);
            endingText.gameObject.SetActive(!hasNext);
            endingText.text = StoryText.Or(story != null ? story.ending : null, "");

            SetButtonsInteractable(true);
            panel.SetActive(true);
            Time.timeScale = 0f;
        }

        void OnNext()
        {
            SetButtonsInteractable(false);
            StageSession.StartLevel(levelList, currentIndex + 1, faderPrefab);
        }

        void OnReplay()
        {
            SetButtonsInteractable(false);
            if (levelList != null && levelList.Get(currentIndex) != null)
                StageSession.StartLevel(levelList, currentIndex, faderPrefab);
            else
                ScreenFader.LoadScene(faderPrefab, SceneManager.GetActiveScene().name);
        }

        void OnLevelSelect()
        {
            SetButtonsInteractable(false);
            ScreenFader.LoadScene(faderPrefab, levelSelectScene);
        }

        void SetButtonsInteractable(bool value)
        {
            nextButton.interactable = value;
            replayButton.interactable = value;
            levelSelectButton.interactable = value;
        }
    }
}
