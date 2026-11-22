// R2: the style approval sheet. Lays the cleaned SpriteCook options, the text-grid option and the prototype versions side
// by side at 1x and 4x on the night panel colour, and writes tests/Screenshots/r2/style_options.png.
// Batch: -executeMethod SkpArtSheet.R2Batch (cleans the SpriteCook downloads and builds the grids first).
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class SkpArtSheet
{
    public const string Out = "tests/Screenshots/r2/style_options.png";

    public static void R2Batch()
    {
        var errors = new List<string>();
        SkpPixelText.BuildAll(errors);
        foreach (var e in errors) Debug.LogError("[SkpArtSheet] " + e);
        var cleaned = SkpArtCleanup.CleanAll();
        Debug.Log("[SkpArtSheet] cleaned: " + string.Join(", ", cleaned));
        var rows = new List<(string label, string[] files)>
        {
            ("Tala: prototype | A SpriteCook | B SpriteCook | C text grid", new[] {
                "Assets/Art/Characters/spr_pl_girl_0.png", "Assets/Art/Game/Characters/spr_tala_girl_idle_a.png",
                "Assets/Art/Game/Characters/spr_tala_girl_idle_b.png", "Assets/Art/Game/Characters/spr_tala_girl_idle.png" }),
            ("Lantern: prototype flame | new unlit | new lit;  Door: prototype | new", new[] {
                "Assets/Art/Items/spr_star.png", "Assets/Art/Game/Props/prop_lantern_0.png", "Assets/Art/Game/Props/prop_lantern_1.png",
                "Assets/Art/Props/door_spirit_16x28.png", "Assets/Art/Game/Props/prop_door_spirit.png" }),
        };
        Build(rows, new[] { "Assets/Art/Backgrounds/bg_act1.png", "Assets/Art/Game/Backdrops/bg_act1_far.png" }, Out);
        EditorApplication.Exit(errors.Count == 0 ? 0 : 5);
    }

    public const string Picked = "tests/Screenshots/r2/art_bible_sheet.png";

    /// <summary>After the pick: the approved style (Tala option A, the props redrawn to match, the Act I backdrop) beside the prototype.</summary>
    public static void R2PickedBatch()
    {
        var errors = new List<string>();
        SkpPixelText.BuildAll(errors);
        foreach (var e in errors) Debug.LogError("[SkpArtSheet] " + e);
        SkpArtCleanup.CleanAll();
        var rows = new List<(string label, string[] files)>
        {
            ("Tala: prototype | picked style (A)", new[] { "Assets/Art/Characters/spr_pl_girl_0.png", "Assets/Art/Game/Characters/spr_tala_girl_idle_a.png" }),
            ("Lantern unlit, lit; Spirit door: prototype | new", new[] {
                "Assets/Art/Game/Props/prop_lantern_0.png", "Assets/Art/Game/Props/prop_lantern_1.png",
                "Assets/Art/Props/door_spirit_16x28.png", "Assets/Art/Game/Props/prop_door_spirit.png" }),
        };
        Build(rows, new[] { "Assets/Art/Backgrounds/bg_act1.png", "Assets/Art/Game/Backdrops/bg_act1_far.png" }, Picked);
        EditorApplication.Exit(errors.Count == 0 ? 0 : 5);
    }

    static Texture2D Load(string path)
    {
        if (!File.Exists(path)) { Debug.LogWarning("[SkpArtSheet] missing " + path); return null; }
        var t = new Texture2D(2, 2) { filterMode = FilterMode.Point };
        t.LoadImage(File.ReadAllBytes(path));
        return t;
    }

    /// <summary>Sprite rows at 1x then 4x, then the backdrops at 1x one under the other.</summary>
    static void Build(List<(string label, string[] files)> rows, string[] backdrops, string outPath)
    {
        const int pad = 12, scale = 4;
        var bg = new Color32(0x14, 0x10, 0x1c, 255);
        var plates = new List<(int x, int y, Texture2D t, int s)>();
        int y = pad, width = 0;
        foreach (var (label, files) in rows)
        {
            var texs = new List<Texture2D>();
            foreach (var f in files) texs.Add(Load(f));
            int rowH = 0, x = pad;
            foreach (var t in texs) { if (t == null) continue; plates.Add((x, y, t, 1)); x += t.width + pad; rowH = Mathf.Max(rowH, t.height); }
            x += pad * 2;
            foreach (var t in texs) { if (t == null) continue; plates.Add((x, y, t, scale)); x += t.width * scale + pad; rowH = Mathf.Max(rowH, t.height * scale); }
            width = Mathf.Max(width, x);
            y += rowH + pad * 2;
        }
        foreach (var f in backdrops)
        {
            var t = Load(f);
            if (t == null) continue;
            plates.Add((pad, y, t, 1));
            width = Mathf.Max(width, t.width + pad * 2);
            y += t.height + pad;
        }
        int H = y + pad;
        var sheet = new Texture2D(width, H, TextureFormat.RGBA32, false);
        var fill = new Color32[width * H];
        for (int i = 0; i < fill.Length; i++) fill[i] = bg;
        sheet.SetPixels32(fill);
        foreach (var (px, py, t, s) in plates)
        {
            var src = t.GetPixels32();
            for (int ty = 0; ty < t.height; ty++)
                for (int tx = 0; tx < t.width; tx++)
                {
                    var c = src[ty * t.width + tx];
                    if (c.a == 0) continue;
                    for (int dy = 0; dy < s; dy++)
                        for (int dx = 0; dx < s; dx++)
                        {
                            int X = px + tx * s + dx, Y = H - 1 - (py + (t.height - 1 - ty) * s + dy); // top-down layout
                            if (X >= 0 && X < width && Y >= 0 && Y < H) sheet.SetPixel(X, Y, c);
                        }
                }
        }
        sheet.Apply();
        Directory.CreateDirectory(Path.GetDirectoryName(outPath));
        File.WriteAllBytes(outPath, sheet.EncodeToPNG());
        Debug.Log("[SkpArtSheet] wrote " + outPath);
    }
}
