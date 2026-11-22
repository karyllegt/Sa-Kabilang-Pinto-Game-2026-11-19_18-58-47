# Sa Kabilang Pinto (The Lantern Between Worlds)

2D pixel-art Filipino-folklore platformer for Android, made in Unity 6 (6000.6.6f1) by **Pangkat Parol** for the CSEL course. Real deadline: **Dec 1, 2026**. Repo: https://github.com/karyllegt/Sa-Kabilang-Pinto-Game-2026-11-19_18-58-47

## Team

| Member | Role | Owns |
| --- | --- | --- |
| Karylle G. Torres | Game Director, Lead Programmer | the code base, milestones M1 to M8, R1 to R3 (built with Claude Code as the engineer) |
| Mary Rose G. Balcarse | Pixel Artist, UI, **and Audio** (from Nash) | everything the player sees and hears: art (R3 polish, R4 to R7), UI, music and sound (R8) |
| Ronn Lester T. Esquierda | Level Designer, Narrative Writer, **and QA Lead** (from Nash) | everything the player plays and reads, and proof it works: levels, story text, testing, bug list, R9, Android (M9 to M11) |

Nash Abrenica is no longer in the group; his roles are split as above.

## Mary and Ronn: start here

1. **`docs/HANDOFF_MARY_RONN.md`**: setup (Git LFS, Unity version), your task lists in order, how to test without opening Unity, the Git branch and Pull Request workflow.
2. **`docs/PROGRESS_AUDIT.md`**: what is done, partial and not started, with evidence and the owner of each remaining item.
3. **`DECISIONS.md`**: every choice made so far and why (newest at the bottom; the art direction is A-1 to A-4).
4. Play it: open `Assets/Scenes/Boot.unity` in Unity and press Play (keyboard: arrows or A/D move, Space jump, Shift dash, J ward, K veil, L bolt, E interact, Esc back or pause; a gamepad works too).

### Where is what

| Looking for | Go to |
| --- | --- |
| Scenes (Boot > Title > Map > Level) | `Assets/Scenes/` (dev rooms in `Assets/Scenes/Dev/`) |
| Game code | `Assets/Scripts/` : `Player` (movement, animation), `Levels` (generator, level runner, bots), `Enemies` (creatures, the Bellkeeper), `Skills`, `Dimensions`, `UI` (every screen, HUD, map), `Audio`, `Save`, `Core` |
| Level settings (Ronn) | `Assets/Data/Levels/LevelConfig_01..09.asset`, `Assets/Data/DifficultyCurve.asset`, `Assets/Data/DifficultyTable.asset` |
| Story text: letters, intros, hints (Ronn) | `Assets/Data/Levels/LevelConfig_*.asset` |
| Menus, Lola's lines, toasts, the Ending (Ronn) | `Assets/Resources/UI/Strings_en.asset` (defaults in `Assets/Scripts/UI/StringDefaults.cs`) |
| Player feel and skill numbers | `Assets/Data/PlayerTuning.asset`, skill assets made by **Sa Kabilang Pinto > Setup** |
| Art rules and palette (Mary) | `docs/06_ART_BIBLE.md`, `Assets/Art/Source/palette.txt`, `docs/ART_MANIFEST.md` (every art file and its source) |
| New game art (Mary) | `Assets/Art/Game/` (in the game), `Assets/Art/Source/` (text-grid sources); the prototype art in the other `Assets/Art/` folders is the fallback |
| Art tools (Mary) | `tools/tala_frames/` (16x18 Tala), `tools/tala_rig/` (48x54 Tala, in progress), `tools/creature_frames/` (R4 key frames), **Sa Kabilang Pinto > Art** menu |
| Tala's animations | `Assets/Data/Player/TalaAnims_girl.asset`, `TalaAnims_boy.asset` (rebuild: **Setup > Tala Animation (R3)**) |
| Level, boss and map objects | `Assets/Prefabs/Level`, `Boss`, `Map` (listed in `Assets/Resources/Level/LevelPrefabCatalog.asset`), player in `Assets/Prefabs/Player`, UI in `Assets/Prefabs/UI` |
| Music and sounds (Mary) | `Assets/Audio/`, the mixer and `AudioTuning`/`FeelTuning` assets (rebuild: **Setup > Run M8 Audio Setup**) |
| Tests | `Assets/Tests/EditMode`, `Assets/Tests/PlayMode`, the progress video in `Assets/Tests/Video` |
| Manual QA checklist (Ronn) | `tests/MANUAL_QA.md` |
| Screenshots per milestone | `tests/Screenshots/<milestone>/` (layout audit: `m6`, levels: `m7`, art: `r2`, `r3`, `r4`) |
| Milestone and task prompts | `prompts/` (M1 to M11), `prompts/12_real_assets/` (R1 to R9), `prompts/FULL_GAME_PROMPT.md` (the whole game in one prompt) |
| Original design and story | `docs/00_PROJECT_CONTEXT.md`, `docs/03_ORIGINAL_PROPOSAL.md`, the browser prototype `reference/prototype/index.html` |

