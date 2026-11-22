// R2: makes SpriteCook downloads game-ready. A raw file reference/spritecook_raw/<Group>/<name>@<W>x<H>.png becomes
// Assets/Art/Game/<Group>/<name>.png at exactly W x H: trimmed to its content, scaled down by majority vote per block
// (pixel art keeps hard edges; averaging would blur it), every pixel snapped to the art-bible palette, alpha made 0 or
// 255, lone stray pixels removed, and placed on the canvas (bottom centre for Characters, Creatures and Npc, centred
// otherwise; Backdrops fill the canvas). Menu: Sa Kabilang Pinto > Art > Clean SpriteCook Downloads.
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class SkpArtCleanup
{
    public const string RawDir = "reference/spritecook_raw";
    static readonly Regex RawName = new Regex(@"^(?<name>.+)@(?<w>\d+)x(?<h>\d+)\.png$", RegexOptions.IgnoreCase);

    public static bool BottomPivot(string group) => group == "Characters" || group == "Creatures" || group == "Npc";

    [MenuItem("Sa Kabilang Pinto/Art/Clean SpriteCook Downloads")]
    public static void CleanAllMenu() => Debug.Log($"[SkpArtCleanup] {CleanAll().Count} files cleaned");

    public static List<string> CleanAll()
    {
        var written = new List<string>();
        if (!Directory.Exists(RawDir)) return written;
        var palette = PaletteColours();
        foreach (var file in Directory.GetFiles(RawDir, "*.png", SearchOption.AllDirectories))
        {
            var m = RawName.Match(Path.GetFileName(file));
            if (!m.Success) { Debug.LogWarning("[SkpArtCleanup] name it <name>@<W>x<H>.png: " + file); continue; }
            string group = Path.GetFileName(Path.GetDirectoryName(file));
            if (group.StartsWith("_")) continue; // _unpicked and other set-aside downloads are records, not game art
            int w = int.Parse(m.Groups["w"].Value), h = int.Parse(m.Groups["h"].Value);
            var src = new Texture2D(2, 2);
            src.LoadImage(File.ReadAllBytes(file));
            var outTex = Clean(src, w, h, group, palette);
            var path = $"{SkpPixelText.GameDir}/{group}/{m.Groups["name"].Value}.png";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, outTex.EncodeToPNG());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            Object.DestroyImmediate(src); Object.DestroyImmediate(outTex);
            written.Add(path);
        }
        return written;
    }

    /// <summary>The palette's opaque colours.</summary>
    public static List<Color32> PaletteColours()
    {
        var list = new List<Color32>();
        foreach (var kv in SkpPixelText.LoadPalette()) if (kv.Key != '.') list.Add(kv.Value);
        return list;
    }

    /// <summary>The cleanup itself (tests call it on a fixture).</summary>
    public static Texture2D Clean(Texture2D src, int w, int h, string group, List<Color32> palette)
    {
        var px = src.GetPixels32();
        int sw = src.width, sh = src.height;
        bool backdrop = group == "Backdrops";

        // 1. content bounds (alpha at least half); backdrops keep the whole image
        int x0 = 0, y0 = 0, x1 = sw - 1, y1 = sh - 1;
        if (!backdrop)
        {
            x0 = sw; y0 = sh; x1 = -1; y1 = -1;
            for (int y = 0; y < sh; y++) for (int x = 0; x < sw; x++)
                if (px[y * sw + x].a >= 128) { if (x < x0) x0 = x; if (x > x1) x1 = x; if (y < y0) y0 = y; if (y > y1) y1 = y; }
            if (x1 < 0) { x0 = y0 = 0; x1 = sw - 1; y1 = sh - 1; }
        }
        int cw = x1 - x0 + 1, ch = y1 - y0 + 1;

        // 2. scale: fit inside the canvas (sprites) or cover it (backdrops); never scale up past 1:1 for sprites
        float scale = backdrop ? Mathf.Max(w / (float)cw, h / (float)ch) : Mathf.Min(1f, Mathf.Min(w / (float)cw, h / (float)ch));
        if (backdrop && scale > 1f) scale = Mathf.Ceil(scale - 0.001f); // whole-number upscale keeps every pixel the same size; the centre is cropped
        int tw = Mathf.Max(1, Mathf.RoundToInt(cw * scale)), th = Mathf.Max(1, Mathf.RoundToInt(ch * scale));
        if (backdrop) { tw = Mathf.Max(tw, w); th = Mathf.Max(th, h); }
        tw = Mathf.Min(tw, backdrop ? tw : w); th = Mathf.Min(th, backdrop ? th : h);

        var small = new Color32[tw * th];
        var votes = new Dictionary<int, int>();
        for (int ty = 0; ty < th; ty++)
            for (int tx = 0; tx < tw; tx++)
            {
                int sx0 = x0 + tx * cw / tw, sx1 = Mathf.Max(sx0 + 1, x0 + (tx + 1) * cw / tw);
                int sy0 = y0 + ty * ch / th, sy1 = Mathf.Max(sy0 + 1, y0 + (ty + 1) * ch / th);
                votes.Clear();
                int opaque = 0, total = 0;
                for (int y = sy0; y < sy1; y++)
                    for (int x = sx0; x < sx1; x++)
                    {
                        var c = px[y * sw + x];
                        total++;
                        if (c.a < 128) continue;
                        opaque++;
                        int k = Nearest(c, palette);
                        votes.TryGetValue(k, out int n);
                        votes[k] = n + 1;
                    }
                if (opaque * 2 < total && !backdrop) { small[ty * tw + tx] = new Color32(0, 0, 0, 0); continue; }
                int best = -1, bestN = -1;
                foreach (var kv in votes) if (kv.Value > bestN || (kv.Value == bestN && kv.Key < best)) { best = kv.Key; bestN = kv.Value; }
                small[ty * tw + tx] = best >= 0 ? palette[best] : palette[0];
            }

        // 3. lone pixels (no opaque neighbour up, down, left or right) are noise
        if (!backdrop)
        {
            var copy = (Color32[])small.Clone();
            for (int y = 0; y < th; y++)
                for (int x = 0; x < tw; x++)
                {
                    if (copy[y * tw + x].a == 0) continue;
                    bool n = (x > 0 && copy[y * tw + x - 1].a > 0) || (x < tw - 1 && copy[y * tw + x + 1].a > 0)
                          || (y > 0 && copy[(y - 1) * tw + x].a > 0) || (y < th - 1 && copy[(y + 1) * tw + x].a > 0);
                    if (!n) small[y * tw + x] = new Color32(0, 0, 0, 0);
                }
        }

        // 4. place on the exact canvas: bottom centre or centre; backdrops crop the centre
        var outPx = new Color32[w * h];
        int ox = (w - tw) / 2, oy = BottomPivot(group) ? 0 : (h - th) / 2;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int sx = x - ox, sy = y - oy;
                outPx[y * w + x] = sx >= 0 && sy >= 0 && sx < tw && sy < th ? small[sy * tw + sx] : new Color32(0, 0, 0, 0);
            }
        var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        t.SetPixels32(outPx);
        t.Apply();
        return t;
    }

    /// <summary>Index of the nearest palette colour (weighted RGB distance, closer to how the eye sees it).</summary>
    public static int Nearest(Color32 c, List<Color32> palette)
    {
        int best = 0; long bestD = long.MaxValue;
        for (int i = 0; i < palette.Count; i++)
        {
            var p = palette[i];
            int dr = c.r - p.r, dg = c.g - p.g, db = c.b - p.b;
            long d = 3L * dr * dr + 4L * dg * dg + 2L * db * db;
            if (d < bestD) { bestD = d; best = i; }
        }
        return best;
    }
}
