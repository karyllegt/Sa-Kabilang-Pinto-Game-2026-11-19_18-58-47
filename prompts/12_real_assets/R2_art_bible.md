# Prompt R2: Art bible, cleanup pipeline and approval sheet

Set the rules and the tooling, and show me a small sample, before any bulk art. Follow the "Art pipeline" section of `prompts/12_real_assets/README.md`.

1. **Gate.** Ask me to confirm that AI-assisted art is allowed for this CSEL project. Read SpriteCook's terms for using generated art in a game and record them in `DECISIONS.md`. Read the balance with `get_credit_balance`.
2. **Art bible.** Write `docs/06_ART_BIBLE.md`:
   - the 16 px grid and 16 PPU;
   - the palette from `docs/04_ASSET_GUIDE.md`, extended with per-act ramps (Act I house wood and lamplight, Act II balete greens and well water, Act III church stone and bell bronze);
   - the outline rule (1 px dark outline `#0a0710` or none: pick one), light direction, the dimension colours, maximum colours per sprite, animation frame rates;
   - every sprite size from the hitboxes in `PlayerSim` and `PlayerWorld`;
   - naming (`spr_<who>_<action>_<frame>`) and the folder layout under `Assets/Art/Game/`;
   - the game's own identity, in words: what makes Tala, the Anino and the house-towers look like this game and not a generic pixel platformer. Also a SpriteCook prompt template per asset type.
3. **Cleanup pipeline.**
   - Write `Assets/Editor/SkpArtCleanup.cs` (menu **Sa Kabilang Pinto > Art > Clean SpriteCook Downloads**). It takes `reference/spritecook_raw/` and writes cleaned PNGs to `Assets/Art/Game/`: exact size, grid snap, palette quantize, no partial alpha, pivot.
   - Write `Assets/Editor/SkpPixelText.cs` (menu **Sa Kabilang Pinto > Art > Build Text-Grid Art**). It converts every `.pxl.txt` grid in `Assets/Art/Source/` to a PNG in `Assets/Art/Game/`. The grid format: a palette header, then one character per pixel, with frames separated by a line. Add a test that a sample grid converts to the exact pixels.
   - Write `docs/ART_MANIFEST.md`.
   - Add an EditMode test `ArtRules` that checks every PNG under `Assets/Art/Game/`: point filter, no compression, 16 PPU, colours within the palette, no partial alpha, sizes as in the bible.
   - Add a test that runs the cleanup on a fixture image with known stray pixels and checks the result.
4. **Sample sheet.** Use about half the free credits at most, so some remain for the later key frames. Show the cost and ask before generating.
   - SpriteCook: 2 options for Tala idle at 16x18 (`pixel-engine-image-v1`) and 1 Act I backdrop slice.
   - Text grid: a third Tala option drawn as a grid, plus a lantern lit and unlit and one door, so I can judge how grid art looks.
   - Clean everything, and put it in `tests/Screenshots/r2/style_options.png` at 1x and 4x, beside the prototype versions.
   - Stop and ask me to pick. Save the picked images as the style reference: their asset ids go into the manifest as `style_asset_ids`, and the grids copy their proportions and shading.
5. Do not make bulk art in this prompt. Write the credit plan: the remaining balance, and which items from the README priority list it covers. Everything else is a text grid.

Tests: `ArtRules`, the cleanup test and the text-grid test pass; the full EditMode and PlayMode suites pass.

Done when: the bible, both art scripts, the manifest, the tests, the credit plan and the picked style exist, I have approved the look, committed.
