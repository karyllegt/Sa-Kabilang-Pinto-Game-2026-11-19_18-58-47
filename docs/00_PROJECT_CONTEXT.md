# Sa Kabilang Pinto: project context

Last updated Nov 19, 2026. Real deadline: **Dec 1, 2026** (the group's deadline). Current mode: **solo build by Karylle G. Torres**. Ignore the team roles and timeline below unless the group re-forms; the whole game (code, art, UI, writing, audio, testing) is built by one person with Claude's help.

## What this is
Sa Kabilang Pinto ("Beyond the Door", "The Lantern Between Worlds") is a 2D Android Filipino-folklore platformer by Pangkat Parol for the CSEL course. Concept credited to Karylle Torres and Pangkat Parol. Target: Unity 2D, Android build.

## Team (original roles, now inactive)
Karylle G. Torres (Game Director, Lead Programmer), Mary Rose G. Balcarse (Pixel Artist, UI), Ronn Lester T. Esquierda (Level Designer, Narrative Writer), Nash Abrenica (Audio, QA Lead).

## Game design essentials
- Protagonist Tala (boy or girl variant). Three dimensions: Ordinary, Spirit, Inverted (flips gravity upward). A Spirit shadow (Anino) chases the player by replaying recorded positions.
- Enemies: Anino, Tikbalang, Manananggal. Final boss: the Bellkeeper (level 9).
- **Nine levels in three acts, built long**: rooms per level 4, 5, 5, 6, 6, 7, 7, 8 then the boss arena (4). Doors, lanterns (checkpoints), flame pickups, 3 fragments per level, a hidden letter (clue) per level, locked exit door.
- Difficulties: Story, Standard, Nightmare, Endless Nightmare (loop mode).
- Look: 16 px tiles, 480x270 reference, palette gold = safe, teal = spirit, violet = inverted, red = danger.

## Letters grant skills (new feature)
Every hidden letter is a story memo **and** teaches Tala a skill. Found once, kept forever (saved). Replaying a level shows the letter again without re-granting.

| Level | Letter | Skill | Effect |
| --- | --- | --- | --- |
| 1 Fiesta Night | invitation scrap | Double Jump (Hakbang ng Hangin) | press jump again in mid-air; resets on landing |
| 2 Behind the Wall | reversed note | Lambong Veil | immune to all harm for 3 s, 15 s recharge (key C / L, touch VEIL) |
| 3 The Missing Staircase | torn page | Liwanag Bolt (Sinag) | lantern-light projectile; stuns creatures, kills Anino, burns roots for 7 s, can stun-hit the shadow chaser; costs 8 flame (key X / I, touch BOLT) |
| 4 Roots That Remember | carved root | Swift Dash (Dali) | dash costs 8 flame instead of 20, shorter cooldown |
| 5 The Blackwater Well | well bell note | Heart Charm (Puso) | +1 max vitality |
| 6 The Horseman Trail | horseshoe note | Triple Bolt (Tatlong Sinag) | bolt fires a three-ray fan, costs 12 flame |
| 7 Bells Without Hands | rope note | Greater Veil | veil lasts 5 s, recharges in 10 s |
| 8 The Wingbeat Corridor | black feather note | Pakpak Glide | hold jump while falling to glide slowly |
| 9 The Last Bell | none | uses every skill | bolt hits the Bellkeeper only when it is stunned; veil helps during its attacks |

More skills can be added by giving a new level or hidden letter a `skill` id (design backlog: wall cling, flame shield, lantern bomb, shadow swap).

Skill rules: letters are required to open each exit (levels 1-8), so every skill is guaranteed. The HUD shows skill chips with cooldown fill and a key label; passive skills show small badges. Touch buttons for VEIL and BOLT appear only once learned.

## Skill-aware obstacles (every skill changes the level, not just the player)
Rule: when a skill is owned, later levels are generated with obstacles that use it, so each skill makes the game harder in a meaningful way as well as stronger. A level is built from the skills the player has at the time (a level's own letter is never required to reach that letter). Fallback: the Spirit and Inverted routes always stay possible with the base kit, so an old or partial save never soft-locks.

| Skill | Obstacle it introduces | Starts at |
| --- | --- | --- |
| Double Jump | 4 to 5 tile climbs on the ordinary route (too high for one jump), with a flame reward on the high ledge; chance rises each level | level 2 |
| Lambong Veil | Spiked ledges (9 tiles, 12 with Greater Veil) with safe ends and a purple glyph; cross them under the veil | level 3 |
| Liwanag Bolt | Thorn walls (2-tile-wide column, 130 px tall) on a ledge edge toward the next ledge; burn them with a bolt (they stay burnt 10 s); the veil also lets you pass | level 4 |
| Swift Dash | More dash-only gaps in the ordinary route | after level 4 |
| Triple Bolt | Enemy packs: a second Anino on wide ledges | level 6 |
| Pakpak Glide | Wide 5 tile gaps that need a glide to cross | level 4 |
| Heart Charm | Extra heart offsets denser spikes from the level ramp | level 5 |

The first time a skill-gated obstacle appears in a level, a hint toast names the skill to use. The letter room itself uses only skills the player already has. Prototype test: all 35 generated spiked ledges are crossable under the veil, thorn walls burn with the bolt, and 111 of 123 double-jump climbs were cleared by a bot (the rest are not yet verified; wide glide gaps cleared 17 of 25 in the bot, so they need more checking).

## Difficulty ramp (long and harder every level and every room)
- Hazard and enemy density multiplier rises 8 percent per level (hazards) and 6 percent per level (enemies), and again 5 percent and 4 percent per room inside a level (caps 2.4x and 2.2x).
- Per level: shadow chase delay shortens 3.5 percent, flood speed rises 5 percent, enemy speed rises 2 percent.
- Difficulty mode multiplies on top (Story gentlest, Nightmare hardest).
- Content mix grows by act: Anino only (Act I), roots and crumbling ledges (Act II), tikbalang, manananggal and bell mechanics (Act III).
- Expected play time: roughly 4 to 6 minutes per level, 45 to 60 minutes for a first full run; Endless Nightmare after the ending.

## Screens and layout (inspired by the Boomerang2D demo, layout only)
Do not copy the demo's art, font, text or code. Implemented in the prototype and to be ported to Unity:
- Title menu box (New Journey, Continue, Level Map, Settings, How to Play, Credits) with a gold arrow cursor.
- Character select, difficulty select.
- **Level map (overworld)**: scrolling map across three acts, nine nodes on a dotted path, a Tala token that walks between nodes, locked/ready/cleared states, top bar (Back, act name, difficulty toggle, Talk), bottom info panel (name, state, Enter or Replay).
- **Lola**, the guide NPC on the map (a story addition, inferred from the demo's NPC guide idea; confirm it fits the story). She greets the player on first visit and gives hints that change with progress, and points to the letters.
- Portrait dialog box with speaker, subtitle, typewriter text and Next button; used for level intros and letters. A letter that grants a skill shows a second box: NEW SKILL, name, description.
- Circle wipe between screens; heart-row HUD; skill chips; popup toasts; Level Clear box (time, score, fragments, clue and new skill, buttons Next Level, Replay, Map).

## Decisions made
- The browser prototype (single HTML file, published artifact) is the behaviour and visual spec; the real build is Unity 2D for Android.
- Scope is no longer cut to five levels: the whole game is built, solo, in milestone order (see the Claude Code prompts).
- Skills come only from letters (no shop), so progression is tied to story and exploration.

## Open questions
- Does the instructor require Unity, and what counts as submitted (Play upload or signed build)?
- Is Lola acceptable as a story addition?
- Final skill list and balance (cooldown and flame numbers are prototype values).

## Where things are
- Browser prototype: https://claude.ai/artifact/LFi4eyDqpTWuLyPyfnE5mq
- Claude Code prompt 4 (full game build, tests and UI verification): claude_code_prompt_4_full_game.md
- Claude Code prompt 5 (skills, long game, difficulty ramp add-on): claude_code_prompt_5_skills_and_progression.md
- Team plan (Claude Doc, original team version): https://claude.ai/artifact/CLXgLmURuL1AYFkru73aSC
- Team plan (Google Doc): https://docs.google.com/document/d/1jtmGB-ujCN9XY_AvfOGjlJDJa-2K56gQly_9VyGcOEs/edit
- Source proposal: Sa_Kabilang_Pinto_Game_Concept_Proposal.docx
- Layout reference: boomerang-sandbox-windows-2.zip
