# Spec: letter skills, long game, difficulty ramp, skill-aware obstacles

This is a specification file. Claude Code reads it when a milestone prompt tells it to.


## NEW FEATURE 1: LETTERS TEACH SKILLS

Each level (1 to 8) hides one **letter** (the clue pickup). It is a story memo and also grants Tala a skill, permanently. The level 9 boss has no letter. Replaying a level shows the letter but does not re-grant the skill.

Data-driven: create a `SkillDefinition` ScriptableObject (`id`, `displayName`, `filipinoName`, `description`, `tag`, `color`, `type` Passive/Active/Upgrade, tuning numbers) and reference it from `LevelConfig.skill`. A `SkillService` holds the unlocked set, persists it in the save file (`skills: string[]`), and exposes `Has(id)`.

| Level | Skill id | Name | Behavior (exact numbers go in a `SkillTuning` asset) |
| --- | --- | --- | --- |
| 1 | dj | Double Jump (Hakbang ng Hangin) | One extra jump in mid-air, impulse about 92 percent of a ground jump; resets on landing; cannot trigger on the same frame as a ground or coyote jump; no third jump. |
| 2 | veil | Lambong Veil | Active. Immune to all damage 3.0 s, cooldown 15 s from activation. Purple ring effect that flickers in its last 1 s. Key C or L, gamepad button, touch VEIL. |
| 3 | bolt | Liwanag Bolt (Sinag) | Active projectile in facing direction, speed about 4.4 px/frame at 60 Hz (convert to units/s), lifetime about 0.9 s, cost 8 flame, cooldown 0.27 s. Stops on solid ledges in the current dimension. Kills Anino; hits Tikbalang and Manananggal for 1 hp and stuns them 2.8 s; burns a Root hazard (disabled 7 s); stuns the Spirit shadow chaser 2.3 s; damages the Bellkeeper by 1 only while it is stunned. Key X or I, touch BOLT. |
| 4 | swift | Swift Dash (Dali) | Dash flame cost 20 to 8, cooldown 34 to 20 frames-equivalent. |
| 5 | heart | Heart Charm (Puso) | +1 max vitality, applied immediately and on every later level start. |
| 6 | spread | Triple Bolt (Tatlong Sinag) | Bolt fires three rays at -14, 0, +14 degrees, cost 12 flame, cooldown 0.37 s. |
| 7 | veil2 | Greater Veil (Malaking Lambong) | Veil 5.0 s, cooldown 10 s. |
| 8 | glide | Pakpak Glide | Holding jump while falling caps fall speed at about 1.1 px/frame; releases on ground or when the button is released; works in Inverted gravity mirrored. |

Rules:
- Damage immunity (veil, dash, post-hit invulnerability) is one function `CanBeHurt()`; every damage source (spikes, roots, enemies, water, shadow, boss) must go through it. Water drowning ticks and boss attacks must also respect the veil.
- Solvability: the ordinary route may require owned skills (see Feature 4), but the Spirit or Inverted route must always stay completable with the base kit, and a level's own letter is reachable with the skills owned before it.
- Design backlog (do not build unless asked): wall cling, flame shield, lantern bomb, shadow swap.

## NEW FEATURE 2: SKILL UI AND FEEDBACK

- **Letter pickup flow**: dialog box 1 shows the letter text (speaker "A letter, folded small", location subtitle); dialog box 2 shows NEW SKILL (or SKILL KNOWN on replay), the skill name with its Filipino name, and a one-line description with the control hint. Gameplay is paused during both. Same PixelBox style as all other panels.
- **HUD skill chips** under the level name: one chip per owned active skill (key letter, short label, dark cooldown fill that drains upward, gold outline when ready, bright border while active) and a small badge per owned passive or upgrade. Chips never overlap hearts, flame bar or the pause button and are at least 48 dp tall on touch devices in the touch layout (the chip row can sit larger there).
- **Touch buttons** VEIL and BOLT appear only once learned, positioned above the dash and ward buttons, mirrored in left-handed mode, at least 48 dp.
- **Level Clear box** lists the new skill earned this run beside the clue text.
- **Lola** mentions on first meeting that the hidden letters will teach Tala new things. Add a "Skills" page to How to Play and a skill list screen on the Map (owned skills with descriptions, locked ones shown as "?" with the level that unlocks them).
- Audio: a distinct cue for each active skill and a short fanfare for learning one.

