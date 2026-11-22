// R3: Tala's animation sets. Builds the text-grid frames (Assets/Art/Source/Characters, made by
// tools/tala_frames/make_tala_frames.js), makes one PlayerAnimSet per character in Assets/Data/Player and hands them to
// the Player prefab. The prototype's 4 frames stay on the view as the fallback.
// Menu: Sa Kabilang Pinto > Setup > Tala Animation (R3). Batch: -executeMethod SkpTalaAnimSetup.RunBatch
using System.Collections.Generic;
using Skp.Player;
using UnityEditor;
using UnityEngine;

public static class SkpTalaAnimSetup
{
    public const string DataDir = "Assets/Data/Player";
    public static string SetPath(string ch) => $"{DataDir}/TalaAnims_{ch}.asset";

    /// <summary>Animation, file name part, ticks per frame (60 Hz), loop. Frame counts come from the grids.</summary>
    public static readonly (TalaAnim anim, string file, int ticks, bool loop)[] Table =
    {
        (TalaAnim.Idle, "idle", 10, true),          // 6 fps
        (TalaAnim.Run, "run", 6, true),             // 10 fps
        (TalaAnim.JumpRise, "jump_rise", 5, false),
        (TalaAnim.Apex, "apex", 6, false),
        (TalaAnim.Fall, "fall", 8, true),
        (TalaAnim.Land, "land", 4, false),          // PlayerAnimator.LandTicks = 2 x 4
        (TalaAnim.Dash, "dash", 4, false),
        (TalaAnim.Ward, "ward", 5, false),
        (TalaAnim.Hurt, "hurt", 8, false),          // HurtTicks = 2 x 8
        (TalaAnim.Death, "death", 9, false),
        (TalaAnim.Door, "door", 6, false),          // DoorTicks = 4 x 6
        (TalaAnim.Glide, "glide", 10, true),
        (TalaAnim.Bolt, "bolt", 5, false),          // BoltTicks = 2 x 5
    };

    [MenuItem("Sa Kabilang Pinto/Setup/Tala Animation (R3)")]
    public static void Run()
    {
        var errors = new List<string>();
        SkpPixelText.BuildAll(errors);
        foreach (var e in errors) Debug.LogError("[SkpTalaAnimSetup] " + e);
        AssetDatabase.Refresh();
        SkpProjectSetup.EnsureFolder(DataDir);
        var girl = BuildSet("girl");
        var boy = BuildSet("boy");
        var root = PrefabUtility.LoadPrefabContents(SkpTestRoomBuilder.PlayerPrefabPath);
        var view = root.GetComponentInChildren<PlayerView>(true);
        if (view != null)
        {
            view.girlAnims = girl;
            view.boyAnims = boy;
            PrefabUtility.SaveAsPrefabAsset(root, SkpTestRoomBuilder.PlayerPrefabPath);
        }
        PrefabUtility.UnloadPrefabContents(root);
        AssetDatabase.SaveAssets();
        Debug.Log("[SkpTalaAnimSetup] Tala animation sets built and wired");
    }

    public static void RunBatch()
    {
        Run();
        EditorApplication.Exit(0);
    }

    static PlayerAnimSet BuildSet(string ch)
    {
        var path = SetPath(ch);
        var set = AssetDatabase.LoadAssetAtPath<PlayerAnimSet>(path);
        if (set == null) { set = ScriptableObject.CreateInstance<PlayerAnimSet>(); AssetDatabase.CreateAsset(set, path); }
        var clips = new List<PlayerAnimSet.Clip>();
        foreach (var (anim, file, ticks, loop) in Table)
        {
            var frames = new List<Sprite>();
            // A-7: the hand-pixelled 48x54 frames (48 PPU, same world size) win when they exist
            for (int i = 0; i < 16; i++)
            {
                var s48 = AssetDatabase.LoadAssetAtPath<Sprite>($"{SkpAssetImportSettings.HiDetailDir}spr_tala48_{ch}_{file}_{i}.png");
                if (s48 == null) break;
                frames.Add(s48);
            }
            if (frames.Count > 0) { clips.Add(new PlayerAnimSet.Clip { anim = anim, frames = frames.ToArray(), ticksPerFrame = ticks, loop = loop }); continue; }
            string baseName = $"{SkpPixelText.GameDir}/Characters/spr_tala_{ch}_{file}";
            var single = AssetDatabase.LoadAssetAtPath<Sprite>(baseName + ".png");
            if (single != null) frames.Add(single);
            for (int i = 0; single == null && i < 16; i++)
            {
                var s = AssetDatabase.LoadAssetAtPath<Sprite>($"{baseName}_{i}.png");
                if (s == null) break;
                frames.Add(s);
            }
            if (frames.Count == 0) { Debug.LogError($"[SkpTalaAnimSetup] no frames for {ch} {file}"); continue; }
            clips.Add(new PlayerAnimSet.Clip { anim = anim, frames = frames.ToArray(), ticksPerFrame = ticks, loop = loop });
        }
        set.clips = clips.ToArray();
        EditorUtility.SetDirty(set);
        return set;
    }
}
