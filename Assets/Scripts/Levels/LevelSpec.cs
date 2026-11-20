using System;

namespace Skp.Levels
{
    /// <summary>
    /// Plain data for one level, the prototype's LEVELS entry. <see cref="LevelConfig"/> assets hold
    /// it at runtime (data-driven); <see cref="LevelCatalog.Defaults"/> is the prototype's table, used to
    /// create the assets and by tests.
    /// </summary>
    [Serializable]
    public class LevelSpec
    {
        public string name;
        public int act = 1;
        public int rooms = 4;
        /// <summary>Has the Inverted route (violet doors).</summary>
        public bool inverted;
        /// <summary>Enemy types: "anino", "tik", "manan".</summary>
        public string[] enemies = { "anino" };
        public double spike, root, crumble;
        public bool flood;
        public double floodSpeed;
        public bool boss;
        public bool endless;
        public string skill = "";
        public string intro = "", clue = "", hint = "";

        public bool HasEnemy(string t) => Array.IndexOf(enemies ?? new string[0], t) >= 0;
    }

    public static class LevelCatalog
    {
        public static readonly string[] ActNames = { "ACT I - THE HOUSE WAKES", "ACT II - THE ROOTS REMEMBER", "ACT III - THE LAST BELL" };
        public static readonly string[] Realms = { "WAKING WORLD", "THE ROOTED DARK", "THE LAST BELL" };
        public const string Ending = "Tala strikes the bell once, and the house exhales. Dawn pours through the capiz windows. A voice she half remembers says, \"Salamat, apo.\" Her shadow steps up beside her, close at last.";

        /// <summary>The nine levels, verbatim from the prototype (reference/prototype/index.html LEVELS).</summary>
        public static LevelSpec[] Defaults() => new[]
        {
            new LevelSpec { name = "Fiesta Night", skill = "dj", act = 1, rooms = 4, inverted = false, enemies = new[] { "anino" }, spike = .3,
                intro = "The fiesta should be loud. Tonight the house is silent, and a bell keeps calling from above. Take your parol. Climb.",
                clue = "A scrap of invitation: \"Come home before the midnight bell, anak.\"",
                hint = "Reach the lantern above. Enter the TEAL door to change worlds." },
            new LevelSpec { name = "Behind the Wall", skill = "veil", act = 1, rooms = 5, inverted = false, enemies = new[] { "anino" }, spike = .4,
                intro = "Voices whisper inside the walls. Something has borrowed your shadow, and it walks four seconds behind you.",
                clue = "In your own handwriting, reversed: \"I only wanted to stay at the fiesta.\"",
                hint = "Your shadow follows 4 seconds behind in the Spirit world. Any door is refuge." },
            new LevelSpec { name = "The Missing Staircase", skill = "bolt", act = 1, rooms = 5, inverted = true, enemies = new[] { "anino" }, spike = .4,
                intro = "The stairs are gone. The house is folding over itself. Some doors lead down. Others lead up.",
                clue = "A torn page: \"We rang the bell for the dead every year. Then one year, we forgot.\"",
                hint = "VIOLET doors flip gravity. Walk off a ledge to rise." },
            new LevelSpec { name = "Roots That Remember", skill = "swift", act = 2, rooms = 6, inverted = true, enemies = new[] { "anino" }, spike = .2, root = .5, crumble = .3,
                intro = "A balete tree has grown through every floor. Its roots remember everyone who promised to come home.",
                clue = "Carved into a root: \"The promise was never broken. It was only waiting.\"",
                hint = "Dark roots flash before they strike." },
            new LevelSpec { name = "The Blackwater Well", skill = "heart", act = 2, rooms = 6, inverted = true, enemies = new[] { "anino" }, spike = .2, root = .3, crumble = .4, flood = true, floodSpeed = .1,
                intro = "The old well is overflowing. Black water follows every step you take. Climb faster than the water.",
                clue = "At the bottom of the well, a small bell, still warm. A note: \"Answer it.\"",
                hint = "The water is rising. Climb faster than it." },
            new LevelSpec { name = "The Horseman Trail", skill = "spread", act = 2, rooms = 7, inverted = true, enemies = new[] { "tik", "anino" }, spike = .2, root = .3, crumble = .3,
                intro = "A tikbalang walks the trail between the rooms. Do not follow its laughter. Ward it, then slip past.",
                clue = "A horseshoe nailed to the beam: \"It guards the path home, not the way out.\"",
                hint = "WARD stuns the tikbalang. Slip past it." },
            new LevelSpec { name = "Bells Without Hands", skill = "veil2", act = 3, rooms = 7, inverted = true, enemies = new[] { "tik", "anino" }, spike = .3, root = .2, crumble = .4,
                intro = "Bells ring on their own now. Each toll bends the rooms further out of shape.",
                clue = "A rope tied to nothing: \"Ring it three times and the house will listen.\"",
                hint = "Parry nothing yet. Keep climbing." },
            new LevelSpec { name = "The Wingbeat Corridor", skill = "glide", act = 3, rooms = 8, inverted = true, enemies = new[] { "tik", "anino", "manan" }, spike = .2, root = .2, crumble = .3,
                intro = "Something with wings hunts above the corridor. Keep your flame close and your eyes up.",
                clue = "A black feather and a note: \"Hunger is only loneliness with wings.\"",
                hint = "Manananggal strike from above. WARD to repel." },
            new LevelSpec { name = "The Last Bell", act = 3, rooms = 4, inverted = true, enemies = new[] { "tik", "anino", "manan" }, spike = .2, root = .2, crumble = .3, boss = true,
                intro = "One bell. One door. The Bellkeeper has rung it alone since the night nobody answered.",
                clue = "", hint = "Defeat the Bellkeeper. WARD when its ring closes." },
        };

        /// <summary>The prototype's endlessCfg(n).</summary>
        public static LevelSpec Endless(int loop) => new LevelSpec
        {
            name = "Endless " + (loop + 1), act = (loop % 3) + 1, rooms = 4, inverted = true, enemies = new[] { "anino", "tik", "manan" },
            spike = .3, root = .3, crumble = .3, flood = true, floodSpeed = .09 + .015 * loop, endless = true,
        };
    }
}
