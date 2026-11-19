# Prompt 03: M3 Dimensions and the Anino shadow

Implement the three dimensions as a bitmask (Ordinary 1, Spirit 2, Inverted 4). Every platform, hazard, door, item and enemy has a mask; only objects matching the current dimension are visible and solid. Doors switch dimension; lanterns return to Ordinary. Inverted flips gravity and the player's orientation; collisions, jump and glide must mirror correctly.

Spirit shadow: record the player's position, facing and animation every fixed step; the shadow replays it N steps late (N from the difficulty table, shortened per level), hurts on contact, and can be stunned by ward (about 190 frames) or bolt (about 140 frames) when those exist. Tint and audio filter change per dimension (hook for M8).

Tests: EditMode for mask logic across all combinations; PlayMode for door switching, solid-in-dimension only, gravity flip, shadow delay equals configured N steps, shadow damage through `CanBeHurt()`, stun, lantern returning to Ordinary, no dimension state leak after death or respawn.
Manual: a three-dimension test room.

Done when: compiles clean, tests pass, committed.
