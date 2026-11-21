using Skp.Save;

namespace Skp.Core
{
    /// <summary>What the Level scene should play next. Set by the Map (or Continue) before loading it.</summary>
    public class LevelRequest
    {
        /// <summary>0-based level index; -1 = Endless Nightmare.</summary>
        public int Level;
        public string Difficulty = "standard";
        public int Loop;
        /// <summary>Resume from a saved checkpoint (Continue) instead of a fresh start.</summary>
        public ResumeState Resume;
    }

    /// <summary>Hand-off between scenes for the current run (not saved; the save file holds progress).</summary>
    public static class GameSession
    {
        public static LevelRequest Pending;

        /// <summary>Set when returning to the Map from a level: the token starts on that node and walks to the next one. -1 = none.</summary>
        public static int MapFrom = -1;

        /// <summary>
        /// The request the Level scene uses when nothing was set (M4: the placeholder map). Resumes the saved
        /// checkpoint if there is one, otherwise the newest unlocked level.
        /// </summary>
        public static LevelRequest FromSave(SaveData s)
        {
            if (s.HasContinue)
                return new LevelRequest { Level = s.cont.lv, Difficulty = s.cont.dfKey, Loop = s.cont.loop, Resume = s.cont.Clone() };
            return new LevelRequest { Level = s.unlocked - 1, Difficulty = s.diff };
        }

        public static LevelRequest Take(SaveData s)
        {
            var r = Pending ?? FromSave(s);
            Pending = null;
            return r;
        }
    }
}