**Current art direction (2026-11-22):** new art is drawn at **3x detail (48 pixels per unit)** so it matches the quality bar of the detailed prototype sprites, with the camera to move from 480x270 to 1440x810; gameplay sizes do not change. No paid or generated art: every sprite is made by our own tools or by hand. See `DECISIONS.md` A-1 to A-4.

## For Claude Code (the engineer)

Read these first, in this order:
1. `docs/00_PROJECT_CONTEXT.md` : concept, design essentials, skills table, decisions.
2. `docs/01_BUILD_SPEC.md` : UI and layout spec, milestones, testing and verification rules.
3. `docs/02_SKILLS_AND_PROGRESSION.md` : letter skills, long levels, difficulty ramp, skill-aware obstacles, and their tests.
4. `docs/PROTOTYPE_NUMBERS.md` : exact numbers to port from the browser prototype.
5. `docs/03_ORIGINAL_PROPOSAL.md` : the original proposal text (story, characters, concept). The docs above win on conflicts. The source .docx is in `reference/`.
6. `docs/04_ASSET_GUIDE.md` : the provided sprites and audio in `reference/assets/`, the copy map into Unity, the palette, the gaps you must fill and known layout issues to fix.
7. `reference/prototype/index.html` : the playable browser prototype. It is the behavior and visual spec. Open it, read the code, port the numbers. It is NOT Unity code.

## Environment
You run on the developer's own computer, with Unity installed. Use the `unity:unity-cli` skill to drive the Editor, run builds and run tests from the command line (EditMode and PlayMode through the Test Runner in batch mode). Use the `unity:ui`, `unity:2d-pixel-perfect` and related skills when they apply. Node and Chrome may be available: `tests/prototype/verify_prototype.js` runs the prototype's 20 behavior checks and can be used as an oracle (Unity behavior should match it).

## Provided assets (use them)
`reference/assets/` holds 56 sprite PNGs (player boy and girl, enemies, boss, Lola, doors, hazards, tiles, backgrounds, portraits, UI pieces), 18 SFX and 5 music tracks as OGG, and 32 reference screenshots (desktop and phone) of every screen. These are the game's v1 art and audio: import them (see `docs/04_ASSET_GUIDE.md`), do not replace them with generic placeholders, and create anything missing in the same style.

## Non-negotiable rules
- Stay inside the game's Filipino-folklore context (Tala, Lola, Bellkeeper, Anino, tikbalang, manananggal, balete, well, church bell). No generic fantasy swaps.
- Original assets only. The layout was inspired by the Boomerang2D demo; never copy its art, fonts, text or code.
- The project must compile with zero errors after every step. Run the tests before saying anything is done.
- Never delete or overwrite files I did not ask you to. Prefer new files. State project-settings changes before making them.
- One UI framework everywhere (consult the `unity:ui` skill first). One shared `PixelBox` panel style. Input System package only; no hardcoded `Input.GetKey`.
- Data-driven: levels, skills, difficulty curve and tuning live in ScriptableObjects.
- Commit after each milestone. Do not push unless I say so.
- If something is ambiguous, pick the simplest reading, record it in `DECISIONS.md`, continue.
- Report every milestone as: what was built, test table (check, method, result, evidence), what could not be verified, next step.

