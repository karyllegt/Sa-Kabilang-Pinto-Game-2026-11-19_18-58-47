# Prompt 04: M4 Level framework, hazards, levels 1 to 3

Build the data-driven level system: `LevelConfig` (name, act, intro text, clue text, skill id, rooms, hazard and enemy settings, flood, boss flag). Each level is a vertical tower of rooms (22 tiles tall, 32 wide). Choose one approach and record it in `DECISIONS.md`: either hand-authored tilemaps per room with a procedural fill for obstacle tuning, or a seeded generator ported from the prototype's `buildLevel` and `route` (see the prototype source). If you port the generator, keep seeds deterministic.

Include: lanterns (checkpoints, +30 flame, +1 vitality), flame pickups, three flame fragments per level (max flame +5 each), hidden letter in the middle room, exit door locked until the letter is found, spikes, crumbling ledges, moving ledges, Anino patrols. Rooms per level and ramp numbers from `docs/PROTOTYPE_NUMBERS.md`. Implement levels 1 to 3 (4, 5, 5 rooms) with the prototype's intro and clue texts.

Checkpoints and resume: lantern progress saved; Continue resumes at the last lantern.

Tests: seeded generation is deterministic; every room has a start ledge, an ordinary route and a Spirit route, and an exit; a completability bot proves levels 1 to 3 can be finished from spawn on Standard with the base kit (fail on any unreachable required ledge); locked exit stays locked until the letter; checkpoint respawn; no error logs in a scripted run.

Done when: levels 1 to 3 are completable by the bot and by hand, tests pass, committed.
