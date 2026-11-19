# Build spec: full game, UI, milestones, testing

This is a specification file. Claude Code reads it when a milestone prompt tells it to.


## ROLE AND GOAL

You are the sole developer of **Sa Kabilang Pinto (The Lantern Between Worlds)**, a 2D pixel-art Filipino-folklore platformer for Android, built in Unity (2D, URP 2D or Built-in, whichever the project already uses; do not switch). Build the complete game: 9 levels, 3 acts, the Bellkeeper boss, menus, level map, dialog, save system, audio, and touch controls. There is one developer. Ignore any team roles or timeline in the docs; work in the milestone order below.

Reference: the browser prototype at `reference/prototype/index.html` is the behavioral and visual spec; `docs/PROTOTYPE_NUMBERS.md` lists the numbers to port. Read both before Milestone 2 and port numbers rather than guessing.

## HARD RULES

1. Stay inside the game's context: Filipino folklore (Tala, Lola, the Bellkeeper, Anino shadow, tikbalang, manananggal, balete roots, the old well, the church bell). No generic fantasy substitutions.
2. Original assets only. A layout reference was taken from the Boomerang2D demo (menu box, level map with nodes, portrait dialog box, popup boxes, heart HUD, circle wipes). Do NOT copy its art, fonts, text or code.
3. Never leave the project in a non-compiling state. After every milestone, the project must compile with zero errors and zero new warnings, and the tests below must pass.
4. Do not delete or overwrite files I did not ask you to. Prefer new files. Never edit `ProjectSettings` blindly; state the change first.
5. Commit after each milestone with a clear message. Do not push unless I say so.
6. If a requirement is ambiguous, choose the simplest reading, write the choice in `DECISIONS.md`, and continue.

## CORE DESIGN (must match)

- Protagonist **Tala** (choose Boy or Girl variant at start). 2D side-scroller, 16 px tiles, 480x270 reference resolution, Pixel Perfect Camera, point filtering, no compression artifacts on sprites.
- **Dimensions** (bitmask): Ordinary (1), Spirit (2), Inverted (4). Doors switch dimension. Spirit dimension spawns the **Anino** shadow chaser that follows Tala's path with a delay. Inverted flips gravity/orientation rules as in the prototype.
- **Lantern/flame** resource and **vitality** shown as hearts. Fragments (3 per level), clues, checkpoints.
- 9 levels: Fiesta Night, Behind the Wall, The Missing Staircase, Roots That Remember, The Blackwater Well, The Horseman Trail, Bells Without Hands, The Wingbeat Corridor, The Last Bell (Bellkeeper boss).
- Difficulties: Story, Standard, Nightmare, plus Endless Nightmare loop mode. Vitality 6/4/3/4.
- Ending: Tala strikes the bell; Lola says "Salamat, apo."

## LAYOUT AND UI SPEC (the "best layout" requirement)

Use **UI Toolkit or uGUI, but pick one and use it everywhere** (consult the `unity:ui` skill first). One Canvas/UIDocument per screen group, scaled with Scale With Screen Size, reference 960x540, match 0.5, safe-area handling for notches and gesture bars. Every tappable target is at least 48 dp (about 9 mm). Text is readable at 5.5-inch phone size: body at least 14 px at reference, never below 10 px anywhere.

