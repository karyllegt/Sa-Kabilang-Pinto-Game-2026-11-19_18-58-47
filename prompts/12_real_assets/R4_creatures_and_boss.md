# Prompt R4: Creatures, Lola and the Bellkeeper

Replace the 2-frame and 1-frame prototype sprites with full animation sets in the approved style, drawn to the existing hitboxes. Art follows the "Art pipeline" in the README: every frame is a text grid in the R2 style. The only possible spend is one Bellkeeper key frame, if the credit plan from R2 still covers it: state the cost and ask first. Without it, the Bellkeeper key frame is a text grid too.

**Art**
1. Anino: float (4), chase (4), stunned (2), dissolve on a bolt kill (5). The Spirit shadow that replays Tala's path uses the Anino set with a trailing smear.
2. Tikbalang (a horse-headed trickster, tall): walk (6), turn (2), stunned (2). Manananggal (a winged upper body): flap (4), dive telegraph (2), dive (2), repelled by the ward (3).
3. Lola: idle (4) and talk (2) on the map, plus portraits (neutral, smiling, worried) at 24x24.
4. The Bellkeeper (48x56 box): idle (4), wake (6), shockwave (4), bell summon (4), toll wind-up (4), toll (2), struck (2), stunned (4), defeat (8), plus portrait states. These replace the tinted `spr_boss_stun` and `spr_boss_toll` stand-ins.
5. For each character, draw the key frame as a text grid first and show it to me. Then draw the rest of its frames as grids from that key frame, and build them with `SkpPixelText`. Do a 1x readability check against each backdrop and record touch-ups in the manifest. A contact sheet per character goes in `tests/Screenshots/r4`.

**Game work**
6. An Animator per creature and for the boss, driven from its existing state (`Enemy`, `Bellkeeper`) through the R1 prefabs.
7. Telegraph frames must line up with the existing tick timings so the fight reads the same. List each timing next to its frames in `DECISIONS.md`.
8. Dissolve and defeat animations must not delay any sim event. They play on a detached visual if needed.

Tests: every creature and boss state maps to an animation (EditMode); a PlayMode test runs the boss through every state and checks the Animator follows; telegraph frames start on the same tick as the sim's telegraph; `ArtRules` passes; the bots still clear levels 1 to 9 and the boss; screenshots in `tests/Screenshots/r4`; full suites pass.

Done when: no creature and not the boss uses a prototype or tinted sprite, committed.