## Game in one paragraph
Tala (boy or girl) climbs nine haunted house-towers in three acts to reach the Bellkeeper. Doors switch between three dimensions (Ordinary, Spirit, Inverted/gravity up); in Spirit a shadow (Anino) chases along her recorded path. Lanterns are checkpoints. Each level hides a letter that is a story memo and teaches a permanent skill (double jump, Lambong veil, Liwanag bolt, swift dash, heart charm, triple bolt, greater veil, glide). Later levels are longer and generated with obstacles that use the skills you own. Lola, the guide, lives on the level map. Modes: Story, Standard, Nightmare, Endless Nightmare.

## Definition of a good state (acceptance)
The game is only done when all of this is true and shown with evidence: it builds and runs on Android; every screen in `reference/assets/screenshots/` exists in Unity with the same or better layout (and the known layout issues are fixed); all nine levels are completable; every skill and every skill obstacle works; difficulty rises level to level; all tests pass; no console errors in a full scripted run; touch, keyboard and gamepad all work; saves survive an app kill.

## Folder map
See **Where is what** above. New folders follow the same pattern: code in `Assets/Scripts/<area>`, data in `Assets/Data`, art in `Assets/Art/Game/<group>`, tests in `Assets/Tests/{EditMode,PlayMode}`, evidence in `tests/Screenshots/<task>`.

## Status
See `DECISIONS.md` and git log. Milestones M1 to M10 are in `prompts/`.

