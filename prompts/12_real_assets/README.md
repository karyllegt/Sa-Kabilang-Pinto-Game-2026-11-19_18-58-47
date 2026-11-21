# Real assets and game objects: upgrade prompts

These prompts take the game from its prototype state to a finished game with its own identity. That means more than new sprites: real prefabs, Animators, Tilemaps, materials, game code changes, audio and tests. Today the game uses prototype sprites, recoloured and code-drawn stand-ins, and objects spawned in code. The full list of what is still prototype is in `docs/05_PROTOTYPE_REBUILD_INVENTORY.md`.

Run them in order, one at a time, the same way as the milestones: paste the prompt, review the report, say "go" for the next. R1 comes first because every later step drops its art into the prefabs R1 creates. They can run before or after M9/M10; if M10 is already done, re-run its release checks afterwards.

| Prompt | Game work | Art source |
| --- | --- | --- |
| `R1_prefabs_and_catalog.md` | Every level, boss and map object becomes a prefab, listed in a `LevelPrefabCatalog` asset. No visual change. | none |
| `R2_art_bible.md` | Art bible, the cleanup pipeline and its tests, the art manifest, a sample sheet for approval. | SpriteCook free credits: Tala key frame, Act I backdrop |
| `R3_tala_animation.md` | Tala's Animator, `PlayerAnimator`, hit-stop, select cards, map token. | text-grid frames from the SpriteCook key frame |
| `R4_creatures_and_boss.md` | Animators for every creature, Lola and the Bellkeeper, telegraphs synced to the sim. | text-grid frames; Bellkeeper key frame from SpriteCook if credits allow |
| `R5_environment_tiles.md` | Tilemaps with Rule Tiles, parallax system, seeded decoration, sprite atlases. | text-grid tiles; backdrops from SpriteCook if credits allow, otherwise text-grid layers |
| `R6_props_hazards_fx.md` | Prop state views, the dimension shader/material, lantern light, pooled particles. | text-grid props and particles |
| `R7_ui_and_map_art.md` | UI reskin on the M5 layouts, map built from prefabs. | UI panel concept and map from SpriteCook if credits allow; text-grid icons and portraits |
| `R8_audio.md` | Composed music and a new sound set, loop points, mastering. | not SpriteCook (audio) |
| `R9_integration_check.md` | Remove stand-ins, full test run, before/after, report. | none |

## Art pipeline (the mix), used by R2 to R7

Read the SpriteCook skills before the first generation: `spritecook:spritecook-workflow-essentials` and `spritecook-generate-sprites`. Animation, tileset and UI-kit generation cost more credits than the free plan has, so they are not used.

1. **Gate: course rules.** Before R2 spends any credit, ask me to confirm that AI-assisted art is allowed for this CSEL project. If it is not, stop and switch the prompts to hand-drawn art.
2. **SpriteCook free plan only. Never buy credits or a subscription.** On 2026-11-22 the free plan had 40 credits, about 5 to 7 images. So SpriteCook makes only the pieces that set the look, in this priority order, one image each:
   1. Tala key frame (the R2 style pick);
   2. one Act I backdrop;
   3. the UI panel concept;
   4. the Bellkeeper key frame;
   5. Act II and Act III backdrops;
   6. the level map.

   Spend credits only from this list, and ask me before each spend. If the free credits refresh, spend each refresh on the next items in the list. Record the refresh date in the manifest.
3. **Everything else: text-grid pixel art.** Animation frames, other creatures, tiles, props, items, icons, particles, other portraits, and any listed piece the credits did not reach are drawn here as text grids.
   - A grid is a `.pxl.txt` file in `Assets/Art/Source/`: one character per pixel, mapped to the art-bible palette.
   - An editor script, `SkpPixelText`, converts each grid to a PNG.
   - The grids are original, cost nothing, can be read and changed in git, and match the palette exactly.
   - SpriteCook key frames are the style reference: grids copy their proportions, colours and shading.
   - I may touch up frames, or ask you to, in free tools (Pixelorama or LibreSprite).
4. **Models (for the free credits).** Use `pixel-engine-image-v1` (6 credits) for exact-size sprites (16 to 128 px, for example Tala at 16x18). Use a cheap pixel-art option for backdrops at low quality first. Pass the approved R2 pick as `style_asset_ids` so every piece matches.
5. **Cleanup, a setup script (`SkpArtCleanup`) run on every SpriteCook download:**
   - snap to the exact size and the 16 px grid;
   - quantize to the art-bible palette;
   - remove partial alpha and stray pixels;
   - set the pivot to fit the existing hitbox;
   - write the result to `Assets/Art/Game/...` as a new file.

   The raw download goes to `reference/spritecook_raw/` (kept in git as the source). The `ArtRules` test (R2) checks every cleaned file.
6. **Credits.**
   - Before every generation, read the balance (`get_credit_balance`), state the cost, and ask me.
   - Never start a generation the balance cannot finish.
   - Never suggest buying credits or a plan. When the credits run out, the remaining items are made as text grids.
7. **Manifest.** `docs/ART_MANIFEST.md` lists every art file:
   - its source: SpriteCook (asset id, model, prompt, credits, raw file) or a text grid (`.pxl.txt` path);
   - the cleaned PNG;
   - whether it was hand-touched. The Credits screen names SpriteCook and the models used. `DECISIONS.md` records SpriteCook's licence terms for game use, read from their site when R2 starts.

## Rules for all prompts (on top of `README.md`)
- Gameplay numbers and hitboxes do not change. New art is drawn to fit the existing boxes in `PlayerSim` and `PlayerWorld`. If art needs a different box, stop and ask.
- The prototype files in `reference/` and `Assets/Art` are never deleted or overwritten. New art goes in new files, and the catalog or `LevelArt` points at it. The old file stays as the fallback until R9.
- The game gets its own look. The prototype sprites guide only sizes, colour meaning (gold safe or interactive, teal Spirit, violet Inverted, red danger) and readability; they are not traced or upscaled. Everything stays in the Filipino-folklore world: house-towers, balete, well, church bell, Tala's parol, Lola, Anino, tikbalang, manananggal, the Bellkeeper.
- Every prompt ends with the full EditMode and PlayMode run, a screenshot before and after, the report (what was built, test table, what could not be verified, next step) and a commit.
