using Skp.Skills;
using UnityEngine;

namespace Skp.Levels
{
    /// <summary>
    /// One of the nine levels (data-driven). The generator reads <see cref="spec"/>; texts are shown by the
    /// before-stage dialog and the Level Clear box (M5). Created from <see cref="LevelCatalog.Defaults"/>
    /// by the setup script and editable in the Inspector.
    /// </summary>
    [CreateAssetMenu(menuName = "Sa Kabilang Pinto/Level Config", fileName = "LevelConfig_")]
    public class LevelConfig : ScriptableObject
    {
        [Tooltip("0 based.")] public int index;
        public LevelSpec spec = new LevelSpec();
        [Tooltip("Skill taught by this level's letter (none on the boss level).")]
        public SkillDefinition skill;

        public string LevelName => spec.name;
        public int Act => spec.act;
        public int Rooms => spec.rooms;
        public bool Boss => spec.boss;
    }
}
