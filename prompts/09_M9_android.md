# Prompt 09: M9 Android build and performance

Configure Android: IL2CPP, ARM64, minimum API 24, landscape only, safe-area, app icon, splash, package name, versioning. State every project-settings change before making it. Build a development APK and a release candidate app bundle. Do the performance pass: target 60 fps on a mid-range phone, no per-frame GC allocations in gameplay, sprite atlases (see the `unity:manage-sprite-atlas` skill), pooled particles and bolts, texture compression suited to pixel art.

Tests and verification: run the PlayMode suite and the screenshot suite on the Android emulator or a device if one is attached (say clearly what was and was not run on a real device). Report average fps, 1 percent low fps, GC alloc per frame, memory and APK or AAB size. Verify touch targets at real device dpi, rotate and background/resume without soft locks, kill the app mid-level and relaunch (Continue works), delete the save (clean start, Lola's first meeting again).

Done when: a build exists, the numbers are reported honestly, committed.
