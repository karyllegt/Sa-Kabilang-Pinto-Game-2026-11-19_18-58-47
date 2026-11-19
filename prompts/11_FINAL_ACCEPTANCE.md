# Prompt 11: Final acceptance (run last, and any time you want a health check)

You are the QA lead now. Prove the game is in a good state; do not assume. Work through this and fix what fails, then re-run until it passes.

1. **Build and boot**: clean build for the Editor and for Android (development and release). Zero compile errors and zero new warnings. Boot to the Title scene with no console errors.
2. **Automated suites**: run all EditMode and PlayMode tests through the Unity CLI and show the pass/fail counts. Re-run `tests/prototype/verify_prototype.js` and make sure the Unity game matches the same behaviors (boots with a corrupt save, all levels and difficulties build and run, the full UI flow, letter grants a skill, double jump once, veil immunity, bolt cost, clear box, unlock, locked node, pause and resume, ending saves cleared, veil spikes crossable, thorns burnable).
3. **Completability**: a bot completes every level 1 to 9 on Standard with the skills the level expects; the fallback route completes with no skills; the Bellkeeper dies and the ending plays.
4. **Visual verification**: capture every screen at 1280x720, 2400x1080, 2340x1080 with a notch, 1024x768 and 800x480, compare each with `reference/assets/screenshots/`, and list differences. Run the automated overlap, clipping, 48 dp, safe-area and contrast assertions. Show me a contact sheet. Verify the sprites in game: pixel-perfect (no blur, no shimmering when the camera moves), correct pivots (feet on the ledge, no floating or sinking), correct sorting order (player above ledges and below the HUD), correct animation frames, correct flipping in Inverted gravity.
5. **Gameplay objects**: for every game object type (ledges, crumbling and moving ledges, doors, lanterns, flame, fragments, letters, spikes, spike fields, roots, thorn walls, Anino, Tikbalang, Manananggal, Bellkeeper, bolts, water) verify collider shape, trigger or solid behavior, visibility per dimension, and that damage goes through `CanBeHurt()`.
6. **Skills**: every skill and every skill obstacle, with and without the skill, mirrored in Inverted.
7. **Input**: keyboard, gamepad, touch (multi-touch jump plus move plus bolt), left-handed layout.
8. **Audio**: every SFX plays once per event, music changes per act and dimension, toggles work, no clipping, ducking on pause.
9. **Save and lifecycle**: kill the app mid-level and relaunch (Continue works), delete the save (clean start and Lola's first meeting), background and resume, rotate, low memory.
10. **Performance**: report average and 1 percent low fps on the device or emulator, GC allocation per frame in gameplay (target 0), memory, build size.
11. Write `Tests/RELEASE_REPORT.md`: a table of every check with pass or fail and evidence, all screenshots, open bugs by severity, and what could not be verified and why. Be honest: do not mark anything passed that you did not run.

Finish by committing and telling me the one next action.
