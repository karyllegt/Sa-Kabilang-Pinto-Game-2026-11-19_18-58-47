# Prompt 02: M2 Player feel

Port the player from the prototype using `docs/PROTOTYPE_NUMBERS.md`: run, acceleration and friction, gravity, variable-height jump, coyote time, jump buffer, dash (flame cost, cooldown, untouchable frames), ward pulse (radius, cooldown), hurt (knockback, invulnerability), death and respawn at the last lantern.

Rules:
- All numbers in a `PlayerTuning` ScriptableObject, expressed per second (convert from the per-frame values). Use a fixed update that reproduces the prototype's 60 Hz feel; do not let frame rate change jump height.
- A single `CanBeHurt()` function decides immunity (post-hit invulnerability, dash, later the veil). Every damage source must call through it.
- Two characters (boy and girl) share one controller with different sprites. Use placeholder pixel art generated in code or simple sprites; real art can replace them later.
- Flame resource and vitality (hearts) on the player model, difficulty-aware.

Tests: PlayMode tests that measure jump apex height (about 3.9 tiles at full hold), apex time, coyote jump, buffered jump, dash distance and untouchable window, knockback, invulnerability duration, and that results are the same at 30, 60 and 144 fps.
Manual: a short test room scene with ledges at 1 to 4 tiles to feel the jump.

Done when: compiles clean, tests pass, test room playable with keyboard and gamepad, committed.

Use the provided player sprites (`spr_pl_*`) with a simple run, idle and air animator.
