# Prompt R1: Level objects as prefabs

Today `LevelView`, `BossView`, `PlayerView` (bolts), `MapController` and `LevelFx` build every object in code as a bare GameObject with a SpriteRenderer. Turn each into a real prefab an artist can open and edit, without changing how anything looks or plays.

1. Make one prefab per object kind under `Assets/Prefabs/Level/`: ledge (one per LedgeKind and act), ground block, each door kind, exit (locked and open states), spike, spike gauntlet, root, thorns, veil curtain, flame, fragment, letter, lantern, Anino, tikbalang, manananggal, falling bell with its warning mark, water body and surface, decoration (roots, candle), bolt. Under `Assets/Prefabs/Boss/`: the Bellkeeper body, shockwave, bell mark, toll ring. Under `Assets/Prefabs/Map/`: node, path dot, Lola, token.
2. Each prefab carries its SpriteRenderer(s), `DimensionMember`, sorting layer and order, and a small view component that exposes what the code drives (frames, tiled size, lit or unlit state). Code-drawn pieces (lantern post, water body, warning lines, toll ring) become prefabs too, still using today's look.
3. Add a `LevelPrefabCatalog` ScriptableObject (data-driven rule) that maps each kind and act to its prefab. `LevelView.Build` and the others instantiate from the catalog. Objects are created once at level build and reused (pooled where they come and go: bolts, waves, marks, particles); gameplay still allocates 0 B of garbage per frame.
4. Update the setup menu so **Run M1 Setup** rebuilds the prefabs and catalog idempotently.

Tests: every catalog entry is set and its prefab has the required components; a built level has the same object count, positions, sizes and sorting as before R1 (compare against a capture taken before the change, levels 1 to 9); screenshots of levels 1, 5 and 9 match the pre-R1 screenshots pixel for pixel or within a stated tolerance; the zero-GC test still passes; full EditMode and PlayMode suites pass.

Done when: no level, boss or map object is built from a bare `new GameObject` any more, nothing looks different, committed.
