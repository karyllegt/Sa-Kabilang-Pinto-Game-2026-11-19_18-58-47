// A-7: puts the hand-pixelled 48x54 Tala into the game.
//  - The frames are made outside Unity by tools/tala_hand/tala_anim.js (node tools/tala_hand/tala_anim.js export
//    Assets/Art/Game/Characters48) and imported at 48 PPU (SkpAssetImportSettings), so Tala keeps her 16x18 px world box.
//  - Every scene's Pixel Perfect Camera moves to 1440x810 at 48 PPU (A-2): the same world view as 480x270 at 16 PPU,
//    so 48 PPU art shows every pixel and the 16 PPU art draws as exact 3x3 blocks. The UI canvases do not change.
//  - The Tala animation sets are rebuilt; SkpTalaAnimSetup prefers the 48 px frames and falls back to the R3 16 px ones.
// Menu: Sa Kabilang Pinto > Setup > Tala 48 and 3x Camera (A-7). Batch: -executeMethod SkpTala48Setup.RunBatch
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class SkpTala48Setup
{
    public const string PalettePath = "Assets/Art/Source/palette_tala48.txt";

    /// <summary>The Tala 48 palette (symbol hex name per line), the only colours the 48 px frames may use.</summary>
    public static List<Color32> PaletteColours()
    {
        var list = new List<Color32>();
        foreach (var raw in File.ReadAllLines(PalettePath))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("#")) continue;
            var parts = line.Split(' ');
            if (parts.Length < 2 || !ColorUtility.TryParseHtmlString(parts[1], out var c)) continue;
            list.Add((Color32)c);
        }
        return list;
    }

    [MenuItem("Sa Kabilang Pinto/Setup/Tala 48 and 3x Camera (A-7)")]
    public static void Run()
    {
        AssetDatabase.Refresh();
        foreach (var path in Directory.GetFiles("Assets/Scenes", "*.unity", SearchOption.AllDirectories).Select(p => p.Replace('\\', '/')))
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            bool changed = false;
            foreach (var ppc in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<PixelPerfectCamera>(true)))
            {
                ppc.assetsPPU = SkpProjectSetup.CameraPPU;
                ppc.refResolutionX = SkpProjectSetup.CameraRefWidth;
                ppc.refResolutionY = SkpProjectSetup.CameraRefHeight;
                var cam = ppc.GetComponent<Camera>();
                if (cam != null) cam.orthographicSize = SkpProjectSetup.CameraRefHeight / 2f / SkpProjectSetup.CameraPPU;
                EditorUtility.SetDirty(ppc);
                changed = true;
            }
            if (changed) EditorSceneManager.SaveScene(scene);
        }
        SkpTalaAnimSetup.Run();
        Debug.Log("[SkpTala48Setup] cameras at 1440x810 / 48 PPU, Tala 48 frames wired");
    }

    public static void RunBatch()
    {
        Run();
        EditorApplication.Exit(0);
    }
}
