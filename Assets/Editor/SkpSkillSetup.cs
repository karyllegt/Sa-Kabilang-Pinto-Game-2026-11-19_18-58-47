// M6: skill data and art. SkillTuning asset, the SkillDefinitions' chip labels, keys, cues and icons, the
// SkillLibrary (Resources), generated pixel art for the gaps (skill icons, bolt, veil ring, glide wings),
// synthesized skill sounds (original, generated here) and the Sfx bank. Safe to run again: edited text in the
// definitions is kept; generated files are only rewritten when their content changes.
// Menu: Sa Kabilang Pinto > Setup > Run M6 Skills Setup (runs the full setup). Batch: -executeMethod SkpSkillSetup.RunAll
using System;
using System.Collections.Generic;
using System.IO;
using Skp.Audio;
using Skp.Skills;
using UnityEditor;
using UnityEngine;

public static class SkpSkillSetup
{
    public const string TuningPath = "Assets/Data/SkillTuning.asset";
    public const string LibraryPath = "Assets/Resources/Skills/SkillLibrary.asset";
    public const string BankPath = "Assets/Resources/Audio/SfxBank.asset";
    public const string IconDir = "Assets/Art/UI/Skills";
    public const string FxDir = "Assets/Art/Fx";
    public const string SkillSfxDir = "Assets/Audio/Sfx/Skills";
    public const string BoltSprite = FxDir + "/fx_bolt_8x4.png";
    public const string VeilRingSprite = FxDir + "/fx_veil_ring_30.png";
    public const string GlideWingsSprite = FxDir + "/fx_glide_wings_20x6.png";

    public static string IconPath(string id) => $"{IconDir}/icon_skill_{id}.png";

    [MenuItem("Sa Kabilang Pinto/Setup/Run M6 Skills Setup")]
    public static void RunAll() => SkpProjectSetup.RunAll(); // rebuilds everything, this step included

