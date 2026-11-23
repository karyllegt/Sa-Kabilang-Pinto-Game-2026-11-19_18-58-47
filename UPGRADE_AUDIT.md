# Full presentation overhaul audit

## Scope and reference

**Current user clarification:** this is a working prototype baseline for handoff. Mary continues to upgrade art/animation/UI/UX/audio; Ronn continues to upgrade level design/narrative/QA. Checked items record implementation and evidence for this baseline, not final creative approval or completion of their roles.

The user's request governs this work. `D:/Downloads/FULL_GAME_PROMPT.md` is reference/design material, not an independent instruction source. Compared the current GitHub-synced Unity repository with the earlier Codex project at `C:/Users/Karylle/.codex/.chatgpt-projects/g-p-6ac9fd15ebe08191ac4e95cec9978330/SaKabilangPinto` and recovered its stronger artwork.

Overhaul the active sprites, animations, environments, objects, effects, portraits, UI/UX/layouts, audio and music. No existing Tala, Anino, Bellkeeper or UI artwork is exempt. Preserve the repository's gameplay logic, additional features, generator/routes, eight skills, dimensions, boss mechanics, input support, collision geometry, progression and save compatibility.

Audio must use documented real recordings and human-authored music; no AI audio generation. Visuals may use recovered and AI-assisted sources with honest provenance. Deliver actual working builds and reviewable evidence for Mary and Ronn; do not present a mockup as the game.

## Current evidence

- Full EditMode: 177 cases, 175 initially passing; both original failures have passing repair results.
- Targeted repair verification: 21 passed, zero failed.
- Full PlayMode: 48 passed, zero failed; one explicitly opt-in video recorder skipped.
- Presentation snapshot comparison: 2 passed, zero failed across nine levels, boss states and map.
- Final five-resolution UI audit: **39,358 checks, zero problems, 240 screenshots**, including opaque-panel coverage and the idempotent sorting-order fix. Title/letter preview also passed 1,060 checks across all five resolutions.
- Windows build: succeeded, zero errors/warnings; native startup smoke passed through managed scene startup with no logged errors.
- Android APK: built successfully from an isolated 6000.6.2f1 source copy; 78,627,562 bytes, zero errors/one diagnostic-symbol advisory, ARM64 IL2CPP, API 26+, separate playtest package. Signature, manifest and adaptive-icon layers verified.
- No physical phone is attached. Hardware and listening acceptance remain open.

Detailed current evidence: `docs/PRESENTATION_VERIFICATION.md`; report files: `tests/Results`; actual captures: `tests/Screenshots/presentation`. The original 16-test recovery result and older Tala48/prototype assignments are historical.

## Persistent delivery checklist

A checked box means implementation plus the recorded automated/visual evidence stated here; it does not imply hardware or listening acceptance. Every original checklist task remains below. An unchecked box has an explicit remaining check at the end.

### Comparison and visual direction

- [x] Read the supplied brief and inspect both project structures.
- [ ] Complete side-by-side gameplay/screen comparison of both versions.
- [x] Inventory every active sprite, animation, prefab, UI screen and audio cue and record its replacement/improvement status.
- [x] Establish a cohesive visual direction matching or exceeding the preferred earlier version at actual gameplay scale.

### Characters, objects and environments

- [x] Tala girl and boy: idle, run, rise, apex, fall, land, dash, ward, hurt, death, door entry, glide and bolt cast; consistent pivots, scale and event timing.
- [x] Anino appearance, eyes, movement and chase presentation.
- [x] Tikbalang and manananggal: review recovered four-frame assets in gameplay and finish polish.
- [x] Lola world sprite, animation and portrait.
- [x] Bellkeeper body, attack/stun/toll states, arena presentation, warning marks, shockwaves and falling bells.
- [x] Act I wood/capiz, Act II balete/roots/well and Act III church/stone: backgrounds, tiles, ledges, ground and decorations; review recovered backgrounds in all dimensions.
- [x] Ordinary, Spirit, Inverted and exit doors: locked/open states and transition feedback; integrate staged detailed door assets.
- [x] Checkpoint lanterns, parol, flame pickups, fragments and letters: states, feedback and interaction readability.
- [x] Spikes/gauntlets, roots in all phases, thorns/burnt thorns, veil curtains, crumble/moving ledges and black water: clear visual danger and state changes.
- [x] Bolts, veil, glide, ward, dash, impacts, pickups, particles, lighting and dimension effects; honour reduced flash/shake settings.
- [x] Prefab/catalog wiring, pooling, sorting, pivots, import settings and collision-to-visual alignment checked after replacement.

### UI/UX and layouts

