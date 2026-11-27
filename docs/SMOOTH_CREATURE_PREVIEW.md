# Smooth folklore cast preview

The [creature sheet](../reference/Smooth/Creatures/creature-preview-sheet.png) shows the fitted frames at a shared world scale, plus the three dialogue portraits. The finished assets and scene overrides live under `Assets/Art/Smooth/Creatures`. The [source record](../reference/Smooth/Creatures/GENERATION.md) lists the image sources and prompts.

## Open the in-game previews

- **Combat:** In Unity, choose **Sa Kabilang Pinto > Smooth Art > Open Creature Encounters**. Select **Level** in the Hierarchy and choose Anino, Tikbalang, Manananggal, or Bellkeeper under **Encounter** on the **Smooth Creature Preview** component. Press Play. Anino, Tikbalang, and Manananggal start near an existing checkpoint in Levels 1, 6, and 8. The Bellkeeper starts at the arena checkpoint in Level 9.
- **Lola:** Choose **Sa Kabilang Pinto > Smooth Art > Open Lola Map Preview**, press Play, and use **Talk** on the map to see her portrait. The map uses her existing dialogue and pose timing.
- Each preview uses a temporary save. Anino's Spirit chase uses the existing teal door and shadow rules.

## Alignment and scope

The new main sprites replace only the art references in five copied prefabs. I checked the dimensions and normalized pivots for all 20 body frame references and the three portraits. The encounter scene uses the original generated levels, checkpoints, attacks, dimensions, collision model, and player tuning. The portrait override listens to the existing dialogue event in the two preview scenes.

| Cast member | Sprite rectangle in world units |
| --- | --- |
| Anino | 0.964 × 1.125 |
| Tikbalang | 1.625 × 1.625 |
| Manananggal | 1.250 × 1.250 |
| Lola | 1.250 × 1.250 |
| Bellkeeper | 3.500 × 3.500 |

The preview scripts compiled against the local Unity assemblies.

I opened Creature Encounters and checked the Anino patrol in Level 1 in Unity Play Mode. The Lola map and the remaining encounter settings were checked by inspecting their scene and asset references.
