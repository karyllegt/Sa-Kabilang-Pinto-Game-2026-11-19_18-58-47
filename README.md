# Sa Kabilang Pinto

A 2D pixel-art platformer for Android, made in **Unity 6000.6.6f1** by a 4-person student team.

**Deadline: December 1, 2026**

---

## The game

The player moves between **three dimensions**:

| Dimension | What it does |
|-----------|--------------|
| **Ordinary** | The normal world. |
| **Spirit** | The spirit world. A **Spirit shadow** follows the player's recorded path. |
| **Inverted** | Gravity is flipped. |

**Enemies:** Anino, Tikbalang, Manananggal

**Final boss:** the Bellkeeper

**Structure:** nine levels in three acts.

---

## Scope for the Dec 1 deadline

Only these are in scope:

- **Levels:** 1, 2, 4, 5 and the boss
- **Difficulty:** Story and Standard only

Everything else (levels 3, 6, 7, 8, 9 and any other difficulty) waits until after the deadline.

---

## Standards

| Topic | Rule |
|-------|------|
| Tiles | 16x16 pixels |
| Screen | Landscape, 16:9, reference resolution **480x270** |
| Art format | PNG |
| Audio format | OGG |
| File names | lowercase with underscores and a type prefix |

File name examples:

| Prefix | Meaning | Example |
|--------|---------|---------|
| `spr_` | sprite | `spr_player_run_01` |
| `tile_` | tile | `tile_wood_top` |
| `sfx_` | sound effect | `sfx_dash` |
| `mus_` | music | `mus_act1` |

---

## Screen flow

All menus and boxes use **Unity UI (Canvas)**. There is no walkable map.

**Before a stage**

1. **Level select screen** - one node per level. A level stays locked until the previous level is finished.
2. **Screen wipe** transition.
3. **Intro box** - shows the level name and intro text.

**After a stage**

4. **Completion box** - shows the clue text and what the player collected, with three buttons:
   - Next level
   - Replay
   - Level select

**After the boss**

5. Show the **ending text**.

---

## Story text

- All story text lives in `Assets/Story/levels.json`.
- This file is owned by **Ronn Lester**.
- Code **reads** it and **never edits** it.
- The only exception: if the file does not exist, code may create one sample file.

---

## Team folder ownership

| Folder | Owner |
|--------|-------|
| `Assets/Art` | Mary Rose |
| `Assets/Levels` | Ronn Lester |
| `Assets/Story` | Ronn Lester |
| `Assets/Audio` | Nash |

The programmer (Karylle) may only edit:

- `Assets/Scripts`
- `Assets/Scenes/Test`
- `Assets/Scenes/UI`
- `Assets/Prefabs/UI`
- Project settings

**Never edit a folder you do not own.** If you need a change in someone else's folder, ask its owner.

---

## Rules

- **One scene per level.**

---

## Git

- Large files (`*.png`, `*.ogg`, `*.wav`, `*.psd`) are stored with **Git LFS**. Install Git LFS once with `git lfs install` before you clone or pull.
- Never commit Android keystores (`*.keystore`, `*.jks`). They are ignored on purpose.
