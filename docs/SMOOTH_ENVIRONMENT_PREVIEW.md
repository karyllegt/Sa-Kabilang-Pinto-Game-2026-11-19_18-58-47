# Smooth environment previews

Open **Sa Kabilang Pinto → Smooth Art → Environments → Open Act I, II, or III** in Unity, then choose **Sa Kabilang Pinto → Play Open Scene Once**. Each scene starts at the first upper checkpoint on Standard with the skills normally available at that point and a temporary preview save.

| Scene | Campaign area | What to review |
| --- | --- | --- |
| Act I House Towers | Level 3, *The Missing Staircase* | Warm capiz windows and lamps, carved timber, and the teal and violet doors on the same floor. |
| Act II Blackwater Well | Level 5, *The Blackwater Well* | Moss-covered ledge tops, root silhouettes below floors, and the illustrated water along the original rising flood boundary. |
| Act III Bell Tower | Level 7, *Bells Without Hands* | Dressed stone, candle niches, and distant bronze bells behind the stone routes. |

Gold still marks safe or interactive objects, including the checkpoint parols. The existing red hazard signals and dimension visibility rules carry through. Teal Spirit ledges and violet Inverted ledges retain their collision-side edge. Low-opacity scenery sits behind the playable geometry. Stationary background planes provide depth without parallax.

Each preview uses the original campaign level configuration and generator, with a separate presentation catalog and backdrop set. Replacement sprites use their source world dimensions, pivot and tiled border widths. The texture import settings apply only to the smooth artwork. The original renderer places ledges, platforms, hazards and flood from the same world rectangles. The preview scenes are outside the game build list.

The **Capture All Three Acts** command writes five 1280×720 PNGs under `tests/Screenshots/smooth/environments`, including both dimension treatments. The review compositions use the original campaign generator output and the baked sprite textures. The capture command compiled during preflight, and the isolated Unity batch run stopped at an editor assembly post-processing error before rendering.

The [ImageGen prompts and derivation record](../reference/Smooth/Environments/GENERATION.md) document the AI-assisted source artwork and the fitted Unity sprites.
