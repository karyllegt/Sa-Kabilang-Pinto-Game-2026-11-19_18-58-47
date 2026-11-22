# What the Unity game rebuilt from the browser prototype

State on 2026-11-22, after M8 (commit `65aaa04`). This lists every asset, game object and system that still comes from `reference/prototype/index.html` or from `reference/assets/` (the prototype's exports). These are the pieces to upgrade to real game assets. The upgrade prompts are in `prompts/12_real_assets/`.

Legend: **COPY** = the prototype file, imported as is. **DERIVED** = made by a setup script by recolouring a prototype sprite. **GENERATED** = drawn pixel by pixel by a setup script (no source art). **CODE-DRAWN** = no sprite at all: a white square or a LineRenderer stretched and tinted at runtime. **PORT** = prototype logic or numbers rewritten in C#.

## 1. Art

### Characters, creatures, NPC (all COPY, 2 frames or 4, no animations for actions)
| Unity file | Size | What it covers | What it lacks |
| --- | --- | --- | --- |
| `Art/Characters/spr_pl_boy_0..3`, `spr_pl_girl_0..3` | 16x18 | idle, run 1, run 2, air | jump rise/fall split, land, dash, ward, hurt, death, door enter, glide pose, bolt cast, Inverted pose (sprite is just flipped) |
| `Art/Enemies/spr_anino_0..1` | 12x13 | 2-frame float | stunned, dissolve (bolt kill), shadow-chase variant |
| `Art/Enemies/spr_tik_0..1` | 20x26 | 2-frame walk | turn, stunned, attack |
| `Art/Enemies/spr_manan_0..1` | 26x20 | 2-frame flap | dive telegraph, dive, repelled by ward |
| `Art/Enemies/spr_boss` | 48x56 | one still frame | wake, shockwave, bell summon, toll wind-up, struck, defeat |
| `Art/Npc/spr_lola` | 14x20 | one still frame on the map | idle loop, talk |

### Tiles and environment
| Unity file | Kind | Note |
| --- | --- | --- |
| `Art/Tiles/tile_wood/crumble/moving/spirit/inv_ledge48x16` | COPY | one ledge strip each, middle 16 px tiled; no tops/undersides/variants |
| `Art/Tiles/tile_floor_block_64x48` | COPY | one ground block, tiled for every floor |
| `Art/Generated/tile_root_ledge48x16` (Act II) | DERIVED | wood recoloured green with moss dots |
| `Art/Generated/tile_stone_ledge48x16` (Act III) | DERIVED | wood recoloured grey with mortar lines |
| `Art/Backgrounds/bg_act1..3` | COPY | one flat 480x270 backdrop per act, no parallax layers |
| `Art/Backgrounds/bg_map_overworld` | COPY | 1440x270 map |
| `Art/Backgrounds/bg_map_overworld_blended` | DERIVED | the map with the act seams blended (M5 layout fix) |
| `Art/Backgrounds/spr_menubg`, `fx_vignette` | COPY | menu wall, vignette |
| `Art/Generated/prop_balete_roots_16x32`, `prop_church_candle_8x16` | GENERATED | the only decoration in levels |
| `Art/Generated/fx_water_surface_8x3` | GENERATED | flood surface strip |

### Props, hazards, items
| Unity file | Kind |
| --- | --- |
| `Art/Props/door_spirit/invert/home_16x28`, `door_exit_locked/open_20x32` | COPY (no open/close animation) |
| `Art/Props/hazard_spike_24x7`, `hazard_spike_gauntlet_72x7` | COPY |
| `Art/Props/hazard_root_dormant/warning/strike_12x16` | COPY |
| `Art/Props/hazard_thorns_16x130`, `hazard_thorns_burnt_16x130` | COPY |
| `Art/Generated/hazard_curtain_16x130` (veil gauntlet) | DERIVED from the thorns |
| `Art/Generated/hazard_bell_14x20` (Act III falling bell) | GENERATED |
| `Art/Items/spr_star` (flame), `spr_shard` (fragment), `spr_clue` (letter) | COPY, bob in code |
| `Art/Generated/spr_boss_stun`, `spr_boss_toll` | DERIVED (the boss tinted blue / gold) |
| `Art/Generated/fx_shockwave_12x12` | GENERATED |

### CODE-DRAWN (no art exists, built from a white pixel at runtime)
| Object | Where | Currently |
| --- | --- | --- |
| **Lantern checkpoint** | `LevelView.AddLantern` | a 2x34 px dark post with the flame star on top (prototype drawing; asset gap noted in M4-8) |
| **Flood water body** | `LevelView.AddWater` | a stretched dark-blue translucent rectangle |
| **Falling-bell warning line** | `LevelView.AddBell` | a red 2 px stretched rectangle |
| **Boss toll ring** | `BossView` | a gold LineRenderer circle |
| **Boss bell marks** | `BossView` | red stretched rectangles |
| **Map path dots, node frames, corners, Lola "!"** | `MapController` | white pixels scaled and tinted |
| **Spirit/Inverted look** | `DimensionMember` | sprite colour tint and alpha only (no material/shader) |

### FX and UI (GENERATED in M6/M8 unless noted)
- `Art/Fx/fx_bolt_8x4`, `fx_veil_ring_30`, `fx_glide_wings_20x6`, `fx_spark_3x3`, `fx_flame_3x4`, `fx_ash_2x2`, `fx_mote_2x2`, `fx_particles.mat`.
- `Art/UI/Skills/icon_skill_*` (8 skill icons), `Art/UI/map_node_bell`, `Art/UI/ui_white`.
- COPY: `Art/UI/ui_pixelbox_9slice_border6` (the one PixelBox), `ui_heart_full/empty`, `title_art`, `char_card_boy/girl`, `UI/Portraits/portrait_*` and `spr_lolaP` (24x24).
- Font: Press Start 2P (OFL), the prototype's font.

## 2. Audio
| Unity file | Kind |
| --- | --- |
| `Audio/Music/mus_menu, a1, a2, a3, boss.ogg` | COPY: the prototype's generative random-note music recorded to OGG |
| `Audio/Music/Loops/mus_*_loop.wav` | DERIVED: the same, trimmed and crossfaded to loop (M8) |
| `Audio/Sfx/sfx_*.ogg` (18: bell, clue, crumble, dash, die, door, flame, hurt, jump, kill, lantern, locked, select, shard, splash, stun, ward, win) | COPY: prototype WebAudio synth blips |
| `Audio/Sfx/Skills/*.wav` (10) and `Audio/Sfx/Generated/*.wav` (land, letter, toll, parry) | GENERATED: synthesized by the setup scripts in the same blip style |

## 3. Game objects and scenes
| Thing | How it is built now |
| --- | --- |
| Level layout (all 9 levels) | **PORT**: the prototype's seeded generator (`LevelGenerator`), plus M7 obstacle rules. No hand-made rooms, no Tilemap. |
| Ledges, hazards, doors, items, creatures, lanterns, bells, water, decoration | **R1 done:** prefabs in `Assets/Prefabs/Level`, chosen by `LevelPrefabCatalog`. The art inside them is still the prototype's. |
| Bolts | **R1 done:** `Bolt` prefab, 6 pooled when a level starts |
| Boss visuals | **R1 done:** prefabs in `Assets/Prefabs/Boss` (body with 3 states, shockwave, warning line, bell, toll ring), pooled up front |
| Anino shadow | `ShadowView` swaps the 2 Anino frames |
| Player | the player prefab (`Prefabs/Player/Player.prefab`), sprite swap by frame index in `PlayerView`, **no Animator** |
| Level map | **R1 done:** prefabs in `Assets/Prefabs/Map` (dot, node shadow, plate, icon, number, Lola, her marker, token, corner), placed at the prototype's node coordinates |
| UI screens | real prefabs (`Prefabs/UI/Screens/*`), built by `SkpUiSetup`; layout redesigned in M5, art still the prototype's PixelBox, hearts and portraits |
| Particles | **R1 done:** the `Level Fx` prefab (7 ParticleSystems), FeelTuning applied at runtime |

## 4. Behaviour and numbers ported from the prototype (keep these: they are the game, not placeholders)
`PlayerSim` (run, jump, coyote, buffer, dash, ward, hurt, death, lives), `PlayerTuning`, `DifficultyTable`, dimension masks and doors, the Anino replay delay, lanterns, `LevelGenerator` (proven identical on 30 prototype layouts), `LevelSpec` rows, crumble and moving ledges, roots, the 8 skills and `SkillTuning`, the flood, manananggal, falling bells, the Bellkeeper's state machine, the map walk (`MapModel`), save rules (`ProgressRules`), the strings and story text. Art upgrades must not change these numbers: collision boxes stay the sizes in `PlayerSim`/`PlayerWorld`, so new art is drawn to fit the existing hitboxes.

## 5. The current scene output (confirmed from the latest screenshots)
- Screens: `tests/Screenshots/m6/contact_sheet_2400x1080.png` (all menus, map, dialogs, HUD, letters, NEW SKILL, clear, pause, game over, ending).
- Levels: `tests/Screenshots/m7/contact_sheet_m7_levels.png` and `tests/Screenshots/m4_level*.png` (levels in all three acts, obstacles, boss).
- What they show: every screen exists and lays out cleanly. The levels read as flat strips of the same ledge over one backdrop per act, lanterns are sticks, the boss is one still sprite, and Tala has no action poses. That is the gap the upgrade prompts close.
