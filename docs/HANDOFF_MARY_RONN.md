# Handoff: Sa Kabilang Pinto to Mary and Ronn

**24 November prototype baseline update:** Read `PRESENTATION_HANDOFF.md` and the root `UPGRADE_AUDIT.md` first for active asset paths and verified evidence. The game remains a working prototype for Mary and Ronn to upgrade. Mary retains art/animation/UI/UX/audio ownership; Ronn retains level design/narrative/QA ownership. Older implementation instructions below are historical where superseded; the team's creative responsibilities remain active.

From Karylle G. Torres (Game Director, Lead Programmer), 2026-11-22. Deadline: **Dec 1, 2026**.

The game is on GitHub (`main`): https://github.com/karyllegt/Sa-Kabilang-Pinto-Game-2026-11-19_18-58-47

**Team change.** Nash Abrenica is no longer in Pangkat Parol. His two roles, **Audio** and **QA Lead**, are split between Mary and Ronn on top of their own roles:

| Member | Own role | Takes over from Nash | In short |
| --- | --- | --- | --- |
| **Mary Rose G. Balcarse** | Pixel Artist, UI | **Audio** (music and sound) | Everything the player sees and hears |
| **Ronn Lester T. Esquierda** | Level Designer, Narrative Writer | **QA Lead** (testing, bug list, release checks) | Everything the player plays and reads, and proof that it works |

Karylle already built a working version of each part (see `docs/PROGRESS_AUDIT.md`). **Your job is not to start over: upgrade and improve what is there**, in your own role, and show what you changed. Every upgrade has to keep the game compiling and the tests passing.

---

## 1. Getting started (both)

1. Install **Git** and **Git LFS** (`git lfs install`; the PNGs, OGGs and screenshots are LFS files).
2. Clone: `git clone https://github.com/karyllegt/Sa-Kabilang-Pinto-Game-2026-11-19_18-58-47.git`
3. Open the folder in **Unity 6000.6.6f1** (Unity Hub > Add project). The first import takes a while.
4. Play it: open `Assets/Scenes/Boot.unity` and press Play (keyboard: arrows or A/D, Space jump, Shift dash, J ward, K veil, L bolt, E interact; a gamepad works too).
5. Read, in this order: `README.md`, `docs/00_PROJECT_CONTEXT.md`, `docs/06_ART_BIBLE.md` (Mary), `docs/02_SKILLS_AND_PROGRESSION.md` (Ronn), `DECISIONS.md`, `docs/PROGRESS_AUDIT.md`.
6. Watch the progress video (`tests/Video/progress.mp4`, sent separately because of its size) to see the game as it plays today.

**Rules that keep the game safe (from README)**
- Stay in the Filipino-folklore world (Tala, Lola, Bellkeeper, Anino, tikbalang, manananggal, balete, well, church bell). Original assets only.
- The project must compile with **zero errors** after every change. Run the tests before you say something is done (section 5).
- Data lives in ScriptableObjects (`Assets/Data`): change numbers and text there, not in code, when you can.
- One UI style (`PixelBox`), one font, the Input System only.
- Work on your own branch and open a Pull Request to `main` (section 6). Do not push straight to `main`.
- Write every choice you make in `DECISIONS.md` (one line: what and why).

---

## 0. Sprint to the Play Store (2026-11-23, 2 to 3 days)

Karylle's call: the game still **looks like a prototype**. Tala is now real 3x art (48 px per unit, hand-pixelled, every animation, in the game); **everything else on screen must be upgraded to the same level in 2 to 3 days**, so the game can go on Google Play with time left to gather reviews and downloads (goal: 500, then 1000). Work in this order and merge small Pull Requests every day; a finished upgrade in the game beats a perfect one in a folder.

| Day | Mary (art, UI, audio) | Ronn (levels, story, QA, release) |
| --- | --- | --- |
| 1 | Creatures and the Bellkeeper at 3x (R4): Anino, tikbalang, manananggal, Lola, boss; same method as Tala (`tools/tala_hand`, 48 PPU in a `*48` folder) | Android build (M9): install the Android module, build the APK, play it on a phone; start `tests/BUGS.md` |
| 2 | Tiles and backdrops per act (R5), doors, lanterns, hazards, letter, flame stars (R6) | Playtest every level, tune `LevelConfig_*` and the curve, rewrite the letters and Lola's lines; Play Console listing (title, short and full description, 8 phone screenshots, 512 px icon, 1024x500 feature graphic, privacy policy page, content rating, data safety form) |
| 3 | UI frame, buttons, HUD, map (R7), music and sound pass (R8), credits | R9 integration check, MANUAL_QA on a phone (touch, save after app kill, 60 fps), signed AAB, internal testing track, then production |