## NEW FEATURE 3: A LONG GAME THAT GETS HARDER

**Length.** Rooms per level: 4, 5, 5, 6, 6, 7, 7, 8, then the Bellkeeper arena (4 rooms plus arena). Target 4 to 6 minutes per level and 45 to 60 minutes for a first full run. Lanterns remain checkpoints in every room. Keep three fragments per level, placed across the first rooms, and add a few optional secret nooks for flame pickups in longer levels.

**Difficulty ramp**, all multipliers data-driven in a `DifficultyCurve` ScriptableObject so they can be tuned without code changes:
- Hazard density: base x (1 + 0.08 x levelIndex) x (1 + 0.05 x roomIndex), capped at 2.4x.
- Enemy density: base x (1 + 0.06 x levelIndex) x (1 + 0.04 x roomIndex), capped at 2.2x.
- Shadow chase delay: shortens 3.5 percent per level, minimum 100 frames-equivalent (about 1.7 s).
- Flood rise speed: +5 percent per level. Enemy speed: +2 percent per level.
- Difficulty mode multipliers (Story, Standard, Nightmare, Endless) apply on top of the curve.
- Content unlocks by act: Act I Anino only; Act II adds roots and crumbling ledges and the flood; Act III adds Tikbalang, Manananggal and bell mechanics. Platform widths narrow and gaps widen slowly by level.
- Skills are the player's growth: later skills make the later levels survivable, so tune so that a player who has all the skills but plays carelessly still fails, and a player with base kit on Story difficulty can still finish.
- Provide a debug overlay (development builds only) showing current multipliers, level index, room index, hazard and enemy counts per room.

## NEW FEATURE 4: SKILL-AWARE LEVEL DESIGN (obstacles must make sense)

A new skill must change the level, not only the player. Do not build long runs of plain platforms. Generate or hand-author obstacles that **use** each skill the player owns, and ramp them level by level.

Principles:
1. **Teach, then test, then twist.** The level that grants a skill is built without requiring it (the letter room must be reachable with earlier skills). Later levels test it; later still, combine it with other skills and hazards.
2. **Skill-built levels.** Build a level from the skills the player owns at load time, so replays get harder with progress. A save with missing skills must never soft-lock: the Spirit or Inverted route always stays completable with the base kit.
3. **Every obstacle has a readable tell** (color glyph, hint toast on first appearance naming the skill, safe ends and an audio cue) and a fair reward.

Obstacle catalog (numbers in `SkillObstacleTuning`, all ScriptableObject driven):
- **Double Jump**: climbs of 4 to 5 tiles (more than a single jump of about 3.9 tiles), a flame reward on the high ledge, chains of crumbling ledges that need a timed second jump, and optional high nooks with secret flame pickups. Probability rises per level.
- **Lambong Veil**: spiked ledges 9 tiles wide (12 once Greater Veil is owned) with one safe tile at each end and a purple glyph; crossing needs the veil's duration (3 s or 5 s). Also place a veil-timed gap where a Manananggal dive and a spike field coincide.
- **Liwanag Bolt**: thorn walls about 130 px tall at a ledge edge toward the next ledge (cannot be jumped, even with double jump); a bolt burns the wall for 10 s. Also sealed root clusters that must be burned, and flame cages opened by shooting a lantern switch.
- **Swift Dash**: more dash-only gaps (about 5 tiles), moving platforms that need a dash to catch, and timed spike lines with a dash-through window.
- **Triple Bolt**: enemy packs (2 to 3 Anino or a Tikbalang guarded by Anino) and double thorn walls that need a wide fan.
- **Pakpak Glide**: 5 tile gaps that need a glide, glide shafts with moving hazards below, and updraft columns in Act III.
- **Heart Charm / upgrades**: raise hazard density slightly to compensate.
- **Boss**: the Bellkeeper fight requires combining skills (veil for the shockwave, bolt only while it is stunned, glide or double jump to reach the bell platforms).

