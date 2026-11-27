# Tala smooth animation preview

The Act I preview now uses both smooth Tala animation sets in `Assets/Art/Smooth/Tala`. Open **Sa Kabilang Pinto > Smooth Art > Open Act I Preview**. Girl Tala is the default. To see Boy Tala, select **Level** in the Hierarchy, tick **Use Boy** on the **Smooth Act I Preview** component, and press Play. The selector uses the preview's temporary save.

The [animation sheet](../reference/Smooth/Tala/tala-animation-sheet.png) shows all thirteen states for each avatar at the same scale. Each row shows the actual sprite crops in playback order. The baseline clips hold poses during several actions, so repeated slots appear in the sheet. The [source record](../reference/Smooth/Tala/GENERATION.md) documents the AI-assisted pose sheets and their prompts.

## Alignment and timing checks

The new sets were compared with the active recovered sets. All 76 frame slots retain their original world rectangle and normalized pivot. All 26 clips retain their original frame count, ticks per frame, and loop flag. The 0.625 × 0.875 world-unit hitbox and the player render setup still come from the original game. New transparent sprites use the existing smooth importer setup.

The measurements come from the sprite dimensions, pixels per unit, and serialized pivots. Check stance changes and floor contact in Play Mode when reviewing the style.

The original [Act I art preview](SMOOTH_ACT_I_PREVIEW.md) records the first pass with girl Tala and the shared scenery. This pass changes only the avatar art and how the preview selects it.

