# Mary and Ronn: working prototype baseline handoff

This is a **working prototype baseline** prepared for Mary and Ronn to build on. Its presentation overhaul provides integrated examples and playable systems for review. Mary owns further art, animation, UI/UX and audio upgrades; Ronn owns further level design, narrative and QA improvements. Both should use their judgment to enhance or replace the baseline within their roles. The assets and completed technical checks do not close those creative responsibilities.

Read `../UPGRADE_AUDIT.md` for all 41 implementation/checklist tasks and `PRESENTATION_VERIFICATION.md` for actual evidence. Earlier asset paths and superseded implementation instructions are historical; the team's roles remain active.

## Play and edit

Delivery folder: `Builds/Presentation`. Extract **SaKabilangPinto-Playtest-20261124-0751.zip** for the Windows player, Android APK, notices and evidence. Extract **SaKabilangPinto-UnitySource-20261124-0751.zip** for the editable project. Each archive has a top-level `Sa-Kabilang-Pinto` folder. `handoff-sha256-20261124-0751.json` records file sizes and SHA256 hashes. The external report `handoff-verification-20261124-0751.json` is generated after packaging and sits beside the ZIPs; it is not embedded in the archives it verifies.

Windows: extract the complete playtest package, then launch `Builds/Presentation/Windows/SaKabilangPinto.exe`. Keep its Data folder, UnityPlayer DLL and MonoBleedingEdge folder beside it. The native startup smoke check passed; an interactive hardware playthrough is still a separate acceptance step.

Android: `Builds/Presentation/Android/SaKabilangPinto-playtest.apk`, ARM64, Android 8/API 26 or newer. Package identifier is `com.pangkatparol.sakabilangpinto.playtest`; it uses its own app storage. This is a local playtest APK rather than a store submission. No physical phone was attached for installation/performance/touch/audio checks.

Unity source: open the project root with **6000.6.6f1**, then open `Assets/Scenes/Boot.unity` and press Play. The installed Android fallback used an isolated 6000.6.2f1 source copy; do not change the main project version to reproduce it. `tools/build_presentation_android.ps1` prepares that copy. Future copies resolve their own package cache.

Keyboard: arrows/A/D move, Space jump, Shift dash, J ward, K veil, L bolt, E interact, Esc back/pause. Touch and gamepad bindings are retained. The game's How to Play/Skills pages explain dimensions and learned abilities.

## What changed

Recovered detailed avatars, creatures, three act environments, doors and props; added dedicated avatar action keys, Lola and Bellkeeper states, Anino silhouette/eyes, terrain/hazards, pickups, water/curtain, spell/ward/veil/glide effects, hearts, skill icons, portraits, map artwork and launcher branding. Shared screen builders now rebuild the title/selection/map/dialog/HUD/settings/clear/game-over/ending presentation with consistent frames, readable focus/disabled states and responsive layouts.

Active audio uses real recorded foley/field ambience and five human-composed Kevin MacLeod tracks. Footsteps require grounded travel, water splashes trigger on entry and falling bells use an actual church bell. Silent mastering tails are trimmed before reproducible stereo loop joins. No AI audio generation was used.

The nine-level campaign, generator/routes, eight skills, dimensions, enemy/boss simulation, collision geometry, progression and save format remain. Company/product identity is preserved for desktop saves.

## Active editing locations

| Area | Current source |
| --- | --- |
| Art masters and native Sprite regions | `Assets/Art/Recovered` |
| Avatar animation sets | `Assets/Data/Player/TalaRecovered_girl.asset`, `TalaRecovered_boy.asset` |
| World object wiring | `Assets/Resources/Level/LevelPrefabCatalog.asset` |
| UI art/type/palette | `Assets/Resources/UI/UiTheme.asset` and real screen builders under `Assets/Scripts/UI` |
| Audio sources and notices | `Assets/Audio/Licensed`, `AUDIO_SOURCES.md`, `CREDITS.md` |
| Repeatable integration | `Assets/Editor/SkpPresentationUpgrade.cs` |
| Actual captures and baselines | `tests/Screenshots/presentation` |
| Regression/build evidence | `tests/Results/presentation_*` |

