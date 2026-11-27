# Presentation art manifest

This is an integration/provenance record, not final visual acceptance. The authoritative outstanding-work list is `UPGRADE_AUDIT.md`.

## Recovered from the earlier Codex project

`Assets/Art/Recovered/Girl/00..15.png` and `Boy/00..15.png`: existing detailed avatars and genuine source poses. Derived animation sets preserve all 13 current state/event timings. Source transparency is inspected to anchor visible feet rather than the bottom of a padded PNG; the simulation box stays unchanged. Dedicated generated action keys supplement these source poses; held keys are not represented as additional unique animation frames.

`Tikbalang/00..03.png`, `Manananggal/00..03.png`: four source poses each, source-facing correction, 4 fps fixed-tick playback and preserved canvas height. Three `Environments/*Wall.png` backgrounds, three detailed door PNGs, `Props/Parol.png` and `Props/BellFragment.png` are integrated. Other recovered prop candidates remain source references until explicitly wired. The exact original project path is in `docs/RECOVERED_ASSETS.md`.

## Built-in ImageGen assets

All entries below were created with the built-in ImageGen tool, not the API/CLI fallback. Their generated PNGs are copied into this repository and are not referenced from Codex's private generation folder. These are AI-assisted assets; do not label them as Mary's hand-drawn work.

| Saved project PNG | Production prompt / constraints | Consumers |
| --- | --- | --- |
| Assets/Art/Recovered/Brand/AppIcon.png | Opaque square launcher art: a single golden five-point Filipino parol inside a carved ancestral wooden doorway, warm luminous door slit, cream/gold/brass against deep indigo/violet with small teal accents. Bold crisp pixel clusters, central safe composition for launcher masking, no lettering, UI, device, character or mockup. | Default/desktop and supported Android legacy launcher slots through the presentation/build setup; verify the resulting player/APK |
| Assets/Art/Recovered/Anino/Portrait.png | Referenced the recovered Girl/00.png. Square head/shoulders portrait of Tala's living shadow: ponytail silhouette, near-black indigo fill, two pale teal eyes, sparse violet/teal wavering edges, no other facial features; no mask, skull, cloak, frame, text or background. Crisp pixel clusters readable at 88 pixels. | Anino dialogue portrait; gameplay shadow still follows the actual active avatar silhouette |
| Assets/Art/Recovered/Skills/SkillsAtlas.png | Transparent 4x2 set of eight compact, detailed brass/teal/violet pixel icons: double feather jump, fire bolt, woven veil, streaking boot, three fire spears, winged parol, protective shield ring, ruby vitality sunburst. No text or UI panels; dark outlines, generous gutters, small-scale readability. | Existing eight skill definitions, cards, HUD chips and skill list; shield ring denotes upgraded veil |
| Assets/Art/Recovered/Effects/EffectsAtlas.png | Transparent equal 4x3 atlas, crisp detailed pixel clusters: parol flame pickup, empty-centre gold ward ring, right-facing gold bolt, violet gauze curtain; black teal water strip, leafy balete clump, church bell masonry clump, empty-centre violet veil ring; paired glide wings, ruby heart, empty heart, ember diamond. Generous cell gutters, no text/UI/background. | Pickup, projectile, skill rings/wings, curtain, water surface, act decoration, HUD health and pooled particles |
| Assets/Art/Recovered/Girl/ActionKeysAtlas.png and Boy/ActionKeysAtlas.png | Transparent 4x2 action atlas referencing the recovered avatar: cream camisa, red sash, dark blue trousers and brass parol; girl ponytail, boy short dark hair. Top row dash, ward, overhead parol glide, bolt cast; bottom landing crouch, hurt recoil, defeated kneel, doorway back view. Distinct poses, consistent cell scale and foot baseline, detailed pixel clusters, no labels or UI. | Eight dedicated action states per avatar; original simulation and animation durations preserved |
| Assets/Art/Recovered/Bellkeeper/BellkeeperAtlas.png | Transparent 2x2 atlas: the same bronze church-bell spirit, carved stone face and rope arms, in idle/stunned/toll/defeated poses. Detailed ink-outlined pixel clusters, bronze/purple/teal palette, red danger only at toll clapper. Equal cells, consistent scale/baseline, no UI/text. Full prompt record: adjacent GENERATION.md. | Four visual slots including defeat; existing boss mechanics and fade timing retained |
| Assets/Art/Recovered/Lola/LolaAtlas.png | Transparent 2x2 atlas: small Filipino grandmother, white bun, lilac baro, lavender patadyong, cane; idle, speaking, nodding and pointing. Detailed pixel clusters, warm light, consistent scale/baseline, no UI/text. Full prompt record: adjacent GENERATION.md. | Map NPC poses and cropped face portrait |
| Assets/Art/Recovered/Map/ThreeActs.png | Wide 3:1 seamless three-region night panorama: capiz-window ancestral town/parols, balete grove/roots/well, Philippine stone church/bell tower. Restrained violet, teal and bronze, layered hills, stars and moon; dark terrain behind gold separately drawn markers; no nodes, digits, labels, characters or UI. | Existing nine-node map geometry and navigation |
| Assets/Art/Recovered/Terrain/TerrainAtlas.png | Transparent 3x3 atlas of flat-topped platform strips: carved ancestral wood, balete-root beam, church masonry; teal Spirit, violet Inverted, rotten crumble; bracketed moving platform, solid timber floor and masonry floor. Side view, capped ends/repeating centre, detailed pixel material texture, no characters/text/UI. | Native Sprite slices on existing ledge/floor prefabs; no collision edits |
| Assets/Art/Recovered/Hazards/HazardsAtlas.png | Transparent 3x3 atlas: iron spikes, spike gauntlet, dormant root; warning root, striking root, tall thorn wall; burnt thorns, bronze falling church bell, cream sealed letter. Detailed dark outlines/material clusters, red danger highlights, no labels/UI/readable lettering. | Existing hazard states, bell views, letter pickup and portrait |

