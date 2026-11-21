// M7 gap art (docs/04 gaps list), generated in the provided palette from the provided sprites where possible:
// Act II balete-root ledge and Act III church-stone ledge (recolored wood ledge with new detail), the violet thorn
// curtain (recolored thorns), the falling bell, the water surface, the shockwave, two decoration props, and the
// Bellkeeper's stunned and tolling frames (recolors of spr_boss). Also the SkillObstacleTuning asset.
// Files are rewritten only when their content changes. Called by SkpSkillSetup.CreateAssets (full setup).
using System;
using System.IO;
using Skp.Levels;
using UnityEditor;
using UnityEngine;

public static class SkpLevelArtSetup
{
    public const string Dir = "Assets/Art/Generated";
    public const string RootLedge = Dir + "/tile_root_ledge48x16.png";
    public const string StoneLedge = Dir + "/tile_stone_ledge48x16.png";
    public const string Curtain = Dir + "/hazard_curtain_16x130.png";
    public const string Bell = Dir + "/hazard_bell_14x20.png";
    public const string Water = Dir + "/fx_water_surface_8x3.png";
    public const string Shockwave = Dir + "/fx_shockwave_12x12.png";
    public const string PropRoots = Dir + "/prop_balete_roots_16x32.png";
    public const string PropCandle = Dir + "/prop_church_candle_8x16.png";
    public const string BossStun = Dir + "/spr_boss_stun.png";
    public const string BossToll = Dir + "/spr_boss_toll.png";
    public const string ObstacleTuningPath = "Assets/Data/SkillObstacleTuning.asset";