- **M1 Foundation: done (2026-11-20).** Assets imported, Input System actions (keyboard, gamepad, touch), SaveService, Boot/Title/Map/Level flow with a wipe hook, pixel-perfect camera, data-asset shells, debug overlay. 29 EditMode + 2 PlayMode tests pass. Re-run the generated setup with **Sa Kabilang Pinto > Setup > Run M1 Setup** (or `-executeMethod SkpProjectSetup.RunAll`).
- **M2 Player feel: done (2026-11-20).** Deterministic 60 Hz player port (run, variable jump, coyote, buffer, dash, ward, hurt, death, lanterns, lives), frame-rate independent, `PlayerTuning` per second, `DifficultyTable`. Try it: open `Assets/Scenes/Dev/TestRoom.unity` and press Play. 53 EditMode + 3 PlayMode tests pass.
- **M3 Dimensions and Anino: done (2026-11-21).** Ordinary/Spirit/Inverted bitmask on ledges, hazards and doors; doors switch dimension (optional teleport); lanterns return to Ordinary; Inverted flips gravity, landing, jump and sprite; the Anino shadow replays the path N ticks late, hurts through `CanBeHurt()`, ward stuns it 190 ticks; death resets everything. Try it: `Assets/Scenes/Dev/DimensionRoom.unity`. 77 EditMode + 4 PlayMode tests pass.
- **M4 Level framework, levels 1 to 3: done (2026-11-21).** Seeded generator ported from the prototype and proven identical against 30 prototype layouts; `LevelConfig`/`SkillDefinition` assets for all 9 levels; lanterns save checkpoints and Continue resumes there; letter unlocks the exit; flame stars, fragments, spikes, roots, thorns, crumbling and moving ledges, Anino and tikbalang patrols. A bot completes levels 1 to 3 on Standard with the base kit (zero hits on the gentlest path). Play: Title > Map > Jump. 98 EditMode + 5 PlayMode tests pass.
- **M5 UI layer: done (2026-11-21).** All screens in uGUI with one PixelBox style and one font: title menu, character and difficulty select, settings, how to play, credits, the level map (walking token, node states, Lola and her first meeting, difficulty toggle), portrait dialogs with typewriter, before-stage intro then circle wipe, HUD, toasts, the letter dialog, pause, Level Clear, Game Over, Ending. Strings in one table, safe area, 48 dp targets, a new thumb layout with a left-handed mirror. A layout audit checks every screen at 5 resolutions (screenshots and contact sheets in `tests/Screenshots/m5`). 110 EditMode + 13 PlayMode tests pass. Rebuild the UI with **Sa Kabilang Pinto > Setup > Run M5 UI Setup**.
- **M6 Skills: done (2026-11-21).** Eight letter skills from `SkillDefinition` and `SkillTuning` assets: Double Jump, Lambong Veil, Liwanag Bolt (kills Anino, stuns creatures and the shadow, burns roots and thorns), Swift Dash, Heart Charm, Triple Bolt, Greater Veil, Pakpak Glide, all mirrored in Inverted. `SkillService` saves them; one `CanBeHurt()` gate for every damage source. Letter then NEW SKILL (or SKILL KNOWN) boxes, HUD chips with cooldowns, VEIL and BOLT touch buttons, Level Clear skill line, Skills page in How to Play, skill list on the map, Lola's skill lines, generated icons, effects and sounds. 119 EditMode + 38 PlayMode tests pass; layout audit 36,642 checks, 0 problems (`tests/Screenshots/m6`).
- **M7 Levels 4 to 9: done (2026-11-21).** Rising black water (level 5), manananggal that dive, Act III falling bells, the Bellkeeper (wave, bells, toll, parry with ward, stun, ward and bolt while stunned, last door, Ending, cleared saved), Endless loops. `DifficultyCurve` and `SkillObstacleTuning` drive a generator that builds obstacles truly needing their skill (double-jump climbs, veil gauntlets under a thorn curtain, solid thorn walls, dash gaps, triple-bolt packs, glide gaps); a bot proves each one (report: `tests/Screenshots/m7/obstacle_report.md`). All nine levels pass room-by-room bots with the base kit, with all skills and with the Spirit/Inverted fallback; a scripted run of levels 1 to 9 ends on the Ending with no errors. Screenshots in `tests/Screenshots/m7`. Debug overlay shows the ramp.
- **M8 Audio and juice: done (2026-11-22).** An Audio Mixer (Music, SFX > Game and UI) wired to the Settings toggles; pause ducks the music and silences game sounds. Seamless music loops per act plus menu and boss (crossfaded from the provided OGGs). Spirit muffles and Inverted pitches the music down, with a short glide. Every gameplay sound (new: land, letter, toll, parry), screen shake, hit-stop, vibration and gamepad rumble (both honour the settings), and pixel particles for flame, fragments, veil, bolts and burnt thorns. Game code allocates 0 B per frame. Numbers in `AudioTuning` and `FeelTuning`. Rebuild with **Sa Kabilang Pinto > Setup > Run M8 Audio Setup**.
- **R1 Level objects as prefabs: done (2026-11-22).** Every level, boss and map object is a prefab (`Assets/Prefabs/Level`, `Boss`, `Map`) listed in `LevelPrefabCatalog` (Resources/Level); the views instantiate from it and pool bolts and boss pieces up front. Nothing moved or resized (proven against a pre-R1 capture of levels 1 to 9, the boss and the map); a play-mode bug that drew lantern posts and the flood water white is fixed. Rebuild with **Run M1 Setup** or **Setup > Rebuild Level Prefabs (R1)**. Next upgrade prompts: `prompts/12_real_assets/`.
- **R2 Art bible and style: done (2026-11-22).** `docs/06_ART_BIBLE.md` (identity, palette, outline and shading, sizes that keep every canvas), text-grid art (`Assets/Art/Source`, **Art > Build Text-Grid Art**), the SpriteCook cleanup (**Art > Clean SpriteCook Downloads**), `docs/ART_MANIFEST.md` with the credit plan, and `ArtRulesTests`. Picked style: Tala option A and the SpriteCook Act I backdrop (`tests/Screenshots/r2/art_bible_sheet.png`). 14 of 40 free credits spent. The game does not use the new art yet (R3 onwards).
- **R3 Tala animation: done (2026-11-22).** 13 animations for the girl and the boy (idle, run, jump rise, apex, fall, land, dash, ward, hurt, death, door, glide, bolt cast; 76 frames) in the R2 style, built from shared body parts (`tools/tala_frames/make_tala_frames.js` writes the text grids), played by `PlayerAnimator` from `Assets/Data/Player/TalaAnims_*.asset` once per simulation tick. Sheet: `tests/Screenshots/r3/tala_contact_sheet.png`. Rebuild with **Setup > Tala Animation (R3)**.
- **Progress video (2026-11-22).** `Assets/Tests/Video` records the game playing itself (bot-driven real input, UI and audio) to `tests/Video/progress.mp4`; run it on its own (see `docs/HANDOFF_MARY_RONN.md`).
- **Handoff (2026-11-22).** Nash left the group; Mary (Pixel Art, UI, and Audio) and Ronn (Level Design, Narrative, and QA Lead) continue from here: `docs/HANDOFF_MARY_RONN.md`. What is done and what is left: `docs/PROGRESS_AUDIT.md`.
