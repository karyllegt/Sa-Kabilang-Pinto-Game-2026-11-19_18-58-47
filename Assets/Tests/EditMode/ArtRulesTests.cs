using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Skp.Tests
{
    /// <summary>
    /// R2: the art bible as tests. Every game-ready PNG in Assets/Art/Game follows docs/06_ART_BIBLE.md (import settings,
    /// palette only, no partial alpha, colour limit, size as listed in docs/ART_MANIFEST.md); the text-grid builder and the
    /// SpriteCook cleanup do what they promise.
    /// </summary>
    public class ArtRulesTests
    {
        const string GameDir = "Assets/Art/Game";

        static Dictionary<string, string> ManifestSizes()
        {
            var d = new Dictionary<string, string>();
            foreach (var line in File.ReadAllLines("docs/ART_MANIFEST.md"))
            {
                var m = Regex.Match(line, @"^\| (Assets/Art/Game/[^ |]+\.png) \| (\d+x\d+) \|");
                if (m.Success) d[m.Groups[1].Value] = m.Groups[2].Value;
            }
            return d;
        }

        static IEnumerable<string> GamePngs() => Directory.Exists(GameDir)
            ? Directory.GetFiles(GameDir, "*.png", SearchOption.AllDirectories).Select(p => p.Replace('\\', '/'))
            : Enumerable.Empty<string>();

        [Test]
        public void Palette_HasTheBibleColours_WithUniqueSymbols()
        {
            var pal = SkpPixelText.LoadPalette();
            Assert.AreEqual(44, pal.Count, "43 colours and clear");
            Assert.AreEqual(0, pal['.'].a);
            Assert.AreEqual(new Color32(0x0a, 0x07, 0x10, 255), pal['k'], "ink");
            Assert.AreEqual(new Color32(0xe8, 0xb8, 0x5a, 255), pal['g'], "gold = safe");
            Assert.AreEqual(new Color32(0x41, 0xe0, 0xd0, 255), pal['t'], "teal = Spirit");
            Assert.AreEqual(new Color32(0xc0, 0x7d, 0xff, 255), pal['u'], "violet = Inverted");
            Assert.AreEqual(new Color32(0xff, 0x5a, 0x4a, 255), pal['d'], "red = danger");
        }

        [Test]
        public void TextGrid_BuildsExactPixels_AndRejectsBadGrids()
        {
            var pal = SkpPixelText.LoadPalette();
            var errors = new List<string>();
            var g = SkpPixelText.Parse("name: t\nsize: 3x2\nout: Fx\nframe\nkg.\n.tk\nframe\n...\nddd\n", pal, errors);
            Assert.IsEmpty(errors);
            Assert.AreEqual(2, g.Frames.Count);
            var t = SkpPixelText.ToTexture(g, 0);
            Assert.AreEqual((Color)pal['k'], t.GetPixel(0, 1), "top-left is ink (rows are written top first)");
            Assert.AreEqual((Color)pal['g'], t.GetPixel(1, 1));
            Assert.AreEqual(0f, t.GetPixel(2, 1).a, "clear");
            Assert.AreEqual((Color)pal['t'], t.GetPixel(1, 0));
            Object.DestroyImmediate(t);
            CollectionAssert.AreEqual(new[] { "Assets/Art/Game/Fx/t_0.png", "Assets/Art/Game/Fx/t_1.png" }, SkpPixelText.OutputPaths(g));

            errors.Clear();
            Assert.IsNull(SkpPixelText.Parse("name: t\nsize: 3x2\nout: Fx\nframe\nkg\n.tk\n", pal, errors), "short row");
            StringAssert.Contains("2 columns", string.Join("\n", errors));
            errors.Clear();
            Assert.IsNull(SkpPixelText.Parse("name: t\nsize: 3x2\nout: Fx\nframe\nkgQ\n.tk\n", pal, errors), "unknown symbol");
            StringAssert.Contains("'Q' is not in the palette", string.Join("\n", errors));
            errors.Add("an earlier file's problem");
            Assert.IsNotNull(SkpPixelText.Parse("name: t\nsize: 1x1\nout: Fx\nframe\nk\n", pal, errors), "earlier problems do not block a good file");
        }

        [Test]
        public void Cleanup_Trims_SnapsToThePalette_ClearsStrays_AndFitsTheCanvas()
        {
            var palette = SkpArtCleanup.PaletteColours();
            // a 40 x 40 image: a 20 x 30 block of near-gold at 2x scale, soft edges, an off-palette tint and a stray dot
            var src = new Texture2D(40, 40, TextureFormat.RGBA32, false);
            var px = new Color32[40 * 40];
            for (int y = 4; y < 34; y++) for (int x = 10; x < 30; x++) px[y * 40 + x] = new Color32(0xea, 0xb5, 0x5c, 255);
            for (int y = 4; y < 34; y++) px[y * 40 + 10] = new Color32(0xea, 0xb5, 0x5c, 90); // soft left edge
            px[38 * 40 + 2] = new Color32(0x40, 0xe1, 0xd1, 255); // lone stray pixel, far away
            src.SetPixels32(px); src.Apply();

            var outTex = SkpArtCleanup.Clean(src, 16, 18, "Characters", palette);
            Assert.AreEqual(16, outTex.width); Assert.AreEqual(18, outTex.height);
            var o = outTex.GetPixels32();
            var set = new HashSet<Color32>(palette);
            int opaque = 0;
            foreach (var c in o)
            {
                Assert.IsTrue(c.a == 0 || c.a == 255, "no partial alpha");
                if (c.a == 0) continue;
                opaque++;
                Assert.IsTrue(set.Contains(c), "palette only: " + c);
            }
            Assert.Greater(opaque, 40, "the block survives");
            Assert.IsTrue(o.Where(c => c.a > 0).All(c => c.Equals(new Color32(0xe8, 0xb8, 0x5a, 255))), "near-gold snaps to gold; the teal stray is gone");
            Assert.IsTrue(Enumerable.Range(0, 16).Any(x => o[x].a == 255), "Characters stand on the bottom row");
            Object.DestroyImmediate(src); Object.DestroyImmediate(outTex);
        }

        [Test]
        public void EveryGamePng_FollowsTheArtBible()
        {
            var pal = new HashSet<Color32>(SkpArtCleanup.PaletteColours());
            var pal48 = new HashSet<Color32>(SkpTala48Setup.PaletteColours()); // A-7: the hand-pixelled Tala has its own palette
            var sizes = ManifestSizes();
            var problems = new List<string>();
            foreach (var path in GamePngs())
            {
                if (!sizes.TryGetValue(path, out var size)) problems.Add(path + ": not in docs/ART_MANIFEST.md");
                var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti == null) { problems.Add(path + ": not imported"); continue; }
                if (ti.filterMode != FilterMode.Point) problems.Add(path + ": filter");
                if (ti.textureCompression != TextureImporterCompression.Uncompressed) problems.Add(path + ": compressed");
                if (ti.mipmapEnabled) problems.Add(path + ": mipmaps");
                bool hi = SkpAssetImportSettings.IsHiDetail(path);
                if (!Mathf.Approximately(ti.spritePixelsPerUnit, hi ? 48 : 16)) problems.Add(path + ": PPU");
                var t = new Texture2D(2, 2);
                t.LoadImage(File.ReadAllBytes(path));
                if (size != null && size != $"{t.width}x{t.height}") problems.Add($"{path}: {t.width}x{t.height}, manifest says {size}");
                var colours = new HashSet<Color32>();
                foreach (var c in t.GetPixels32())
                {
                    if (c.a != 0 && c.a != 255) { problems.Add(path + ": partial alpha"); break; }
                    if (c.a == 0) continue;
                    if (!(hi ? pal48 : pal).Contains(c)) { problems.Add(path + ": off-palette " + c); break; }
                    colours.Add(c);
                }
                bool big = path.Contains("/Backdrops/") || path.Contains("/Tiles/") || path.Contains("portrait_") || path.Contains("bellkeeper");
                int limit = hi ? 40 : big ? 43 : 12;
                if (colours.Count > limit) problems.Add($"{path}: {colours.Count} colours, limit {limit}");
                Object.DestroyImmediate(t);
            }
            foreach (var listed in sizes.Keys) if (!File.Exists(listed)) problems.Add(listed + ": listed in the manifest but missing");
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test]
        public void CharacterArt_GetsTheBottomCentrePivot()
        {
            foreach (var path in GamePngs().Where(p => SkpAssetImportSettings.IsGameCharacterFolder(p)))
            {
                var ti = (TextureImporter)AssetImporter.GetAtPath(path);
                var s = new TextureImporterSettings();
                ti.ReadTextureSettings(s);
                Assert.AreEqual((int)SpriteAlignment.BottomCenter, s.spriteAlignment, path);
            }
        }
    }
}
