using Skp.Skills;
using UnityEngine;

namespace Skp.Levels
{
    /// <summary>One of the nine levels. Filled with the prototype's data in M4 and M7.</summary>
    [CreateAssetMenu(menuName = "Sa Kabilang Pinto/Level Config", fileName = "LevelConfig_")]
    public class LevelConfig : ScriptableObject
    {
        [Tooltip("0 based.")] public int index;
        public string levelName;
        [Range(1, 3)] public int act = 1;
        [Min(1)] public int rooms = 4;
        [TextArea(2, 5)] public string intro;
        [TextArea(2, 5)] public string clue;
        [Tooltip("Skill taught by this level's letter (none on the boss level).")]
        public SkillDefinition skill;
        public bool boss;
    }
}
