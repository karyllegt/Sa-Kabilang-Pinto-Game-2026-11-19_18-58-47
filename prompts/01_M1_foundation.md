# Prompt 01: M1 Foundation

Goal: a clean, compiling project skeleton with the services every later milestone depends on.

Do:
- Copy `unity_starter/Assets/Editor/SkpAssetImportSettings.cs` into `Assets/Editor/`, then copy the sprites and audio from `reference/assets/` into the folders given in `docs/04_ASSET_GUIDE.md`. Open the Editor through the unity-cli skill, confirm every texture imported as a Sprite with Point filter, 16 PPU and no compression, and that the 9-slice border on the UI box is set. Fix the importer script if anything is wrong.
- Create the folder structure in `README.md` and assembly definitions for runtime, EditMode tests and PlayMode tests.
- Set up the 2D camera with Pixel Perfect Camera (reference 480x270 world, 16 pixels per unit, point filtering, no sprite compression). Landscape only.
- Input System: one action map (Move, Jump, Dash, Ward, Veil, Bolt, Interact, Pause) with keyboard, gamepad and on-screen touch controls. No `UnityEngine.Input` calls.
- `SaveService`: JSON file in `persistentDataPath`, versioned, atomic write (temp file then replace), `.bak` kept, corrupt file recovers to defaults. Fields: `unlocked` (count), `char`, `diff`, `best`, `cont` (resume state or null), `cleared`, `metLola`, `lolaSeen`, `skills` (string array).
- `GameFlow`: scenes Boot, Title, Map, Level. Scene loader with a placeholder circle-wipe hook (real wipe comes in M5).
- ScriptableObject types (empty shells for now): `LevelConfig`, `SkillDefinition`, `DifficultyCurve`, `PlayerTuning`.
- A `Debug` overlay toggle for development builds.

Tests (write and run): SaveService round trip, defaults on missing file, corrupt-file recovery with `.bak`, version migration stub, unlock count never decreases, `cleared` only set by the final level, skills never duplicate. EditMode.

Done when: compiles clean, tests pass, committed. Report in the standard format.
