# Prompt R6: Props, hazards, items and effects

Replace the remaining prototype, recoloured and code-drawn pieces. Art follows the "Art pipeline" in the README: every prop, hazard, item and particle is a text grid in the R2 style, built with `SkpPixelText`. No credits are spent here.

**Art and objects**
1. Lantern checkpoint (today a 2 px post): a capiz lantern on a post, with unlit, lighting (4 frames) and lit loop (4) states.
2. Doors: Spirit, Inverted, Home and Exit, with idle, open (4) and closing frames, plus the exit's locked and unlocked states with a letter-shaped lock.
3. Hazards:
   - spikes and the gauntlet;
   - roots (dormant, warning, strike, burnt);
   - thorn walls and their burnt state;
   - the veil curtain, with its own art rather than recoloured thorns;
   - the Act III falling bell, with a readable warning shadow instead of the red line.
4. Items: the flame star, fragment and letter, each with an idle loop and a pickup frame.
5. The flood: a water-surface loop, and a body with depth shading. The boss toll ring and bell marks as sprites.
6. Particle sprites for flame, fragment, veil, bolt, ash and embers.

**Game work**
7. Each prop's view component (from R1) plays its states from the sim: lantern lit, door used, root phase, thorns burnt, item picked up.
8. Lantern light: a URP 2D light or a sprite glow (pick one, record it, and keep the Android cost in mind).
9. Dimensions: a Spirit and an Inverted material (shader) replaces the tint-and-alpha look of other-dimension objects. It keeps the colour rules (teal Spirit, violet Inverted) and honours the Reduced flash and High contrast settings.
10. Particles stay pooled in `LevelFx` with the new sprites. `LevelView` and `BossView` stop using the white pixel and the LineRenderer.

Tests:
- PlayMode: light a lantern, open each door, trigger each hazard, burn thorns, drown in the flood, and check each view's state.
- Every other-dimension object uses the dimension material, and High contrast still passes the layout audit.
- `ArtRules` passes and zero GC holds.
- Screenshots in `tests/Screenshots/r6`.
- The full suites pass.

Done when: `LevelView` and `BossView` draw nothing from a white pixel, committed.
