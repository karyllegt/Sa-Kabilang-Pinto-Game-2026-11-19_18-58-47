# Real assets and game objects: upgrade prompts

These prompts take the game from its prototype state to a finished game with its own identity. That means more than new sprites: real prefabs, Animators, Tilemaps, materials, game code changes, audio and tests. Today the game uses prototype sprites, recoloured and code-drawn stand-ins, and objects spawned in code. The full list of what is still prototype is in `docs/05_PROTOTYPE_REBUILD_INVENTORY.md`.

Run them in order, one at a time, the same way as the milestones: paste the prompt, review the report, say "go" for the next. R1 comes first because every later step drops its art into the prefabs R1 creates. They can run before or after M9/M10; if M10 is already done, re-run its release checks afterwards.

| Prompt | Game work | Art source |
| --- | --- | --- |
| `R1_prefabs_and_catalog.md` | Every level, boss and map object becomes a prefab, listed in a `LevelPrefabCatalog` asset. No visual change. | none |
| `R2_art_bible.md` | Art bible, the cleanup pipeline and its tests, the art manifest, a sample sheet for approval. | SpriteCook (small sample) |
| `R3_tala_animation.md` | Tala's Animator, `PlayerAnimator`, hit-stop, select cards, map token. | SpriteCook base + cleanup |
| `R4_creatures_and_boss.md` | Animators for every creature, Lola and the Bellkeeper, telegraphs synced to the sim. | SpriteCook base + cleanup |
| `R5_environment_tiles.md` | Tilemaps with Rule Tiles, parallax system, seeded decoration, sprite atlases. | SpriteCook tilesets and backdrops |
| `R6_props_hazards_fx.md` | Prop state views, the dimension shader/material, lantern light, pooled particles. | SpriteCook props + cleanup |
| `R7_ui_and_map_art.md` | UI reskin on the M5 layouts, map built from prefabs. | SpriteCook UI kit, portraits, map |
| `R8_audio.md` | Composed music and a new sound set, loop points, mastering. | not SpriteCook (audio) |
| `R9_integration_check.md` | Remove stand-ins, full test run, before/after, report. | none |

## Art pipeline (the mix), used by R2 to R7

Read the SpriteCook skills before the first generation: `spritecook:spritecook-workflow-essentials`, then `spritecook-generate-sprites`, `spritecook-animate-assets`, `spritecook-generate-tilesets` or `spritecook-build-ui-kits` as the step needs.

1. **Gate: course rules.** Before R2 spends any credit, ask me to confirm that AI-assisted art is allowed for this CSEL project. If it is not, stop and switch the prompts to hand-drawn art.
2. **Who makes what.**
   - SpriteCook makes the big, low-risk pieces almost ready to use: backdrops and parallax layers, the level map, portraits, title, character-card and Ending art, tilesets, UI kit pieces, props and items.
   - SpriteCook makes only a *base* for characters and animation frames (Tala, creatures, the boss). Every frame then goes through cleanup (step 4). Frames that still fail are listed for a hand touch-up in Pixelorama or Aseprite. I may do that myself or tell you to do it.
3. **Models.** Use `pixel-engine-image-v1` for sprites at exact sizes (16 to 128 px, for example Tala at 16x18). Use a larger pixel-art model for backdrops, map and portraits. Always pass the approved R2 style sheet as `style_asset_ids` so every piece matches.
4. **Cleanup, a setup script (`SkpArtCleanup`) run on every downloaded image:**
   - snap to the exact size and the 16 px grid;
   - quantize to the art-bible palette;
   - remove partial alpha and stray pixels;
   - set the pivot to fit the existing hitbox;
   - write the result to `Assets/Art/Game/...` as a new file.

   The raw download goes to `reference/spritecook_raw/` (kept in git as the source). The `ArtRules` test (R2) checks every cleaned file.
5. **Credits budget.**
   - Before each batch, read the balance (`get_credit_balance`) and estimate the cost.
   - Ask me before any batch over 20 credits, or when the balance is short; never start a batch the balance cannot finish.
   - Generate the cheapest test first, then the rest.
   - The free tier (40 credits) covers about the R2 sample sheet only, so say what plan the next step needs.
6. **Manifest.** `docs/ART_MANIFEST.md` lists every generated asset: SpriteCook asset id, model, prompt, credits, raw file, cleaned file, and whether it was hand-touched. The Credits screen names SpriteCook and the models used. `DECISIONS.md` records SpriteCook's licence terms for game use, read from their site when R2 starts.

## Rules for all prompts (on top of `README.md`)
- Gameplay numbers and hitboxes do not change. New art is drawn to fit the existing boxes in `PlayerSim` and `PlayerWorld`. If art needs a different box, stop and ask.
- The prototype files in `reference/` and `Assets/Art` are never deleted or overwritten. New art goes in new files, and the catalog or `LevelArt` points at it. The old file stays as the fallback until R9.
- The game gets its own look. The prototype sprites guide only sizes, colour meaning (gold safe or interactive, teal Spirit, violet Inverted, red danger) and readability; they are not traced or upscaled. Everything stays in the Filipino-folklore world: house-towers, balete, well, church bell, Tala's parol, Lola, Anino, tikbalang, manananggal, the Bellkeeper.
- Every prompt ends with the full EditMode and PlayMode run, a screenshot before and after, the report (what was built, test table, what could not be verified, next step) and a commit.
