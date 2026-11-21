# Prompt R7: UI and level map art

Keep the M5 layouts (they pass the audit) and replace the art inside them. Art follows the "Art pipeline" in the README. SpriteCook is used only where the R2 credit plan covers it: one UI panel concept image, and the level map (state the cost and ask first). The UI kit tools cost more than the free plan has and are not used. Everything else is a text grid.

**Art**
1. A UI panel style with capiz-pattern corners. It follows the SpriteCook concept image if one was made, otherwise it is designed directly as a grid. Draw as text grids:
   - the one PixelBox panel (keep a single shared style, 9-slice border checked);
   - buttons (normal, pressed, disabled, focused) and toggles;
   - hearts, the touch buttons and the skill chips.

   Redraw the 8 skill icons to the bible, replacing the generated `icon_skill_*`.
2. Portraits at 24x24 for Tala (boy, girl), Lola, Anino, the Bellkeeper and the letter, each with 2 or 3 expressions.
3. Title art, character cards and Ending art, as text grids built from the new Tala, the lantern and the backdrop layers.
4. A new level map, from SpriteCook if the credits cover it (cleaned), otherwise assembled from the R5 backdrop layers and text-grid landmarks: the three acts as one landscape (houses, the balete and the well, the church), node art (house, well, church bell), the path, and Lola's spot.

**Game work**
5. `SkpUiSetup` uses the new sprites. Every screen keeps its layout, the safe area and the 48 dp targets.
6. Dialog lines pick a portrait expression (a field on each line in the string table, default neutral).
7. `MapController` builds the map from the R1 map prefabs with the new art, replacing the code-drawn dots, frames and corners.
8. Keep the font (Press Start 2P, OFL) unless the bible says otherwise.

Tests: the M5 layout audit at all 5 resolutions with 0 problems; the 48 dp targets still hold; every dialog line's expression exists; `ArtRules` passes; a screenshot of every screen in `tests/Screenshots/r7` beside the old one; the full suites pass.

Done when: every screen in `reference/assets/screenshots/` has a matching Unity screen with the new art, and no prototype UI sprite is still in use, committed.
