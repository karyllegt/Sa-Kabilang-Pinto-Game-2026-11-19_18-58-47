# Art bible: Sa Kabilang Pinto

The rules every new sprite follows (R2 to R7). The prototype's sprites only fix sizes, colour meaning and readability; the look itself is the game's own.

## 1. Identity in words

A Filipino town on fiesta night, seen through a child's lantern. Warm lamplight against deep violet night; capiz-shell windows; old narra wood; the balete's hanging roots; a church of grey stone and bronze bells. Tala carries a star-shaped **parol** (her lantern); light is safety, so gold always means "safe or yours to use". The spirit world is cool teal, the inverted world violet, danger red. Shapes are chunky and readable at 1x on a phone: a creature is recognisable from its silhouette alone.

- **Tala**: a small, round-faced child in a cream camisa with a red sash (girl: red patadyong skirt; boy: red shorts), black hair, bare feet, the gold parol held up. Big readable head (about 7 px of her 18).
- **Anino**: a living shadow, Tala's own outline in ink with two pale eyes; edges flicker.
- **Tikbalang**: tall, horse-headed, long limbs, brown with a mane; it walks hunched.
- **Manananggal**: the winged upper half of a woman, bat wings, long hair, no legs (a trailing torn edge).
- **The Bellkeeper**: a bronze bell-shaped body with a stone face and rope arms, tied to the church tower.
- **Lola**: a small grandmother in a lilac baro with a white bun and a cane.
- **Places**: Act I house-towers (wood, capiz windows, lamps), Act II the balete and the well (roots, moss, dark water), Act III the church (stone, candles, bells).

## 1b. The picked style (R2, 2026-11-22)

- **Characters, creatures, props:** Tala option A, SpriteCook asset `ab5f2792-5479-4b52-acef-8f582cdfd7d8` (`Assets/Art/Game/Characters/spr_tala_girl_idle_a.png`). Pass it as `style_asset_ids` to every later SpriteCook generation; text grids copy its proportions (head about 6 px of 18), full outline and two-tone shading. The lantern and the Spirit door grids are already redrawn to match it.
- **Backdrops:** the Act I far layer, SpriteCook asset `dfb323aa-d110-42cf-a260-51dfbf4122aa` (`Assets/Art/Game/Backdrops/bg_act1_far.png`): the style reference for the Act II and III backdrops and the level map.
- Approval sheet: `tests/Screenshots/r2/art_bible_sheet.png` (the options it was picked from: `tests/Screenshots/r2/style_options.png`).

## 2. Grid, size, import

- 16 px per tile, **16 pixels per unit**, point filter, no compression, no mipmaps (the import rules in `SkpAssetImportSettings` already do this for `Assets/Art/`).
- **No anti-aliasing and no partial transparency.** Every pixel is a palette colour at full opacity, or fully clear. The only exceptions are in-engine effects (ghost alpha, glows), never the PNG.
- **Outline rule: a full 1 px outline in ink `#0a0710` on characters, creatures, items and props** (as the picked Tala, option A). Tiles and backdrops have no outline.
- **Shading: two tones per material** (a base and one shadow, plus a highlight only for glow and eyes), chunky simple shapes.
- **Light comes from the upper left**, plus Tala's parol in her own sprites.
- At most **12 colours per sprite** (outline included), **16 for the Bellkeeper and portraits**.

## 3. Palette

Every pixel uses one of these colours. The text-grid art uses the symbol; `Assets/Art/Source/palette.txt` is the machine copy (the tests read it).

