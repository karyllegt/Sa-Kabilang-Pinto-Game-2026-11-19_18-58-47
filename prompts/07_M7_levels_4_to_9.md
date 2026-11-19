# Prompt 07: M7 Levels 4 to 9, enemies, Bellkeeper, skill-aware obstacles, difficulty curve

Build levels 4 to 9 with their intro and clue texts and skills: roots that flash before striking (Act II), the rising black-water flood (level 5), the tikbalang (charges when you are on its ledge, ward stuns), manananggal (hovers, dives, ward or bolt), bell mechanics in Act III, and the Bellkeeper boss (cycle wave, bells, toll; parry the toll with the ward when the golden ring closes for 4 damage and a stun; ward and bolt hits only while stunned; veil helps against the shockwave). Endless Nightmare loop mode unlocked after the ending. Ending screen with the ending text.

Then implement `docs/02_SKILLS_AND_PROGRESSION.md` features 3 and 4: levels built from the skills the player owns, rooms per level 4, 5, 5, 6, 6, 7, 7, 8 then boss, the `DifficultyCurve` ScriptableObject and the full obstacle catalog (double-jump climbs with flame rewards, veil spike gauntlets, bolt thorn walls, dash gaps, triple-bolt enemy packs, glide gaps), first-appearance hint toasts, the development debug overlay for multipliers, and the rule that the Spirit/Inverted route always stays completable with the base kit.

Tests: all tests in the spec for features 3 and 4: per-obstacle bots (crossed with skill, blocked without on the ordinary route, fallback route still completable), 100 percent for veil spikes and bolt thorns and 95 percent for jump-based obstacles, statistics on density and first appearance, boss defeat flow (bar to zero, ending, `cleared` saved), a full scripted run of levels 1 to 9 with no error logs, difficulty-mode checks (vitality 6/4/3/4).

Done when: all nine levels completable by the bot with the intended skills, the boss is beatable, tests pass, committed.

Use the provided enemy, boss, door, hazard, tile and background sprites. Create the missing boss frames, tile variety and Act II and III props in the same style (see the gaps list in `docs/04_ASSET_GUIDE.md`).
