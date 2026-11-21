using UnityEngine;

namespace Skp.Skills
{
    public enum SkillType { Passive, Active, Upgrade }

    /// <summary>One skill taught by a level's letter. Its numbers live in the shared <see cref="SkillTuning"/> asset.</summary>
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
        [Tooltip("HUD chip or badge label, e.g. VEIL, BOLT, 2J.")]
        public string shortLabel;
        [Tooltip("Keyboard key shown on an active skill's chip (C, X).")]
        public string keyHint;
        [Tooltip("16x16 icon for the HUD badge, the skill card and the skill list.")]
        public Sprite icon;
        [Tooltip("0 based level whose letter teaches it.")]
        public int level;
        [Tooltip("Sound cue played when the skill is used (Sfx bank name).")]
        public string cue;

        public bool IsActive => type == SkillType.Active;
        /// <summary>"LAMBONG VEIL (Lambong)".</summary>
        public string FullName => string.IsNullOrEmpty(filipinoName) ? displayName : $"{displayName} ({filipinoName})";
    }
}
