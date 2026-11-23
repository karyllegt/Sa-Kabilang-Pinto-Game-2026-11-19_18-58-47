# Presentation verification

Evidence for the expanded presentation overhaul. The persistent scope/checklist is `../UPGRADE_AUDIT.md`.

**Status: working prototype baseline for Mary and Ronn.** These results establish the current technical baseline. Mary retains art/animation/UI/UX/audio improvement ownership; Ronn retains level design/narrative/QA ownership. Automated passes do not constitute final team acceptance.

## Automated results

| Check | Actual result |
| --- | --- |
| Full EditMode | 177 cases: 175 initially passed; both failures repaired and rerun |
| Repair verification | 21 passed, zero failed |
| Full PlayMode | 48 passed, zero failed; one opt-in video recorder skipped |
| Snapshot comparison | 2 passed, zero failed across nine levels, boss states and map |
| Final five-resolution UI audit | **39,358 checks, zero problems, 240 screenshots** |
| Final capture regression | **1 passed, zero failed**, including opaque-panel pixel coverage; `presentation_ui_final.xml` |

Reports are under `tests/Results`: `presentation_editmode.xml`, `presentation_repaired_editmode.xml`, `presentation_playmode.xml`, `presentation_baseline_compare.xml` and `presentation_acceptance_summary.json`. The expensive full EditMode simulation suite was not repeated after UI/audio-only repairs. Its 175 passing results and the 21 repair checks are separate evidence, not additive unique-test counts.

The original failed checks were the Act II music seam loudness test and the UI layout audit. Silent mastering tails are now trimmed before the stereo crossfade; the notched title fits; the flame number has a dark backing. The same assertions pass without relaxed thresholds.

PlayMode includes `FullRunTests.ScriptedRun_Levels1To9_LettersSkillsExitsBossEnding_NoErrors`, save/scene flows, learned skills, dialogs, pause/input blocking and left-handed controls. The skipped test is `Video.ProgressVideoRun.Record`, which is explicitly opt-in.

## Actual visual review

Current world/action captures are in `tests/Screenshots/presentation`; five-resolution UI images/contact sheets are in its `ui` directory. Reviewed the title, selection/difficulty, map, letters/skills, HUD/touch, pause/settings, credits, clear/game-over and ending.

World review covers both avatars' 13 states, four-frame creatures, three acts, detailed doors/checkpoints/pickups, hazards, water/curtain, four boss states, Spirit and Inverted. The Spirit close-up shows the active-avatar silhouette, pale eyes and aligned feet; Inverted hangs from the solid underside. Original pre-overhaul snapshots remain preserved separately.

An audit-only ordering error repeatedly subtracted a world-sprite sorting offset as it visited resolutions. Some signed sorting orders wrapped above the camera-space UI. The offset now uses an idempotent threshold, preserving the original materials and lighting. Runtime overlay canvases are unchanged. Added opaque-panel pixel checks to detect missing UI or world overdraw; a two-screen preview across all five resolutions precedes the complete rerun. Map inspection also disables pixel-perfect camera adjustment before positioning the edit-mode capture.

## Audio checks

Sources, licences and processing are in `../AUDIO_SOURCES.md`. Active audio uses licensed physical recordings, outdoor crickets and five human-composed Kevin MacLeod themes. No AI audio generation is used. Original MP3s remain unmodified.

Five stereo loops trim faded mastering tails before two-second equal-power joins and normalize to 0.85 sample peak. Boundary quarter-second RMS differences are below 4.24 dB; sample jumps are below 0.0025. Unity seam/headroom checks pass. Music imports as streamed stereo Vorbis. Signal checks do not establish perceived mix or listening acceptance.

Footsteps require grounded travel and stop in air/dash/water/stopped simulation. Splash is entry-triggered; falling bells use the actual credited church-bell recording. Mixer controls, dimension filters, pause ducking and reduced-flash/shake behavior are preserved.

## Builds

- **Windows:** Unity 6000.6.6f1; succeeded with zero errors and zero warnings. Native player ran for 56 seconds, initialized Direct3D 11 and completed managed scene startup with no logged errors. This was a batch startup smoke check, not an interactive playthrough or listening test.
- **Android:** an isolated source copy built with installed 6000.6.2f1 tooling; the main project remains on 6000.6.6f1. Final APK: **78,627,562 bytes**, ARM64 IL2CPP, minimum API 26, target API 36, separate package `com.pangkatparol.sakabilangpinto.playtest`. Build succeeded with **zero errors and one diagnostic-symbol advisory**. APK v2 signature, manifest and adaptive foreground/background layers all passed inspection; `presentation_android_verification.json` records its SHA256.
- Build reports: `presentation_windows_build.txt`, `presentation_android_build.txt`; native startup report: `presentation_windows_smoke.json`. APK file size is measured directly; Unity's aggregate build-report byte count includes additional build output and is not the APK size.

The initial five Android warnings are superseded by the final report. Adaptive layers are present; the three TextMeshPro IL2CPP advisories did not recur in the incremental build. The remaining advisory recommends diagnostic symbols for detailed crash reports. Gradle also reports deprecated Unity-generated Kotlin/Jetifier options for future plugin versions; these did not fail the build.

## Delivery packages

`Builds/Presentation/SaKabilangPinto-Playtest-20261124-0751.zip` contains both players, notices, handoff and evidence. `SaKabilangPinto-UnitySource-20261124-0751.zip` contains editable Assets/Packages/ProjectSettings, tools, documents and evidence without generated Library/Builds/.git data. SHA256 manifest: `handoff-sha256-20261124-0751.json`. The external archive-validation report, generated after packaging, is `tests/Results/presentation_package_verification.json`; its delivery copy sits beside the ZIPs as `handoff-verification-20261124-0751.json`.

Modified Unity metadata was mechanically stripped of trailing whitespace with editors closed. `git diff --check` passed. Core movement, generation, level session and save source were compared to the repository baseline and have no diff. The user authorized committing and pushing the prototype baseline; consult Git history for the shared source revision. Build archives are local artifacts, excluded from Git.

## Unverified hardware acceptance

ADB reports no attached phone. Physical Android performance, touch/safe areas and save-after-app-kill; physical gamepad behavior; and headphone/phone-speaker listening remain unverified. Automated campaign/input/save tests do not replace these hardware checks.

Unity's matching Android module installer failed twice; the isolated installed-editor fallback produced the actual APK. Package recovery restored the exact official Input System 1.20.0 source and rescanned its stale index; no gameplay data or source was removed to hide the package fault.