Sprite regions are reviewed before slicing. Hazard crops use explicit reviewed source regions because the striking root extends beyond its nominal grid cell; blindly treating this sheet as nine equal cells would cut its tip and contaminate the spike sprite. The terrain extractor ignores isolated fringe pixels above the load-bearing top. Generated sheets retain source pixels; Unity Sprite assets define their regions, pivots, pixel scale and tiled borders.

## Native presentation effects

`ShadowSilhouette.shader` renders the alpha outline of the active Tala avatar uniformly, with separate pale eye sprites. The Spirit trail still reads the original recorded position/facing data. The Anino creature prefab uses the same silhouette language. UI brass inlays are native uGUI geometry, not raster stand-ins for requested sprite assets. Existing input, collision, level generation, skills, damage and save code are not redesigned by these visual effects.

## Launcher layers

The Android build setup supplies adaptive and round icon slots through Unity's platform-icon API. Adaptive icons keep the complete doorway artwork on the background and use a native transparent utility foreground (`AdaptiveClear.png`); no extra drawing is claimed for that utility texture. The generator queries supported icon kinds, so the main desktop editor does not require an Android module to compile. API reference: [Unity platform icon source](https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/PlatformSupport/PlayerSettingsPlatformIcons.bindings.cs).

## Acceptance follow-up

Actual world/action and UI review is recorded in `PRESENTATION_VERIFICATION.md`. Continue physical-device performance and headphone/phone-speaker acceptance. An atlas preview alone is not a gameplay screenshot or proof that its consuming prefab is correct.

## Smooth folklore cast preview

The creature pass adds illustrated Tikbalang, Manananggal, Lola and Bellkeeper poses, three dialogue portraits, and an Anino patrol sourced from the smooth Tala animation. The source sheets use the built-in ImageGen tool and are AI-assisted. The patrol and Spirit chase retain their existing silhouette material and eye renderers. The smooth assets are wired to two isolated preview scenes. See the [generation and derivation record](../reference/Smooth/Creatures/GENERATION.md) and [preview guide](SMOOTH_CREATURE_PREVIEW.md).

## Smooth environment previews

Three isolated scenes add AI-assisted backgrounds, wooden, root and stone surfaces, dimension-specific ledges and scenery, flood water, and foreground details. They draw the existing campaign room layouts and collision geometry through the original level renderer. See the [generation and derivation record](../reference/Smooth/Environments/GENERATION.md) and [preview guide](SMOOTH_ENVIRONMENT_PREVIEW.md).

## Smooth interaction previews

The interaction pass adds illustrated doors, checkpoints, pickups, hazard states, and ability and dimension effects to three isolated scenes. The preview catalogs reuse the smooth blackwater art. See the [art source and derivation record](../reference/Smooth/Interactions/GENERATION.md) and [preview guide](SMOOTH_INTERACTION_PREVIEW.md).

## Smooth UI preview

The UI pass adds an illustrated title and map, talk portraits, skill icons, new panel art and a smoother text treatment. The isolated journey applies the theme through the existing screen controllers. See the [art provenance record](../reference/Smooth/UI/GENERATION.md) and [preview guide](SMOOTH_UI_PREVIEW.md).

## Smooth campaign art

The production Title, Map and Level scenes use the Smooth art selection by default. The campaign profile draws from the existing review stages and adds smooth glide wings and a Bellkeeper floor shockwave. The legacy scenes, theme and catalog remain the fallback. See the [active art guide](SMOOTH_ART_GUIDE.md) and [rollout provenance](../reference/Smooth/Rollout/GENERATION.md).
