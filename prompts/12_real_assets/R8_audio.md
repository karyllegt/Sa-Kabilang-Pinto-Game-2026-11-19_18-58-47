# Prompt R8: Music and sound

The music is the prototype's generative random notes, looped in M8; the effects are synth blips. Replace them. SpriteCook makes images only, so it is not used here. Before starting, ask me how the audio will be made: composed and recorded by us, made with a music or sound tool (name it), or licensed packs. Record the choice and its licence terms in `DECISIONS.md`, and list every file in `docs/ART_MANIFEST.md` under an Audio section.

1. Music: menu, Act I, Act II, Act III and the boss, written to loop (loop points set in the file, not crossfaded afterwards), with Filipino colour where it fits (kulintang, bamboo, a rondalla guitar band, church bells in Act III). The Spirit filter and the Inverted pitch on the mixer stay; check they still sound right on the new tracks.
2. Effects: every cue in `SfxBank`, redone as one family (wooden house, lantern fire, spirit air, bronze bell). Keep the cue names so no code changes.
3. Sources and licences: original recordings or composition, or clearly licensed packs or tools. Record each source and licence in `DECISIONS.md` and in Credits.
4. Mastering: music peaks at or under the M8 headroom numbers, and effects are normalised so the headroom test still passes.
5. The old files stay in place as fallbacks until R9.

Tests: the M8 audio suite passes on the new files (headroom, loop seam, every cue has a clip, mixer toggles, pause duck, dimension glide); a test checks the loop points; Credits list every source; the full suites pass.

Done when: no prototype or synth placeholder is assigned in `AudioTuning` or `SfxBank`, committed.
