# Smooth interaction previews

In Unity, choose **Sa Kabilang Pinto → Smooth Art → Interactions → Open Act I, II, or III**, then choose **Sa Kabilang Pinto → Play Open Scene Once**. Each scene opens the corresponding campaign room from the environment preview with a temporary save.

| Scene | What to review |
| --- | --- |
| Act I Interactions | Find both dimension doors near the checkpoint, enter one, and use Ward to check the ring against Tala at gameplay scale. |
| Act II Interactions | Review root telegraphs and strike poses, the rising blackwater, and the Liwanag Bolt and Lambong Veil effects. |
| Act III Interactions | Check that the red bell warning reads against stone and that the dash streak stays behind Tala. |

The existing code drives door reach and lock states, pickup collection, root phases, burnt thorns, curtain bounds, the flood, and the bell warning and drop. The preview catalogs replace sprites on isolated prefab copies and retain the existing fittings for world size, pivot, borders, layer, and sorting order. The letter pickup also supplies the matching dialog image at its existing layout size. The falling bell artwork puts the visible bronze body inside its existing damage footprint.

Gold marks safe or interactive objects, teal marks Spirit, violet marks Inverted, and red identifies damage and danger warnings. The Veil skill keeps its authored violet ring while the hazardous curtain has a red thorn border.

**Capture Gameplay Views** saves PNGs under `tests/Screenshots/smooth/interactions` when run from the Unity editor. It samples the campaign rooms and movement or skill states at the existing simulation coordinates.

The review sheet uses the baked textures, and the Act I layout preview uses the original campaign layout. The scripts passed C# preflight. The detached Unity run stopped during package assembly post-processing before it could save a gameplay capture.

The [source atlases and prompts](../reference/Smooth/Interactions/GENERATION.md) record the AI-assisted artwork and derived textures.
