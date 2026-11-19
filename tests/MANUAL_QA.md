# Manual QA checklist

Tick each box per input method as it is verified on a real device or in the Editor. `[ ]` = not yet checked, `[x]` = passed, `[!]` = failed (add a note). Items come from `docs/01_BUILD_SPEC.md` section C and `docs/02_SKILLS_AND_PROGRESSION.md`; milestone-specific items are added as each milestone lands.

| Check | Keyboard | Gamepad | Touch | Notes |
| --- | --- | --- | --- | --- |
| **M1 foundation** | | | | |
| Boot goes to the Title placeholder with no console errors | [ ] | [ ] | [ ] | |
| Jump/Interact walks Title → Map → Level; Pause goes back | [ ] | [ ] | [ ] | |
| Touch buttons appear only on a touch device; each lights the right action | n/a | n/a | [ ] | |
| F1 / gamepad Select toggles the debug overlay (development build only) | [ ] | [ ] | n/a | |
| Pixel art is sharp (no blur, no shimmer) at 1280x720 and 2400x1080 | [ ] | | | |
| Screen stays landscape when the phone is rotated | n/a | n/a | [ ] | |
| **Spec section C (fill in from M5 onward)** | | | | |
| Menu navigation | [ ] | [ ] | [ ] | |
| Tap/confirm completes the typewriter line, then advances | [ ] | [ ] | [ ] | |
| Map selection by tap and by arrows | [ ] | [ ] | [ ] | |
| Enter and replay a cleared level | [ ] | [ ] | [ ] | |
| Rapid double-tap on ENTER does not start the level twice | [ ] | [ ] | [ ] | |
| Rotate the device during a dialog: landscape lock holds | n/a | n/a | [ ] | |
| Background/resume the app during play and during a wipe: no soft lock | n/a | n/a | [ ] | |
| Low vitality and low flame are clearly visible | [ ] | [ ] | [ ] | |
| Audio ducks on pause | [ ] | [ ] | [ ] | |
| Vibration and screen shake toggles work | n/a | [ ] | [ ] | |
| Left-handed layout mirrors without overlap | n/a | n/a | [ ] | |
| **Skills (docs/02, from M6)** | | | | |
| Levels 1–4 back to back: each new skill introduced safely, hint on first use | [ ] | [ ] | [ ] | |
| Difficulty clearly higher each level; last room harder than the first | [ ] | [ ] | [ ] | |
| Pause during veil and during a bolt in flight; resume restores them | [ ] | [ ] | [ ] | |
| Quit and relaunch: skills and hearts correct; deleting the save removes skills | [ ] | [ ] | [ ] | |
| Rapid double-tap on VEIL and BOLT; two-finger jump plus bolt | n/a | n/a | [ ] | |
