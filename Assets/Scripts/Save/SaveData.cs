using System;

namespace Skp.Save
{
    /// <summary>
    /// Persistent progress. Field names match the browser prototype's save object
    /// (unlocked, char, diff, best, cont, cleared, metLola, lolaSeen, skills).
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public int version = SaveService.CurrentVersion;
        /// <summary>Number of unlocked level nodes, 1..LevelCount. Never decreases.</summary>
        public int unlocked = 1;
        /// <summary>"girl" or "boy".</summary>
        public string @char = "girl";
        /// <summary>"story", "standard", "nightmare" or "endless".</summary>
        public string diff = "standard";
        /// <summary>Best Endless Nightmare score.</summary>
        public int best;
        /// <summary>Resume state; <see cref="ResumeState.active"/> false means "no continue".</summary>
        public ResumeState cont = new ResumeState();
        /// <summary>True only after finishing the final level.</summary>
        public bool cleared;
        public bool metLola;
        /// <summary>The <see cref="unlocked"/> value when Lola last spoke (drives her "!" marker).</summary>
        public int lolaSeen;
        /// <summary>Learned skill ids, no duplicates, never removed.</summary>
        public string[] skills = new string[0];

        public bool HasContinue => cont != null && cont.active;

        public SaveData Clone()
        {
            var c = (SaveData)MemberwiseClone();
            c.cont = cont?.Clone() ?? new ResumeState();
            c.skills = (string[])(skills ?? new string[0]).Clone();
            return c;
        }
    }

    /// <summary>Mirror of the prototype's save.cont. JsonUtility cannot store null objects, so a flag marks "none".</summary>
    [Serializable]
    public class ResumeState
    {
        public bool active;
        /// <summary>Level index, 0 based; -1 = Endless loop.</summary>
        public int lv;
        public string dfKey = "standard";
        public int loop;
        /// <summary>Checkpoint (lantern) index.</summary>
        public int cp;
        public int vit;
        public int flame;
        /// <summary>-1 means infinite (Story).</summary>
        public int lives = -1;
        public int score;
        public bool clue;
        public int shards;

        public ResumeState Clone() => (ResumeState)MemberwiseClone();
    }
}
