# Prompt R3: Tala animation

Give Tala (boy and girl) a full animation set in the approved style, and drive it with an Animator. Art follows the "Art pipeline" in the README. The SpriteCook key frame picked in R2 is the style reference, and every frame is a text grid. No credits are spent here.

**Art**
1. Frames for both characters, with the same timing:

   | Animation | Frames |
   | --- | --- |
   | idle | 4 |
   | run | 6 |
   | jump rise | 2 |
   | apex | 1 |
   | fall | 2 |
   | land | 2 |
   | dash | 3 |
   | ward | 3 |
   | hurt | 2 |
   | death | 5 |
   | door enter | 4 |
   | glide | 2 |
   | bolt cast | 2 |
   | the veil overlay | loop |

   Tala's parol lantern is part of her design.
2. Draw the frames as text grids in `Assets/Art/Source/Characters/`, one file per animation per character. Start from the picked idle frame: copy its proportions, palette and shading, and keep the boy and girl to one shared body so the timing matches. Build the PNGs with `SkpPixelText`.
3. Check every frame at 1x for the silhouette and the bottom-centre pivot on the 16x18 box (the hitbox does not change). Put a contact sheet of each animation in `tests/Screenshots/r3` for my review. List any frame I want touched up (in Pixelorama or LibreSprite, or by editing the grid), and record it in the manifest.
4. Inverted uses the same frames, flipped by the existing code; check they still read well upside down.

**Game work**
5. One Animator Controller, plus an override controller for the second character, and a `PlayerAnimator` component that maps `PlayerSim` state to parameters. The sim stays deterministic and never reads the animation. Hit-stop pauses the Animator.
6. Update the character select cards, the map token and the dialog portraits (24x24 text grids) to the new Tala.
7. The old 4 frames stay in `Assets/Art/Characters/` as the fallback.

Tests: every sim state maps to an animation state (EditMode table test); a PlayMode test drives run, jump, dash, ward, hurt, death, door and glide and checks the Animator state for each; `ArtRules` passes; the bots still clear levels 1 to 9; zero GC still holds; screenshots of each action at 1x and 4x in `tests/Screenshots/r3`; full suites pass.

Done when: Tala animates every action for both characters in the new style, committed.
