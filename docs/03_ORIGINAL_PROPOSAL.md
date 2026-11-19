# Original proposal (plain text of the group's docx)

Source of truth for the concept and story. Where it conflicts with docs/00 to 02 (solo build, skills, longer game, map layout), the docs win.

GAME CONCEPT PROPOSAL

Sa Kabilang Pinto

A Filipino Folklore Dimension Platformer

[]

Figure 1. Title screen mockup: a pixel-art horror platformer designed for Android phones.

  -----------------------------------------------------------------------
  Genre                 2D Narrative Platformer
  --------------------- -------------------------------------------------
  Target Platform       Android mobile devices and Google Play

  Developer             Karylle Torres and Pangkat Parol

  Project Status        Working game concept and release build proposal
  -----------------------------------------------------------------------

Submitted for 1st Week of Finals

Game Title and Genre

Sa Kabilang Pinto translates to “Beyond the Door.” It is a 2D narrative-driven platformer for Android that combines Filipino folklore, precision movement, branching door choices, and a supernatural mystery. The player climbs through a haunted vertical world where each doorway leads to a different dimension, changing the platforms, enemies, hazards, and route forward.

Premise and Player Objective

Tala, a young Filipino traveler, awakens inside a sealed ancestral house during a distorted fiesta night. A bell keeps calling from above. To escape, Tala must collect fragments of a protective flame, learn why the house is bending reality, and defeat the Bellkeeper guarding the final door. The campaign moves through nine escalating stages inspired by Filipino frightening folklore: shadow spirits, a balete tree, a blackwater well, a tikbalang trail, and the Manananggal.

Design Direction

-   Mobile first: The game is built for landscape phone play with large touch buttons placed away from the action area. It adapts to different screen sizes while keeping the pixel art sharp.

-   Filipino folklore: Every act uses a distinct visual motif, sound bed, enemy set, and story fragment rather than reusing one generic haunted-house level.

-   Dimension twist: Ordinary, Spirit, and Inverted worlds occupy the same location but reveal different paths. Entering a door may help the player climb, or place them in a more dangerous version of the room.

-   Spooky but readable: Warm parol light, capiz windows, wood panels, roots, and deep shadows create tension while clear color cues signal doors, hazards, health, and objectives.

[]

Figure 2. Character selection mockup. Players can choose a boy or girl protagonist before the story begins.

Visuals and Illustrations

The game uses original 2D pixel-art scenes, sprites, UI, effects, and environmental tiles. The look is inspired by Filipino ancestral homes and folklore rather than a realistic horror style. Gold light signals safety, teal signals spirit paths, and red warns of direct danger.

+-----------------------------------------------------------------------------------------------------------------------------------+---------------------------------------------------------------------------------------------------------+
| []                                                                                                                                | []                                                                                                      |
|                                                                                                                                   |                                                                                                         |
| Ordinary World: Fiesta Night. The HUD shows chapter, vitality, flame, lives, level progress, score, and large one-thumb controls. | Inverted World: the same tower becomes an unstable dimension with altered gravity, hazards, and routes. |
+===================================================================================================================================+=========================================================================================================+
+-----------------------------------------------------------------------------------------------------------------------------------+---------------------------------------------------------------------------------------------------------+

[]

Figure 3. Bellkeeper boss encounter. A dedicated boss bar, ward timing, and a moving arena turn the final level into a climax rather than a simple finish line.

Interface and Accessibility

The interface prioritizes quick recognition while the player is moving. Vitality and flame are always visible at the top; the current level objective appears beneath them. On-screen buttons support movement, jump, dash, ward, and door interaction. Pause options include sound controls, vibration, reduced flash, screen shake, and a left-handed control layout.

Core Gameplay Mechanics

How the Game Is Played

Each level is a vertical and horizontal climb. The player reads the room, avoids traps, defeats or bypasses folklore creatures, collects flame, and reaches a colored doorway. Some doors are locked until a clue, switch, or flame fragment is collected. Choosing a door shifts the player into a new dimension; platforms may appear, disappear, reverse, or become unsafe. The main camera follows the player up, down, and across the tower so the world feels tall and connected.

Controls

  ----------------------------------------------------------------------------------
  Action                  Touch Control            Keyboard and Controller Support
  ----------------------- ------------------------ ---------------------------------
  Move                    Left and right arrows    A/D, left stick, or D-pad

  Jump                    JUMP button              Space or gamepad south button

  Dash                    DASH button              Q or shoulder button

  Ward                    WARD button              F or gamepad north button

  Door / interact         ENTER DOOR button        E or gamepad east button

  Pause                   Top-right pause button   Escape or menu button
  ----------------------------------------------------------------------------------

Core Loop

