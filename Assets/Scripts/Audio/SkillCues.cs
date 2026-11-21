using Skp.Player;
using Skp.Skills;

namespace Skp.Audio
{
    /// <summary>
    /// A distinct sound for each skill: double jump, veil on and off, bolt (single and triple), bolt impact,
    /// burning roots and thorns, and the start of a glide. Cue names come from the SkillDefinitions.
    /// </summary>
    public class SkillCues
    {
        public const string VeilEnd = "skill_veil_end", BoltHit = "bolt_hit", Burn = "burn";

        bool _veiled, _gliding;

        public static string CueOf(string id, string fallback)
        {
            var d = SkillLibrary.Find(id);
            return d != null && !string.IsNullOrEmpty(d.cue) ? d.cue : fallback;
        }

        public SkillCues(PlayerSim sim)
        {
            sim.AirJumped += () => Sfx.Cue(CueOf(SkillIds.DoubleJump, "skill_dj"));
            sim.VeilStarted += () => Sfx.Cue(CueOf(SkillIds.Veil, "skill_veil"));
            sim.BoltFired += n => Sfx.Cue(n > 1 ? CueOf(SkillIds.TripleBolt, "skill_bolt3") : CueOf(SkillIds.Bolt, "skill_bolt"));
            sim.BoltHit += (_, t) =>
            {
                if (t == BoltTarget.Expired) return;
                Sfx.Cue(t == BoltTarget.Root || t == BoltTarget.Thorns ? Burn : BoltHit);
            };
        }

        /// <summary>Per tick: the veil fading and a glide starting.</summary>
        public void Tick(PlayerSim sim)
        {
            bool v = sim.VeilTicks > 0;
            if (_veiled && !v && sim.Life == PlayerLife.Playing) Sfx.Cue(VeilEnd);
            _veiled = v;
            if (sim.Gliding && !_gliding) Sfx.Cue(CueOf(SkillIds.Glide, "skill_glide"));
            _gliding = sim.Gliding;
        }
    }
}