**Getting downloads and reviews the right way.** Google Play removes apps that buy, trade or reward reviews or installs, and fake reviews get deleted. Grow honestly: closed testing with classmates and family first (new personal developer accounts must run a closed test with at least 12 testers for 14 days before production, so start that on day 1), a clear store page with real screenshots and a short trailer from the progress video, posts in school, Filipino gaming and indie game groups, and an in-game "rate us" prompt after Level Clear that asks once and never blocks play.

---

## 2. Mary: Pixel Artist, UI, and Audio

### What exists today
- **Art bible** `docs/06_ART_BIBLE.md`: palette (43 colours, `Assets/Art/Source/palette.txt`), outline and shading rules, sprite sizes, naming. The `ArtRules` test checks every PNG in `Assets/Art/Game/`.
- **Tools**: text-grid sprites (`Assets/Art/Source/**/*.pxl.txt`, built by **Sa Kabilang Pinto > Art > Build Text-Grid Art**), SpriteCook cleanup (**Art > Clean SpriteCook Downloads**), the Tala frame maker `tools/tala_frames/make_tala_frames.js`.
- **Tala (A-5 to A-7, 2026-11-23)**: hand-pixelled at **48x54, 48 px per unit** (3x detail), all 13 animations for the girl and the boy (38 frames each) in `Assets/Art/Game/Characters48/`, played by `PlayerAnimator` from `Assets/Data/Player/TalaAnims_*.asset`. Source: `tools/tala_hand/tala_hero3.js` (the approved hero grids and palette) and `tools/tala_hand/tala_anim.js` (leg poses and one short spec per frame). Preview: `tests/Screenshots/r3/tala48_all_animations.png`. The camera is now 1440x810 at 48 PPU, so the rest of the art can move to 3x too (16 px art still works and shows as 3x3 blocks).
- **Everything else on screen** still uses the prototype art (creatures, Lola, the boss, tiles, backdrops, props, UI frames, the map), except the Act I backdrop, the lantern and the Spirit door samples.
- **Audio (M8)**: mixer (Music, SFX > Game and UI), 5 music loops from the provided OGGs, every gameplay sound, Spirit muffle and Inverted pitch. Numbers in `AudioTuning`; build with **Setup > Run M8 Audio Setup**.

