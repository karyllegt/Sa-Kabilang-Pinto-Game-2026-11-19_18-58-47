using UnityEngine;

namespace Skp.Skills
{
    public enum SkillType { Passive, Active, Upgrade }

    /// <summary>One skill taught by a level's letter. Tuning numbers move to a SkillTuning asset in M6.</summary>
    [CreateAssetMenu(menuName = "Sa Kabilang Pinto/Skill Definition", fileName = "Skill_")]
    public class SkillDefinition : ScriptableObject
    {
        [Tooltip("Stable id stored in the save file, e.g. dj, veil, bolt, swift, heart, spread, veil2, glide.")]
        public string id;
        public string displayName;
        public string filipinoName;
        [TextArea] public string description;
        [Tooltip("Short control or type label shown on the skill card, e.g. \"C / L / VEIL\".")]
        public string tag;
        public Color color = Color.white;
        public SkillType type;
    }
}
