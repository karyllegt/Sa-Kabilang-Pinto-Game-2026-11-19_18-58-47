# Smooth campaign UI polish

The production Smooth profile uses this kit. Review the seven paired Unity captures in the [campaign screen comparison](../tests/Screenshots/smooth/campaign-review/screens-before-after.png). Use **Sa Kabilang Pinto → Smooth Art → Campaign → Capture Before and After** to regenerate it. The sheet uses the original 684 px tile width and shows the same 2340 × 1080 simulated notch for both columns. The audit also checks 800 × 480. Before Stage is the seventh screen alongside Title, Level Map, Gameplay HUD, Pause, Lola Dialogue and Level Clear.

## Type and color

| Role | Face | Reference size at 960 × 540 |
| --- | --- | ---: |
| Wordmark | Press Start 2P | 44 main, 20 prefix |
| Modal heading | Press Start 2P | 26 |
| Map card heading and dialogue speaker | Press Start 2P | 22 and 20 |
| Body | Liberation Sans | 18 |
| HUD and control labels | Liberation Sans | 16–22 |
| Captions | Liberation Sans | 15 or larger |

The Smooth UI uses a 15 unit reference floor for screen space text, 12.8 displayed px or larger at 800 × 480. Map numbers use world space text over their own opaque labels. The same font assets already ship with the game. Both are covered by SIL Open Font License 1.1.

- Press Start 2P from the Press Start 2P Project Authors. [Bundled notice](../Assets/Art/Fonts/PressStart2P-OFL.txt)
- Liberation Sans from Google and Red Hat, reused from the project's TextMesh Pro assets. [Bundled notice](../Assets/TextMesh%20Pro/Fonts/LiberationSans%20-%20OFL.txt)

| Token | Color or usage | WCAG ratio on Night Purple |
| --- | --- | ---: |
| Lantern Gold | #F1C777 for ordinary-world and interaction accents | 11.26:1 |
| Night Purple | #191521 for opaque panels and label backings | Surface |
| Teal Accent | #41E0D0 for Spirit and dialogue subtitles | 10.93:1 |
| Violet Accent | #C07DFF for Inverted | 6.51:1 |
| Danger Red | #FF5A4A for low flame and danger | 5.82:1 |
| Ink and Dim | #0C0914 shadow and 95% modal scrim | Backing |
| Warm Cream | #FFF1DA for primary text | 16.10:1 |
| Fog Lilac | #D9CEE4 for secondary text | 11.87:1 |

Cream text measures 9.25:1 on the selected button fill and 7.92:1 on the pressed fill. The muted label measures 9.18:1 on the disabled panel tint. These are source color calculations. See the [rendered layout audit](../tests/Screenshots/smooth/campaign-review/ui-polish-audit.txt) for checks against actual screen pixels.

## Artwork and provenance

| Asset | Source and rights | Use |
| --- | --- | --- |
| `Theme/Panel.png` | Original programmatically drawn project artwork | 128 × 128 reusable 9-slice with capiz diamonds, a dark center and carved edging |
| `Theme/LanternMarks.png` | Original programmatically drawn project artwork | 256 × 256 atlas of 64 × 64 tiles for controls, parol, capiz, woven trim, map states, flame and the half heart |
| `Artwork/SceneryGrade.mat` and `SceneryGrade.shader` | Original project code, applied to the existing Smooth illustrations | Warm doorway light and a cool middle distance for the title and map |
| Full and empty hearts, eight skills, portraits, title and map paintings | Existing project assets and sources | [Smooth UI provenance](../reference/Smooth/UI/GENERATION.md) |

The trim uses a stylized interlace inspired by banig weaving. The parol, capiz and carved wood echo motifs already present in the game's environments. The new artwork has no external source material. The replacement panel keeps its original GUID and importer. The atlas adds approximately 31 KiB to the source textures after accounting for the smaller panel. Its maximum uncompressed RGBA footprint is 256 KiB. It has no mipmaps.

## Screen behavior

- Touch controls use the original hit boxes and Input System bindings. Buttons show normal and pressed faces. Dash, Ward, Veil and Bolt also tint their faces when unavailable. Menu focus uses a capiz mark.
- The HUD groups its larger hearts, flame meter, objective and skill marks by purpose. Vitality is displayed in whole hearts. The half heart is available for artwork reuse.
- Pause and Level Clear conceal the HUD and keep the scene visible under a strong scrim. The smaller dialogue card leaves more of Lola's scene visible.
- The map uses a lock, parol, check and focus corners to distinguish node states. Its background treatment keeps the existing landmarks and composition.
- Tala's larger silhouette and rim follow the animation, facing, gravity and hit blink. The view stays anchored at the original feet position. Backdrops use realm tints to separate hazards and platforms from scenery.
