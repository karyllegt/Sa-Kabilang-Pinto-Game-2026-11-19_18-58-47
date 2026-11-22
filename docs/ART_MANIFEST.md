# Art manifest

Every game-ready PNG in `Assets/Art/Game/` (R2 onwards), where it came from, and what was done to it. The `ArtRules` test reads this table: every PNG must have a row, and its size must match.

SpriteCook rule: free plan only, never paid (`prompts/12_real_assets/README.md`). Free-plan generations are also shown in SpriteCook's public gallery.

## Credits

| Date | Balance before | Spent | Balance after | On |
| --- | --- | --- | --- | --- |
| 2026-11-22 | 40 | 12 | 28 | R2: two Tala idle options (pixel-engine-image-v1, 16x18, 6 each) |
| 2026-11-22 | 28 | 2 | 26 | R2: Act I backdrop sample (gpt-image-2, low, 1K) |

## Credit plan (after R2)

26 free credits left. The README priority list, with the cheapest model that fits each piece, all with the R2 picks as style references:

| # | Piece | Prompt | Model | Credits | Covered |
| --- | --- | --- | --- | --- | --- |
| 1 | Tala key frame | R2 | pixel-engine-image-v1 | 6 (spent) | done |
| 2 | Act I backdrop (far layer) | R2 | gpt-image-2 low 1K | 2 (spent) | done |
| 3 | UI panel concept | R7 | gpt-image-2 low 1K, mode ui | 2 | yes |
| 4 | Bellkeeper key frame, 48x56 | R4 | pixel-engine-image-v1 | 6 | yes |
| 5 | Act II and Act III far layers | R5 | gpt-image-2 low 1K | 2 + 2 | yes |
| 6 | Level map, two halves | R7 | gpt-image-2 low 1K | 2 + 2 | yes |
| | **Total still to spend** | | | **16** | **10 spare** (a retry or two) |

Everything else (all animation frames, other creatures, tiles, props, items, icons, particles, other portraits and the near backdrop layers) is a text grid. Each spend is still asked for first. 2K and 4K output need a paid plan, so every SpriteCook image is 1K.

## Files

| File | Size | Source | SpriteCook asset id / model / credits | Raw file | Hand-touched | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| Assets/Art/Game/Characters/spr_tala_girl_idle_a.png | 16x18 | SpriteCook | ab5f2792-5479-4b52-acef-8f582cdfd7d8 / pixel-engine-image-v1 / 6 | reference/spritecook_raw/Characters/spr_tala_girl_idle_a@16x18.png (sha12 d118f7830108) | no | **R2 pick: the style reference** (`style_asset_ids` for every later SpriteCook generation; grids copy its proportions and shading) |
| Assets/Art/Game/Backdrops/bg_act1_far.png | 480x270 | SpriteCook | dfb323aa-d110-42cf-a260-51dfbf4122aa / gpt-image-2 low 1K / 2 | reference/spritecook_raw/Backdrops/bg_act1_far@480x270.png (sha12 6c1647b44c3c) | no | **R2 pick: Act I far layer and the backdrop style reference**; delivered at 283x159, cleaned at 2x and centre-cropped |
| Assets/Art/Game/Props/prop_lantern_0.png | 16x40 | text grid `Assets/Art/Source/Props/prop_lantern.pxl.txt` | | | no | lantern unlit, redrawn in the option-A style |
| Assets/Art/Game/Props/prop_lantern_1.png | 16x40 | text grid `Assets/Art/Source/Props/prop_lantern.pxl.txt` | | | no | lantern lit, redrawn in the option-A style |
| Assets/Art/Game/Props/prop_door_spirit.png | 20x32 | text grid `Assets/Art/Source/Props/prop_door_spirit.pxl.txt` | | | no | Spirit door, redrawn in the option-A style |

## Unpicked R2 options (kept as records, not in the game)

| Option | Where | Why |
| --- | --- | --- |
| B (SpriteCook 046df41f-09b7-40f3-961c-a4d6407020a8, 6 credits) | reference/spritecook_raw/_unpicked/spr_tala_girl_idle_b@16x18.png | not picked; also 13 colours after cleanup, over the 12 limit |
| C (text grid) | reference/art_samples/spr_tala_girl_idle_option_c.pxl.txt | not picked |

## Audio

(R8.)
