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
| **M2 player feel (open Assets/Scenes/Dev/TestRoom.unity, press Play)** | | | | |
| Run feels like the prototype: quick start, short slide on release | [ ] | [ ] | n/a | |
| Jump reaches the 1, 2, 3 tile ledges from the floor; 4 tiles needs the step before it | [ ] | [ ] | n/a | |
| Short tap gives a small hop, holding gives the full jump | [ ] | [ ] | n/a | |
| Jump just after walking off a ledge still works (coyote) | [ ] | [ ] | n/a | |
| Jump pressed just before landing fires on landing (buffer) | [ ] | [ ] | n/a | |
| Dash (Q/Shift, LB) bursts about 3.7 tiles and costs flame; spikes do not hurt mid-dash | [ ] | [ ] | n/a | |
| Spikes knock back, sprite blinks while invulnerable | [ ] | [ ] | n/a | |
| Touching the far lanterns lights them; dying respawns at the last one | [ ] | [ ] | n/a | |
| Camera follows smoothly and never shows outside the room | [ ] | [ ] | n/a | |
| **M3 dimensions (open Assets/Scenes/Dev/DimensionRoom.unity, press Play)** | | | | |
| E at the teal door enters Spirit: teal tint, Spirit stairs solid, Ordinary ledge and spikes faint and harmless | [ ] | [ ] | n/a | |
| In Spirit the Anino appears about 4 s behind on the same path and hurts on touch | [ ] | [ ] | n/a | |
| Ward (F) next to the Anino stuns it (faint teal) for about 3 s | [ ] | [ ] | n/a | |
| Reaching the lantern on the top ledge returns to the Waking World | [ ] | [ ] | n/a | |
| E at the violet door: gravity flips, Tala falls up onto ceiling ledges, sprite upside down, jump goes down | [ ] | [ ] | n/a | |
| Hanging home door at the top returns to the lantern in the Waking World | [ ] | [ ] | n/a | |
| Dying in Spirit or Inverted respawns in the Waking World with no tint and no shadow | [ ] | [ ] | n/a | |
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
