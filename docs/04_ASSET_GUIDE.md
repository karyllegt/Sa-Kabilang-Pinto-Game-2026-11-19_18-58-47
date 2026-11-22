# Asset guide: what is already provided, what you must still make

All provided assets were exported from the working browser prototype and live in `reference/assets/`. They are the game's v1 art and audio. Copy them into the Unity project (see the copy map below) and use them. Do not replace them with generic placeholders. Anything missing must be made in the same style: 16 px grid, hard pixels, no anti-aliasing, the palette below.

## Palette and rules
Gold `#e8b85a` and `#ffd36a` mean safe or interactive. Teal `#41e0d0` means Spirit. Violet `#c07dff` means Inverted. Red `#ff5a4a` means danger. Panels `#14101c` with gold border `#e8b85a` and brown inner line `#6b4a22`. Text `#f6e7c1`, shadows `#0a0710`. Reference size 480x270, 16 px tiles, 16 pixels per unit, point filtering, no compression.

## Copy map (reference/assets -> Unity project)
- `sprites/spr_pl_boy_0..3.png`, `spr_pl_girl_0..3.png` (16x18; frame 0 idle, 1 and 2 run, 3 air) -> `Assets/Art/Characters/`
- `sprites/spr_anino_0..1.png` (12x13), `spr_tik_0..1.png` (20x26), `spr_manan_0..1.png` (26x20), `spr_boss.png` (48x56), `spr_lola.png` (14x20) -> `Assets/Art/Enemies/` and `Assets/Art/Npc/`
- `sprites/spr_star.png` (flame pickup), `spr_shard.png` (fragment), `spr_clue.png` (letter) -> `Assets/Art/Items/`
- `sprites/tile_*_ledge48x16.png` (wood, crumble, moving, spirit, inv; the middle 16 px repeats), `tile_floor_block_64x48.png` -> `Assets/Art/Tiles/`
- `sprites/door_*.png`, `hazard_*.png` -> `Assets/Art/Props/`
- `sprites/portrait_*.png` (24x24 dialog portraits), `spr_lolaP.png` -> `Assets/Art/UI/Portraits/`
- `sprites/ui_heart_full.png`, `ui_heart_empty.png`, `ui_pixelbox_9slice_border6.png` (16x16, 9-slice border 6) -> `Assets/Art/UI/`
- `sprites/bg_act1..3.png` (480x270 parallax backdrops), `bg_map_overworld.png` (1440x270 scrolling level map), `title_art.png`, `char_card_*.png`, `fx_vignette.png`, `spr_menubg.png` -> `Assets/Art/Backgrounds/` and `Assets/Art/UI/`
- `audio/sfx_*.ogg` -> `Assets/Audio/Sfx/`, `audio/mus_*.ogg` -> `Assets/Audio/Music/`

(Done in M1: the starter importer now lives in `Assets/Editor/SkpAssetImportSettings.cs` and the `unity_starter/` copy was removed.) Copy `unity_starter/Assets/Editor/SkpAssetImportSettings.cs` into `Assets/Editor/` first so imports get the right settings (Point filter, 16 PPU, uncompressed, bottom-center pivot for characters, 9-slice border). Review it in the Editor; it was written without running Unity.

## Known gaps you must fill (same style, then add to `Assets/Art` or `Assets/Audio`)
- Sprites: animation frames for enemy attacks and the Bellkeeper phases (wave, bells, toll, stun, death), the Tala death and hurt frames, the lantern checkpoint (lit and unlit), the water surface, the bell and the church tower for the map, skill icons for the HUD chips (veil, bolt, double jump, dash, heart, triple bolt, greater veil, glide), the bolt projectile and impact, the veil ring, the glide wings, particles (flame, spark, ash), tile variety (several wood tops and undersides, wall and window tiles, balete roots for Act II, church stone for Act III), and a Spirit/Inverted tint material.
- Audio: the prototype music is generative random notes, so these OGGs are placeholders and do not loop seamlessly. Re-author or loop them properly (crossfade the ends) and keep one track per act plus menu and boss, with a Spirit filter and Inverted pitch-down on the mixer. Missing SFX: double jump, veil on and off, bolt fire and hit, burn, new skill fanfare, letter pickup, thorn burn, Bellkeeper toll hit. Use original or licensed audio and record licenses in Credits.
- Font: Press Start 2P (SIL OFL) is used by the prototype; download it, keep its license file, and record it in Credits.

## Known layout issues in the prototype (fix them in Unity, do not copy them)
1. On phones the touch cluster is crowded: VEIL and BOLT sit above DASH and WARD, and ENTER DOOR sits above JUMP, covering the play area. Design a cleaner thumb layout: left thumb move, right thumb jump, dash, interact, and a skill radial or two skill buttons, all at least 48 dp, mirrored for left-handed.
2. The hint toast and the objective line can crowd each other under the HUD. Give them separate lanes.
3. The map's bottom info panel hides the lower part of the map and node numbers; keep it shorter or move node labels above the nodes.
4. The seam between acts on the level map background is visible as a hard color change; blend it.
5. HUD skill chips are tiny at phone size; make them at least 48 dp tall on touch devices.