| Sym | Hex | Use |
| --- | --- | --- |
| `.` | (clear) | transparent |
| `k` | `#0a0710` | ink: outline, deepest shadow |
| `K` | `#14101c` | panel night |
| `n` | `#241a2e` | night violet |
| `N` | `#3a2f4a` | dusk violet |
| `s` | `#f0c08a` | skin light |
| `S` | `#c98a5a` | skin mid |
| `z` | `#8a5434` | skin shadow |
| `h` | `#2a1a14` | hair |
| `H` | `#4a2e1f` | hair light |
| `r` | `#c0392b` | red cloth |
| `R` | `#8a2420` | red cloth shadow |
| `w` | `#f6e7c1` | cream cloth, text |
| `W` | `#d9c49a` | cream shadow |
| `p` | `#fff8e8` | bone white, eye shine |
| `g` | `#e8b85a` | gold (safe, interactive) |
| `G` | `#ffd36a` | gold bright |
| `y` | `#a8782e` | gold shadow |
| `b` | `#6b4a22` | brown line |
| `l` | `#ffefb0` | flame core |
| `L` | `#ff9f3a` | flame orange |
| `4` | `#3a2410` | deep wood |
| `1` | `#5a3820` | dark wood |
| `2` | `#8b5a2b` | wood |
| `3` | `#b07a3e` | light wood |
| `5` | `#1e2e22` | balete deep green |
| `6` | `#2e4a2c` | balete green |
| `7` | `#4a6a3a` | moss |
| `8` | `#7a9a4a` | leaf light |
| `9` | `#1a3a4a` | well water deep |
| `0` | `#2e5a6a` | well water |
| `c` | `#3a3444` | church stone dark |
| `C` | `#5a5466` | church stone |
| `v` | `#8a8496` | church stone light |
| `x` | `#8a5a2a` | bronze |
| `X` | `#c08a44` | bronze light |
| `t` | `#41e0d0` | Spirit teal |
| `T` | `#1f8a80` | Spirit teal shadow |
| `u` | `#c07dff` | Inverted violet |
| `U` | `#6a3ea0` | Inverted violet shadow |
| `d` | `#ff5a4a` | danger red |
| `D` | `#a02a22` | danger red shadow |
| `m` | `#c8a8e0` | lilac (Lola) |
| `M` | `#8a6aa8` | lilac shadow |

**Colour meaning is fixed:** gold = safe or interactive (lanterns, doors home, the letter, flame stars), teal = Spirit, violet = Inverted, red = danger. Nothing harmful is gold; nothing safe is red.

**Per-act ramps.** Act I: `4 1 2 3` wood with `l L g G` lamplight on `n N` night. Act II: `5 6 7 8` greens with `9 0` water and `1 2` root wood. Act III: `c C v` stone with `x X` bronze and `l L` candlelight.

## 4. Sizes: the canvas stays, the hitbox rules

New art keeps the **current canvas size and pivot** of the sprite it replaces, so it drops into the R1 prefabs without moving anything. The hitbox is the part that touches; the drawing may overhang it by the margins below, never shrink inside it.

| Sprite | Canvas (px) | Pivot | Hitbox in code (px) |
| --- | --- | --- | --- |
| Tala (boy, girl), every frame | 16 x 18 | bottom centre | 10 x 14 (`PlayerTuning.hitbox`) |
| Anino | 12 x 13 | bottom centre | 10 x 11 |
| Tikbalang | 20 x 26 | bottom centre | 14 x 24 |
| Manananggal | 26 x 20 | bottom centre | 20 x 14 |
| The Bellkeeper | 48 x 56 | bottom centre | 40 x 50 (`Bellkeeper.BodySize`) |
| Lola | 14 x 20 | bottom centre | (none) |
| Flame star / fragment / letter | 9 x 11 / 7 x 7 / 8 x 9 | centre | 14 x 14 pickup |
| Door (Spirit, Inverted, Home) | 20 x 32 | centre | 16 x 28 |
| Exit door (locked, open) | 24 x 36 | centre | 20 x 32 |
| Falling bell | 14 x 20 | centre | 14 x 16 (`BellDrop.Size`) |
| Lantern (new) | 16 x 40 | bottom centre | (none; reached at its post) |
| Ledge strip | 48 x 16 | centre, middle 16 px repeats | the ledge rectangle |
| Ground block | 64 x 48 | centre | the floor rectangle |
| Spike / gauntlet | 24 x 10 / 72 x 28 | centre, tiled | the hazard rectangle |
| Root (3 phases) | 16 x 20 | centre | the hazard rectangle |
| Thorn wall (whole, burnt) | 16 x 130 | centre | the hazard rectangle |
| Portrait | 24 x 24 | centre | |
| Backdrop layer | 480 x 270 | centre, tiles horizontally | |

