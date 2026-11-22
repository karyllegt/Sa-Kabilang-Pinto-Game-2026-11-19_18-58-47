# Full game prompt: Sa Kabilang Pinto (The Lantern Between Worlds)

Paste everything below the line into a coding agent working on another copy of this game. It describes the whole game as it stands on 2026-11-22 (milestones M1 to M8, R1 to R3 of the Unity build), top to bottom. It tells the agent to **keep its own sprites where they are better** and bring everything else up to this description.

---

You are the engineer on **Sa Kabilang Pinto** ("Beyond the Door", subtitle "The Lantern Between Worlds"), a 2D pixel-art Filipino-folklore platformer for Android (landscape phones), made for a CSEL course by the group Pangkat Parol. Real deadline: Dec 1, 2026.

This project is an older copy of the game. Below is the current, complete design. Bring this project up to it.

## 0. How to work

1. **First, audit.** Compare this project with every section below and write `UPGRADE_AUDIT.md`: for each item, say if it is *missing*, *outdated* (and how) or *already as good or better*. Do this before you change anything.
2. **Keep what is better here.** Some sprites in this project are better than the reference build. **Do not replace a sprite, animation or visual that is already better** with a worse or more generic one. Keep it, and make the rest of the game match its quality and style. If a sprite is better but breaks a rule below (size, palette, pivot), adapt it to the rule instead of throwing it away. List every kept asset in the audit.
3. **Update everything else** to this spec: rules, numbers, screens, text, systems, tests.
4. Work in small steps. The project must build with **zero errors after every step**. Write and run tests for each step before you call it done.
5. Never delete or overwrite files without saying why in `DECISIONS.md`. If something is ambiguous, pick the simplest reading, record it in `DECISIONS.md` and continue.
6. Report each step as: what was built, a test table (check, method, result, evidence), what could not be verified, next step.

**Non-negotiable**
- Stay in the Filipino-folklore world: Tala, Lola, the Bellkeeper, the Anino, tikbalang, manananggal, the balete tree, the well, the church bell, fiesta night, the parol. No generic fantasy swaps.
- Original assets only (or properly licensed and credited). The screen layout was inspired by the Boomerang2D demo: never copy its art, fonts, text or code.
- Data-driven: levels, skills, difficulty, tuning and text live in data assets (ScriptableObjects in Unity), not hard-coded.
- One UI framework, one shared panel style ("PixelBox"), one pixel font. Input through an input-action system (keyboard, gamepad, touch), never hard-coded key polling.

If this project is Unity: Unity 6, URP 2D, uGUI + TextMeshPro, the Input System package, Pixel Perfect Camera, 16 pixels per unit, point filtering, no compression on sprites. Folder map: `Assets/Scripts/{Core,Player,Skills,Dimensions,Enemies,UI,Audio,Save,Levels}`, `Assets/Prefabs`, `Assets/Scenes`, `Assets/Art`, `Assets/Audio`, `Assets/Data`, `Assets/Tests/{EditMode,PlayMode}`.

## 1. The game in one paragraph

