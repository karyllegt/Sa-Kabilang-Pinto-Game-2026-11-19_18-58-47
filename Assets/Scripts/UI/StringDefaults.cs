using System.Collections.Generic;

namespace Skp.UI
{
    /// <summary>
    /// English source text for the string table (texts from the prototype where it had them).
    /// The setup copies these into Assets/Resources/UI/Strings_en.asset; edit the asset to change text.
    /// </summary>
    public static class StringDefaults
    {
        public static readonly Dictionary<string, string> En = new Dictionary<string, string>
        {
            // common
            ["common.back"] = "BACK",
            ["common.next"] = "NEXT",
            ["common.prev"] = "PREV",
            ["common.close"] = "CLOSE",
            ["common.begin"] = "BEGIN",
            ["common.continue"] = "CONTINUE",
            ["common.on"] = "ON",
            ["common.off"] = "OFF",

            // title
            ["title.logo"] = "SA KABILANG\nPINTO",
            ["title.tagline"] = "THE LANTERN BETWEEN WORLDS",
            ["title.footer"] = "9 haunted levels / 3 realms / one borrowed shadow",
            ["title.new"] = "NEW JOURNEY",
            ["title.continue"] = "CONTINUE",
            ["title.map"] = "LEVEL MAP",
            ["title.settings"] = "SETTINGS",
            ["title.how"] = "HOW TO PLAY",
            ["title.credits"] = "CREDITS",

            // character and difficulty
            ["chars.title"] = "WHO CARRIES THE PAROL?",
            ["chars.sub"] = "Choose your character. Both share the same skills.",
            ["chars.boy"] = "BOY",
            ["chars.girl"] = "GIRL",
            ["diff.title"] = "CHOOSE YOUR NIGHT",
            ["diff.story"] = "STORY",
            ["diff.story.desc"] = "More vitality, unlimited lives, slower foes. Experience the whole tale.",
            ["diff.standard"] = "STANDARD",
            ["diff.standard.desc"] = "4 vitality, 5 lives. The intended experience.",
            ["diff.nightmare"] = "NIGHTMARE",
            ["diff.nightmare.desc"] = "3 vitality, 3 lives, faster foes, harsher dimensions.",
            ["diff.endless"] = "ENDLESS NIGHTMARE",
            ["diff.endless.locked"] = "Clear the story to unlock an endless climb for score.",
            ["diff.endless.desc"] = "Rising water, every dimension, no end. Best score {0}.",

            // how to play (pages)
            ["how.title"] = "HOW TO PLAY",
            ["how.page"] = "{0} / {1}",
            ["how.1"] = "Climb each haunted tower to the lantern above. Every room has a fork: take the ledges of the waking world, or enter a doorway.\n\n<color=#41e0d0>TEAL doors</color> open the Spirit world: new ledges appear, but your shadow follows your path 4 seconds behind. Any door is refuge.\n\n<color=#c07dff>VIOLET doors</color> flip gravity upward: walk off a ledge and rise to the next. Rise to HOME above.",
            ["how.2"] = "<color=#ff6b5a>Red</color> means danger. <color=#ffd36a>Gold</color> means safety. <color=#41e0d0>Teal</color> marks spirit paths.\n\nDASH spends flame and makes you untouchable for a moment. WARD stuns or harms creatures and parries the Bellkeeper's toll.\n\nLight the lanterns to save your progress.",
            ["how.3"] = "<color=#ffd36a>LETTERS</color> hidden in each tower are more than memos. Every one you find teaches Tala a new skill: double jump, the Lambong veil, the Liwanag bolt, and more.\n\nKeys: A/D or arrows move. Space jump. Q dash. F ward. C veil. X bolt. E door. Esc pause.\n\nGamepad and touch are supported.",

            // settings
            ["settings.title"] = "SETTINGS",
            ["settings.music"] = "Music",
            ["settings.sfx"] = "Sound effects",
            ["settings.vibration"] = "Vibration",
            ["settings.shake"] = "Screen shake",
            ["settings.flash"] = "Reduced flash",
            ["settings.contrast"] = "High contrast",
            ["settings.lefty"] = "Left-handed layout",
            ["settings.touch"] = "Touch controls",
            ["settings.touch.auto"] = "AUTO",
            ["settings.touch.on"] = "ON",
            ["settings.touch.off"] = "OFF",
            ["settings.note"] = "Reduced flash replaces white flashes and blinking roots with soft tints.",

            // credits
            ["credits.title"] = "CREDITS",
            ["credits.body"] = "Game concept: Karylle G. Torres and Pangkat Parol (CSEL).\nBuilt by Karylle G. Torres.\n\nInspired by Filipino folklore: the Anino, Tikbalang, Manananggal and the balete tree.\n\nFont: Press Start 2P by CodeMan38, SIL Open Font License 1.1.\n\nArt and sound are original to Sa Kabilang Pinto.",

            // map
            ["map.talk"] = "TALK",
            ["map.enter"] = "ENTER",
            ["map.replay"] = "REPLAY",
            ["map.ready"] = "READY",
            ["map.cleared"] = "CLEARED",
            ["map.locked"] = "LOCKED: clear level {0} first",
            ["map.node"] = "{0}. {1}",
            ["map.sub"] = "{0} - {1}",

            // Lola
            ["how.4"] = "Every letter teaches a skill for good:\n<color=#8fd3ff>DOUBLE JUMP</color>  JUMP again in mid-air\n<color=#c07dff>LAMBONG VEIL</color>  C: no harm for 3 s\n<color=#ffd36a>LIWANAG BOLT</color>  X: stun creatures, burn roots\n<color=#ff9a5a>SWIFT DASH</color>  a cheaper, quicker dash\n<color=#ff6b7d>HEART CHARM</color>  one more heart\n<color=#ffd36a>TRIPLE BOLT</color>  three rays in a fan\n<color=#c07dff>GREATER VEIL</color>  5 s, recharges sooner\n<color=#8fd3ff>PAKPAK GLIDE</color>  hold JUMP to float down",
            ["map.skills"] = "SKILLS",
            ["skills.title"] = "SKILLS",
            ["skills.locked"] = "? ? ?",
            ["skills.lockedsub"] = "Hidden in the letter of level {0}.",
            ["skill.new.sub"] = "NEW SKILL - {0}",
            ["skill.known.sub"] = "SKILL KNOWN - {0}",
            ["clear.newskill"] = "NEW SKILL: {0}",
            ["toast.noflame"] = "Not enough flame for a bolt.",
            ["hint.dj"] = "Some ledges sit too high for one jump. Press JUMP again in mid-air.",
            ["hint.gaunt"] = "Spiked ledges ahead. Cross them under the LAMBONG VEIL (C).",
            ["hint.thorn"] = "Thorn walls block narrow ledges. Burn them with the LIWANAG BOLT (X).",
            ["hint.wide"] = "Wide gaps ahead. Hold JUMP in the air to glide across.",
            ["lola.skill.dj"] = "You leapt twice on the stair, apo? Your lolo did that at every fiesta. When one jump is not enough, jump again in the air.",
            ["lola.skill.veil"] = "The Lambong is an old kindness. Wear it when the spikes crowd close, then give it time to gather again.",
            ["lola.skill.bolt"] = "Liwanag remembers the lantern that made it. Throw it at the shadows and the thorn walls. It burns away what blocks you.",
            ["lola.skill.swift"] = "Dali, dali! Your dash costs less flame now. Use it often, apo, but never blindly.",
            ["lola.skill.heart"] = "Puso. You carry more of us with you now. One more heart against the long night.",
            ["lola.skill.spread"] = "Three rays, like the points of a parol. Even a tikbalang cannot dodge them all.",
            ["lola.skill.veil2"] = "The greater veil lasts longer and gathers sooner. Trust it under the bells.",
            ["lola.skill.glide"] = "Pakpak. Hold the jump and let the night air carry you over the wide gaps.",
            ["lola.name"] = "LOLA",
            ["lola.sub"] = "THE LANTERN PATH",
            ["lola.first.1"] = "Apo, you came home late. The bell has been calling for you since the fiesta began.",
            ["lola.first.2"] = "Every lantern on this path is a room of the old house. Walk to one and open its door.",
            ["lola.first.3"] = "Teal doors lead to the spirit world, but your shadow will follow behind you. Violet doors turn the world upside down. Gold light is always safe.",
            ["lola.first.4"] = "Look for the folded letters hidden in each room. They are more than memories. Each one will teach you something: to leap twice, to hide in a veil, to throw light.",
            ["lola.first.5"] = "Light the lanterns in order. When you reach the last bell, I will be waiting.",
            ["lola.cleared"] = "Salamat, apo. The house is quiet at last. Come back and walk the path whenever you like.",
            ["lola.act1"] = "The house wakes slowly. Stay near the gold light. When your shadow starts to follow, any door is a place to rest.",
            ["lola.act2a"] = "The roots flash before they strike, apo. Count the flash, then move. And the black water rises faster than you think.",
            ["lola.act2b"] = "A tikbalang laughs to pull you off the path. Do not follow it. Use your ward, then slip past. Something with wings hunts above the corridor, so keep your flame close.",
            ["lola.act3"] = "The Bellkeeper only listens to its own toll. Use your ward when the golden ring closes on you.",

            // level dialogs
            ["level.node"] = "{0}. {1}",
            ["level.endless.sub"] = "ENDLESS NIGHTMARE - LOOP {0}",
            ["level.endless.intro"] = "The house loops. Rooms rise, water climbs, and every dimension bites harder. Climb as high as you can.",
            ["clue.name"] = "A LETTER, FOLDED SMALL",
            ["clue.sub"] = "FOUND IN {0}",

            // HUD and objective
            ["hud.level"] = "{0} / 9 - {1}",
            ["hud.lives"] = "Lives {0}  Score {1}",
            ["hud.lives.inf"] = "Lives -  Score {0}",
            ["hud.flame"] = "FLAME {0}",
            ["hud.dim.1"] = "WAKING WORLD",
            ["hud.dim.2"] = "SPIRIT",
            ["hud.dim.4"] = "INVERTED / GRAVITY UP",
            ["obj.boss"] = "DEFEAT THE BELLKEEPER - WARD when the ring closes",
            ["obj.bossdone"] = "ENTER THE LAST DOOR",
            ["obj.spirit"] = "SPIRIT - reach the lantern above or take the door home",
            ["obj.inverted"] = "INVERTED - rise to the HOME door above",
            ["obj.arena"] = "CLIMB INTO THE BELL ARENA",
            ["obj.findfinal"] = "FIND THE CLUE, THEN THE FINAL DOOR",
            ["obj.final"] = "ENTER THE FINAL DOOR",
            ["obj.findroom"] = "FIND THE CLUE IN THIS ROOM",
            ["obj.climb"] = "CLIMB TO THE LANTERN ABOVE - room {0} of {1}",
            ["boss.name"] = "THE BELLKEEPER",

            // toasts
            ["toast.lantern"] = "LANTERN LIT - progress saved",
            ["toast.fragment"] = "FLAME FRAGMENT - max flame up",
            ["toast.locked"] = "LOCKED - find the clue hidden in this tower first",

            // pause, clear, game over, ending
            ["pause.title"] = "PAUSED",
            ["pause.resume"] = "RESUME",
            ["pause.settings"] = "SETTINGS",
            ["pause.lantern"] = "TO LANTERN",
            ["pause.restart"] = "RESTART LEVEL",
            ["pause.quit"] = "SAVE AND QUIT",
            ["clear.title"] = "LEVEL {0} CLEAR",
            ["clear.loop"] = "LOOP {0} CLEAR",
            ["clear.loop.sub"] = "The water rises faster next loop.",
            ["clear.time"] = "TIME",
            ["clear.score"] = "SCORE",
            ["clear.fragments"] = "FRAGMENTS",
            ["clear.clue"] = "CLUE",
            ["clear.next"] = "NEXT LEVEL",
            ["clear.nextloop"] = "NEXT LOOP",
            ["clear.replay"] = "REPLAY",
            ["clear.map"] = "MAP",
            ["clear.menu"] = "MENU",
            ["clear.seconds"] = "{0}s",
            ["over.title"] = "THE NIGHT TAKES YOU",
            ["over.stats"] = "Level {0}. Score {1}. Fragments {2}/3.",
            ["over.retry"] = "TRY AGAIN",
            ["over.map"] = "MAP",
            ["ending.title"] = "DAWN",
            ["ending.stats"] = "Time {0}s. Score {1}. Fragments {2}. Endless Nightmare unlocked.",
            ["ending.menu"] = "MAIN MENU",
        };
    }
}