### Your tasks (in order)
1. **Bring the rest of the game to Tala's level** (section 0). Tala herself is done; to touch her up, edit the grids in `tools/tala_hand/tala_hero3.js` (body) or the `LEGS`/`ANIMS` tables in `tala_anim.js`, then run `node tools/tala_hand/tala_anim.js preview <folder>` to look and `node tools/tala_hand/tala_anim.js export Assets/Art/Game/Characters48` plus **Setup > Tala 48 and 3x Camera (A-7)** to put it in the game. Keep 48x54, the bottom-centre pivot, the colours in `Assets/Art/Source/palette_tala48.txt`, no partial alpha.
2. **R4 Creatures, Lola and the Bellkeeper** (`prompts/12_real_assets/R4_creatures_and_boss.md`): Anino, tikbalang, manananggal, Lola, the boss, portraits.
3. **R5 Environment** (`R5_environment_tiles.md`): ledge tiles per act, ground, Act II and III backdrops in the style of `bg_act1_far.png`.
4. **R6 Props, hazards, items, effects** (`R6_props_hazards_fx.md`): doors, spikes, roots, thorns, flame stars, letter, bells, particles.
5. **R7 UI and map** (`R7_ui_and_map_art.md`): PixelBox frame, buttons, HUD icons, the level map.
6. **R8 Audio** (Nash's part, `R8_audio.md`): replace or improve music and sounds with original or properly licensed ones (list every licence in Credits), balance the mix on a phone speaker and on headphones.
7. **UI upgrades**: anything that makes the screens clearer on a phone (bigger touch buttons where the layout audit allows, nicer transitions). Run the layout audit after (section 5).

SpriteCook: free plan only. 26 credits are left; the plan for them is in `docs/ART_MANIFEST.md`. Everything else is drawn as text grids or by hand.

**Done means**: the new art is in the game (not only in folders), `ArtRules` and the layout audit pass, every file is in `docs/ART_MANIFEST.md`, and before/after screenshots are in `tests/Screenshots/<task>`.

---

## 3. Ronn: Level Designer, Narrative Writer, and QA Lead

### What exists today
- **Levels**: 9 levels in 3 acts, generated from a seed by `LevelGenerator` with the settings in `Assets/Data/Levels/LevelConfig_01..09.asset`, the ramp in `Assets/Data/DifficultyCurve.asset`, the skill obstacles in `SkillObstacleTuning`. A bot proves every room can be finished (`LevelBot`, `SkillBot`; report `tests/Screenshots/m7/obstacle_report.md`).
- **Story text**: the letters, the before-stage intros and the level hints in `LevelConfig_*.asset`; Lola's lines, menus, toasts, the Ending in `Assets/Resources/UI/Strings_en.asset` (defaults in `Assets/Scripts/UI/StringDefaults.cs`). The original story is in `docs/03_ORIGINAL_PROPOSAL.md`.
- **Tests**: 170+ EditMode and 49 PlayMode tests, a layout audit of every screen at 5 resolutions, a scripted run of levels 1 to 9, `tests/MANUAL_QA.md`.
- **The progress video tool**: `Assets/Tests/Video/ProgressVideoRun.cs` records the game playing itself (the bot plays through real input).

### Your tasks (in order)
1. **Play every level on Story and Standard** and write what feels unfair, boring or confusing in `tests/PLAYTEST_NOTES.md` (level, room, what happened, what you expected).
2. **Level design upgrades**: tune `LevelConfig_*.asset` and `DifficultyCurve.asset` so difficulty clearly rises level to level; add or change obstacle mixes per act. After every change run the bots (section 5): no level may become impossible.
3. **Narrative upgrades**: rewrite and extend the letters, Lola's lines (first meeting, per-level talk, after the boss), the intros and the Ending so the story of Tala, her Lola and the Bellkeeper reads as one arc. Keep lines short (the dialog box shows about 3 lines). Check every change in the game, not only in the asset.
4. **QA Lead** (Nash's part):
   - Own `tests/MANUAL_QA.md`: run every row on keyboard, gamepad and (once the Android build exists) touch, mark pass or fail with the date.
   - Keep a bug list in `tests/BUGS.md` (severity, steps, expected, actual, who fixes it).
   - Run the full test suites before every merge to `main` and paste the counts in the Pull Request.
   - **R9 integration check** (`prompts/12_real_assets/R9_integration_check.md`) once Mary's art is in.
   - **M9 Android** and **M10 release** (`prompts/09_M9_android.md`, `prompts/10_M10_polish_release.md`) and the final acceptance (`prompts/11_FINAL_ACCEPTANCE.md`): build the APK, test on a real phone (save survives killing the app, touch works, 60 fps).
5. Re-record the progress video after big changes (section 5) so the team can see the game without opening Unity.

**Done means**: your changes are in the game, the bots still finish every level, the suites pass, and the playtest notes and bug list show what you found and fixed.

---

## 4. Working together

- Mary's art goes into the **same prefabs** Ronn's levels use (`Assets/Prefabs/Level`, listed in `LevelPrefabCatalog`). Keep each new sprite on the **same canvas size and pivot** as the one it replaces (art bible section 4) and nothing in the levels moves.
- Ronn tests Mary's work (QA) and Mary checks Ronn's text fits the dialog boxes (UI).
- Talk before you both touch the same file. The usual split: Mary owns `Assets/Art`, `Assets/Audio`, `Assets/Prefabs/UI`, UI scripts; Ronn owns `Assets/Data/Levels`, `Assets/Data/DifficultyCurve.asset`, `Assets/Resources/UI/Strings_en.asset`, `tests/`.

## 5. Checking your work (no Unity window needed)

From the project folder, with the `unity` CLI (Unity Hub's command line) and Unity closed:

| What | Command |
| --- | --- |
| All EditMode tests (includes art rules, generator, bots) | `unity test --mode EditMode --output tests/Results/editmode.xml .` |
| All PlayMode tests (real scenes, full run) | `unity test --mode PlayMode --output tests/Results/playmode.xml .` |
| Only some tests | add `--filter "Skp.Tests.ArtRulesTests"` |
| Rebuild text-grid art | `unity run . -- -executeMethod SkpPixelText.BuildAllBatch` |
| Rebuild Tala's animation sets | `unity run . -- -executeMethod SkpTalaAnimSetup.RunBatch` |
| Tala 48 frames into the game, cameras at 1440x810 | `node tools/tala_hand/tala_anim.js export Assets/Art/Game/Characters48`, then `unity run . -- -executeMethod SkpTala48Setup.RunBatch` |
| Re-record the progress video (about 1 to 2 hours) | `unity test --mode PlayMode --filter "Skp.Tests.Video.ProgressVideoRun" --timeout 7200 --output tests/Results/video.xml .` |

The full PlayMode suite includes the layout audit and takes 20 to 40 minutes. Close every other Unity editor first.

## 6. Git workflow

1. `git checkout main && git pull`
2. `git checkout -b mary/r4-creatures` (or `ronn/level-tuning`, ...)
3. Commit small steps with clear messages; push your branch: `git push -u origin mary/r4-creatures`
4. Open a Pull Request to `main` on GitHub with: what you built, the test counts, before/after screenshots, what you could not check.
5. The other member reviews (Ronn as QA checks every PR), then merge.

Report each task like the milestones in `README.md`: what was built, a test table (check, method, result, evidence), what could not be verified, next step.