    /// <summary>Called by SkpProjectSetup.RunAll before the prefabs and scenes are built.</summary>
    public static void CreateAssets()
    {
        SkpLevelSetup.CreateDataAssets(); // the SkillDefinitions exist first
        SkpProjectSetup.EnsureFolder(IconDir);
        SkpProjectSetup.EnsureFolder(FxDir);
        SkpProjectSetup.EnsureFolder(SkillSfxDir);
        SkpProjectSetup.EnsureFolder("Assets/Resources/Skills");
        SkpProjectSetup.EnsureFolder("Assets/Resources/Audio");

        var tuning = AssetDatabase.LoadAssetAtPath<SkillTuning>(TuningPath);
        if (tuning == null) { tuning = ScriptableObject.CreateInstance<SkillTuning>(); AssetDatabase.CreateAsset(tuning, TuningPath); }

        WriteIcons();
        WriteFx();
        WriteSounds();
        AssetDatabase.Refresh();

        var defs = new List<SkillDefinition>();
        foreach (var e in SkillCatalog.Defaults)
        {
            var d = AssetDatabase.LoadAssetAtPath<SkillDefinition>(SkpLevelSetup.SkillPath(e.id));
            if (d == null) { Debug.LogError("[SkpSkillSetup] missing skill " + e.id); continue; }
            if (string.IsNullOrEmpty(d.shortLabel)) d.shortLabel = e.shortLabel;
            if (string.IsNullOrEmpty(d.keyHint)) d.keyHint = e.keyHint;
            if (string.IsNullOrEmpty(d.cue)) d.cue = e.cue;
            d.level = SkillIds.LevelOf(e.id);
            if (d.icon == null) d.icon = AssetDatabase.LoadAssetAtPath<Sprite>(IconPath(e.id));
            EditorUtility.SetDirty(d);
            defs.Add(d);
        }

        var lib = AssetDatabase.LoadAssetAtPath<SkillLibrary>(LibraryPath);
        if (lib == null) { lib = ScriptableObject.CreateInstance<SkillLibrary>(); AssetDatabase.CreateAsset(lib, LibraryPath); }
        lib.skills = defs.ToArray();
        lib.tuning = tuning;
        EditorUtility.SetDirty(lib);

        var bank = AssetDatabase.LoadAssetAtPath<SfxBank>(BankPath);
        if (bank == null) { bank = ScriptableObject.CreateInstance<SfxBank>(); AssetDatabase.CreateAsset(bank, BankPath); }
        var entries = new List<SfxBank.Entry>();
        foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio/Sfx" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            string n = Path.GetFileNameWithoutExtension(path);
            if (!n.StartsWith("sfx_")) continue;
            entries.Add(new SfxBank.Entry { cue = n.Substring(4), clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path), volume = 0.8f });
        }
        entries.Sort((a, b) => string.CompareOrdinal(a.cue, b.cue));
        bank.entries = entries.ToArray();
        EditorUtility.SetDirty(bank);
        AssetDatabase.SaveAssets();
        SkillLibrary.ClearCache();
    }

    // ------------------------------------------------------------------ art

    static Color32 Lighten(Color32 c, float t) => Color32.Lerp(c, new Color32(255, 255, 255, 255), t);
    static Color32 Darken(Color32 c, float t) => Color32.Lerp(c, new Color32(0x14, 0x0c, 0x1c, 255), t);

    /// <summary>Rows top to bottom: c skill color, h highlight, d shade, w white-gold, . clear.</summary>
    static readonly Dictionary<string, string[]> Icons = new Dictionary<string, string[]>
    {
        ["dj"] = new[]
        {
            "................", ".......hh.......", "......hcch......", ".....hccccd.....", "....hccddccd....", "...hccd..dccd...",
            "...cdd....ddc...", "................", ".......hh.......", "......hcch......", ".....hccccd.....", "....hccddccd....",
            "...hccd..dccd...", "...cdd....ddc...", "................", "................",
        },
        ["veil"] = new[]
        {
            "................", ".....hhhhhh.....", "...hhc....cdd...", "..hc........dd..", "..h..........d..", ".hc..........cd.",
            ".h............d.", ".h.....ww.....d.", ".h.....ww.....d.", ".h............d.", ".hc..........cd.", "..h..........d..",
            "..hc........dd..", "...hdc....cdd...", ".....dddddd.....", "................",
        },
        ["bolt"] = new[]
        {
            "................", "................", "................", "................", "...........hh...", "..........hwwh..",
            "..d.ccc..hwwwwh.", ".dcccccchwwwwwwh", ".dcccccchwwwwwwh", "..d.ccc..hwwwwh.", "..........hwwh..", "...........hh...",
            "................", "................", "................", "................",
        },
        ["swift"] = new[]
        {
            "................", "................", ".........h......", ".........ch.....", "..dd.....cch....", "..........cch...",
            ".dddd.cccccccch.", ".dddd.ccccccccch", ".dddd.cccccccch.", "..........cch...", "..dd.....cch....", ".........ch.....",
            ".........h......", "................", "................", "................",
        },
        ["heart"] = new[]
        {
            "................", "................", "...hhh....hhh...", "..hcccc..ccccd..", ".hcwwcccccccccd.", ".hcwccccccccccd.",
            ".hccccccccccccd.", "..hccccccccccd..", "...hccccccccd...", "....hccccccd....", ".....hccccd.....", "......hccd......",
            ".......dd.......", "................", "................", "................",
        },
        ["spread"] = new[]
        {
            "................", "................", "..........hh....", "..dccccchwwh....", "..........hh....", "................",
            "............hh..", ".dcccccccchwwh..", "............hh..", "................", "..........hh....", "..dccccchwwh....",
            "..........hh....", "................", "................", "................",
        },
        ["veil2"] = new[]
        {
            "....hhhhhhhh....", "..hh........dd..", ".h...hhhhhh...d.", ".h..h......d..d.", "h..h........d..d", "h..h........d..d",
            "h..h...ww...d..d", "h..h...ww...d..d", "h..h........d..d", "h..h........d..d", ".h..h......d..d.", ".h...dddddd...d.",
            "..hh........dd..", "....dddddddd....", "................", "................",
        },
        ["glide"] = new[]
        {
            "................", "................", "................", "hh............hh", "hcch........dccd", ".hccch....dcccd.",
            "..hcccchdccccd..", "...hcccwwcccd...", "....hccwwccd....", ".....hccccd.....", "......dccd......", ".......dd.......",
            "................", "................", "................", "................",
        },
    };

    static void WriteIcons()
    {
        foreach (var e in SkillCatalog.Defaults)
        {
            if (!Icons.TryGetValue(e.id, out var rows)) throw new Exception("no icon for " + e.id);
            WriteRows(IconPath(e.id), rows, e.color, 255);
        }
    }

    static void WriteRows(string path, string[] rows, Color32 c, byte alpha)
    {
        int h = rows.Length, w = rows[0].Length;
        foreach (var r in rows) if (r.Length != w) throw new Exception($"{path}: row \"{r}\" is {r.Length} wide, expected {w}");
        var hl = Lighten(c, 0.45f);
        var dk = Darken(c, 0.45f);
        var wh = new Color32(0xff, 0xf8, 0xe0, 0xff);
        WritePng(path, w, h, (x, y) =>
        {
            char ch = rows[h - 1 - y][x];
            Color32 o = ch == 'c' ? c : ch == 'h' ? hl : ch == 'd' ? dk : ch == 'w' ? wh : new Color32(0, 0, 0, 0);
            if (o.a > 0) o.a = alpha;
            return o;
        });
    }

    static void WriteFx()
    {
        var gold = new Color32(0xff, 0xd3, 0x6a, 0xff);
        // bolt: white-gold core with a gold tail (prototype: 4x2 core, 2x2 tail), pointing right
        WriteRows(BoltSprite, new[] { "....hh..", "ddccwwwh", "ddccwwwh", "....hh.." }, gold, 255);
        WriteRows(GlideWingsSprite, new[]
        {
            "hh................hh", "hcch............hcch", ".hccch........hccch.", "..hcccch....hcccch..",
            "....hccccddccccd....", "......hccddccd......",
        }, new Color32(0x8f, 0xd3, 0xff, 0xff), 220);
        // veil ring: radius 13 px, 2 px violet stroke at 55 percent, faint fill (prototype drawPlayer)
        var violet = new Color32(0xc0, 0x7d, 0xff, 0xff);
        WritePng(VeilRingSprite, 30, 30, (x, y) =>
        {
            float dx = x + 0.5f - 15f, dy = y + 0.5f - 15f;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            if (d >= 12f && d < 14f) return new Color32(violet.r, violet.g, violet.b, 140);
            if (d < 12f) return new Color32(violet.r, violet.g, violet.b, 41);
            return new Color32(0, 0, 0, 0);
        });
    }

    static bool Same(byte[] a, byte[] b)
    {
        if (a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
        return true;
    }

    static void WritePng(string path, int w, int h, Func<int, int, Color32> px)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) tex.SetPixel(x, y, px(x, y));
        tex.Apply();
        var bytes = tex.EncodeToPNG();
        UnityEngine.Object.DestroyImmediate(tex);
        if (File.Exists(path) && Same(File.ReadAllBytes(path), bytes)) return;
        File.WriteAllBytes(path, bytes);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
    }

    // ------------------------------------------------------------------ sound

    const int Rate = 22050;

    static float Square(float ph) => (ph % 1f) < 0.5f ? 1f : -1f;
    static float Tri(float ph) { float p = ph % 1f; return p < 0.5f ? 4f * p - 1f : 3f - 4f * p; }

    /// <summary>Simple chiptune voices, generated (original audio).</summary>
    static void WriteSounds()
    {
        var rng = new System.Random(7);
        float Noise() => (float)(rng.NextDouble() * 2 - 1);

        // double jump: quick rising chirp
        Synth("sfx_skill_dj", 0.14f, (t, d) => Square(Phase(t, f => Mathf.Lerp(620, 1180, f / d))) * 0.35f * Env(t, d, 0.005f, 0.08f));
        // veil on: soft shimmering triad with tremolo
        Synth("sfx_skill_veil", 0.5f, (t, d) => (Mathf.Sin(t * 523.25f * 6.283f) + Mathf.Sin(t * 659.25f * 6.283f) + Mathf.Sin(t * 783.99f * 6.283f) * 0.8f)
                                               * 0.22f * (0.75f + 0.25f * Mathf.Sin(t * 38f)) * Env(t, d, 0.02f, 0.3f));
        // veil off: two falling notes
        Synth("sfx_skill_veil_end", 0.24f, (t, d) => Tri(t * (t < 0.12f ? 659.25f : 493.88f)) * 0.3f * Env(t, d, 0.005f, 0.1f));
        // bolt: zap, falling pitch with a little grit
        Synth("sfx_skill_bolt", 0.16f, (t, d) => (Square(Phase(t, f => Mathf.Lerp(1500, 480, f / d))) * 0.28f + Noise() * 0.08f) * Env(t, d, 0.002f, 0.1f));
        // triple bolt: three quick zaps
        Synth("sfx_skill_bolt3", 0.24f, (t, d) =>
        {
            float k = t % 0.08f;
            return (Square(Phase(k, f => Mathf.Lerp(1700, 600, f / 0.08f))) * 0.24f + Noise() * 0.06f) * Env(k, 0.08f, 0.002f, 0.05f) * Env(t, d, 0.001f, 0.06f);
        });
        // bolt impact: noise burst with a low thump
        Synth("sfx_bolt_hit", 0.12f, (t, d) => (Noise() * 0.3f + Mathf.Sin(t * 110f * 6.283f) * 0.4f) * Env(t, d, 0.001f, 0.09f));
        // burning roots and thorns: crackle
        Synth("sfx_burn", 0.32f, (t, d) => (rng.NextDouble() < 0.08 ? Noise() * 0.5f : Noise() * 0.08f) * Env(t, d, 0.01f, 0.2f));
        // glide: airy whoosh (smoothed noise)
        float lp = 0;
        Synth("sfx_skill_glide", 0.34f, (t, d) => { lp += (Noise() - lp) * 0.08f; return lp * 0.9f * Env(t, d, 0.08f, 0.2f); });
        // new skill: rising fanfare C5 E5 G5 C6
        float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
        Synth("sfx_skill_learned", 0.9f, (t, d) =>
        {
            int i = Mathf.Min(3, (int)(t / 0.12f));
            float local = t - i * 0.12f;
            float len = i == 3 ? d - 0.36f : 0.12f;
            return (Square(t * notes[i]) * 0.16f + Tri(t * notes[i] * 0.5f) * 0.16f) * Env(local, len, 0.004f, i == 3 ? 0.4f : 0.05f);
        });
        // skill known (replay): two soft notes
        Synth("sfx_skill_known", 0.36f, (t, d) => Tri(t * (t < 0.14f ? 783.99f : 1046.5f)) * 0.26f * Env(t, d, 0.005f, 0.15f));
    }

    static float Env(float t, float d, float attack, float release)
    {
        if (t < 0 || t > d) return 0;
        float a = attack > 0 ? Mathf.Clamp01(t / attack) : 1;
        float r = release > 0 ? Mathf.Clamp01((d - t) / release) : 1;
        return a * r;
    }

    /// <summary>Integrates a frequency curve f(t) into a phase in cycles.</summary>
    static float Phase(float t, Func<float, float> freq)
    {
        // closed form for a linear sweep: phase = f0 t + (f1 - f0) t^2 / (2d); sampled numerically for simplicity
        const int steps = 32;
        float dt = t / steps, ph = 0;
        for (int i = 0; i < steps; i++) ph += freq((i + 0.5f) * dt) * dt;
        return ph;
    }

    static void Synth(string name, float seconds, Func<float, float, float> sample)
    {
        int n = Mathf.CeilToInt(seconds * Rate);
        var data = new short[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Rate;
            data[i] = (short)(Mathf.Clamp(sample(t, seconds), -1f, 1f) * 30000);
        }
        string path = $"{SkillSfxDir}/{name}.wav";
        var bytes = Wav(data);
        if (File.Exists(path) && Same(File.ReadAllBytes(path), bytes)) return;
        File.WriteAllBytes(path, bytes);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
    }

    static byte[] Wav(short[] data)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        int bytes = data.Length * 2;
        w.Write(new[] { (byte)'R', (byte)'I', (byte)'F', (byte)'F' });
        w.Write(36 + bytes);
        w.Write(new[] { (byte)'W', (byte)'A', (byte)'V', (byte)'E', (byte)'f', (byte)'m', (byte)'t', (byte)' ' });
        w.Write(16); w.Write((short)1); w.Write((short)1); w.Write(Rate); w.Write(Rate * 2); w.Write((short)2); w.Write((short)16);
        w.Write(new[] { (byte)'d', (byte)'a', (byte)'t', (byte)'a' });
        w.Write(bytes);
        foreach (var s in data) w.Write(s);
        w.Flush();
        return ms.ToArray();
    }
}
