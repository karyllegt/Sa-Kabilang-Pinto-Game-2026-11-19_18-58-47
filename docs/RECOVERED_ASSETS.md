# Recovered visual assets

Recovered from the user's earlier local SaKabilangPinto project on 2026-11-23. These are existing project images, not newly generated images. The older README describes its art as generated; do not relabel these as hand-drawn work by Mary or Ronn.

Source root: `C:/Users/Karylle/.codex/.chatgpt-projects/g-p-6ac9fd15ebe08191ac4e95cec9978330/SaKabilangPinto/Assets/Game/Resources`.

| Destination under Assets/Art/Recovered | Source under Resources | Use |
| --- | --- | --- |
| Tikbalang/00.png through 03.png | Animation/Tikbalang/00.png through 03.png | Four-frame patrol cycle |
| Manananggal/00.png through 03.png | Animation/Manananggal/00.png through 03.png | Four-frame wing cycle |
| Environments/HouseWall.png | PixelArt/HouseWall.png | Act I capiz interior |
| Environments/ForestWall.png | PixelArt/ForestWall.png | Act II forest |
| Environments/BelfryWall.png | PixelArt/BelfryWall.png | Act III church |
| Environments/DoorSpirit.png, DoorOrdinary.png, DoorInverted.png | PixelArt, same filenames | Staged for later integration; not yet wired |

Run `SkpRecoveredArtSetup.Run` in the Unity Editor or its Art menu to configure import settings and wire creature prefabs and the LevelArt backdrops. It preserves creature canvas height in world units, bottom-centre pivots and source aspect ratio. Recovered images retain their source palettes and resolutions in this explicitly separate collection. The setup does not alter simulation hitboxes.

Current Tala, Anino and Bellkeeper remain in place: the old cloaked Anino and wooden-mask boss do not match the current game identities. Do not copy old release signing files, saves or credentials.

Verification still requires actual scene captures and review at game scale. The original large creature source sheet has more detail than the recovered runtime frames; the latter must be judged in gameplay before claiming visual completion.

## Verification on 2026-11-23

Unity 6000.6.6f1 compiled the changed scripts and completed `SkpRecoveredArtSetup.Run`. Its per-frame world-height assertions passed. The selected EditMode art, prefab and Tala animation regression suites passed **16/16**, with zero failures: `tests/Results/codex_recovery_editmode.xml` (local test output). `git diff --check` passed. This does not establish full PlayMode, layout, Android or listening QA. The first sandboxed Editor could not initialize licensing; the approved external batch run completed integration and testing.
