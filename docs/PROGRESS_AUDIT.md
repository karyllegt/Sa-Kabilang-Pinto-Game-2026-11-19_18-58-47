# Progress audit: Sa Kabilang Pinto

Snapshot of 2026-11-22, made at the handoff from Karylle to Mary and Ronn; updated 2026-11-23 for Tala 48 and the Play Store sprint (`docs/HANDOFF_MARY_RONN.md` section 0) (`docs/HANDOFF_MARY_RONN.md`). Evidence is in the repo (tests, screenshots, git log). Deadline: Dec 1, 2026.

Legend: **Done** = built, tested and committed. **Partial** = some of it works, the rest is listed. **Not started** = nothing built yet.

## 1. Milestones (prompts/)

| # | Milestone | Status | Evidence | What is left |
| --- | --- | --- | --- | --- |
| M1 | Foundation (assets, input, save, scene flow, pixel camera) | Done | commit history, `ProjectSetupTests` | |
| M2 | Player feel (60 Hz port, jump, dash, ward, lives) | Done | `PlayerSimTests`, `Assets/Scenes/Dev/TestRoom.unity` | |
| M3 | Dimensions and the Anino | Done | `DimensionTests`, `DimensionRoom.unity` | |
| M4 | Level framework, levels 1 to 3 | Done | `LevelGeneratorTests` (30 prototype layouts identical), bots | |
| M5 | UI layer (every screen, map, dialogs, HUD) | Done | layout audit 39,217 checks, 0 problems (`tests/Screenshots/m6`) | |
| M6 | Eight letter skills | Done | `SkillTests`, `SkillPlayModeTests` | |
| M7 | Levels 4 to 9, the Bellkeeper, skill obstacles, difficulty curve | Done | `M7Tests`, `FullRunTests`, `tests/Screenshots/m7/obstacle_report.md` | |
| M8 | Audio and juice | Done | `M8AudioFeelTests`, `AudioFeelPlayModeTests` | Better music and sounds: R8 |
| M9 | Android build and performance | **Not started** | | Everything (Ronn, QA). Needs disk space for Android support; see `DECISIONS.md` |
| M10 | Polish and release candidate | **Not started** | | Everything (Ronn, QA, with Mary for visual bugs) |
| M11 | Final acceptance | **Not started** | | Everything (Ronn, QA) |

## 2. Real-asset upgrade (prompts/12_real_assets/)

| # | Task | Status | Evidence | What is left (owner) |
| --- | --- | --- | --- | --- |
| R1 | Level objects as prefabs | Done | `R1PrefabTests`, `R1PrefabBaselineTests`, `tests/Screenshots/r1` | |
| R2 | Art bible and style | Done | `docs/06_ART_BIBLE.md`, `ArtRulesTests`, `tests/Screenshots/r2` | |
| R3 | Tala animation | **Done (3x art, 2026-11-23)** | `R3TalaAnimTests` (48x54 frames at 48 PPU), `R3TalaAnimPlayModeTests`, `tests/Screenshots/r3/tala48_all_animations.png` | New character select cards, map token and portraits at 3x (Mary) |
| R4 | Creatures, Lola, the Bellkeeper | **Not started** | | Everything (Mary) |
| R5 | Environment, tiles, backdrops | **Partial** | Act I far backdrop made (not in the game yet) | Tiles per act, Act II and III backdrops, wiring (Mary) |
| R6 | Props, hazards, items, effects | **Partial** | Lantern and Spirit door samples (not in the game yet) | Everything else and wiring (Mary) |
| R7 | UI and level map art | **Not started** | | Everything (Mary) |
| R8 | Music and sound | **Not started** | M8 audio works with the provided OGGs | Original or licensed music and sounds, mix (Mary, Nash's part) |
| R9 | Integration and clean-up | **Not started** | | Everything, after R4 to R8 (Ronn, QA) |

### 2a. R3 detail

- **Done:** `PlayerAnimator` (13 states from the simulation, one step per tick, hit-stop pauses it, 0 B per frame), `PlayerAnimSet` data per character, the setup step (**Setup > Tala Animation (R3)**), the Player prefab wired, 76 frames (girl and boy) at 16x18 built from shared parts, tests (state table, one-shots, sets, prefab, every action in the real Level scene for both characters).
- **Art direction changed after R3 (DECISIONS A-1 to A-4):** you set the quality bar at the detailed sprites of the old Codex copy and chose 3x detail (48 px per unit, camera 1440x810) with no SpriteCook. The 16x18 frames are the interim set and the fallback.
- **Tala 48 in the game (2026-11-23, A-5 to A-7):** the hero frames (girl and boy, parol unlit and lit) were approved area by area, then every animation was drawn from them by `tools/tala_hand/tala_anim.js` (38 frames per character, lit parol) and wired in at 48 PPU; every scene camera is now 1440x810 at 48 PPU; `ArtRulesTests` check the frames against `palette_tala48.txt`. This replaces the rig and the interim 16x18 set (kept as the fallback).
- **R4 key frames** (16 px, approved) are in `reference/art_samples/r4` and `tests/Screenshots/r4/key_frames.png`; they are superseded by the 3x direction and serve as design references for the 3x creatures.

## 3. Acceptance (README "Definition of a good state")

| Requirement | Status |
| --- | --- |
| Builds and runs on Android | Not yet (M9) |
| Every reference screen exists in Unity with the same or better layout | Done in the Editor (layout audit); not checked on a phone |
| All nine levels completable | Done (bots and the scripted full run) |
| Every skill and every skill obstacle works | Done (M6, M7 tests, obstacle report) |
| Difficulty rises level to level | Done by the curve; needs a human balance pass (Ronn, M10) |
| All tests pass | Yes at this snapshot (see section 5) |
| No console errors in a full scripted run | Done (`FullRunTests`) |
| Touch, keyboard and gamepad all work | Keyboard and gamepad tested in the Editor; touch needs a phone (M9) |
| Saves survive an app kill | Tested in the Editor (`SaveServiceTests`); needs a phone (M9) |

## 4. Other work done in this session

- **Progress video tool** (`Assets/Tests/Video`): records the game playing itself to MP4 in a batch editor (bot-driven real input, UI included, game audio through AudioRenderer). `LevelBot.PlanRoom` turns the bot's search into input frames.
- **Handoff** to Mary and Ronn (`docs/HANDOFF_MARY_RONN.md`), Nash's roles split between them.

## 5. Tests at this snapshot

| Suite | Result | File |
| --- | --- | --- |
| EditMode (all) | 173 / 173 pass (2026-11-23, after Tala 48 and the 1440x810 camera) | tests/Results/a7_editmode.xml (local) |
| PlayMode (all) | 48 pass, 0 fail, 1 skipped (the progress video runs only on its own), 2026-11-23 | tests/Results/a7_playmode.xml (local) |
| Layout audit | 39,217 checks, 0 problems | tests/Screenshots/m6/layout_report.md |

## 6. Untouched or open items

- Android: nothing configured yet (M9). The C: drive was short of space for Android support on 6000.6.6f1.
- Real-device checks: touch, performance, save after app kill, safe areas on a notched phone.
- Tala (3x, hand-pixelled) is the only finished art in the game; creatures, tiles, props, UI and map are still the prototype's.
- Music is the five provided OGG tracks, looped; sound effects partly generated (M6, M8).
- No human playtest notes yet; no bug list file yet (Ronn creates `tests/PLAYTEST_NOTES.md`, `tests/BUGS.md`).
- Credits screen lists the team; licences for any new audio or fonts must be added when they come in.
