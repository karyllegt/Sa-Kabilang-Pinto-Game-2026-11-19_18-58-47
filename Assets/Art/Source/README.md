# Text-grid pixel art

Sprites drawn as text, one character per pixel, using the palette symbols in `palette.txt` (from `docs/06_ART_BIBLE.md`). **Sa Kabilang Pinto > Art > Build Text-Grid Art** (`SkpPixelText`) turns every `*.pxl.txt` here into PNGs in `Assets/Art/Game/<out>/`.

```
# a comment
name: spr_tala_girl_idle
size: 16x18
out: Characters
frame
................
......kkkk......
(18 rows of 16 symbols)
frame
(the next frame, same size)
```

- `name`: the PNG name. One frame writes `<name>.png`; several write `<name>_0.png`, `<name>_1.png`, ...
- `size`: width x height in pixels; every frame must have exactly that many rows and columns.
- `out`: the folder under `Assets/Art/Game/` (Characters, Creatures, Npc, Props, Items, Tiles, Backdrops, UI, Fx). Characters, Creatures and Npc get a bottom-centre pivot; everything else is centred.
- `.` is clear; every other symbol must be in `palette.txt`. A wrong row length or an unknown symbol is reported and nothing is written for that file.

## 3x art (48 px per unit)

`palette_tala48.txt` is the palette of the hand-pixelled 48x54 Tala in `Assets/Art/Game/Characters48/`. Those frames are not built from `*.pxl.txt` files: they come from `tools/tala_hand/tala_hero3.js` (the approved grids) and `tools/tala_hand/tala_anim.js` (every animation), exported with `node tools/tala_hand/tala_anim.js export Assets/Art/Game/Characters48`. See `docs/06_ART_BIBLE.md` section 8.
