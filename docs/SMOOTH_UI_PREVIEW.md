# Smooth UI preview

Open **Sa Kabilang Pinto → Smooth Art → UI → Open UI Journey** in Unity, then choose **Sa Kabilang Pinto → Play Open Scene Once**. The preview uses the production title, map and level controllers with a temporary save. **Continue** starts at the first upper checkpoint in *The Blackwater Well*. **Level Map** opens with the first five nodes available. **Talk** opens Lola’s dialogue.

The preview applies the new panels, type, artwork and portrait sprites to the existing screen objects. The hit targets, focus graph, event system, safe-area fitters and screen navigation come from those same objects. A dark fade under the HUD keeps small labels legible over the world. The existing High Contrast setting strengthens the fade. The smooth skill library supplies display art while sharing the original skill tuning.

The two main palette checks give 16.4:1 for cream text on a panel and 9.3:1 for cream text on a selected button. Map controls use the original node positions and tap radius. The masked map prefab comparison allows changes to artwork and text materials.

**Capture Phone Screens** runs the existing layout audit for the title, map, HUD with all eight skills, pause, Lola dialogue, and level clear with a learned skill at 2340×1080 with a simulated notch and at 800×480. It writes PNGs and an audit report under `tests/Screenshots/smooth/ui`.

The six delivered 960×444 review images use the new art and the original phone layouts as their reference. The separate Unity batch run reached package assembly post-processing and stopped before saving captures. The standalone C# compile and serialized asset checks passed.

The [source artwork and prompts](../reference/Smooth/UI/GENERATION.md) record the AI-assisted illustration, derived textures and reused project fonts.
