# Prompt 00: Kickoff (paste this first, once)

Read `README.md`, then every file in `docs/`, look at the screenshots in `reference/assets/screenshots/` and the sprites in `reference/assets/sprites/`, then open `reference/prototype/index.html` and read its code structure (do not edit it). Do not write any game code yet.

Then do these checks and report back in one message:
1. Which Unity version, render pipeline (URP 2D, URP, or Built-in) and packages does this project already use? Is the Input System package installed? Is TextMeshPro or UI Toolkit available? Is a 2D Pixel Perfect Camera available?
2. List the current folder structure and anything that already exists that could conflict with the folder map in `README.md`.
3. Summarize the game, the nine levels, the eight skills and the milestone order in your own words (10 lines maximum) so I can confirm you understood.
4. List any ambiguities, missing information or risks you see in the docs, and your proposed default for each. Do not ask more than five questions.
5. Confirm whether Node and Google Chrome are installed so `tests/prototype/verify_prototype.js` can be run, and run it now against `reference/prototype/index.html`; report the result (expected: 20 of 20 pass).
6. Propose the file/asset naming convention (PNG art, OGG audio) and where `DECISIONS.md` and `Tests/MANUAL_QA.md` will live.

Create `DECISIONS.md` with the answers and defaults. Do not start M1 until I say "go M1".
