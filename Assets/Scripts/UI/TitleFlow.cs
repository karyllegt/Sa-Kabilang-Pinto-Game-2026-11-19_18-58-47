using Skp.Core;
using Skp.Save;
using UnityEngine;

namespace Skp.UI
{
    /// <summary>
    /// The Title scene's controller: title menu, character select, difficulty select, settings, how to
    /// play and credits. Sub-screens swap in place; leaving the scene (Map, a level) goes through the
    /// circle wipe. Back always goes up one screen and never quits from the title.
    /// </summary>
    public class TitleFlow : MonoBehaviour
    {
        public TitleScreen title;
        public CharacterScreen characters;
        public DifficultyScreen difficulty;
        public SettingsScreen settings;
        public HowToScreen howTo;
        public CreditsScreen credits;

        GameRoot Root => GameRoot.Instance;
        SaveData Save => Root.Save.Data;

        void Start()
        {
            Skp.Audio.Music.SetDimension(Skp.Dimensions.Dim.Ordinary);
            Skp.Audio.Music.Play(Skp.Audio.MusicTrack.Menu);
            UiNavigator.Fallback = () => true; // Back on the title itself does nothing
            title.newJourney.Clicked += () => Swap(characters);
            title.continueButton.Clicked += ContinueRun;
            title.levelMap.Clicked += () => Root.Loader.Load(SceneNames.Map);
            title.settings.Clicked += () => { settings.Bind(Root.Settings, () => Swap(title)); Swap(settings); };
            title.howToPlay.Clicked += () => Swap(howTo);
            title.credits.Clicked += () => Swap(credits);

            characters.back.Clicked += () => Swap(title);
            characters.boy.Clicked += () => PickCharacter("boy");
            characters.girl.Clicked += () => PickCharacter("girl");

            difficulty.back.Clicked += () => Swap(characters);
            difficulty.story.Clicked += () => PickDifficulty("story");
            difficulty.standard.Clicked += () => PickDifficulty("standard");
            difficulty.nightmare.Clicked += () => PickDifficulty("nightmare");
            difficulty.endless.Clicked += () => PickDifficulty("endless");

            howTo.back.Clicked += () => Swap(title);
            credits.back.Clicked += () => Swap(title);

            foreach (var s in new UiScreen[] { characters, difficulty, settings, howTo, credits }) s.gameObject.SetActive(false);
            title.Bind(Save.HasContinue);
            title.Open();
        }

        void OnDestroy()
        {
            UiNavigator.Fallback = null;
        }

        public UiScreen Current => UiNavigator.Top;

        public void Swap(UiScreen to)
        {
            var top = UiNavigator.Top;
            if (top != null && top != to) top.Close();
            if (to == title) title.Bind(Save.HasContinue);
            if (to == difficulty) difficulty.Bind(Save.cleared, Save.best);
            to.Open();
        }

        void PickCharacter(string c)
        {
            Save.@char = c;
            Root.Save.Save();
            Swap(difficulty);
        }

        void PickDifficulty(string key)
        {
            // A new journey replaces the old continue point; unlocked levels and skills stay.
            Save.diff = key;
            Save.cont = new ResumeState();
            Root.Save.Save();
            if (key == "endless")
            {
                GameSession.Pending = new LevelRequest { Level = -1, Difficulty = "endless" };
                Root.Loader.Load(SceneNames.Level);
            }
            else
            {
                GameSession.MapFrom = -1;
                Root.Loader.Load(SceneNames.Map);
            }
        }

        void ContinueRun()
        {
            if (!Save.HasContinue) return;
            Save.diff = Save.cont.dfKey;
            GameSession.Pending = GameSession.FromSave(Save);
            Root.Loader.Load(SceneNames.Level);
        }
    }
}
