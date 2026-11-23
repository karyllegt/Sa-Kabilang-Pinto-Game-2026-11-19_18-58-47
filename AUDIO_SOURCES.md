# Audio provenance and integration

## Recorded foley

Kenney Vleugels, **RPG Audio**, published 2014. Publisher: https://kenney.nl/assets/rpg-audio . Download mirror: https://opengameart.org/sites/default/files/RPGsounds_Kenney.zip . Licence: CC0 1.0, included verbatim at `Assets/Audio/Licensed/Foley/LICENSE.txt`.

The delivered OGGs are copied without resynthesis. They are recordings of cloth, footsteps, paper/book handling, creaking wood, doors, leather and metal objects. Skill sounds are designed with these physical recordings; for example cloth movement accompanies dash/glide and a metal impact accompanies ward/parry. A metal pot impact is not represented as a recording of a church bell.

The authoritative cue-to-source mapping is the `replacements` dictionary in `Assets/Editor/SkpPresentationUpgrade.cs`. The completed mapping covers the existing cue bank plus dimension changes and alternating footsteps. Footsteps advance with grounded distance and stop during air movement, dashing, water and stopped simulation. Splash fires on entering water rather than repeating on every damage tick. Falling bells now trigger the bell recording rather than the crumble cue. Cue volumes retain headroom through the existing mixer.

## Human-authored music

Kevin MacLeod (incompetech.com):

| Game use | Track | Composer's source |
| --- | --- | --- |
| Menu / ending | Evening Fall (Harp) | https://incompetech.com/music/royalty-free/mp3-royaltyfree/Evening%20Fall%20-%20Harp.mp3 |
| Act I | Ghost Story | https://incompetech.com/music/royalty-free/mp3-royaltyfree/Ghost%20Story.mp3 |
| Act II | Darkest Child | https://incompetech.com/music/royalty-free/mp3-royaltyfree/Darkest%20Child.mp3 |
| Act III | Ossuary 6 - Air | https://incompetech.com/music/royalty-free/mp3-royaltyfree/Ossuary%206%20-%20Air.mp3 |
| Boss | The House of Leaves | https://incompetech.com/music/royalty-free/mp3-royaltyfree/The%20House%20of%20Leaves.mp3 |

Attribution: these tracks by Kevin MacLeod (incompetech.com) are licensed under Creative Commons Attribution 4.0, https://creativecommons.org/licenses/by/4.0/ . Composer licensing information: https://incompetech.com/music/royalty-free/licenses/ . Attribution appears in the game's Credits screen. `tools/media-codec/build_music_loops.ps1` decodes the recordings and `MusicLoop.cs` creates frame-aligned, equal-power two-second tail-to-head crossfades, peak-normalized to 0.85. Outputs in `Assets/Audio/Licensed/Music/Loops` import as streamed Vorbis with stereo preserved. Dimension-dependent pitch/filter and in-engine mixing remain. No AI music generation is used. Loop joins still require both regression checks and listening review.

The repaired loop preparation trims the final mastering fade/silence before the overlap: it retains the last 100 ms block whose RMS reaches at least 10% of the loudest block. This prevents the Act II join from dipping into silence. Original MP3s remain unmodified; all five runtime loops were regenerated with this rule.

## Bell, water and fire recordings

The church-bell recording is by Ulrich Metzner (2008-2010), cut/faded by qubodup: https://opengameart.org/content/church-bell . Original: https://opengameart.org/sites/default/files/churchbell.flac . It is **CC BY-SA 3.0**, not CC0. `church_bell.wav` is a 16-bit stereo PCM conversion and remains under https://creativecommons.org/licenses/by-sa/3.0/ . Source, conversion and licence are recorded in `Assets/Audio/Licensed/Foley/RECORDING_CREDITS.txt` and in-game credits. The build should include this recording and credit file as separately accessible media alongside the game.

Fire Crackling by AntumDeluge: https://opengameart.org/content/fire-crackling/ , CC0. Water Splash and sand footsteps by Peludo: https://opengameart.org/content/water-splash-and-sand-footsteps , CC0. The author explicitly describes recording the water with a webcam and bucket. Source links and optional author credits are retained.

The local conversion helper was obtained from the published `@ffmpeg-installer/win32-x64` 4.1.0 npm package: https://registry.npmjs.org/@ffmpeg-installer/win32-x64/-/win32-x64-4.1.0.tgz . It is a build-time tool, not a game dependency. Its older complex-filter path crashed during loop preparation; those incomplete outputs were discarded. The final crossfade algorithm is the reproducible C# implementation above, using FFmpeg only for decoding.

## Still awaiting acceptance

Source replacement covers the cue bank, including bell/toll, fire/burn, water/splash and UI reward sounds. Outdoor night ambience uses **Crickets Ambient Noise - loopable** by Wolfgang_ (optional credit Ted Kerr), CC0: https://opengameart.org/content/crickets-ambient-noise-loopable . Source https://opengameart.org/sites/default/files/crickets_1.mp3 is copied as `night_crickets.mp3`, with no synthesis. It fades in quietly in Acts I/II, recedes in Spirit, and fades out indoors/in menus; the existing Game mixer group makes SFX mute and pause ducking apply. Natural-loop listening, full gameplay listening, headphones/phone-speaker mixing and physical-device acceptance remain outstanding. Downloads and cue mapping alone do not complete acceptance.
