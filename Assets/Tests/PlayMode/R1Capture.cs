using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Skp.Dimensions;
using TMPro;
using UnityEngine;

namespace Skp.Tests
{
    /// <summary>
    /// R1: a name-free description of everything a view drew (sprite, world rectangle, sorting, colour, flips, visibility,
    /// dimension rule), one sorted line per renderer. Captured before the prefab change and compared after it, so R1
    /// can prove that nothing moved, resized, re-sorted or changed colour.
    /// </summary>
    public static class R1Capture
    {
        // The intentional 2026-11-23 art overhaul has its own baseline; pre-overhaul R1 records remain intact.
        public static string Dir => Path.GetFullPath(Path.Combine(Application.dataPath, "../tests/Screenshots/presentation/baseline"));

        static string F(float v) => (Mathf.Round(v * 1000f) / 1000f).ToString("0.###", CultureInfo.InvariantCulture);
        static string V(Vector2 v) => F(v.x) + "," + F(v.y);

        public static List<string> Lines(Transform root)
        {
            var lines = new List<string>();
            if (root == null) return lines;
            foreach (var sr in root.GetComponentsInChildren<SpriteRenderer>(true)) lines.Add(Line(sr));
            foreach (var lr in root.GetComponentsInChildren<LineRenderer>(true))
                lines.Add($"line|{lr.positionCount}|{lr.sortingOrder}|{F(lr.widthMultiplier)}|{lr.enabled && lr.gameObject.activeInHierarchy}");
            foreach (var t in root.GetComponentsInChildren<TextMeshPro>(true))
                lines.Add($"text|{t.text}|{V(t.transform.position)}|{t.sortingOrder}|{F(t.fontSize)}|{t.gameObject.activeInHierarchy}");
            lines.Sort(System.StringComparer.Ordinal);
            return lines;
        }

        static string Line(SpriteRenderer sr)
        {
            var s = sr.sprite;
            var tr = sr.transform;
            Vector2 scale = tr.lossyScale;
            Vector2 size, center;
            if (s == null) { size = Vector2.zero; center = tr.position; }
            else
            {
                size = sr.drawMode == SpriteDrawMode.Simple ? (Vector2)s.bounds.size : sr.size;
                size = Vector2.Scale(size, scale);
                // the sprite pivot: tiled sprites are centred, simple ones keep their pivot offset
                Vector2 off = sr.drawMode == SpriteDrawMode.Simple ? Vector2.Scale(s.bounds.center, scale) : Vector2.zero;
                if (sr.flipX) off.x = -off.x;
                if (sr.flipY) off.y = -off.y;
                center = (Vector2)tr.position + off;
            }
            string sprite = s == null ? "-" : IsWhite(s) ? "WHITE" : s.name;
            var dm = sr.GetComponentInParent<DimensionMember>(true);
            string dim = dm == null ? "-" : $"{(int)dm.mask}/{dm.ghost}/{(int)dm.ghostTint}";
            Color c = sr.color;
            return $"sr|{sprite}|{V(center)}|{V(size)}|{sr.sortingLayerName}:{sr.sortingOrder}|{F(c.r)},{F(c.g)},{F(c.b)},{F(c.a)}|{(sr.flipX ? 1 : 0)}{(sr.flipY ? 1 : 0)}|{(sr.enabled && sr.gameObject.activeInHierarchy ? 1 : 0)}|{sr.drawMode}|{dim}";
        }

        /// <summary>A 1x1 white pixel, whether made at runtime or imported (R1 moves to the imported ui_white).</summary>
        static bool IsWhite(Sprite s) => s.rect.width == 1 && s.rect.height == 1;

        /// <summary>
        /// The one intended visual change in R1. Before R1, DimensionMember saved each sprite's colour in Awake, which ran when the
        /// view added the component, before the view set the colour; every dimension refresh then reset these pieces to white in
        /// play mode (edit-mode screenshots were right). The prefabs carry their colour, so play mode now shows the intended one.
        /// </summary>
        public static string FixKnownColourBug(string line)
        {
            var p = line.Split('|');
            if (p.Length < 10 || p[0] != "sr") return line;
            bool white = p[1] == "WHITE";
            if (white && p[4] == "Default:2" && p[3] == "0.125,2.125" && p[5] == "1,1,1,1") p[5] = "0.227,0.141,0.063,1"; // lantern post #3a2410
            else if (white && p[4] == "Default:20" && p[5] == "1,1,1,1") p[5] = "0.024,0.047,0.078,0.82";                // flood water
            else if (white && p[4] == "Default:2" && p[9].Contains("/Hidden/") && p[5] == "1,1,1,1") p[5] = "1,0.353,0.29,0.627"; // bell warning line
            else if (p[4] == "Default:-2" && p[5] == "1,1,1,1") p[5] = "1,1,1,0.85";                                      // act decoration
            return string.Join("|", p);
        }

        /// <summary>A line for something actually drawn (hidden pooled pieces made up front do not count).</summary>
        public static bool Visible(string line)
        {
            var p = line.Split('|');
            if (p[0] == "sr") return p.Length > 7 && p[7] == "1";
            return p[p.Length - 1] == "True";
        }

        /// <summary>Writes the baseline when it is missing; otherwise returns the differences in what is drawn (empty when identical).</summary>
        public static List<string> CompareOrWrite(string name, List<string> now, out bool wrote)
        {
            Directory.CreateDirectory(Dir);
            var path = Path.Combine(Dir, name + ".txt");
            wrote = false;
            if (!File.Exists(path))
            {
                File.WriteAllLines(path, now, new UTF8Encoding(false));
                wrote = true;
                return new List<string>();
            }
            var before = File.ReadAllLines(path).Where(Visible).Select(FixKnownColourBug).ToList();
            now = now.Where(Visible).ToList();
            var diff = new List<string>();
            var a = Count(before);
            var b = Count(now);
            foreach (var kv in a) { b.TryGetValue(kv.Key, out int n); if (n < kv.Value) diff.Add($"missing x{kv.Value - n}: {kv.Key}"); }
            foreach (var kv in b) { a.TryGetValue(kv.Key, out int n); if (n < kv.Value) diff.Add($"extra x{kv.Value - n}: {kv.Key}"); }
            return diff;
        }

        static Dictionary<string, int> Count(List<string> lines)
        {
            var d = new Dictionary<string, int>();
            foreach (var l in lines) { d.TryGetValue(l, out int n); d[l] = n + 1; }
            return d;
        }
    }
}
