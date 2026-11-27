# Act I smooth art preview

Open **Sa Kabilang Pinto > Smooth Art > Open Act I Preview** and press Play in Unity. The scene is configured to load Level 1 on Standard at the first checkpoint with a temporary save. The preview uses the existing level simulation and a local art catalog.

The new art covers girl Tala in idle, run, jump rise, apex and fall, plus a wooden ledge, the capiz wall, a checkpoint parol, standard iron spikes, and the full vitality heart. The other Tala actions use the baseline clips. The preview camera keeps the baseline framing and uses regular orthographic rendering. Bilinear filtering is scoped to the new textures.

## Alignment

Source dimensions and pixels per unit give the same world rectangle before and after each swap. The normalized pivots match. Tala's 0.625 × 0.875 world-unit hitbox comes from the original player tuning. The ledge, lantern, and spike prefabs differ only in their main sprite reference. The level view continues to set platform and spike drawing size from the original level rectangles. Tala's original frame counts, tick timing, and looping match for every clip. The vitality heart still occupies the existing HUD image.

| Replacement | PNG size | World sprite size, before and after | Normalized pivot |
| --- | --- | --- | --- |
| CapizBackground | 2048 × 1536 | 32.0000 × 24.0000 | 0.5000, 0.5000 |
| WoodPlatform | 1286 × 236 | 5.4492 × 1.0000 | 0.5000, 0.5000 |
| CheckpointParol | 480 × 480 | 1.4000 × 1.4000 | 0.5000, 0.5000 |
| IronSpikes | 736 × 220 | 1.4636 × 0.4375 | 0.5000, 0.5000 |
| Tala_Idle_1 | 288 × 336 | 0.9643 × 1.1250 | 0.5000, 0.0893 |
| Tala_Idle_2 | 288 × 336 | 0.9643 × 1.1250 | 0.5000, 0.0893 |
| Tala_Idle_3 | 288 × 336 | 0.9643 × 1.1250 | 0.5000, 0.1071 |
| Tala_Idle_4 | 288 × 336 | 0.9643 × 1.1250 | 0.5000, 0.0893 |
| Tala_Run_1 | 288 × 336 | 0.9643 × 1.1250 | 0.5000, 0.0893 |
| Tala_Run_2 | 288 × 336 | 0.9643 × 1.1250 | 0.5000, 0.0893 |
| Tala_Run_3 | 288 × 336 | 0.9643 × 1.1250 | 0.5000, 0.0893 |
| Tala_Run_4 | 288 × 336 | 0.9643 × 1.1250 | 0.5000, 0.1071 |
| Tala_Run_5 | 288 × 336 | 0.9643 × 1.1250 | 0.5000, 0.0893 |
| Tala_Run_6 | 288 × 336 | 0.9643 × 1.1250 | 0.5000, 0.1071 |
| Tala_JumpRise_1 | 288 × 336 | 0.9643 × 1.1250 | 0.5000, 0.0893 |
| Tala_JumpRise_2 | 288 × 336 | 0.9643 × 1.1250 | 0.5000, 0.0893 |
| Tala_Apex_1 | 288 × 336 | 0.9643 × 1.1250 | 0.5000, 0.0893 |
| Tala_Fall_1 | 336 × 392 | 0.9643 × 1.1250 | 0.5000, 0.1071 |
| Tala_Fall_2 | 336 × 392 | 0.9643 × 1.1250 | 0.5000, 0.1071 |
| VitalityHeart | 402 × 362 | 1.1105 × 1.0000 | 0.5000, 0.5000 |

## Verification scope

The sprite metrics, prefab changes, scene references, and animation timing were checked from the project files. In-editor visual alignment, controls, and phone rendering still need visual review. The [art sources](../reference/Smooth/ActI/GENERATION.md) record the ImageGen prompts and provenance.

The [1280 × 720 art composition](../reference/Smooth/ActI/phone-art-preview.png) uses the finished preview sprites at the planned gameplay scale. It is a static view for judging proportions and contrast. Unity's batch editor stopped at the script post-processing worker, so a Unity screenshot and Play Mode check are pending. In the editor, review Tala's foot placement during run and jump and check whether the spike tips and platform edges are easy to read.
