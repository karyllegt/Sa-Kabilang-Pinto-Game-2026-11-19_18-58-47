# Numbers from the browser prototype (port these, then tune in Unity)

The prototype runs at a fixed 60 Hz with 16 px tiles on a 480x270 canvas. Convert per-frame values to per-second by multiplying by 60, and px to Unity units by dividing by your pixels-per-unit (use 16).

## Player
- Size 10x14 px. Max run speed 1.95 px/frame. Ground acceleration 0.6, air acceleration 0.38. Ground friction x0.72 per frame, air x0.92.
- Gravity 0.42 px/frame^2. Terminal fall speed 6.6 px/frame.
- Jump impulse 7.3 px/frame (about 3.9 tiles high at full hold). Releasing jump early multiplies upward speed by 0.45 once.
- Coyote time 6 frames. Jump buffer 7 frames.
- Dash: 11 frames at 5.4 px/frame, cooldown 34 frames, costs 20 flame, untouchable while dashing. Swift Dash skill: cost 8, cooldown 20.
- Ward: pulse lasts 24 frames, hit radius 46 px, cooldown 72 frames. Stuns creatures, kills Anino, parries the Bellkeeper toll.
- Hurt: 1 damage, knockback vy -3.2, vx 2.4 away from source, invulnerable 85 frames (110 on Story).
- Flame: max 100 (rises 5 per fragment, cap 160), regenerates +1 every 90 frames, lantern lit gives +30 flame and +1 vitality.

## Skills (added)
- Double jump: second impulse 6.7 px/frame, once per airtime, not on the same frame as a ground or coyote jump.
- Veil: 180 frames (300 Greater) immunity, cooldown 900 (600 Greater).
- Bolt: speed 4.4 px/frame, life 56 frames, cost 8 flame (12 triple), cooldown 16 (22 triple) frames; triple fan angles -0.24, 0, +0.24 rad.
- Glide: while holding jump and falling faster than 1.1, clamp fall speed to 1.1.
- Burn: roots 420 frames, thorn walls 600 frames.

## Dimensions and Anino
- Masks: Ordinary 1, Spirit 2, Inverted 4. Inverted flips gravity sign. Doors switch dimension; lanterns return you to Ordinary.
- Shadow chaser replays the player's position from N frames ago: Story 300, Standard 240, Nightmare 180, Endless 200; shortens 3.5 percent per level (minimum 100). Ward stuns the shadow for about 190 frames.

## Difficulty table (vitality, lives, speed mult, chase frames, flood mult, boss hp, hazard mult, enemy mult)
- Story: 6, infinite, 0.8, 300, 0.7, 8, 0.6, 0.6
- Standard: 4, 5, 1.0, 240, 1.0, 12, 1.0, 1.0
- Nightmare: 3, 3, 1.25, 180, 1.4, 16, 1.4, 1.4
- Endless: 4, 3, 1.15, 200, 1.2, 12, 1.3, 1.3
- Per level index L (0 based): hazard mult x (1 + 0.08 L), enemy mult x (1 + 0.06 L), chase x (1 - 0.035 L), flood x (1 + 0.05 L), enemy speed x (1 + 0.02 L). Per room index r: hazards x (1 + 0.05 r), enemies x (1 + 0.04 r). Caps 2.4 and 2.2.

## Levels and rooms
- Room height 22 tiles, level width 32 tiles. Rooms per level: 4, 5, 5, 6, 6, 7, 7, 8, then the boss (4 rooms plus arena of 19 tiles).
- Acts: 1 = levels 1-3, 2 = levels 4-6, 3 = levels 7-9. Level 5 has a rising flood. Level 6 adds tikbalang, 8 adds manananggal.
- Ordinary route: ledges rise 1 to 3 tiles each, widths 4 to 7 (3 to 7 from Act II). Spirit route: rise 1 to 2, width 3 to 5. Three flame fragments per level in the first three rooms, a clue letter in the middle room (room index floor(rooms/2); boss level: rooms-2), exit locked until the letter is found (levels 1 to 8).
- Skill obstacles: spike gauntlet 9 tiles (12 with Greater Veil), safe tile each end; thorn wall 16 px wide and 130 px tall on a ledge edge; double-jump climbs of 4 to 5 tiles; glide gaps of 4 to 5 tiles.

## Bellkeeper
- hp from difficulty table. Cycle: wave, bells, toll. A parried toll (ward when the ring closes) deals 4 and stuns; ward hits while stunned deal 1 each; bolt hits only while stunned deal 1.