## 5. Animation

Frame rates are tied to the 60 Hz simulation so they never drift: idle 6 fps (10 ticks a frame), run 10 fps (6 ticks), creatures 4 fps (`tick >> 4`, as today), flames 8 fps. Frame counts per animation are in the R3 and R4 prompts. A frame never changes the pivot point (feet stay on the same pixel row).

## 6. Naming and folders

- Names: `spr_<who>_<action>_<frame>` (`spr_tala_girl_run_3`), `tile_<act>_<piece>`, `prop_<what>_<state>`, `bg_<act>_<layer>`, `portrait_<who>_<mood>`, `icon_<what>`.
- Sources: text grids in `Assets/Art/Source/<group>/<name>.pxl.txt`; SpriteCook raw downloads in `reference/spritecook_raw/` (kept in git, never imported).
- Game-ready PNGs: `Assets/Art/Game/<group>/` (Characters, Creatures, Props, Tiles, Backdrops, UI, Fx). Only these are used by the game; the prototype art in the older folders stays as the fallback until R9.

## 7. How a sprite is made

1. **Text grid** (default, free): written as `.pxl.txt` (format in `Assets/Art/Source/README.md`) and built to PNG by **Sa Kabilang Pinto > Art > Build Text-Grid Art** (`SkpPixelText`).
2. **SpriteCook** (free credits only, for the pieces that set the look; priority list in `prompts/12_real_assets/README.md`): downloaded to `reference/spritecook_raw/`, then **Sa Kabilang Pinto > Art > Clean SpriteCook Downloads** (`SkpArtCleanup`) snaps it to the size, quantises it to this palette, drops partial alpha and writes the PNG to `Assets/Art/Game/`.
3. Every file is listed in `docs/ART_MANIFEST.md`, and the `ArtRules` test checks every PNG in `Assets/Art/Game/` against sections 2 to 4.

### SpriteCook prompt template

> `<subject from section 1>, side view, 2D pixel art game sprite, <W>x<H> pixels, 1 px dark outline, limited palette of warm lamplight gold, night violet <+ act ramp>, light from upper left, transparent background, no anti-aliasing, Filipino fiesta night folklore`

Backdrops: `<act place>, side-scrolling game background layer, 480x270, pixel art, deep violet night, warm lamplight, no characters, seamless left-right, Filipino town folklore`.

## 8. 3x detail art (A-1 to A-7, 2026-11-23)

The quality bar moved to detailed sprites drawn **natively at 3x**: 48 pixels per unit, the same world size as the 16 px art (a 16x18 px box becomes a 48x54 canvas). Tala is the first, and the model for every 3x sprite:

- **Where**: `Assets/Art/Game/<Group>48/` (Tala: `Characters48`), imported at 48 PPU with a bottom-centre pivot (`SkpAssetImportSettings.HiDetailDir`). The camera renders 1440x810 at 48 PPU, so every 3x pixel shows and 16 px art draws as 3x3 blocks.
- **How**: hand-pixelled full grids, one character per pixel (`tools/tala_hand/tala_hero3.js`), never formula rigs or generated images. Animation frames reuse the approved grids and change only what moves (`tala_anim.js`: leg poses, bob, swings, trails).
- **Look**: warm dark selective outline (`#2a1420`), 3 to 4 tones per material with light from the parol side, clean deliberate clusters (no noise or dither speckle), friendly faces (small warm eyes with a glint, blush), no partial alpha.
- **Palette**: its own ramp per character family (Tala: `Assets/Art/Source/palette_tala48.txt`, 35 colours, max 40 per frame), checked by `ArtRulesTests`; keep the bible's meaning colours (gold = safe, teal = Spirit, violet = Inverted, red = danger) for anything interactive.
- **Review**: approve the hero frame area by area (head, top, lower half, prop) before drawing the animation; preview sheets in `tests/Screenshots/<task>/`.