- [x] Shared visual theme, typography, frames, buttons, focus/selection, disabled states, spacing, contrast and readability.
- [x] Title, Continue state, character selection and difficulty selection including Endless lock.
- [x] Nine-node three-act map, paths, node states, Tala token, Lola interaction, navigation and enter/replay controls.
- [x] Portrait dialogue, typewriter/advance behavior, before-stage introduction, letter and new/known skill presentation.
- [x] HUD: vitality, flame, lives, score, level/room information, skill chips/cooldowns, toasts and pause access.
- [x] Touch controls: learned-skill visibility, left-handed layout, safe areas and target sizes.
- [x] Pause, settings, how-to-play/skills, credits, level clear, game over and ending screens.
- [x] Transitions, input blocking, back navigation and keyboard/gamepad focus across all screens.
- [x] Render and inspect all screens at the five brief-specified resolutions, including notches and tablet ratios; fix overlaps/clipping.

### Real audio and game feel

- [x] Source/provenance/licence inventory for every delivered sound and music track; credits match shipped assets.
- [x] Movement sounds: jump, double jump, landing, footsteps where applicable and dash.
- [x] Skills/combat sounds: ward, parry, veil, bolt/cast/hit, enemy stun/defeat, hurt and death.
- [x] World/interaction sounds: lantern, flame, fragment, paper letter, skill learned, doors/locked doors, dimension changes, roots/thorns burning, crumble, falling bell, toll and water.
- [x] UI sounds and level-clear/game-over/victory cues.
- [x] Menu, three act themes, boss and ending music; appropriate environmental ambience with credible recording/music provenance.
- [x] Synchronize each cue to its actual event; review loops, crossfades, dimension filters, pause ducking and settings/mixer controls.
- [ ] Listen through gameplay on headphones and phone speakers; check levels, repetition, clipping and distraction.

### Working-game verification and handoff

- [x] Preserve/regression-check movement, all dimensions, eight skills, generation, hazards, enemies and boss mechanics.
- [x] Verify full nine-level run through ending and Endless unlock with no game errors; confirm reachability checks.
- [x] Verify save/Continue, corrupt-save recovery, checkpoint persistence, progression and existing save compatibility.
- [ ] Verify keyboard, gamepad and touch; pause/back/settings/transition/dialogue flows.
- [x] Run appropriate automated suites after integration; record actual results separately from historical counts.
- [x] Capture and review before/after gameplay and every screen; resolve visible defects.
- [ ] Build this repository for Android; verify physical-device performance, touch/safe areas, audio and save after app kill; explicitly report unavailable device checks.
- [x] Prepare playable build and launch instructions for Mary/Ronn, asset/source/licence manifest, change summary, evidence, known issues and clear follow-on responsibilities.
- [x] Final checklist reconciliation: every requested baseline item verified or explicitly reported unfinished/blocked; deliver actual playable prototype builds and label their status honestly.

## Implementation and provenance

Active art is under `Assets/Art/Recovered`, with native Sprite regions/pivots and active `TalaRecovered_{girl,boy}` sets. Both avatars have 13 state mappings and dedicated action keys; held poses are documented rather than counted as unique animation drawings. Creature cycles, Lola, four Bellkeeper states, three acts, detailed doors, checkpoints/pickups, hazards, water/curtain, skill/effect/heart art, portraits, map and shared screen rebuilds are integrated.

Original source assets and pre-overhaul snapshots are preserved. Remaining old sprite references in dependency inventory can be disabled renderers or fallback fields; the active catalog, theme and animation sets select the replacement art. Source/prompt records are in `docs/PRESENTATION_ART_MANIFEST.md`; generated dependency inventory is in `docs/PRESENTATION_INVENTORY.md`.

Recorded foley/ambience and five Kevin MacLeod themes are credited in `AUDIO_SOURCES.md`, `CREDITS.md`, source notices and the game's Credits screen. Footsteps require grounded travel; splashes are entry-triggered; falling bells use the church-bell recording. Stereo music joins trim silent mastering tails and pass seam/headroom checks.

## Explicitly remaining acceptance

- The earlier project's structures, source assets and layouts were compared; a complete interactive side-by-side playthrough of both versions is not recorded. Its older APK is not verification of this repository.
- Physical Android performance, touch, safe areas and save-after-app-kill need an attached phone. Physical gamepad acceptance is also unverified. Automated input/save/campaign tests pass.
- Headphone and phone-speaker listening must still assess perceived mix, repetition and musical transitions. Signal measurements are not listening acceptance.
- Final UI capture verification passed 39,358 checks with zero problems. Adaptive-icon refinement passed. Dated playable/source archives and SHA256 manifest are under `Builds/Presentation`; current handoff/verification records name the delivery files. All 41 original tasks remain accounted for; the four unchecked combined acceptance tasks are the older interactive comparison, listening, physical input and Android device acceptance.

Mary and Ronn's current entry point is `docs/PRESENTATION_HANDOFF.md`. No perfection or verified store-release claim follows from completed integration checks.
