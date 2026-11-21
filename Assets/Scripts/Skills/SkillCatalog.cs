using UnityEngine;

namespace Skp.Skills
{
    /// <summary>The prototype's SKILLS table (texts verbatim), used to create the SkillDefinition assets.</summary>
    public static class SkillCatalog
    {
        public struct Entry
        {
            public string id, name, filipino, description, tag, shortLabel, keyHint, cue;
            public Color32 color;
            public SkillType type;
        }

        public static readonly Entry[] Defaults =
        {
            new Entry { id = "dj", name = "DOUBLE JUMP", filipino = "Hakbang ng Hangin", description = "Press JUMP again in mid-air for a second jump.", tag = "PASSIVE", color = new Color32(0x8f, 0xd3, 0xff, 0xff), type = SkillType.Passive , shortLabel = "2J", keyHint = "", cue = "skill_dj" },
            new Entry { id = "veil", name = "LAMBONG VEIL", filipino = "Lambong", description = "A shimmering veil makes you immune to harm for 3 seconds. Recharges slowly.", tag = "C / L / VEIL", color = new Color32(0xc0, 0x7d, 0xff, 0xff), type = SkillType.Active , shortLabel = "VEIL", keyHint = "C", cue = "skill_veil" },
            new Entry { id = "bolt", name = "LIWANAG BOLT", filipino = "Sinag", description = "Throw a bolt of lantern light. It stuns creatures and burns roots. Costs flame.", tag = "X / I / BOLT", color = new Color32(0xff, 0xd3, 0x6a, 0xff), type = SkillType.Active , shortLabel = "BOLT", keyHint = "X", cue = "skill_bolt" },
            new Entry { id = "swift", name = "SWIFT DASH", filipino = "Dali", description = "Dash costs far less flame and recharges faster.", tag = "PASSIVE UPGRADE", color = new Color32(0xff, 0x9a, 0x5a, 0xff), type = SkillType.Upgrade , shortLabel = "DSH", keyHint = "", cue = "" },
            new Entry { id = "heart", name = "HEART CHARM", filipino = "Puso", description = "Your vitality grows by one heart.", tag = "PASSIVE UPGRADE", color = new Color32(0xff, 0x6b, 0x7d, 0xff), type = SkillType.Upgrade , shortLabel = "+1", keyHint = "", cue = "" },
            new Entry { id = "spread", name = "TRIPLE BOLT", filipino = "Tatlong Sinag", description = "Your Liwanag Bolt now fires three rays in a fan.", tag = "UPGRADE", color = new Color32(0xff, 0xd3, 0x6a, 0xff), type = SkillType.Upgrade , shortLabel = "3X", keyHint = "", cue = "skill_bolt3" },
            new Entry { id = "veil2", name = "GREATER VEIL", filipino = "Malaking Lambong", description = "The veil lasts 5 seconds and recharges faster.", tag = "UPGRADE", color = new Color32(0xc0, 0x7d, 0xff, 0xff), type = SkillType.Upgrade , shortLabel = "V+", keyHint = "", cue = "" },
            new Entry { id = "glide", name = "PAKPAK GLIDE", filipino = "Pakpak", description = "Hold JUMP while falling to glide slowly down.", tag = "PASSIVE", color = new Color32(0x8f, 0xd3, 0xff, 0xff), type = SkillType.Passive , shortLabel = "GL", keyHint = "", cue = "skill_glide" },
        };
    }
}
