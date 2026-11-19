# Prompt 10: M10 Polish and release candidate

1. Run the entire EditMode and PlayMode suites, the screenshot suite and every item of `Tests/MANUAL_QA.md` (keyboard, gamepad and touch separately). Produce `Tests/RELEASE_REPORT.md` with pass/fail per check, the screenshots, and open bugs sorted by severity.
2. Fix every crash, soft lock and layout bug. Do not add features.
3. Balance pass using the debug overlay: difficulty should clearly rise level to level and room to room, Story must stay finishable with the base kit on the Spirit route, Nightmare must stay fair (every death attributable to a readable hazard).
4. Final checks: licenses in Credits, privacy text for a Play listing if needed, version number, build the signed release bundle (ask me for keystore details; never commit keystores or passwords).
5. Update `README.md` status, `DECISIONS.md`, and write `Tests/MANUAL_QA.md` ticks.

Done when: the release report shows no open crash or soft-lock bugs, committed.
