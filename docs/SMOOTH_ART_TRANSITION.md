# Sa Kabilang Pinto smooth 2D art transition

The [active Smooth art guide](SMOOTH_ART_GUIDE.md) records the campaign rollout, current asset routes, rollback switch, and review results. The inventory and steps below describe the pre-rollout project and planning baseline.

Start with the first checkpoint room in Act I. Draw Tala and the room as smooth, high-resolution storybook art with clean contours, soft cel shadows, restrained texture, and warm parol light against a deep violet night. Keep Tala's round face, cream camisa and red sash. Carry the capiz windows and carved wood of Act I into a consistent treatment of the balete and well in Act II and the stone church and bells in Act III. Use gold for safe or interactive objects, teal for Spirit, violet for Inverted, and red for danger. Keep background detail quieter than the player, platform tops, and hazards.

## Active assets

| Group | Verified references in the current Unity project |
| --- | --- |
| Tala and shadow | `Assets/Prefabs/Player/Player.prefab` points to both `TalaRecovered_*.asset` sets in `Assets/Data/Player`. Their poses and action sprites come from `Assets/Art/Recovered/Girl` and `Boy`. The Spirit shadow uses the avatar silhouette, a separate material, and pale eyes. |
| Backgrounds | `Assets/Scenes/Level.unity` references `Assets/Data/LevelArt.asset`. `LevelRunner` selects `HouseWall.png`, `ForestWall.png`, or `BelfryWall.png` from `Assets/Art/Recovered/Environments` by act. |
| Terrain and objects | `LevelView` loads `Assets/Resources/Level/LevelPrefabCatalog.asset` from Resources. Act I wood ledges and spikes use the recovered `Terrain/Tile0.asset` and `Hazards/Hazard0.asset` Sprite slices. The Spirit door and lantern use recovered `Environments/DoorSpirit.png` and `Props/Parol.png`. Use the catalog to trace the remaining doors, hazards, pickups, and effects. |
| Creatures and boss | Catalog prefabs point to recovered Tikbalang and Manananggal pose PNGs and sliced Bellkeeper and Lola states. Anino uses a silhouette treatment. |
| UI and map | `Assets/Resources/UI/UiTheme.asset` uses the Press Start 2P font, a legacy PixelBox frame, and recovered portraits, cards and hearts. The skill library points to definitions in `Assets/Data/Skills`. `Assets/Scenes/Map.unity` uses the recovered `Map/ThreeActs.png`, Tala poses and Lola frames. Title and some shared screens also pick assets from the theme. |

Use the [presentation inventory](PRESENTATION_INVENTORY.md) for the wider dependency list and the [art manifest](PRESENTATION_ART_MANIFEST.md) for source records. A serialized reference can point to a fallback. Follow the scene or prefab field and the runtime selection before scheduling a replacement.

## Rendering and alignment

- **Texture import.** `SkpAssetImportSettings` forces Point filtering under `Assets/Art` and defaults most legacy sprites to 16 pixels per unit. It has separate handling for Recovered and Characters48. Give future sprites under `Assets/Art/Smooth/` a scoped exception. Start with newly drawn transparent art, bilinear filtering, and uncompressed textures for visual review. Use full rectangles and preserve borders for sprites that tile or stretch at their edges. Compare Android texture limits and compression after checking edge quality and memory.
- **Camera and movement.** Level uses a Pixel Perfect Camera with a 1440 × 810 reference, 48 assets pixels per unit, and Upscale Render Texture. The current URP asset uses render scale 1 and a single MSAA sample. Compare the same new art with the current camera and with Pixel Perfect disabled in the preview. Keep orthographic size 8.4375 and compare framing on wide phones. `PlayerView` also rounds the interpolated drawing position to 1/16 world unit. If that still looks stepped, compare renderer-only subpixel motion. Keep the fixed simulation tick, camera follow behavior, and gameplay timing.
- **Sprite footprints.** Record each active Sprite rectangle, normalized pivot, pixels per unit, renderer size and sorting. Match the old world dimensions and visible anchor per replacement. For example, a 288 px replacement rectangle at 256 pixels per unit gives Tala's existing 1.125-unit action frame height before renderer scaling. Check foot placement on the existing 0.625 × 0.875-unit hitbox in both gravity directions. Keep animation clip tick counts and check ledge tops, hazard extents, trigger positions and atlas padding.
- **UI layout.** Use the existing 960 × 540 canvas reference, anchors, safe area, touch targets and focus states. Match panel slice thickness at screen size and recheck wrap and clipping when changing fonts. Keep global theme changes out of the first preview.