    public static void WriteArt()
    {
        SkpProjectSetup.EnsureFolder(Dir);
        var wood = Load("Assets/Art/Tiles/tile_wood_ledge48x16.png");
        // Act II: darker, greener wood with root strands hanging from the underside
        WriteFrom(RootLedge, wood, (x, y, c) =>
        {
            if (c.a == 0) return (y < 4 && (x * 7 + y * 3) % 11 == 0) ? new Color32(0x3a, 0x2a, 0x14, 0xff) : c;
            var o = Color32.Lerp(c, new Color32(0x2e, 0x3a, 0x22, 0xff), 0.35f);
            if ((x * 5 + y * 11) % 17 == 0) o = new Color32(0x4a, 0x5a, 0x2a, 0xff); // moss
            return o;
        });
        // Act III: grey church stone with mortar lines every 8 px
        WriteFrom(StoneLedge, wood, (x, y, c) =>
        {
            if (c.a == 0) return c;
            float l = (c.r * 0.3f + c.g * 0.5f + c.b * 0.2f) / 255f;
            byte v = (byte)Mathf.Clamp(60 + l * 150, 0, 255);
            var o = new Color32(v, v, (byte)Mathf.Min(255, v + 12), 0xff);
            if (x % 16 == 0 || y == 8) o = new Color32(0x3a, 0x34, 0x44, 0xff);
            return o;
        });
        // Veil gauntlet curtain: the thorns in the veil's violet (the purple glyph)
        var thorns = Load("Assets/Art/Props/hazard_thorns_16x130.png");
        WriteFrom(Curtain, thorns, (x, y, c) =>
        {
            if (c.a == 0) return c;
            float l = (c.r + c.g + c.b) / (3f * 255f);
            return new Color32((byte)(80 + l * 140), (byte)(40 + l * 80), (byte)(120 + l * 135), c.a);
        });
        var boss = Load("Assets/Art/Enemies/spr_boss.png");
        WriteFrom(BossStun, boss, (x, y, c) => c.a == 0 ? c : Color32.Lerp(c, new Color32(0x6a, 0xb0, 0xff, c.a), 0.35f));
        WriteFrom(BossToll, boss, (x, y, c) => c.a == 0 ? c : Color32.Lerp(c, new Color32(0xff, 0xe2, 0x8a, c.a), 0.3f));

        // the prototype's bell (drawBoss): body, crown, hanger, clapper
        Rows(Bell, new[]
        {
            "......hh......", "......hh......", "......hh......", "....gggggg....", "...gggggggg...", "...gggggggg...",
            "..ccccccccccc.", "..ccccccccccc.", ".cccccccccccc.", ".cccccccccccc.", ".cccccccccccc.", ".ccccccccccccc",
            "cccccccccccccc", "cccccccccccccc", "cccccccccccccc", "dddddddddddddd", "......kk......", "......kk......",
            "..............", "..............",
        }, ch => ch == 'c' ? new Color32(0xc9, 0x92, 0x3a, 0xff) : ch == 'g' ? new Color32(0xe8, 0xb8, 0x5a, 0xff)
               : ch == 'h' ? new Color32(0x3a, 0x24, 0x10, 0xff) : ch == 'd' ? new Color32(0x8a, 0x5a, 0x22, 0xff)
               : ch == 'k' ? new Color32(0x2a, 0x1c, 0x0c, 0xff) : new Color32(0, 0, 0, 0));
        Rows(Water, new[] { "hhhhhhhh", "sswwssww", "ssssssss" },
             ch => ch == 'h' ? new Color32(0x78, 0xc8, 0xdc, 0x60) : ch == 'w' ? new Color32(0x2c, 0x6a, 0x7a, 0xff) : new Color32(0x1c, 0x4a, 0x5a, 0xff));
        Rows(Shockwave, new[]
        {
            "gggggggggggg", "gggggggggggg", "gggggggggggg", "gggrrrrrrggg", "gggrrrrrrggg", "gggrrwwrrggg",
            "gggrrwwrrggg", "gggrrwwrrggg", "gggrrwwrrggg", "gggrrwwrrggg", "gggrrwwrrggg", "gggrrwwrrggg",
        }, ch => ch == 'g' ? new Color32(0xff, 0xd3, 0x6a, 0xff) : ch == 'r' ? new Color32(0xff, 0x5a, 0x4a, 0xff) : new Color32(0xff, 0xff, 0xff, 0xff));
        Rows(PropRoots, new[]
        {
            "rrrrrrrrrrrrrrrr", ".rr.rrr..rr.rrr.", ".rr..rr..r...rr.", "..r..rr..r...r..", "..r...r..r...r..", "..r...r..rr..r..",
            "..rr..r...r..r..", "...r..r...r..r..", "...r..rr..r..rr.", "...r...r..r...r.", "...r...r..r...r.", "..rr...r..r...r.",
            "..r....r..r...r.", "..r....r......r.", "..r....r......r.", "..r...rr......r.", "..r...r.......r.", "..d...r.......d.",
            "......r.........", "......r.........", "......d.........", "................", "................", "................",
            "................", "................", "................", "................", "................", "................",
            "................", "................",
        }, ch => ch == 'r' ? new Color32(0x4a, 0x2e, 0x1a, 0xff) : ch == 'd' ? new Color32(0x2e, 0x1c, 0x10, 0xff) : new Color32(0, 0, 0, 0));
        Rows(PropCandle, new[]
        {
            "...y....", "..yoy...", "..ooo...", "...o....", "..www...", "..www...", "..wwh...", "..wwh...",
            "..wwh...", "..wwh...", "..wwh...", "..wwh...", ".bbbbb..", ".bbbbb..", "bbbbbbb.", "........",
        }, ch => ch == 'y' ? new Color32(0xff, 0xe9, 0xa0, 0xff) : ch == 'o' ? new Color32(0xff, 0x9a, 0x3a, 0xff)
               : ch == 'w' ? new Color32(0xe8, 0xdc, 0xc0, 0xff) : ch == 'h' ? new Color32(0xb8, 0xac, 0x90, 0xff)
               : ch == 'b' ? new Color32(0x6a, 0x5a, 0x4a, 0xff) : new Color32(0, 0, 0, 0));
        AssetDatabase.Refresh();

        if (AssetDatabase.LoadAssetAtPath<SkillObstacleTuning>(ObstacleTuningPath) == null)
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<SkillObstacleTuning>(), ObstacleTuningPath);
        var curve = AssetDatabase.LoadAssetAtPath<DifficultyCurve>("Assets/Data/DifficultyCurve.asset");
        if (curve != null && curve.obstacles == null)
        {
            curve.obstacles = AssetDatabase.LoadAssetAtPath<SkillObstacleTuning>(ObstacleTuningPath);
            EditorUtility.SetDirty(curve);
        }
        AssetDatabase.SaveAssets();
    }

    /// <summary>Fills the M7 fields of the LevelArt asset (called after the asset exists).</summary>
    public static void Assign(LevelArt art)
    {
        Sprite S(string p) => AssetDatabase.LoadAssetAtPath<Sprite>(p);
        art.rootLedge = S(RootLedge); art.stoneLedge = S(StoneLedge);
        art.waterSurface = S(Water); art.bell = S(Bell); art.curtain = S(Curtain);
        art.propRoots = S(PropRoots); art.propCandle = S(PropCandle);
        art.boss = S("Assets/Art/Enemies/spr_boss.png"); art.bossStun = S(BossStun); art.bossToll = S(BossToll); art.shockwave = S(Shockwave);
        EditorUtility.SetDirty(art);
    }

    static Texture2D Load(string path)
    {
        var t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        t.LoadImage(File.ReadAllBytes(path));
        return t;
    }

    static void WriteFrom(string path, Texture2D src, Func<int, int, Color32, Color32> map)
    {
        int w = src.width, h = src.height;
        var px = src.GetPixels32();
        var outp = new Color32[px.Length];
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) outp[y * w + x] = map(x, h - 1 - y, px[y * w + x]); // y from the top
        Write(path, w, h, outp);
    }

    static void Rows(string path, string[] rows, Func<char, Color32> col)
    {
        int h = rows.Length, w = rows[0].Length;
        foreach (var r in rows) if (r.Length != w) throw new Exception($"{path}: row \"{r}\" is {r.Length} wide, expected {w}");
        var outp = new Color32[w * h];
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) outp[y * w + x] = col(rows[h - 1 - y][x]);
        Write(path, w, h, outp);
    }

    static void Write(string path, int w, int h, Color32[] px)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.SetPixels32(px);
        tex.Apply();
        var bytes = tex.EncodeToPNG();
        UnityEngine.Object.DestroyImmediate(tex);
        if (File.Exists(path) && File.ReadAllBytes(path).AsSpan().SequenceEqual(bytes)) return;
        File.WriteAllBytes(path, bytes);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
    }
}