Original avatar sets are preserved fallbacks. Dedicated action poses may be held across several timed frames; the manifest does not count held frames as additional drawings. Preserve fixed animation durations, dimension membership, visual pivots/sorting and collider alignment. `presentationWidth` is a renderer-only fit for root/thorn states.

Art provenance and generation prompts: `PRESENTATION_ART_MANIFEST.md`. Dependency inventory: `PRESENTATION_INVENTORY.md`. AI-assisted visuals must not be credited as Mary's hand-drawn work. Source, audio credits and font notices accompany the packages. Keep the church-bell WAV/FLAC and CC BY-SA credit separately accessible when distributing.

## Verification and reproduction

- Full EditMode: 177 cases, 175 initially passed; both original failures have passing repair results.
- Repair verification: 21 passed, zero failed. Full PlayMode: 48 passed, zero failed, one opt-in video recorder skipped.
- Presentation snapshot comparison: two tests passed across all nine levels/boss/map.
- Final five-resolution audit: **39,358 checks, zero problems and 240 screenshots**, including opaque-panel pixel checks. The title/letter preview also passed 1,060 checks across all five resolutions.
- Windows built with zero errors/warnings; native graphics and managed scene startup passed. Final Android APK: 78,627,562 bytes, zero errors, one diagnostic-symbol advisory; signature, manifest and both adaptive-icon layers verified.

Use `tools/verify_presentation.ps1` for full suites. Direct Unity test invocations use `-batchmode -runTests -testPlatform EditMode|PlayMode -testResults <path>`; do not add `-quit` to test runs. `--burst-disable-compilation` and worker limits are used on this low-memory machine.

Integration/captures: `SkpPresentationUpgrade.RunBatch`. Capture-only: `SkpPresentationUpgrade.Capture`. Windows build: `SkpHandoffBuild.Windows`. Android: `-buildTarget Android -executeMethod SkpHandoffBuild.Android` with matching module/tool paths. Graphics captures require graphics. Android-only batch building can use `-nographics`.

Prepare music with `tools/media-codec/build_music_loops.ps1` and the documented FFmpeg decoder. Source originals are retained; music imports as streamed stereo Vorbis. To regenerate archives and checksums, run `tools/package_presentation_handoff.ps1` after successful build/verification reports.

## Remaining acceptance and ownership

Repository cleanup removed one-off session recovery/finalization scripts and the agent resume checkpoint. Use the reusable `build_presentation_android.ps1`, `verify_presentation.ps1`, `verify_presentation_android.ps1`, `smoke_presentation_windows.ps1`, `summarize_presentation_results.ps1`, `prepare_presentation_source_package.ps1`, `package_presentation_handoff.ps1` and `verify_presentation_packages.ps1` tools. Unity-generated IDE files and build/cache folders are excluded from source control. Historical design material, original/fallback assets and regression evidence remain available for the team's work.

Mary: own and improve character/environment/object art, animation, UI/UX and audio. Evaluate the baseline's style and readability, redraw or replace assets as needed, refine transitions and screen flows, and improve the sound design and mix. Listen on headphones and phone speakers, checking repeated movement, crowded combat, music joins, Spirit filtering and quiet dialogue. Retain provenance and accessibility settings; prototype assets are editable starting points.

Ronn: own and improve level design, narrative and QA. Play the full campaign, refine pacing, challenge, routes, obstacle placement, story letters, dialogue and hints through the generator/configuration/text sources. Run on a physical phone and gamepad; verify touch/notches/tablets, performance, pause/back, Continue/checkpoints, corrupt-save recovery and persistence after app kill. Automated campaign/save/input tests pass, but hardware checks remain unverified. Record defects with level, dimension, seed, device and reproduction steps, and regression-check design changes.

A complete interactive side-by-side playthrough of both old and new versions is not recorded; the old project's source/assets/layouts were compared and its stronger art recovered. Do not interpret integration or automated results as a perfection/store-release claim.

The user authorized committing and pushing this prototype baseline to the existing GitHub repository. Git history identifies the shared source snapshot. Build ZIPs/APKs under ignored `Builds/Presentation` remain local downloads to share separately; no teammate messages or store publication are part of this handoff.
