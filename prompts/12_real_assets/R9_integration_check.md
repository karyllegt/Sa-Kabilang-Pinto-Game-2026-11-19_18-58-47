# Prompt R9: Integration and clean-up

1. Find every remaining use of a prototype, recoloured or generated stand-in, using `docs/05_PROTOTYPE_REBUILD_INVENTORY.md` as the checklist, and list each one with where it is used. Anything still in use gets replaced, or I am asked about it.
2. Remove the fallback wiring only, not the files. Once I confirm the list, unused prototype art and audio move out of `Assets/` to `reference/assets_retired/`, kept in git.
3. Re-run everything:
   - the full EditMode and PlayMode suites;
   - the layout audit;
   - the level bots 1 to 9, with the base kit and with all skills;
   - the scripted full run;
   - the zero-GC check;
   - the screenshot suite.
4. Check `docs/ART_MANIFEST.md` against the project:
   - every file under `Assets/Art/Game/` has a manifest row (asset id, model, raw file, credits, touch-up);
   - every raw file in `reference/spritecook_raw/` is listed;
   - the Credits screen names SpriteCook, its models and every audio source.

   Add an EditMode test that fails on an unlisted file.
5. Make `tests/Screenshots/r9/before_after.png`: every screen and one shot per level, the prototype state beside the new state.
6. Update:
   - the inventory doc, with a "done" column;
   - the `README.md` status and `DECISIONS.md`;
   - `Tests/MANUAL_QA.md`, with rows for an art and audio review on a phone.

Report: what was replaced, the test table (check, method, result, evidence), what could not be verified (looks and sound need a human), next step.

Done when: nothing in the inventory is still in use as a stand-in, all tests pass, committed.