Tala (play as a boy or a girl) wakes inside a sealed ancestral house on a distorted fiesta night. A bell keeps calling from above. Tala climbs nine haunted house-towers in three acts to reach the **Bellkeeper**, the spirit guarding the last door. Doors switch between three dimensions: **Ordinary**, **Spirit** (a shadow, the **Anino**, chases along Tala's own recorded path) and **Inverted** (gravity points up). **Lanterns** are checkpoints. Each level hides a **letter**: a piece of the story that also teaches a permanent **skill**, and the exit stays locked until it is found. Later levels are longer and are generated with obstacles that need the skills Tala owns. **Lola**, Tala's grandmother and guide, lives on the level map. Modes: Story, Standard, Nightmare and, after the ending, Endless Nightmare.

## 2. Story and characters

- **Tala**: a small, round-faced Filipino child in a cream camisa with a red sash (girl: red patadyong skirt and long black hair down her back; boy: red shorts, short hair), bare feet, holding up a gold star-shaped **parol** lantern. Light means safety.
- **Lola**: Tala's grandmother, small, lilac baro, white bun, a cane. Greets Tala on the map the first time ("Apo, you came home late. The bell has been calling for you since the fiesta began."), gives hints that change with progress and points to the letters. Five lines on the first meeting, shown once and saved.
- **The Anino**: a living shadow in Tala's own outline with two pale eyes. It replays Tala's path N ticks late in the Spirit dimension.
- **Tikbalang**: tall, horse-headed, long-limbed; patrols ledges.
- **Manananggal**: winged upper half of a woman, bat wings, long hair, no legs; flies and dives from above.
- **The Bellkeeper**: a bronze bell-bodied spirit with a stone face and rope arms, bound to the church tower. Final boss.
- **Acts**: I *The House Wakes* (levels 1 to 3: wood, capiz windows, lamps), II *The Roots Remember* (4 to 6: the balete tree, roots, moss, the blackwater well), III *The Bell Tower* (7 to 9: stone church, candles, bronze bells).
- Levels: 1 Fiesta Night, 2 Behind the Wall, 3 The Missing Staircase, 4 Roots That Remember, 5 The Blackwater Well, 6 The Horseman Trail, 7 Bells Without Hands, 8 The Wingbeat Corridor, 9 The Last Bell.
- Each level opens with a before-stage portrait dialog (the level's name as speaker, a short intro), then a circle wipe into play. Each letter is a short memo in the story's voice ("Come home before the midnight bell..."). The ending plays after the last door: Tala rings the bell, the house is quiet, the fiesta goes on; the save is marked cleared and Endless Nightmare unlocks.

## 3. Player movement (deterministic, 60 Hz)

Run the player on its **own fixed 60 Hz clock**, independent of frame rate (accumulate time, step N ticks, render with interpolation, snap to the 16 px grid). Latch button presses between ticks so none are lost or repeated. Numbers are per tick at 16 px per tile (multiply by 60 for per second):

- Hitbox 10x14 px. Max run 1.95 px/tick; ground acceleration 0.6, air 0.38; friction x0.72 ground, x0.92 air.
- Gravity 0.42 px/tick², terminal fall 6.6. Jump impulse 7.3 (about 3.9 tiles at full hold); releasing early multiplies upward speed by 0.45 once (variable jump). Coyote time 6 ticks, jump buffer 7 ticks.
- **Dash**: 11 ticks at 5.4 px/tick, cooldown 34, costs 20 flame, untouchable while dashing.
- **Ward**: a pulse for 24 ticks, radius 46 px, cooldown 72. Stuns creatures, kills the Anino, stuns the shadow chaser about 190 ticks, parries the Bellkeeper's toll.
- **Hurt**: 1 damage, knockback vy -3.2 and vx 2.4 away from the source, invulnerable 85 ticks (110 on Story), blinking.
- **Flame**: max 100 (+5 per fragment, cap 160), regenerates +1 every 90 ticks; lighting a lantern gives +30 flame and +1 vitality.
- Lives and vitality per difficulty (section 7). At zero vitality Tala dies and respawns at the last lit lantern; at zero lives it is Game Over (Retry, Map). Story has infinite lives.
- One `CanBeHurt()` gate for every damage source (not while invulnerable, dashing or veiled).

## 4. Dimensions

- Masks: Ordinary 1, Spirit 2, Inverted 4. Ledges, hazards, doors and items each have a mask; anything not in the current dimension is drawn as a faint ghost and has no effect.
- **Doors** switch to their target dimension (optionally teleport). **Lanterns** always return Tala to Ordinary.
- **Inverted** flips gravity: Tala falls up, lands on ceilings, jumps down, her sprite flips vertically.
- **Spirit**: the shadow chaser replays Tala's position from N ticks ago (Story 300, Standard 240, Nightmare 180, Endless 200; 3.5 percent shorter per level, minimum 100) and hurts on touch. Ward stuns it; a bolt can stun-hit it. Changing dimension or dying clears its trail.
- Colour meaning is fixed everywhere: **gold = safe or interactive** (lanterns, letter, flame, doors home), **teal = Spirit**, **violet = Inverted**, **red = danger**. Nothing harmful is gold; nothing safe is red.
- Audio follows the dimension: Spirit muffles the music (low-pass), Inverted pitches it down, with a short glide.

## 5. Skills (from the letters, kept forever)

The letter in each of levels 1 to 8 teaches one skill, saved permanently. Replaying a level shows the letter again with a "SKILL KNOWN" box instead of granting it twice. The letter is required to open each exit, so every skill is guaranteed.

| Level | Skill (Filipino name) | Effect | Numbers |
| --- | --- | --- | --- |
| 1 | Double Jump (Hakbang ng Hangin) | jump again once in mid-air, reset on landing | impulse 6.7, not on the same tick as a ground or coyote jump |
| 2 | Lambong Veil | immune to all harm | 180 ticks, cooldown 900 |
| 3 | Liwanag Bolt (Sinag) | lantern-light shot: stuns creatures, kills the Anino, burns roots and thorn walls, stun-hits the shadow | speed 4.4, life 56, cost 8 flame, cooldown 16; burn roots 420 ticks, thorns 600 |
| 4 | Swift Dash (Dali) | cheaper, faster dash | cost 8, cooldown 20 |
| 5 | Heart Charm (Puso) | +1 max vitality | |
| 6 | Triple Bolt (Tatlong Sinag) | the bolt fires a three-ray fan | angles -0.24, 0, +0.24 rad; cost 12, cooldown 22 |
| 7 | Greater Veil | longer veil, faster recharge | 300 ticks, cooldown 600 |
| 8 | Pakpak Glide | hold jump while falling to glide | fall speed clamped to 1.1 |
| 9 | (none) | the boss fight uses all of them | |

Pickup flow: touching the letter freezes play and opens two portrait boxes: the letter itself, then **NEW SKILL** (name, Filipino name, description, controls), which ignores taps for 0.6 s so it cannot be skipped by accident. The HUD shows a chip per skill with a cooldown fill and its key; passives show small badges. The VEIL and BOLT touch buttons appear only once learned. Every skill works mirrored in Inverted. Skill effects: a purple veil ring that flickers in its last second, glide wings, pooled bolt sprites.

## 6. Levels: generated, long, and skill-aware

- A **seeded generator** builds each level from its data asset. Room height 22 tiles, level width 32 tiles. Rooms per level: 4, 5, 5, 6, 6, 7, 7, 8, then level 9 is 4 rooms plus a 19-tile boss arena.
- **Ordinary route**: ledges rise 1 to 3 tiles each, widths 4 to 7 (3 to 7 from Act II). **Spirit route**: rise 1 to 2, widths 3 to 5. A lantern at the start of every room. Three flame **fragments** in the first three rooms, flame stars along the way, the **letter** in the middle room (floor(rooms/2); boss level rooms-2), the **exit door** in the last room, locked until the letter is found (message: "LOCKED - find the clue hidden in this tower first").
- **Hazards and creatures by act**: Act I spikes and the Anino; Act II roots (they grow in 3 phases), crumbling ledges, moving ledges, the rising **black water** in level 5 (rises 1 step per tick, slows Tala, burns 1 every 36 ticks while she is in it), tikbalang from level 6; Act III falling church bells (warning line, then the drop), manananggal from level 8 (they dive).
- **Skill obstacles** (only from skills already owned; the level's own letter room never needs its own skill; the Spirit and Inverted routes always stay possible with the base kit so no save soft-locks):

| Skill | Obstacle | From |
| --- | --- | --- |
| Double Jump | 4 to 5 tile climbs on the ordinary route, flame reward on top | level 2 |
| Lambong Veil | spiked gauntlet 9 tiles (12 with Greater Veil), safe tile at each end, purple glyph | level 3 |
| Liwanag Bolt | thorn wall 16 px wide, 130 px tall on a ledge edge; burn it (stays burnt 10 s) or veil through | level 4 |
| Swift Dash | dash-only gaps | after level 4 |
| Pakpak Glide | 4 to 5 tile gaps that need a glide | level 4 |
| Triple Bolt | creature packs (a second Anino on wide ledges) | level 6 |
| Heart Charm | denser spikes from the ramp | level 5 |

  The first time an obstacle type appears, a hint toast names the skill ("Spiked ledges: raise the Lambong Veil to cross", etc.), once per level.
- **Difficulty ramp**: per level index L, hazards x(1 + 0.08L), enemies x(1 + 0.06L), chase x(1 - 0.035L), flood x(1 + 0.05L), enemy speed x(1 + 0.02L); per room r, hazards x(1 + 0.05r), enemies x(1 + 0.04r); caps 2.4 and 2.2. The difficulty mode multiplies on top.
- Expected play: 4 to 6 minutes a level, 45 to 60 minutes for a first run.
- **Proof of completability**: a bot does a breadth-first search over places Tala can stand, moving with macro-moves (walks, walk-offs, jumps with short and long run-ups and holds, doors) run through the real player simulation. Every room of every level must reach its next lantern (and the letter, and the exit) with the base kit, with all skills, and with the Spirit/Inverted fallback. Skill obstacles are proven both ways: crossable with the skill, blocked without it.

## 7. Difficulty modes

| Mode | Vitality | Lives | Speed | Chase ticks | Flood | Boss HP | Hazards | Enemies |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Story | 6 | infinite | 0.8 | 300 | 0.7 | 8 | 0.6 | 0.6 |
| Standard | 4 | 5 | 1.0 | 240 | 1.0 | 12 | 1.0 | 1.0 |
| Nightmare | 3 | 3 | 1.25 | 180 | 1.4 | 16 | 1.4 | 1.4 |
| Endless | 4 | 3 | 1.15 | 200 | 1.2 | 12 | 1.3 | 1.3 |

Endless Nightmare unlocks after the ending and loops the levels with rising pressure for score.

## 8. The Bellkeeper (level 9)

Lighting the arena lantern wakes it (intro). It cycles **wave** (shockwaves along the floor; jump them), **bells** (bells fall on marked spots) and **toll** (a ring closes in on Tala over 72 ticks; ward as it closes to **parry**: 4 damage and a stun of 210 ticks). While stunned, ward hits deal 1 each and Liwanag bolts deal 1 (bolts do nothing when it is not stunned). A boss HP bar shows at the top. When it falls, the **last door** opens; going through it plays the Ending. Boss music during the fight.

## 9. Screens and UI (one style, phone-first)

Reference resolution 960x540 (pixel art drawn at 480x270, scaled 2x), Scale With Screen Size (match 0.5), safe-area aware, touch targets at least 48 dp, every string from one string table.

- **Title**: logo, a menu box with a gold arrow cursor: New Journey, Continue (greyed, not hidden, without a save), Level Map, Settings, How to Play, Credits.
- **Character select** (girl, boy) and **difficulty select** (Endless locked until the story is cleared).
- **Settings**: music, sound effects, vibration, screen shake, flash, high contrast, left-handed touch layout, touch controls on/off.
- **How to Play**: pages with controls and a Skills page.
- **Credits**: the team, tools and every licence.
- **Level map**: a night town on a scrolling three-act map, nine nodes on a dotted path (locked, ready, cleared; the last node is a bell), a Tala token that walks between nodes, fiesta lantern strings, the moon. Top bar: Back, act name, Skills (the skill list), Story (difficulty toggle), Talk (Lola). Bottom: node name, state, Enter or Replay. Lola stands by the first node.
- **Portrait dialog box**: portrait, subtitle, speaker name, typewriter text (0.022 s a character; a tap completes the line, the next tap advances), Next button.
- **Circle wipe** between scenes (0.35 s close, 0.35 s open, centred on the point of interest); it blocks input while running.
- **HUD**: level name and act subtitle, a row of hearts, the flame bar, "1 / 9 - Story", lives and score, the room banner ("CLIMB TO THE LANTERN ABOVE - room 1 of 4"), skill chips with cooldowns, pause button, toasts.
- **Touch layout**: move left and right on one side, jump, dash, ward, VEIL and BOLT on the other, mirrored by the left-handed setting; hidden while any box is open.
- **Pause**: Resume, Settings, To Lantern, Restart, Quit to Map. **Level Clear**: time, score, fragments x/3, the letter line and the new skill, buttons Next Level, Replay, Map. **Game Over**: Retry, Map. **Ending** screen with Menu.
- Back/Escape always goes up one level and never quits from gameplay; pause is ignored during dialogs and wipes.
- A layout audit checks every screen at 5 resolutions (phone and tablet aspect ratios, with notch safe areas): nothing off-screen, no overlaps, text contrast against the real pixels behind it, touch target sizes.

## 10. Art direction

- 16 px tiles at 16 pixels per unit, point filter, no anti-aliasing, **no partial transparency in the PNGs** (only in-engine effects fade).
- **Full 1 px ink outline (#0a0710) on characters, creatures, items and props**; tiles and backdrops have none. **Two tones per material**, light from the upper left plus Tala's parol. At most 12 colours a sprite (16 for the boss and portraits).
- One shared palette of about 43 colours with fixed meanings: night violets (#241a2e, #3a2f4a), skin (#f0c08a, #c98a5a, #8a5434), hair (#2a1a14), red cloth (#c0392b, #8a2420), cream (#f6e7c1, #d9c49a), gold (#e8b85a, #ffd36a, shadow #a8782e), flame (#ffefb0, #ff9f3a), wood ramp (#3a2410, #5a3820, #8b5a2b, #b07a3e), balete greens, well water, church stone, bronze (#8a5a2a, #c08a44), Spirit teal (#41e0d0, #1f8a80), Inverted violet (#c07dff, #6a3ea0), danger red (#ff5a4a, #a02a22), Lola lilac (#c8a8e0, #8a6aa8).
- **Sizes (canvas, pivot, hitbox)**: Tala 16x18 bottom centre (hitbox 10x14); Anino 12x13 (10x11); tikbalang 20x26 (14x24); manananggal 26x20 (20x14); Bellkeeper 48x56 (40x50); Lola 14x20; flame star 9x11, fragment 7x7, letter 8x9 (pickup 14x14); doors 20x32 (16x28); exit door 24x36 (20x32); falling bell 14x20; lantern 16x40; ledge strip 48x16 (middle 16 px repeats); spike 24x10, gauntlet 72x28; root 16x20 (3 phases); thorn wall 16x130 (whole, burnt); portraits 24x24; backdrop layers 480x270 tiling horizontally. New art keeps the canvas and pivot of what it replaces so nothing in the levels moves.
- **Tala's animations** (both characters, same timing, rates tied to the 60 Hz clock): idle 4 frames (6 fps), run 6 (10 fps), jump rise 2, apex 1, fall 2, land 2, dash 3 (lean and speed lines), ward 3 (parol raised), hurt 2 (white flash), death 5 (kneel and fade), door enter 4 (darken), glide 2 (parol overhead), bolt cast 2 (parol thrust with a flare). Creatures animate at 4 fps, flames at 8 fps. A frame never moves the feet off their pixel row. State priority: death > hurt > dash > ward > door, bolt > glide > rise/apex/fall > land > run > idle; one-shots (land, hurt, door, bolt) start from game events. Animation never feeds back into the simulation, and hit-stop pauses it.
- **Your better sprites**: where this project's sprites are better than this description, keep them and fit them to these canvases, pivots and the palette.
- Every level object is a prefab listed in one catalog (ledges per act, doors, exit, hazards, items, lantern, creatures, falling bell, water, decorations, bolt, effects; boss pieces; map pieces). Views instantiate from the catalog and pool bolts and boss pieces up front, so gameplay never instantiates.

## 11. Audio and game feel

- An audio mixer: Music, SFX (Game, UI), wired to Settings; pause ducks the music and silences game sounds.
- Seamless music loops: menu, Act I, Act II, Act III, boss (crossfades between them; the Ending plays the menu theme).
- A sound for every action: jump, double jump, land, dash, ward, parry, hurt, death, lantern, flame, fragment, letter, skill learned, door, dimension change, veil, bolt, bolt hit, root burn, bell fall, toll, water, Level Clear, Game Over, win, every UI tap.
- Game feel: screen shake, hit-stop on hits and parries, phone vibration and gamepad rumble (both obey Settings), pixel particles for flame, fragments, veil, bolts and burnt thorns.
- Gameplay code allocates 0 bytes per frame (pools, no LINQ or closures per tick).

## 12. Save, input, flow

- Save file (JSON) with: character, difficulty, unlocked level, cleared levels, skills, Lola met, lantern checkpoint per level, settings, cleared flag. It survives killing the app; a corrupt save falls back to a fresh one without errors.
- Continue resumes at the last lit lantern. Scene flow: Boot > Title > Map > Level (> Map); a loader with the circle wipe between them.
- Controls: keyboard (arrows or A/D, Space jump, Shift dash, J ward, K veil, L bolt, E interact, Esc back/pause), gamepad, touch.
- A debug overlay (dev builds) shows the player state, the difficulty ramp numbers and the generator seed.

## 13. Tests (write them as you go)

- Unit tests: player physics against the reference numbers, the generator (same seed, same layout), dimensions, every skill, the difficulty table and curve, save and load (including corrupt files), the string table, art rules (every game PNG uses the palette, has no partial alpha, keeps its canvas and pivot, stays within the colour limit).
- Play tests in the real scenes: new journey to the map and Lola once; enter a level, read the letter, learn the skill, exit, Level Clear, the token walks on; locked nodes; double Enter starts a level once; Back and Escape; wipes block input; pause; Game Over and Retry; every Tala action drives its animation.
- Bots: every room of every level completable (base kit, all skills, Spirit/Inverted fallback); every skill obstacle crossable with its skill and blocked without.
- A scripted run of levels 1 to 9 that ends on the Ending with the save cleared and **no console errors**.
- The layout audit of every screen at 5 resolutions, with screenshots.

## 14. Definition of done

Builds and runs on Android (IL2CPP, ARM64, min API 24, landscape); every screen exists with the layout above; all nine levels completable; every skill and obstacle works; difficulty rises level to level; all tests pass; no console errors in a full scripted run; touch, keyboard and gamepad work; saves survive an app kill; 60 fps on a mid-range phone.

Start with the audit (section 0), then upgrade in this order: core movement and save, dimensions, levels and generator, UI and screens, skills, levels 4 to 9 and the boss, audio and feel, art. Keep the better sprites from this project throughout.
