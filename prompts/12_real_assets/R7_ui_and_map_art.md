# Prompt R7: UI and level map art

Keep the M5 layouts (they pass the audit) and replace the art inside them. Art follows the "Art pipeline" in the README: SpriteCook's UI kit tools for the interface (concept, component sheets, extraction, finalize, with the 9-slice borders checked), and image generation for portraits, title and map. Estimate the credits and ask before each batch.

**Art**
1. A UI kit concept in the approved style, with capiz-pattern corners on the panels. From it, extract:
   - the one PixelBox panel (keep a single shared style, 9-slice border checked);
   - buttons (normal, pressed, disabled, focused) and toggles;
   - hearts, the touch buttons and the skill chips.

   Redraw the 8 skill icons to the bible, replacing the generated `icon_skill_*`.
2. Portraits at 24x24 for Tala (boy, girl), Lola, Anino, the Bellkeeper and the letter, each with 2 or 3 expressions.
3. Title art, character cards and Ending art.
4. A new level map: the three acts as one landscape (houses, the balete and the well, the church), node art (house, well, church bell), the path, and Lola's spot.

**Game work**
5. `SkpUiSetup` uses the new sprites. Every screen keeps its layout, the safe area and the 48 dp targets.
6. Dialog lines pick a portrait expression (a field on each line in the string table, default neutral).
7. `MapController` builds the map from the R1 map prefabs with the new art, replacing the code-drawn dots, frames and corners.
8. Keep the font (Press Start 2P, OFL) unless the bible says otherwise.

Tests: the M5 layout audit at all 5 resolutions with 0 problems; the 48 dp targets still hold; every dialog line's expression exists; `ArtRules` passes; a screenshot of every screen in `tests/Screenshots/r7` beside the old one; the full suites pass.

Done when: every screen in `reference/assets/screenshots/` has a matching Unity screen with the new art, and no prototype UI sprite is still in use, committed.