## Act I preview and rollout

Use the [first checkpoint capture](../tests/Screenshots/presentation/00_level1_first_lantern.png) as the baseline. In a separate dev scene, use a fixed Level 1 seed, the same difficulty and avatar, and a throwaway save. Assign preview copies of the level art data, prefab catalog and affected prefabs. Start with girl Tala in idle, run and jump, one wood platform, one capiz backdrop, a gold checkpoint, a teal door, one spike strip, and one HUD element.

Capture the same area at 1280 × 720 and 2400 × 1080. Compare Tala and the ledge tops at normal play scale. Walk, jump, touch the checkpoint and try the Spirit door. Verify visible feet and spikes against their original bounds. Review color cues, frame transitions, camera motion, text and sparkles during scrolling.

Once the preview is accepted, review later batches in this order: both Tala variants and all 13 states, creatures and boss, environments for all three acts, props and effects, UI and map, then campaign integration. Record each replacement's source, creator or tool, consumer, and rollback reference.

## Risks and open checks

| Risk | How to resolve it |
| --- | --- |
| Smooth art looks soft or shimmers in motion | Compare camera modes at matching framing and inspect alpha fringes on a phone. Check fixed-grid drawing separately from texture sampling. |
| A sprite appears to float, clip or mislead the player | Compare the old and new pivots, visible bounds, tiled borders and hitboxes in play. Include inverted Tala and hazard warning states before rolling the style out. |
| Larger textures raise Android memory or load time | Profile the preview on a real phone. Choose atlas sizes and platform compression based on measurements and edge quality. |
| Shared setup or theme overwrites preview art | Keep preview references separate. `SkpPresentationUpgrade.RunBatch` can reapply baseline art and rebuild shared scenes. Trace the live dependencies again before rollout. |

## Outdated or conflicting documents

- [Art bible](06_ART_BIBLE.md), sections 2 and 6–8, describes hard pixels, binary alpha, and the old `Assets/Art/Game` and Tala48 workflow. Keep its Filipino references and gameplay color cues. Apply its pixel rules and tests to the legacy set.
- [Asset guide](04_ASSET_GUIDE.md) describes the browser prototype's 16 px copy map. Use it for history. Current art is selected by Unity scene, prefab and Resources references.
- [Progress audit](PROGRESS_AUDIT.md) still lists several art batches as unstarted. The [presentation handoff](PRESENTATION_HANDOFF.md) records the later integrated prototype baseline. The older sprint steps in the [Mary and Ronn handoff](HANDOFF_MARY_RONN.md) and Tala48 notes in the [README](../README.md) describe earlier implementations.

## Art direction review

Use the existing [Act I concept](../reference/Smooth/act-i-storybook-concept.png) to review Tala's design, the amount of carved detail, and whether the parol and door stand out. The concept is a static reference and depicts Tala and the checkpoint larger than the baseline. Use the measured game scale for the playable preview and try quieter platform trim so interaction cues stand out.

## Concept provenance

`reference/Smooth/act-i-storybook-concept.png` uses the built-in ImageGen tool. It is AI-assisted concept art using the repository's first checkpoint screenshot as a layout reference. It sits outside Unity's `Assets` folder for art review.

<details>
<summary>ImageGen prompt</summary>

> Use case: stylized-concept. Create one landscape 16:9 art-direction concept for Sa Kabilang Pinto, an original Filipino folklore side-scrolling Unity game. The attached current Act I screenshot is a layout and camera reference: keep the straight side view, small playable character scale, stacked walkable ledges and platforms, tall capiz windows and carved wooden house-tower. Repaint the scene from scratch into clean high-resolution hand-drawn 2D storybook illustration, with smooth antialiased contours, disciplined line weight, soft cel shading, and restrained paper texture only in background shapes. Deep violet night, muted narra wood and capiz-shell panels in the background, warm amber parol light. Show a small round-faced Filipino girl named Tala, with black hair, cream camisa and red sash over a red patadyong skirt, bare feet, holding a golden five-point Filipino parol. Her silhouette must read at gameplay scale. Put her on the broad floor near a gold checkpoint lantern. Show a distinct teal Spirit door and a short strip of red-tipped spikes. Keep decorative background contrast below the walkable platforms and Tala. Teal is reserved for the Spirit cue, violet for background and Inverted cues, gold for safe or interactive accents, red for hazards. Focus on clear readable platform tops, restrained glows and storybook charm. No pixel art, no pixel blocks, no faux low-resolution dithering, no 3D, no text, no labels, no UI, no border. This is a cohesive static concept for review.

</details>
