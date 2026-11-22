# R1 screenshots: before (M7 render) and after (prefabs)

| Shot | Pixels differing | Largest channel difference | Result |
| --- | --- | --- | --- |
| 00_level1_first_lantern.png | no pre-R1 render | | rendered for the record |
| 01_double_jump_climb.png | 80 | 5 | DIFFERENT |
| 02_veil_gauntlet.png | 2408 | 29 | DIFFERENT |
| 03_bolt_thorn_wall.png | 2472 | 29 | DIFFERENT |
| 04_dash_gap.png | 248 | 5 | DIFFERENT |
| 05_triple_bolt_pack.png | 2408 | 29 | DIFFERENT |
| 06_glide_gap.png | 2472 | 29 | DIFFERENT |
| 07_flood_level5.png | 2472 | 29 | DIFFERENT |
| 08_act3_falling_bell.png | 2532 | 29 | DIFFERENT |
| 09_manananggal.png | 0 | 0 | identical |
| 10_bellkeeper_toll.png | 0 | 0 | identical |
| 11_bellkeeper_shockwave_bells.png | 0 | 0 | identical |

## Why shots 01 to 08 differ

Every room puts its Home door on the same spot as an Enter door, both at sorting order 1 and depth 0. Unity broke that tie arbitrarily, and the renders before R1 did not even agree with each other: the M7 shots above drew the Enter door on top, the M5/M6 layout-audit shots drew the Home door's faint ghost on top. R1 makes it a rule that matches the prototype (it draws doors in list order, Home after Enter): the Home door sits 0.01 units nearer the camera (DECISIONS R1-9).

The only pixels that change are that one door rectangle: `diff_02_veil_gauntlet_door.png` shows them in magenta over the new render. Shots 09 to 11 have no such door in view and are identical. The world drawing is otherwise unchanged, which the PlayMode baseline test checks renderer by renderer for all nine levels, the Bellkeeper and the map.
