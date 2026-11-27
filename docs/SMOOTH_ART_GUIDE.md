# Sa Kabilang Pinto smooth art guide

The production **Title**, **Map** and **Level** scenes use the Smooth art selection. Play **Boot** to review the normal journey. Existing progress and options are used by the campaign. Earlier Smooth review scenes still use their own temporary save and stage-specific art.

## Restore the previous presentation

1. Stop Play Mode. In Unity, choose **Sa Kabilang Pinto → Smooth Art → Campaign → Restore Previous Art**.
2. Start Play Mode again. For device builds, rebuild the player after switching. For a desktop player, launching with `-legacy-art` selects the previous presentation for that launch.
3. Choose **Use Smooth Art** from the same menu to re-enable the rollout.

The menu only saves `Assets/Resources/Presentation/CampaignArtSelection.asset`. The legacy paths remain serialized in the original scenes, prefab catalogs and theme. The rollback selection skips loading the Smooth profile.

## Active artwork

| Where | Active source |
| --- | --- |
| Tala and Anino | Both Smooth Tala sets supply all 13 states each. The same poses drive the Spirit silhouette. The original collision model and eye placement still drive the view. |
| Act I | Capiz house-towers, carved wood and warm checkpoint parols. |
| Act II | Balete roots, moss bands, the well and the illustrated blackwater drawn at the original flood boundary. |
| Act III | Church stone, dim candles, warning marks and bronze bells. The final boss uses the Smooth Bellkeeper and floor shockwave. |
| Spirit and Inverted | Teal and violet ledges and decorative backdrop overlays. The original dimension masks still decide which objects collide. |
| Doors, hazards and skills | Interaction catalogs select the door, item, hazard and effect art. The Smooth profile supplies the original-timing particle tune with new textures and the new glide sprite. Reduced Flash suppresses the added dimension flare and softens the dash trail. |
| UI and map | Smooth UI theme, Liberation Sans SDF, illustrated title and map, Tala and character portraits, new hearts, eight skill icons and existing navigation and touch layouts. |

Gold identifies safe or interactive objects, teal identifies Spirit, violet identifies Inverted and red identifies danger. Use flat dark surfaces behind small text. Keep cel shading and contours crisp at phone size. Decorative depth stays behind the playable geometry.

World sprites use bilinear filtering and view-native rendering for the campaign. Original orthographic height, simulation ticks, collision geometry, triggers, sprite pivots and sorting order still drive play. Legacy art found in unused fallback slots, a disabled root renderer, the operating-system launcher icon, and original development scenes is deliberate. Flat utility textures used for shadows, fills, eyes, and selection do not draw identifiable pixel-art illustrations.

## Verification and phone checks

- The [asset audit](../tests/Screenshots/smooth/campaign-review/asset-check.json) paired 76 animation frame slots across both Tala sets and confirmed state counts, world dimensions, pivots, ticks per frame and loop flags. The dependency review traced the campaign’s active terrain, creatures, hazards and pickups to the Smooth art, with neutral utility sprites retained.
- The [Tala alignment sheet](../tests/Screenshots/smooth/campaign-review/tala-alignment.png) overlays each animation state's representative frame on the original 0.625×0.875 world-unit hitbox. The gold line marks the game’s renderer anchor at the bottom in the Waking World and at the top in Inverted.
- The [gameplay comparison](../tests/Screenshots/smooth/campaign-review/gameplay-before-after.png) shows both art sets on matched 960×444 layouts from the generated campaign rooms. The [screen comparison](../tests/Screenshots/smooth/campaign-review/screens-before-after.png) pairs archived Unity baselines with Smooth renders based on the phone layouts. These are representative layout images. The runtime and edited editor code compiled. Unity’s separate capture run stopped during package assembly processing.
- The Unity command **Sa Kabilang Pinto → Smooth Art → Campaign → Capture Before and After** is set up to render both looks from the same production scene definitions and checkpoints. It targets 2340×1080 with a simulated notch and 800×480 under `tests/Screenshots/smooth/campaign`.
- On a real Android phone, check near and far movement for jitter or shimmer, and check edge sharpness and color on the device’s rendering backend. Compare results on wide notched and lower-resolution phones, both landscape orientations, gesture bars, and both touch layouts.
- Check the spike strip, root warning, rising water, falling bell and boss shockwave during motion. Review both gravity directions and both Tala variants beside platform edges. Verify focus and control reach with touch and a paired controller. Profile texture memory, load time and frame pacing on a low-memory phone before changing Android compression.

See the [art manifest](PRESENTATION_ART_MANIFEST.md) and [rollout provenance](../reference/Smooth/Rollout/GENERATION.md) for source art and derivatives.