Visual language (shared by all panels): dark plum panel (#14101c), double gold border (#e8b85a outer, #6b4a22 inner), pixel font (an original or open-license pixel font such as Press Start 2P under OFL; record the license in `CREDITS`). Create a single `PixelBox` 9-slice sprite + prefab and reuse it. No panel may use a different border style.

Screens (each is a prefab/UXML with a controller script):

1. **Title**: logo, tagline "The Lantern Between Worlds", a left-aligned **menu box** with: New Journey, Continue (disabled if no save), Level Map, Settings, How to Play, Credits. Selected row shows a gold arrow and highlighted row. Keyboard/gamepad up/down + confirm; touch tap. Continue is greyed, not hidden.
2. **Character select** (Boy/Girl) then **Difficulty select** (Story/Standard/Nightmare/Endless), each with Back.
3. **Level Map (overworld)**: horizontally scrolling map across the 3 acts (Act I: nipa huts and parol string lights; Act II: balete trees and the old well; Act III: the church and bell tower). 9 level nodes connected by a dotted path. A Tala token walks the path between nodes when selecting. Node states: locked (dim, shows "LOCKED: clear level N first"), ready (glowing), cleared (gold). Top bar: Back, Act name panel, Difficulty toggle, Talk. Bottom info panel: level number and name, act and state, ENTER/REPLAY button. Camera follows the token with smoothing. Lola stands at the left end with a "!" marker when she has something new to say. Tapping a node selects it; tapping the selected node enters it.
4. **Dialog box** (used for Lola, level intros, clue finds, Anino/Bellkeeper lines): bottom of screen, portrait frame on the left (square), speaker name in gold, location subtitle in teal, body text with typewriter effect (tap/confirm completes the line first, then advances), NEXT/BEGIN/CONTINUE button. Never overlaps the pause button.
5. **Before-stage**: dialog with the level name and intro text, then a circle wipe into the level.
6. **After-stage (Level Clear box)**: title, level name, three stat tiles (Time, Score, Fragments), the clue found as a quoted line, buttons Next Level, Replay, Map. After the final level, show the Ending screen instead.
7. **HUD**: hearts (row, filled and empty states), flame bar, current dimension label, level label, score, objective line, boss bar (boss level only), pause button top-right. Must not overlap the safe area or the touch controls.
8. **Pause**, **Game Over** (Retry, Map), **Settings** (music, SFX, vibration, screen shake, high-contrast, left-handed touch layout), **How to Play**, **Credits**.
9. **Popup/toast** for hints, locked doors, lantern lit, fragment collected: small boxed message, auto-dismiss in 2.5 s, one at a time (new replaces old).
10. **Circle wipe** transition between every major screen change (about 0.35 s close, 0.35 s open), centered on the relevant point (the node when entering a level). Input is blocked during a wipe.

Touch controls: left/right buttons on the left, Jump and Door/Interact on the right, a Ward/Dash button; all semi-transparent, remappable to left-handed layout. Input must also work with keyboard (Arrows/WASD, Space/Z jump, E/Enter interact) and gamepad. Use the **Input System** package, one action map, no hardcoded `Input.GetKey`.

## MILESTONES (the per-milestone prompts are in `prompts/`; do them in order)

M1 Foundation. M2 Player feel. M3 Dimensions and Anino. M4 Level framework, hazards and levels 1 to 3. M5 UI layer (all screens, dialog, level map, Lola, wipes). M6 Skills system, letters, HUD chips, touch buttons. M7 Levels 4 to 9, enemies, Bellkeeper, skill-aware obstacles, difficulty curve and longer levels. M8 Audio and juice. M9 Android build and performance. M10 Polish and release candidate. Each milestone ends with the full test run and a commit.

## TESTING AND VERIFICATION (mandatory)

Do not mark a milestone done until its checks pass. Report results as a table: check, method, result, evidence.

### A. Automated tests (Unity Test Framework)

Edit Mode:
- SaveService: round-trip, missing file creates defaults, corrupt JSON recovers to defaults and keeps a `.bak`, version migration, unlock count never decreases, `cleared` only set by finishing level 9.
- Level unlock logic: node i unlocked iff i < unlocked; cleared iff i < unlocked-1 or (i == 8 and cleared).
- Difficulty table: vitality 6/4/3/4, Endless loop scaling is monotonic.
- Dimension bitmask: every object/tile resolves visibility correctly for Ordinary, Spirit, Inverted, and combined masks.
- Dialog data: every LevelConfig has a non-empty name, intro, clue; no text exceeds the dialog box line budget (measure with TMP/preferred height at reference resolution).

Play Mode:
- Boot -> Title -> New Journey -> Character -> Difficulty -> Map reaches the Map scene with node 1 selected and Lola's first-meeting dialog shown exactly once.
- Entering a level plays the before-stage dialog, then wipe, then gameplay; completing it shows the Level Clear box with correct Time/Score/Fragments and unlocks the next node; Map shows the token walking to the new node.
- Game Over -> Retry restarts at the right checkpoint with the right vitality for the difficulty; Game Over -> Map works.
- Pause during play freezes physics, timers, music-driven events and Anino; Resume restores them; pause during a wipe or dialog is ignored safely.
- Each of the 9 levels is **completable**: write a bot or recorded input test that reaches the exit from the spawn in every level on Standard (use waypoint or input-replay; fail the test if the exit is unreachable). Also test that fragments are collectable and no required path is blocked in Spirit or Inverted.
- Boss: Bellkeeper can be defeated, boss bar goes to zero, ending screen shows, `cleared` saved true.
- Locked node cannot be entered (button disabled and tapping plays the locked cue).
- No exceptions or error logs during a full scripted run of all 9 levels (fail the test on any `LogType.Error/Exception`).

### B. UI and layout verification

Script a screenshot test that captures every screen at these resolutions and aspect ratios: 1280x720 (16:9), 2400x1080 (20:9), 2340x1080 with a simulated notch safe area, 1024x768 (4:3), and 800x480. For each screenshot assert programmatically, and also inspect visually:
- No text is clipped or overflows its panel; no overlapping interactive elements (compute RectTransform/worldBound overlaps among buttons, HUD, pause button and touch controls and fail on any).
- All interactive targets are at least 48 dp at the physical device size (use `Screen.dpi`, with a fallback of 160).
- Everything important stays inside the safe area.
- Dialog box never covers the pause button; level-clear box fits at every resolution.
- Contrast: body text against panel at least 7:1; gold titles at least 4.5:1.
Save the screenshots to `Tests/Screenshots/` and show me the contact sheet.

Layout rules to verify by code review: single `PixelBox` style everywhere, one font asset, no magic pixel offsets (anchors and layout groups only), all strings referenced from one table (ready for localization), no UI built per-frame.

### C. Manual checklist (generate `Tests/MANUAL_QA.md` and tick it as you go)

For keyboard, gamepad and touch separately: menu navigation, tap-to-complete typewriter then advance, map selection by tap and by arrows, enter/replay a cleared level, rapid double-tap on ENTER does not start the level twice, rotate the device during a dialog (landscape-lock holds), background/resume the app during play and during a wipe (no soft lock), low-vitality and low-flame states are clearly visible, audio ducks correctly on pause, vibration and screen shake toggles actually work, left-handed layout mirrors without overlap.

### D. Edge cases that must be proven

- Killing the app mid-level and relaunching: Continue resumes at the last checkpoint or the map, never a broken state.
- Deleting the save file: the game starts clean, Lola's first meeting shows again.
- Spamming Jump/Interact during dialogs and wipes does not leak input into gameplay.
- Changing difficulty on the map affects the next level started, not a level in progress.
- Back (Android back button / Escape) behaves consistently: closes dialogs, goes up one screen, pauses in gameplay, never quits unexpectedly.
- Performance: profile on a device or the Android emulator; report average and 1% low FPS, GC alloc per frame in gameplay (target 0), and APK size.

### E. Definition of done

A milestone is done only when: compile is clean, all automated tests pass, the screenshot/layout checks pass, the relevant manual checks are ticked, `DECISIONS.md` and `README.md` are updated, and you have committed. At the end of each milestone give me: what was built, the test table, anything you could not verify and why, and the next milestone.

## START

Begin with M1. First read `README.md`, list the existing project structure, and tell me what packages and render pipeline the project already uses. Then proceed.
