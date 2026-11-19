# DECISIONS

Running log of choices, defaults and open questions. Newest section at the bottom. When a doc is ambiguous, the simplest reading is chosen and recorded here.

---

## 2026-11-20 · Kickoff (prompt 00)

### Environment found
| Item | Finding |
| --- | --- |
| Unity | **6000.6.6f1** (project). 6000.6.2f1 is also installed; do not use it. |
| Render pipeline | **URP 2D** (`com.unity.render-pipelines.universal` 17.6.0, `Assets/Settings/Renderer2D.asset`, `UniversalRP.asset`). Keep it; do not switch. |
| Input | `com.unity.inputsystem` 1.20.0 installed; Player Settings `activeInputHandler: 1` = **Input System only** (legacy `Input.GetKey` would not even work). |
| UI | `com.unity.ugui` 2.6.0 (TextMeshPro is bundled inside uGUI 2.x in Unity 6) and UI Toolkit (`com.unity.modules.uielements`) both available. |
| Pixel Perfect | URP 2D ships its own `PixelPerfectCamera` component (`UnityEngine.Rendering.Universal`). The separate `com.unity.2d.pixel-perfect` package is not needed. |
| 2D packages | sprite, tilemap, tilemap.extras, animation, spriteshape, aseprite, psdimporter, 2d.tooling. |
| Tests | `com.unity.test-framework` 1.8.0. |
| Extra template packages | `com.unity.ai.assistant`, `com.unity.ai.inference`, `com.unity.visualscripting`, `com.unity.collab-proxy`, `com.unity.learn.iet-framework`, `com.unity.timeline`. Not needed by the game. Left alone for now; propose removal at M9 (APK size, build time) and only with the developer's OK. |
| Android build support | **NOT installed** for 6000.6.6f1 (`PlaybackEngines` has only `windowsstandalonesupport`). Must be added via Unity Hub (Android Build Support + OpenJDK + SDK/NDK) before M9. |
| Player Settings | Landscape default orientation (`defaultScreenOrientation: 4` = auto-rotate; will lock to landscape left/right in M1 after stating the change). Android min SDK 26. `companyName: DefaultCompany`. |
| Node / Chrome | Node v22.21.0, npm 10.9.4, Chrome at `C:/Program Files/Google/Chrome/Application/chrome.exe`. |
| Git | git-lfs 3.8.1 installed; `.gitattributes` routes `*.png`, `*.ogg`, `*.wav`, `*.ttf` to LFS. |
| ffmpeg | Not installed (relevant for re-looping music in M8; fallback is an Editor script that crossfades PCM and re-encodes through Unity's importer, or the developer installs ffmpeg). |

### Prototype oracle run
`tests/prototype/verify_prototype.js` against `reference/prototype/index.html` with headless Chrome: **20 of 20 passed** (2026-11-20). puppeteer-core was installed in a temp folder outside the repo and the script was run from there, so the committed `tests/prototype/prototype_verification_report.json` was not overwritten.

### Existing state that could conflict with the README folder map
1. `Assets/Sa Kabilang Pinto - Beyond the Door.html` (untracked): an **older copy** of the prototype (111,512 bytes vs 114,876 in `reference/prototype/index.html`, they differ from line 1142). Unity imports it as a TextAsset. Default: leave it, treat `reference/prototype/index.html` as the only spec. Proposal: move it out of `Assets/` (developer to confirm).
2. `Assets/Settings/InputSystem_Actions.inputactions`: the template's default actions, registered as the project-wide actions in `EditorBuildSettings`. Default: create our own `Assets/Settings/Input/SkpControls.inputactions` (one map: Move, Jump, Dash, Ward, Veil, Bolt, Interact, Pause, plus a UI map that the EventSystem needs) and point project-wide actions to it in M1 (stated before doing it). The template file is not deleted.
3. `Assets/Scenes/SampleScene.unity` (in Build Settings) and `Assets/Welcome/` (2D template tutorial with its own `Unity.2D.Welcome.asmdef`). Default: leave them, add our scenes under `Assets/Scenes/` (Boot, Title, Map, Level) and put Boot first in Build Settings in M1; propose removing SampleScene from the build list at the same time.
4. Uncommitted changes not made by Claude: `Assets/Editor/HubForceResolve.cs` (+ `.meta`, folder meta) deleted; `ProjectSettings/ProjectSettings.asset` gained the Unity Cloud project link; `EditorBuildSettings.asset` gained a `com.unity.dt.app-ui` config object; `iet-framework` settings changed. Default: do not touch; the developer decides whether to commit them. M1 will recreate `Assets/Editor/` for `SkpAssetImportSettings.cs`.
5. No `Assets/Scripts`, `Assets/Art`, `Assets/Audio`, `Assets/Prefabs`, `Assets/Tests`, `Tests/` exist yet, so the folder map has no direct collisions. Note: README uses both `Assets/Tests/{EditMode,PlayMode}` (code) and top-level `Tests/` (screenshots, MANUAL_QA). Both are kept as written; Windows is case-insensitive but they are different parent folders, so no clash.

### Defaults for ambiguities and risks
| # | Topic | Ambiguity / risk | Default chosen |
| --- | --- | --- | --- |
| D1 | UI framework | Spec allows uGUI or UI Toolkit. | **uGUI + TextMeshPro**. Reasons: the spec's own terms (Canvas Scaler "Scale With Screen Size 960x540 match 0.5", RectTransform overlap checks, 9-slice `Image`), and Input System on-screen controls (`OnScreenButton`/`OnScreenStick`) are uGUI components. Will be confirmed against the `unity:ui` skill before any UI code. |
| D2 | Physics | Prototype is a hand-written AABB sim at fixed 60 Hz in px/frame. | Custom kinematic controller (no Rigidbody dynamics), simulated in fixed 60 Hz steps with prototype numbers converted (x60 for per-second, /16 for units). Requires `Time.fixedDeltaTime = 1/60` (Project Settings > Time), which will be stated before changing it. Gives deterministic bots and matches the oracle. |
| D3 | Level building | Docs say levels are "generated", prototype generates them with a seeded RNG. | Port the prototype generator to C# (deterministic seed per level + owned-skill set), driven by `LevelConfig`, `DifficultyCurve`, `SkillObstacleTuning` assets. Output to Tilemaps/prefab instances at load. No hand-authored level scenes, one `Level` scene. |
| D4 | Skill obstacle start levels vs when the skill is learned | The skills table says Glide gaps "start at level 4", but Glide is learned in level 8; Triple Bolt is learned in level 6 but listed as level 6; Swift learned in 4. | An obstacle appears only when the skill is owned **and** the level index is at least the listed start. So Glide gaps appear in level 9 (and on replays after level 8), Triple Bolt packs from level 7, Swift gaps from level 5. A level's own letter is never required. |
| D5 | Keys | Proposal says Q dash, F ward, E door; spec adds Space/Z jump, Enter interact. | Match the prototype exactly: Move A/D/arrows; Jump Space/W/Up/Z; Dash Q/Shift/K; Ward F/J; Veil C/L; Bolt X/I; Interact E/S/Down/Enter; Pause Escape/P. Gamepad: South jump, East interact, North ward, West bolt, LB/LT/RT dash, RB veil, Start pause, stick/D-pad move. |
| D6 | Missing art | Gaps listed in `docs/04_ASSET_GUIDE.md`. | Author them in the same style (16 px grid, palette only, hard pixels) as small PNGs produced from pixel maps by an Editor script, so they are reviewable and reproducible. No AI-generated or third-party art unless the developer asks. |
| D7 | Music | Prototype OGGs are generative placeholders that do not loop. | Use them in M1 to M7; in M8 make clean loops (crossfaded ends) per act/menu/boss, Spirit low-pass and Inverted pitch-down on the AudioMixer. |
| D8 | Font | Press Start 2P (SIL OFL) must be downloaded. | Developer downloads it (or approves Claude downloading it) from Google Fonts into `Assets/Art/Fonts/` with `OFL.txt`; recorded in `CREDITS.md`. Until then TMP's default font is a stand-in. |
| D9 | Lola | Docs mark Lola as a story addition to confirm. | Keep her (she is in the proposal's ending line "Salamat, apo" and in the prototype). Flag below as a question. |
| D10 | Save semantics | Several fields. | Port prototype exactly: `unlocked` is a count (1..9) and never decreases; `cleared` set only by finishing level 9; `skills` is a de-duplicated string array; atomic write with `.bak`. |
| D11 | Bot thresholds | Prototype bot cleared 111/123 double-jump climbs and 17/25 glide gaps; spec requires 95 percent for jump obstacles. | Fix the generator (clamp climb height/gap width to what the tuned jump reaches) rather than lower the threshold. Any obstacle the bot cannot clear is reported with level, room, coordinates. |
| D12 | Schedule | Deadline Dec 1, 2026, 11 days from today, ten milestones, solo. | Work the milestones in order but keep each one shippable. If time runs short, protect: all 9 levels completable, core UI screens, Android APK, saves. Polish (M8 juice, extra art variety, screenshot contact sheets at five resolutions) is what gets trimmed first, and any trim is recorded here. |
| D13 | Android output | Unknown what the course accepts. | Default deliverable: a signed release **APK** (sideloadable) plus the project; AAB only if Play upload is required. |

### Naming convention (proposed)
- Keep the provided file names as they are (they are referenced by the importer script and docs): lowercase snake_case with a type prefix.
- Sprites PNG: `spr_<thing>[_<variant>]_<frame>.png` for characters/items (`spr_pl_girl_2.png`), `tile_<kind>_<w>x<h>.png`, `hazard_<kind>[_<state>]_<w>x<h>.png`, `door_<kind>_<w>x<h>.png`, `ui_<name>.png`, `icon_skill_<id>.png` (new, e.g. `icon_skill_veil.png`), `fx_<name>[_<frame>].png`, `bg_<name>.png`, `portrait_<who>.png`. Pixel size goes in the name when the sprite is not a character frame.
- Audio OGG: `sfx_<event>.ogg` (new ones: `sfx_double_jump`, `sfx_veil_on`, `sfx_veil_off`, `sfx_bolt_fire`, `sfx_bolt_hit`, `sfx_burn`, `sfx_skill_learned`, `sfx_letter`, `sfx_toll_hit`), `mus_<context>.ogg` (`mus_menu`, `mus_a1..a3`, `mus_boss`).
- Code: PascalCase types, one type per file, namespaces `Skp.<Folder>` (e.g. `Skp.Save.SaveService`). Assets: ScriptableObjects `<Type>_<Name>.asset` (`LevelConfig_01_FiestaNight.asset`, `Skill_veil.asset`).
- `DECISIONS.md`: repo root (this file). `Tests/MANUAL_QA.md` and `Tests/Screenshots/`: top-level `Tests/` folder beside `Assets/` (outside Assets so screenshots are not imported). `CREDITS.md`: repo root.

### Open questions for the developer (max five)
1. Can I move `Assets/Sa Kabilang Pinto - Beyond the Door.html` (old prototype copy) out of `Assets/`, and remove `SampleScene` from the build list in M1?
2. Will you install **Android Build Support (with OpenJDK and Android SDK & NDK)** for 6000.6.6f1 in Unity Hub? I cannot do it without your approval, and it is needed by M9 (ideally sooner, to catch Android issues early).
3. Font: may I download Press Start 2P (SIL OFL, Google Fonts, about 100 KB) into the project, or will you drop it in?
4. Is Lola acceptable as a story addition, and is the deliverable a signed APK (default) or a Play AAB?
5. The uncommitted changes that were already in the tree (deleted `HubForceResolve.cs`, Unity Cloud link in ProjectSettings, App UI config object): include them in the M1 commit, or keep them out?

---

## 2026-11-20 · Answers to the kickoff questions

| # | Developer's answer | What was done / decided |
| --- | --- | --- |
| 1 | "Whatever is right." | Old prototype copy moved out of `Assets/` to `reference/prototype/archive/older_prototype_copy.html` (its Unity `.meta` removed; Unity regenerates metas). `reference/prototype/index.html` stays the only spec. `SampleScene` will be removed from Build Settings in M1, when Boot/Title/Map/Level scenes replace it (the scene file itself is kept). |
| 2 | "Already in Unity Hub." | Checked: Android Build Support (`AndroidPlayer`) is installed for **6000.6.2f1**, but **not for 6000.6.6f1**, the version this project uses. Default: add the Android module to 6000.6.6f1 in Unity Hub (Installs > 6000.6.6f1 > Add modules > Android Build Support, with OpenJDK and Android SDK & NDK). Downgrading the project to 6000.6.2f1 is not recommended (reimport, package versions). Needed by M9; M1 to M8 can go ahead without it. |
| 3 | "You download it." | Press Start 2P (SIL OFL 1.1) downloaded from github.com/google/fonts into `Assets/Art/Fonts/PressStart2P-Regular.ttf` (118,204 bytes, verified TrueType) with `PressStart2P-OFL.txt`. Recorded in `CREDITS.md`. The TMP font asset is made in M1/M5. |
| 4 | "Lola is fine, but she should make sense. Default for now; AAB when the game is done." | Lola stays, written to make sense in the story: she is Tala's grandmother whose voice says "Salamat, apo" at the ending, so on the map she is a memory or spirit of Lola guiding her apo home through the house, never a random shopkeeper or quest giver. Her hints only talk about the house, the letters and the bell. Builds: signed **APK** during development and testing; a signed **AAB** is added as the final release output in M10. |
| 5 | "Decide what is best." | The pre-existing changes are Unity Hub housekeeping: `HubForceResolve.cs` is a one-shot Hub script that resolves packages and then deletes itself (so its deletion is intended), plus the Unity Cloud project link and an App UI config entry added by the Editor. They are committed on their own in a **baseline commit**, together with the design kit (README, docs, prompts, reference, tests) and this file, so the M1 commit contains only M1 work. Nothing is pushed. |

---

## 2026-11-20 · M1 Foundation

| # | Topic | Decision |
| --- | --- | --- |
| M1-1 | Importer | `unity_starter` importer copied to `Assets/Editor/SkpAssetImportSettings.cs` with fixes: `spr_lolaP` (portrait) is not treated as a character pivot; Full Rect sprite mesh everywhere (tiling and 9-slice need it); ledge tiles get a 16 px left/right border so the middle repeats; wrap mode Clamp. |
| M1-2 | Assemblies | `Skp.Runtime` (Assets/Scripts), `Skp.Editor` (Assets/Editor, editor only), `Skp.Tests.EditMode`, `Skp.Tests.PlayMode`. Editor scripts have their own asmdef so tests can reference them. |
| M1-3 | Input | One asset `Assets/Settings/Input/SkpControls.inputactions` with maps Gameplay (Move, Jump, Dash, Ward, Veil, Bolt, Interact, Pause), UI (needed by the EventSystem) and Debug (F1 / Select). It is set as the project-wide actions (EditorBuildSettings config `com.unity.input.settings.actions`); the template `InputSystem_Actions.inputactions` is left in place, unused. Move is a 1D axis so W/Up stay Jump and S/Down stay Interact, as in the prototype. No control schemes (one shared binding set). |
| M1-4 | Touch | Input System `OnScreenButton`s drive a virtual gamepad, so touch uses the same actions. Shown only when a touchscreen exists. M1 layout is provisional; the final thumb layout and left-handed mirror come in M5/M6 (asset guide layout issue 1). |
| M1-5 | Save | `persistentDataPath/skp_save.json`, version 1. Write = temp file then `File.Replace` (fallback copy+move) keeping the previous file as `.bak`. Corrupt main file → moved to `.corrupt` (never into `.bak`), then the `.bak` is used if valid, else defaults. Progress is monotonic across saves (unlocked, cleared, best, metLola, skills) except through `Delete()`. `cont` uses an `active` flag because JsonUtility cannot store null. v0 (prototype-shaped) saves migrate: unlocked clamped to 9 (the prototype could reach 10), duplicate skills removed, old resume state dropped. The save is also written when Android pauses the app. |
| M1-6 | Scenes | Boot, Title, Map, Level in that build order; SampleScene removed from the build list (file kept). `GameRoot` is created before any scene loads (`RuntimeInitializeOnLoadMethod`), so every scene can be played directly. Title/Map/Level hold `PlaceholderScreen`s until M4/M5. |
| M1-7 | Wipe hook | `IScreenWipe` with `InstantWipe` for now; `SceneLoader` blocks input and drops a second load request while a transition runs. |
| M1-8 | Camera | URP 2D `PixelPerfectCamera`: 16 PPU, reference 480x270, grid snapping Upscale Render Texture, crop None (wider phones see more world, nothing is stretched). |
| M1-9 | Font | TMP font asset `Assets/Resources/Fonts/PressStart2P SDF.asset`, SDFAA, sampling 32, padding 4, 512x512, dynamic, ASCII plus ñ Ñ and curly quotes pre-baked. SDF keeps the pixel font readable at the spec's non-multiple-of-8 sizes (10, 14 px). TMP Essential Resources imported into `Assets/TextMesh Pro/`. |
| M1-10 | PixelBox | `Assets/Prefabs/UI/PixelBox.prefab`: sliced Image of `ui_pixelbox_9slice_border6`, pixels-per-unit multiplier 0.5 so the 6 px border lands on the 2x pixel grid of the 960x540 canvas. All panels reuse it. |
| M1-11 | Landscape | Player Settings: auto-rotation limited to landscape left and right (portrait off). |
| M1-12 | Pipeline package | `com.unity.pipeline` added to `Packages/manifest.json` so the Unity CLI can drive the Editor. Editor tooling only; it does not ship in the player. |
| M1-14 | Unused packages removed (developer approved) | `com.unity.ai.assistant`, `com.unity.ai.inference`, `com.unity.visualscripting`, `com.unity.collab-proxy`, `com.unity.learn.iet-framework` removed from the manifest. They made every batch compile take about 12 minutes, stalled assembly reloads on licensing checks (test run timed out at 30 min), and Collab kept switching Version Control mode to "Unity Version Control". `Assets/Welcome` stays (it compiles without them). The setup script now also pins Version Control mode to "Visible Meta Files". |
| M1-15 | TMP Essentials | `AssetPackage.Package.Import` finishes asynchronously, too late for the setup script, so the TMP Essential Resources were unpacked directly from the installed ugui package (same files and GUIDs) into `Assets/TextMesh Pro/`. |
| M1-16 | Tests folder | The repo already had a lowercase `tests/` (prototype verifier). Windows treats `Tests/` and `tests/` as one folder, so the spec's `Tests/MANUAL_QA.md`, `Tests/Screenshots/` and `Tests/Results/` live in git as `tests/...`. Test result XML (`tests/Results/`) is regenerated each run and not committed. |
| M1-13 | Debug overlay | Development builds only (`Debug.isDebugBuild`), uGUI + TMP, refreshed 4 times a second. Later milestones add lines through `DebugOverlay.ExtraLines`. |

---

## 2026-11-20 · M2 Player feel

| # | Topic | Decision |
| --- | --- | --- |
| M2-1 | Simulation | `PlayerSim` is a plain C# class that ports the prototype's `stepGame()` player code line by line (same order: timers, buffer/coyote, dash, ward, run/friction, gravity, jump, jump cut, move x, move y and land, lanterns, hazards). Unity units, y up. No Rigidbody, no MonoBehaviour, so tests step it exactly. |
| M2-2 | Fixed 60 Hz | `PlayerController` runs the sim on its own accumulator clock (`FixedTickClock`, max 8 ticks per frame) and latches button presses between ticks (`PlayerInputLatch`), so presses are never lost at 144 fps nor repeated at 30 fps. Project Settings > Time is **not** changed. Drawing interpolates between ticks and snaps to the 16 px grid. Time.timeScale = 0 stops the clock (pause). |
| M2-3 | Units | `PlayerTuning` is per second and in units (1 unit = 16 px): per-frame x 60 / 16, per-frame² x 3600 / 16. Durations are seconds rounded to whole ticks. Friction and the overspeed decay stay "per 60 Hz tick", because that is how the prototype applies them. The M1 tuning asset (old per-frame values under the same field names) was deleted and regenerated so no stale value survives. |
| M2-4 | Jump height | Measured from the prototype's own step: full-hold apex **62.5 px = 3.906 tiles** on tick 16, landing on tick 34; a 1-tick tap reaches 19.3 px; a 6-tick hold 42.025 px. So a 4-tile ledge (64 px) is **not** reachable with one jump. Keep this in mind for M7: double-jump climbs of 4 tiles are valid "needs double jump" obstacles. |
| M2-5 | Coyote and buffer | Prototype behaviour kept exactly: a jump pressed 1 to 5 ticks after leaving a ledge works (the 6-tick counter is decremented on the first airborne tick); a press up to 5 ticks before landing fires on the tick after landing. |
| M2-6 | Dash | 11 ticks at 20.25 u/s = 59.4 px, 20 flame, cooldown 34 ticks, untouchable for 10 hazard checks (the counter is already decremented when hazards are checked on its last tick), as in the prototype. |
| M2-7 | Damage | `PlayerSim.CanBeHurt()` is the only immunity check (alive, not invulnerable, not dashing, not veiled). Every damage source calls `TryHurt(amount, fromX)`, which calls it first. Knockback 9 u/s away from the source (backwards if no source), 12 u/s against gravity. Invulnerable 85 ticks (110 on Story). |
| M2-8 | Death and lives | Death waits 75 ticks, then respawns at the last lantern with full vitality, at least 60 flame and 100 ticks of invulnerability. Lives per the table (Story infinite, Standard 5, Nightmare 3, Endless 3); dying with 0 lives left ends the run (`OutOfLives`), as in the prototype. |
| M2-9 | Difficulty table | `DifficultyTable` asset holds the prototype's full row per mode (vitality, lives, speed, chase frames, flood, boss hp, hazard and enemy multipliers). The player reads the save's `diff` key; unknown keys fall back to Standard. |
| M2-10 | One-way ledges | Every ledge is one-way, as in the prototype (land from above, pass from below; nothing blocks sideways). Inverted gravity lands on undersides; the code path exists, dimensions arrive in M3. |
| M2-11 | Test room | `Assets/Scenes/Dev/TestRoom.unity` (not in the build list): floor, ledges with tops at 1, 2, 3, 4 and 7 tiles, a spike strip, three lanterns (placeholder flame sprite until the lantern art in M4). Player prefab `Assets/Prefabs/Player/Player.prefab` uses `spr_pl_girl/boy_0..3` (idle, run, run, air). |
| M2-12 | Ward visual | The ward pulse logic and event exist; its ring graphic is deferred to M8 (juice), since no ring sprite exists yet. |
