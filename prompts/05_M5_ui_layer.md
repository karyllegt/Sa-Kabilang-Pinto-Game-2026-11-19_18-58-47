# Prompt 05: M5 UI layer (the best layout)

Build every screen in the "Layout and UI spec" of `docs/01_BUILD_SPEC.md`: Title menu box, Character select, Difficulty select, Level Map (overworld with nine nodes, Tala token walking the path, Lola with a "!" marker, top bar, bottom info panel), Dialog box with portrait and typewriter text, Before-stage dialog then circle wipe, Level Clear box (time, score, fragments, clue, Next Level, Replay, Map), HUD (hearts, flame bar, dimension label, level label, score, objective line, boss bar placeholder, pause button), Pause, Game Over, Settings (music, SFX, vibration, screen shake, high contrast, left-handed layout, touch controls), How to Play, Credits, toast popups, and circle wipes between all major screens with input blocked during a wipe.

Rules: one UI framework (consult `unity:ui`), one `PixelBox` 9-slice style, one pixel font under an open license (record the license), Scale With Screen Size at 960x540 reference, safe-area handling, 48 dp minimum touch targets, text never below 10 px at reference. Strings from one table. Lola's first-meeting dialog shows once (`metLola`); her lines change with progress (see prototype `talkLola`).

Tests and verification (all required): the screenshot test at 1280x720, 2400x1080, 2340x1080 with notch, 1024x768 and 800x480 for every screen; programmatic overlap, clipping, 48 dp and safe-area assertions; contrast checks; PlayMode flows Boot to Title to Map to Level to Clear to Map; locked node cannot be entered; double-tap ENTER starts the level once; Back/Escape behaves consistently; wipe blocks input. Save screenshots to `Tests/Screenshots/` and show me a contact sheet.

Done when: all tests pass, I have reviewed the screenshots, committed.

Use the provided UI art: `ui_pixelbox_9slice_border6`, hearts, portraits, title art, level-map background and the screenshots in `reference/assets/screenshots/` as the visual target. Match or beat every screenshot, and fix the known layout issues listed in `docs/04_ASSET_GUIDE.md`, especially the crowded phone touch layout.
