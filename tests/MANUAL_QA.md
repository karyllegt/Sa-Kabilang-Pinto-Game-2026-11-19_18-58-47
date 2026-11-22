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
| **M4 levels (Title > Map > Jump starts the newest level)** | | | | |
| Level 1, 2 and 3 can each be finished by hand on Standard | [ ] | [ ] | [ ] | |
| The exit says LOCKED until the letter is found; the letter is in the middle room | [ ] | [ ] | [ ] | |
| Lanterns light, add a heart and flame, and dying returns to the last one | [ ] | [ ] | [ ] | |
| Quit at a lantern, relaunch, Map > Jump: resumes at that lantern | [ ] | [ ] | [ ] | |
| Anino patrols turn at ledge ends; ward kills them | [ ] | [ ] | [ ] | |
| Fragments raise max flame; flame stars refill it | [ ] | [ ] | [ ] | |
| Level 3 violet doors lead up the inverted route to a hanging door home | [ ] | [ ] | [ ] | |
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
| Left-handed layout mirrors without overlap | n/a | n/a | [ ] | Layout audit 10b: mirrored, no overlap (automated) |
| **M5 screens (automated evidence in tests/Screenshots/m5; still to try by hand)** | | | | |
| Title: arrow and highlight follow keys, gamepad and hover; Continue greyed without a save | [ ] | [ ] | [ ] | |
| Map: Up reaches the top bar, Down returns to the nodes; Talk by button or tapping Lola | [ ] | [ ] | [ ] | |
| Lola's first meeting once; her "!" returns after a new unlock | [ ] | [ ] | [ ] | |
| Pause button and Escape/Start pause; Back from Settings returns to Pause | [ ] | [ ] | [ ] | |
| DOOR touch button appears only at a door | n/a | n/a | [ ] | |
| Circle wipe looks right on a real screen (pixel edge, centered on the node) | [ ] | [ ] | [ ] | |
| **Skills (docs/02, from M6)** | | | | |
| Levels 1–4 back to back: each new skill introduced safely, hint on first use | [ ] | [ ] | [ ] | |
| Difficulty clearly higher each level; last room harder than the first | [ ] | [ ] | [ ] | |
| Pause during veil and during a bolt in flight; resume restores them | [ ] | [ ] | [ ] | |
| Quit and relaunch: skills and hearts correct; deleting the save removes skills | [ ] | [ ] | [ ] | |
| Rapid double-tap on VEIL and BOLT; two-finger jump plus bolt | n/a | n/a | [ ] | |
| **M6 skills (automated evidence in tests/Screenshots/m6; still to try by hand)** | | | | |
| Levels 1 to 4 back to back: each new skill is learned at the letter, and its first-use hint shows once | [ ] | [ ] | [ ] | |
| Pause during the veil and during a bolt in flight; resume restores timers and bolts exactly | [ ] | [ ] | [ ] | |
| Quit and relaunch: skills and the extra heart stay; deleting the save removes the skills | [ ] | [ ] | [ ] | |
| Rapid double-tap on VEIL and BOLT; two-finger jump plus bolt | n/a | n/a | [ ] | |
| Each skill sound is distinct and the fanfare plays on NEW SKILL (sound effects setting off silences them) | [ ] | [ ] | [ ] | |
| Map SKILLS button with keyboard/gamepad (Up to the top bar) and touch | [ ] | [ ] | [ ] | |
| **M7 levels (automated evidence: M7Tests, FullRunTests, tests/Screenshots/m7/obstacle_report.md, tests/Screenshots/m7)** | | | | |
| Play levels 1 to 4 back to back: each new skill introduced safely, its hint once; difficulty clearly higher each level | [ ] | [ ] | [ ] | |
| Level 5: the water feels fair (rises, slows, burns), and resets after a death | [ ] | [ ] | [ ] | |
| Level 8: a manananggal dive can be read and dodged; ward and bolt stun it | [ ] | [ ] | [ ] | |
| Level 9: parry the toll by hand with the ward; bolt and ward only hurt while stunned; the veil blocks the shockwave | [ ] | [ ] | [ ] | |
| Veil gauntlet: the violet curtain reads as "use the veil"; crossing under it feels right | [ ] | [ ] | [ ] | |
| Endless Nightmare unlocks after the Ending and loops with rising water | [ ] | [ ] | [ ] | |
| **M8 audio and feel (automated evidence: M8AudioFeelTests, AudioFeelPlayModeTests, FullRunTests)** | | | | |
| Doors: the Spirit muffle and the Inverted pitch-down are clear and not jarring; a lantern brings the music back smoothly | [ ] | [ ] | [ ] | |
| Music loops with no audible seam (let each act play for 2 minutes) and the boss theme starts when the Bellkeeper wakes | [ ] | [ ] | [ ] | |
| No clipping or distortion in a busy moment (bolts, bells and a hurt together), on the phone speaker and on headphones | [ ] | [ ] | [ ] | |
| Pause: the music drops, game sounds stop, menu clicks still sound; resume brings everything back | [ ] | [ ] | [ ] | |
| Screen shake and hit-stop feel punchy, not nauseating; Screen shake off removes all shake | [ ] | [ ] | [ ] | |
| Vibration on the phone and rumble on a gamepad; Vibration off removes both | [ ] | [ ] | [ ] | |
| Particles read at phone size (flame, fragment, veil, bolt, burnt thorns) | [ ] | [ ] | [ ] | |
| **R1 prefabs (automated evidence: R1PrefabTests, R1PrefabBaselineTests, tests/Screenshots/r1)** | | | | |
| Level 5: the rising flood is dark, translucent water (not a white block); lantern posts are dark wood | [ ] | [ ] | [ ] | |
| Open a few prefabs in Assets/Prefabs/Level (lantern, a creature, the exit): change a colour, Play, and see it in the level | [ ] | [ ] | [ ] | |
