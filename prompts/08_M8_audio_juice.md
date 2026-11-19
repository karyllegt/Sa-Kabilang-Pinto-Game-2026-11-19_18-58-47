# Prompt 08: M8 Audio and juice

Add music per act with a dimension filter or crossfade (Ordinary, Spirit, Inverted), a menu theme, and SFX for jump, double jump, land, dash, ward, veil, bolt, bolt hit, burn, hurt, die, lantern, flame, fragment, letter, new skill, door, locked, select, boss bell and toll. Use an Audio Mixer with Music and SFX groups wired to the Settings toggles, ducking on pause, no clipping. Screen shake (respects the setting), hit-stop, particles for flame, fragments, veil, bolts and burnt thorns, vibration (respects the setting). Use original or properly licensed OGG audio only and record licenses in the credits.

Tests: the mixer toggles mute the right groups; screen shake and vibration respect settings; pause ducks and resumes audio; no audio exceptions in the scripted full run; GC allocations per frame in gameplay stay at 0 after warm-up (Profiler recorder test).
Manual: dimension audio transitions are clear and not jarring.

Done when: tests pass, committed.

The provided OGGs are placeholders (the music does not loop seamlessly): re-author or loop them properly and add the missing SFX listed in `docs/04_ASSET_GUIDE.md`.
