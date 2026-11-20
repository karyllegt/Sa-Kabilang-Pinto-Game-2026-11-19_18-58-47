# Sa Kabilang Pinto (The Lantern Between Worlds)

2D pixel-art Filipino-folklore platformer for Android, built in Unity 2D by one developer (Karylle G. Torres, group Pangkat Parol, CSEL course). Real deadline: Dec 1, 2026. You (Claude Code) are the engineer; I direct.

## Read these first, in this order
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

## Folder map (create as needed)
`Assets/Scripts/{Core,Player,Skills,Dimensions,Enemies,UI,Audio,Save,Levels}`, `Assets/Prefabs`, `Assets/Scenes`, `Assets/Art`, `Assets/Audio`, `Assets/Tests/{EditMode,PlayMode}`, `Tests/Screenshots`, `DECISIONS.md`, `Tests/MANUAL_QA.md`.

## Status
See `DECISIONS.md` and git log. Milestones M1 to M10 are in `prompts/`.

- **M1 Foundation: done (2026-11-20).** Assets imported, Input System actions (keyboard, gamepad, touch), SaveService, Boot/Title/Map/Level flow with a wipe hook, pixel-perfect camera, data-asset shells, debug overlay. 29 EditMode + 2 PlayMode tests pass. Re-run the generated setup with **Sa Kabilang Pinto > Setup > Run M1 Setup** (or `-executeMethod SkpProjectSetup.RunAll`).
- **M2 Player feel: done (2026-11-20).** Deterministic 60 Hz player port (run, variable jump, coyote, buffer, dash, ward, hurt, death, lanterns, lives), frame-rate independent, `PlayerTuning` per second, `DifficultyTable`. Try it: open `Assets/Scenes/Dev/TestRoom.unity` and press Play. 53 EditMode + 3 PlayMode tests pass.
- **M3 Dimensions and Anino: done (2026-11-21).** Ordinary/Spirit/Inverted bitmask on ledges, hazards and doors; doors switch dimension (optional teleport); lanterns return to Ordinary; Inverted flips gravity, landing, jump and sprite; the Anino shadow replays the path N ticks late, hurts through `CanBeHurt()`, ward stuns it 190 ticks; death resets everything. Try it: `Assets/Scenes/Dev/DimensionRoom.unity`. 77 EditMode + 4 PlayMode tests pass.
