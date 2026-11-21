# Prompt R5: Environment, tiles and backdrops

Make each act look like its place: Act I the haunted house-towers, Act II the balete and the well, Act III the church and its bell. Consult the `unity:create-tile-palette-or-rule-tiles` and `unity:manage-sprite-atlas` skills. Art follows the "Art pipeline" in the README (here SpriteCook output needs light cleanup only). Estimate the credits per act and ask before each batch.

**Art**
1. Per act, a 16 px tile set, made with SpriteCook's tileset generation (`list_tileset_options` first, side-view platformer pieces), replacing the recoloured `tile_root_ledge` and `tile_stone_ledge`:
   - ledge top and underside, left and right ends, and middle variants;
   - the ground block with edges;
   - wall and window tiles for the room backs;
   - Spirit and Inverted ledge versions, and the crumble and moving ledges.

   Clean them with `SkpArtCleanup`.
2. Per act, 3 or 4 parallax backdrop layers at 480x270 (far sky, silhouettes, near room wall), made with SpriteCook using the R2 style reference, then cleaned. The layers must tile horizontally (check the seam).
3. Decoration sprites per act: hanging roots, candles, capiz lamps, picture frames, bell ropes.

**Game work**
4. Draw level geometry on Tilemaps. `LevelView` writes the generated ledges into a Tilemap with Rule Tiles (ends and variants pick themselves), instead of one stretched sprite per ledge. Collision stays in `PlayerWorld`; the Tilemap only draws. Moving and crumbling ledges stay as prefabs.
5. A parallax component moves the layers by factors kept in a data asset.
6. Decoration is placed by seeded rules per room, so each room differs but stays the same for a given seed. It never overlaps a hazard, door, item or the player's path.
7. A sprite atlas per act. Report draw calls and batches per level, before and after.

Tests: every generated ledge of levels 1 to 9 has tiles at exactly its rectangle (EditMode compares the Tilemap to the ledge rects); the same seed gives the same decoration; decoration never overlaps (test); backdrop layers tile without a visible seam (pixel test on the edges); `ArtRules` passes; screenshots of every level in `tests/Screenshots/r5`; the bots clear levels 1 to 9; full suites pass.

Done when: no level uses the prototype ledge strip or flat backdrop, committed.