Tests for this feature:
- For each skill obstacle type, a bot proves it can be crossed **with** the skill and that it blocks progress **without** it on the ordinary route while the Spirit/Inverted fallback stays completable. Report counts (crossed / generated) per obstacle type; fail the build below 100 percent for veil spikes and bolt thorns and below 95 percent for jump-based obstacles, and list every failure with level, room and coordinates.
- Statistics test: obstacles using a skill appear only from the intended level onward, density is non-decreasing across levels, and the first-appearance hint fires exactly once per level.
- Screenshot every obstacle type with its tell visible.

## TESTING AND VERIFICATION (in addition to Prompt 4's)

Automated:
- Edit Mode: each skill's tuning numbers load from the asset and match the table; `SkillService` persists, reloads, never duplicates and never removes a skill; `CanBeHurt()` truth table (veil on, dash, invulnerable, none) for every damage source.
- Play Mode, one test per skill: double jump works once per airtime and not on a ground-jump frame; veil blocks spike, root, enemy, water tick, shadow and boss damage for exactly its duration and respects its cooldown; bolt kills an Anino, stuns a Tikbalang, burns a Root for the set time, is stopped by a ledge, costs the right flame, refuses to fire when flame is too low; triple bolt fires three rays; swift dash cost and cooldown; heart charm adds exactly one max heart once and again after reload; glide caps fall speed and ends on release; all mirrored in Inverted gravity.
- Letter flow: picking up a letter pauses gameplay, shows two dialog boxes, grants the skill once, saves immediately, and replay shows "SKILL KNOWN". Skipping typewriter text with rapid taps does not skip the skill box.
- Progression: generate every level at every difficulty with a fixed seed; assert room counts, assert hazard and enemy counts are non-decreasing room to room within a level on average and level to level on average, and assert the multipliers never exceed their caps.
- Completability bot: for every level, with **no skills** (base kit) on Story difficulty, the exit must be reachable; with all skills the exit must also be reachable and faster. Fail on any unreachable required platform.
- No error logs during a scripted run of levels 1 to 9 using the skills.

UI and layout:
- Screenshot the HUD with 0, 2 and all skills, in both normal and left-handed touch layouts, at the same resolutions as Prompt 4. Assert no overlap with hearts, flame bar, objective line, pause button or touch controls, chips readable (text at least 10 px at reference) and touch targets at least 48 dp.
- Screenshot the two letter dialog boxes for every skill; assert the description fits without clipping at every resolution.
- Screenshot the Level Clear box with a skill earned and the Map skill list.

Manual (add to `Tests/MANUAL_QA.md`):
- Play levels 1 to 4 back to back and confirm each new skill is introduced in a safe spot, with a hint on first use; confirm difficulty feels clearly higher at each level and that the last room of a level is harder than the first.
- Pause during veil and during a bolt in flight; resume restores timers and projectiles exactly.
- Quit and relaunch: skills, hearts and cooldown state (cooldowns may reset) behave correctly; deleting the save removes skills.
- Rapid double-tap on VEIL and BOLT buttons; two-finger jump plus bolt on touch.

Definition of done: compile clean, all new and old tests pass, screenshots reviewed, `DECISIONS.md` and `README.md` updated, committed. Report the test table and anything unverified.

## START

Implement in this order and stop after each: (1) `SkillDefinition`, `SkillService`, save, `CanBeHurt()` unification; (2) Double Jump, Veil, Bolt with tests; (3) letter flow and HUD chips and touch buttons with screenshots; (4) the remaining skills; (5) level lengths and `DifficultyCurve` with the progression tests and debug overlay.
