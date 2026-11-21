using System;
using System.Collections.Generic;
using Skp.Save;

namespace Skp.Skills
{
    /// <summary>The eight skill ids, in the order the levels teach them (level 1 to 8).</summary>
    public static class SkillIds
    {
        public const string DoubleJump = "dj", Veil = "veil", Bolt = "bolt", Swift = "swift", Heart = "heart",
                            TripleBolt = "spread", GreaterVeil = "veil2", Glide = "glide";

        public static readonly string[] All = { DoubleJump, Veil, Bolt, Swift, Heart, TripleBolt, GreaterVeil, Glide };

        /// <summary>0 based level whose letter teaches <paramref name="id"/>, or -1.</summary>
        public static int LevelOf(string id) => Array.IndexOf(All, id);

        /// <summary>Skills with a button and a cooldown chip; the rest are passive or upgrades.</summary>
        public static bool IsActive(string id) => id == Veil || id == Bolt;
    }

    /// <summary>Read-only view of the learned skills (what the simulation and the generator ask).</summary>
    public interface ISkillSet
    {
        bool Has(string id);
    }

    /// <summary>An in-memory skill set (tests, dev rooms).</summary>
    public class SkillSet : ISkillSet
    {
        readonly HashSet<string> _ids = new HashSet<string>();
        public static readonly SkillSet None = new SkillSet();

        public SkillSet(params string[] ids) { foreach (var i in ids) if (!string.IsNullOrEmpty(i)) _ids.Add(i); }
        public bool Has(string id) => _ids.Contains(id);
        public void Add(string id) { if (!string.IsNullOrEmpty(id)) _ids.Add(id); }
        public static SkillSet AllSkills() => new SkillSet(SkillIds.All);
    }

    /// <summary>
    /// The learned skills, stored in the save file (<see cref="SaveData.skills"/>). Learning saves at once,
    /// never duplicates and never removes a skill; only deleting the save forgets them.
    /// </summary>
    public class SkillService : ISkillSet
    {
        readonly SaveService _save;

        /// <summary>Raised with the id after a new skill was learned and saved.</summary>
        public event Action<string> Learned;

        public SkillService(SaveService save) { _save = save ?? throw new ArgumentNullException(nameof(save)); }

        public bool Has(string id) => ProgressRules.HasSkill(_save.Data, id);

        /// <summary>Learned skill ids in learning order (a copy).</summary>
        public string[] Owned => (string[])(_save.Data.skills ?? new string[0]).Clone();

        public int Count => _save.Data.skills?.Length ?? 0;

        /// <summary>Adds the skill and saves immediately. Returns false if it was already known (nothing changes).</summary>
        public bool Learn(string id)
        {
            if (!ProgressRules.AddSkill(_save.Data, id)) return false;
            _save.Save();
            Learned?.Invoke(id);
            return true;
        }
    }
}
