# Prompt 06: M6 Skills, letters, HUD chips, touch buttons

Implement `docs/02_SKILLS_AND_PROGRESSION.md` features 1 and 2: `SkillDefinition` assets for all eight skills, `SkillService` (persisted), the letter pickup flow (letter dialog then NEW SKILL dialog, SKILL KNOWN on replay), Double Jump, Lambong Veil, Liwanag Bolt (kills Anino, stuns Tikbalang and Manananggal, burns roots and thorn walls, stuns the shadow, only hurts the Bellkeeper while stunned), Swift Dash, Heart Charm, Triple Bolt, Greater Veil, Pakpak Glide. HUD skill chips with cooldown fill and key labels, passive badges, VEIL and BOLT touch buttons that appear when learned (mirrored in left-handed mode), Skills page in How to Play and a skill list on the Map, skill lines added to Lola's dialog, distinct audio cues.

Rules: veil and every immunity go through `CanBeHurt()`; all numbers in `SkillTuning`; everything mirrored under Inverted gravity.

Tests: every per-skill Play Mode test and the letter-flow test listed in the spec; HUD screenshots with 0, 2 and 8 skills in normal and left-handed layouts at all resolutions with overlap and 48 dp assertions; letter dialog screenshots for every skill.

Done when: all tests pass, committed.
