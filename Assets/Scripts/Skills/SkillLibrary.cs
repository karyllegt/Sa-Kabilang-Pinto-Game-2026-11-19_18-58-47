using UnityEngine;

namespace Skp.Skills
{
    /// <summary>
    /// The eight <see cref="SkillDefinition"/>s in teaching order plus the shared <see cref="SkillTuning"/>.
    /// Lives in Resources (Assets/Resources/Skills/SkillLibrary.asset) so any screen can find it.
    /// </summary>
    [CreateAssetMenu(menuName = "Sa Kabilang Pinto/Skill Library", fileName = "SkillLibrary")]
    public class SkillLibrary : ScriptableObject
    {
        public const string ResourcePath = "Skills/SkillLibrary";

        public SkillDefinition[] skills = new SkillDefinition[0];
        public SkillTuning tuning;

        static SkillLibrary _cache;

        public static SkillLibrary Load()
        {
            if (_cache == null) _cache = Resources.Load<SkillLibrary>(ResourcePath);
            return _cache;
        }

        public static void ClearCache() => _cache = null;

        /// <summary>The asset's tuning, or the built-in defaults.</summary>
        public static SkillTuning Tuning
        {
            get
            {
                var l = Load();
                return l != null && l.tuning != null ? l.tuning : SkillTuning.Default;
            }
        }

        public SkillDefinition Get(string id)
        {
            foreach (var s in skills) if (s != null && s.id == id) return s;
            return null;
        }

        public static SkillDefinition Find(string id)
        {
            var l = Load();
            return l != null ? l.Get(id) : null;
        }
    }
}