-   Observe: Read the room, identify hazards, enemy patrols, and the safest doorway.

-   Move and platform: Jump between ledges, use the vertical climb, and maintain momentum while the camera tracks the route.

-   Manage resources: Collect flame, spend it on dash movement, and protect vitality from hazards and enemies.

-   Change dimension: Enter a door to expose a new route or create a more difficult version of the room.

-   Survive and progress: Reach checkpoints, carry clues forward, defeat the Bellkeeper, and reveal the ending.

Skills and Enemy Interaction

Dash gives a short burst of movement and temporary safety at the cost of flame. Ward is a timed defensive skill that stuns or harms vulnerable creatures. Anino shadows repeat routes and pressure movement; Tikbalang patrols block paths; Manananggal attacks from above; environmental roots, broken floors, capiz shards, and floodwater punish careless jumps. The Bellkeeper combines movement, ward timing, and arena hazards in a final boss fight.

Winning Losing Progression and Difficulty

Winning and Losing Conditions

  ------------------------------------------------------------------------------------------------------------------------------------------------------------------
  Condition                           Result
  ----------------------------------- ------------------------------------------------------------------------------------------------------------------------------
  Win a room                          Reach the correct doorway after meeting its clue or objective.

  Win a level                         Finish the room sequence and unlock the next narrative chapter.

  Win the campaign                    Defeat the Bellkeeper in Level 9, open the last door, and view the ending.

  Lose vitality                       Taking hazards or enemy damage removes vitality. At zero, the player returns to the latest checkpoint.

  Lose lives                          Standard and Nightmare modes use limited lives. Story mode has unlimited lives so players can experience the full narrative.

  Save progress                       Checkpoints save level, room, vitality, lives, flame, collected clues, and key puzzle state.
  ------------------------------------------------------------------------------------------------------------------------------------------------------------------

Campaign Structure

The campaign has nine levels across three acts. Difficulty grows by layering new platform patterns, faster enemies, and more consequential dimension choices. The player can choose Story, Standard, Nightmare, or Endless Nightmare. Endless Nightmare loops rooms with rising pressure for players who want a score challenge after completing the story.

  --------------------------------------------------------------------------------------------------------------------------------------------------------
  Act                     Levels                     Folklore and Challenge Focus
  ----------------------- -------------------------- -----------------------------------------------------------------------------------------------------
  I The House Wakes       1. Fiesta Night            Teach movement, doors, shadow hazards, and the first Spirit dimension.
                          2. Behind the Wall         
                          3. The Missing Staircase   

  II The Roots Remember   4. Roots That Remember     Balete roots, floodwater, Tikbalang patrols, longer vertical routes, and more demanding clue gates.
                          5. The Blackwater Well     
                          6. The Horseman Trail      

  III The Last Bell       7. Bells Without Hands     Inverted rooms, Manananggal attacks, near-finale escapes, and the Bellkeeper boss fight.
                          8. The Wingbeat Corridor   
                          9. The Last Bell           
  --------------------------------------------------------------------------------------------------------------------------------------------------------

Team Structure and Task Allocation

Pangkat Parol is planned as a four-member development group. The responsibilities below are intentionally distinct so each member owns a clear part of the production. Replace the bracketed member names with the group’s final names before printing.

  -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
  Member                  Role                                        Specific Responsibilities
  ----------------------- ------------------------------------------- -----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
  Karylle Torres          Game Director and Lead Programmer           Owns the game concept, project integration, player movement, camera, core systems, Android build, playtesting decisions, and final submission package.

  [Member 2 Name]         Pixel Artist and UI Designer                Creates character and enemy sprite sheets, tiles, backgrounds, visual effects, menus, HUD layouts, icon assets, and scene-to-scene visual consistency.

  [Member 3 Name]         Level Designer and Narrative Writer         Designs the nine level routes, door and dimension logic, checkpoints, traps, boss arena flow, Filipino folklore research, dialogue, story pacing, and difficulty curve.

  [Member 4 Name]         Audio Designer and Quality Assurance Lead   Produces or curates original-safe music and sound effects, mixes audio, tests every level and control layout, documents bugs, verifies Android behavior, and prepares release evidence.
  -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

Production Goal

Sa Kabilang Pinto aims to feel like a complete mobile horror platformer, not a single-room prototype. Its strongest differentiator is the door-based dimension system: the player does not only jump over obstacles, but continually decides which version of the world to enter, what risk to take, and how to use movement, flame, and ward skills to survive Filipino folklore-inspired threats.

Planned Deliverable

A playable Android release build for manual installation and Google Play submission, including responsive mobile controls, a full nine-level campaign, four difficulty modes, a save and checkpoint system, sound and music, accessibility options, a privacy policy, store art, and a signed app bundle for publication.
