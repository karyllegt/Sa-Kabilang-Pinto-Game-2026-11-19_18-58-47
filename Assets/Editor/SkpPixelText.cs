// R2: text-grid pixel art. Every Assets/Art/Source/**/*.pxl.txt (format in Assets/Art/Source/README.md) becomes PNGs in
// Assets/Art/Game/<out>/, one per frame, using only the art-bible palette (Assets/Art/Source/palette.txt).
// Menu: Sa Kabilang Pinto > Art > Build Text-Grid Art. Batch: -executeMethod SkpPixelText.BuildAllBatch
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class SkpPixelText
{
    public const string SourceDir = "Assets/Art/Source";
    public const string GameDir = "Assets/Art/Game";
    public const string PalettePath = SourceDir + "/palette.txt";

    public class Grid
    {
        public string Name, Out;
        public int W, H;
        public List<Color32[]> Frames = new List<Color32[]>(); // rows bottom-up, as Texture2D wants
    }

    /// <summary>Symbol to colour, from palette.txt ("." is clear).</summary>
    public static Dictionary<char, Color32> LoadPalette(string path = PalettePath)
    {
        var map = new Dictionary<char, Color32> { ['.'] = new Color32(0, 0, 0, 0) };
        foreach (var raw in File.ReadAllLines(path))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line[0] == '#') continue;
            var parts = line.Split(' ');
            if (parts.Length < 2 || parts[0].Length != 1 || !parts[1].StartsWith("#") || parts[1].Length != 7) continue;
            uint v = uint.Parse(parts[1].Substring(1), NumberStyles.HexNumber);
            map[parts[0][0]] = new Color32((byte)(v >> 16), (byte)(v >> 8), (byte)v, 255);
        }
        return map;
    }

    /// <summary>Parses one grid; problems go to <paramref name="errors"/> and the result is null.</summary>
    public static Grid Parse(string text, Dictionary<char, Color32> palette, List<string> errors, string source = "grid")
    {
        int start = errors.Count; // the list is shared across files: only this file's problems count
        var g = new Grid();
        List<string> rows = null;
        var frames = new List<List<string>>();
        int lineNo = 0;
        foreach (var raw in text.Replace("\r", "").Split('\n'))
        {
            lineNo++;
            var line = raw.TrimEnd();
            if (line.StartsWith("#")) continue;
            if (rows == null)
            {
                if (line.Length == 0) continue;
                if (line == "frame") { rows = new List<string>(); frames.Add(rows); continue; }
                int c = line.IndexOf(':');
                if (c < 0) { errors.Add($"{source}:{lineNo}: expected 'key: value' or 'frame'"); continue; }
                string key = line.Substring(0, c).Trim(), val = line.Substring(c + 1).Trim();
                if (key == "name") g.Name = val;
                else if (key == "out") g.Out = val;
                else if (key == "size")
                {
                    var wh = val.ToLowerInvariant().Split('x');
                    if (wh.Length != 2 || !int.TryParse(wh[0], out g.W) || !int.TryParse(wh[1], out g.H) || g.W <= 0 || g.H <= 0)
                        errors.Add($"{source}:{lineNo}: bad size '{val}'");
                }
                else errors.Add($"{source}:{lineNo}: unknown key '{key}'");
                continue;
            }
            if (line == "frame") { rows = new List<string>(); frames.Add(rows); continue; }
            if (line.Length == 0) continue;
            rows.Add(line);
        }
        if (string.IsNullOrEmpty(g.Name)) errors.Add($"{source}: missing name");
        if (string.IsNullOrEmpty(g.Out)) errors.Add($"{source}: missing out");
        if (g.W == 0) errors.Add($"{source}: missing size");
        if (frames.Count == 0) errors.Add($"{source}: no frame");
        for (int f = 0; f < frames.Count && g.W > 0; f++)
        {
            var fr = frames[f];
            if (fr.Count != g.H) { errors.Add($"{source}: frame {f} has {fr.Count} rows, size says {g.H}"); continue; }
            var px = new Color32[g.W * g.H];
            for (int y = 0; y < g.H; y++)
            {
                var row = fr[y];
                if (row.Length != g.W) { errors.Add($"{source}: frame {f} row {y + 1} has {row.Length} columns, size says {g.W}"); continue; }
                for (int x = 0; x < g.W; x++)
                {
                    if (!palette.TryGetValue(row[x], out var col)) { errors.Add($"{source}: frame {f} row {y + 1} col {x + 1}: '{row[x]}' is not in the palette"); continue; }
                    px[(g.H - 1 - y) * g.W + x] = col;
                }
            }
            g.Frames.Add(px);
        }
        return errors.Count == start ? g : null;
    }

    /// <summary>The PNG paths a grid writes.</summary>
    public static List<string> OutputPaths(Grid g)
    {
        var list = new List<string>();
        for (int f = 0; f < g.Frames.Count; f++)
            list.Add($"{GameDir}/{g.Out}/{g.Name}{(g.Frames.Count > 1 ? "_" + f : "")}.png");
        return list;
    }

    public static Texture2D ToTexture(Grid g, int frame)
    {
        var t = new Texture2D(g.W, g.H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        t.SetPixels32(g.Frames[frame]);
        t.Apply();
        return t;
    }

    [MenuItem("Sa Kabilang Pinto/Art/Build Text-Grid Art")]
    public static void BuildAllMenu()
    {
        var errors = new List<string>();
        int n = BuildAll(errors);
        foreach (var e in errors) Debug.LogError("[SkpPixelText] " + e);
        Debug.Log($"[SkpPixelText] {n} PNGs written, {errors.Count} problems");
    }

    public static void BuildAllBatch()
    {
        var errors = new List<string>();
        int n = BuildAll(errors);
        foreach (var e in errors) Debug.LogError("[SkpPixelText] " + e);
        Debug.Log($"[SkpPixelText] {n} PNGs written, {errors.Count} problems");
        EditorApplication.Exit(errors.Count == 0 ? 0 : 5);
    }

    /// <summary>Builds every grid; returns how many PNGs were written.</summary>
    public static int BuildAll(List<string> errors)
    {
        var palette = LoadPalette();
        int written = 0;
        foreach (var file in Directory.GetFiles(SourceDir, "*.pxl.txt", SearchOption.AllDirectories))
        {
            var g = Parse(File.ReadAllText(file), palette, errors, file.Replace('\\', '/'));
            if (g == null) continue;
            var paths = OutputPaths(g);
            for (int f = 0; f < paths.Count; f++)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(paths[f]));
                var t = ToTexture(g, f);
                var bytes = t.EncodeToPNG();
                Object.DestroyImmediate(t);
                if (File.Exists(paths[f]) && System.Linq.Enumerable.SequenceEqual(File.ReadAllBytes(paths[f]), bytes)) continue;
                File.WriteAllBytes(paths[f], bytes);
                AssetDatabase.ImportAsset(paths[f], ImportAssetOptions.ForceUpdate);
                written++;
            }
        }
        AssetDatabase.Refresh();
        return written;
    }
}
