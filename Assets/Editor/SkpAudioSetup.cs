// M8: audio and game feel assets.
// - Missing sounds, synthesized here (original): land, letter pickup, the Bellkeeper's toll hit and the parry.
// - Seamless music loops: each provided mus_*.ogg is decoded, its tail crossfaded into its head (equal power),
//   and written as Assets/Audio/Music/Loops/mus_*_loop.wav (imported as streamed Vorbis, so the shipped music
//   is still OGG Vorbis). The provided OGGs are left untouched.
// - The Audio Mixer (Assets/Audio/SkpMixer.mixer): Master > Music, SFX > (Game, UI), with exposed MusicVolume,
//   SfxVolume, GameVolume, MusicPitch, MusicCutoff (a Lowpass Simple on Music) and a compressor on Master.
//   Unity has no public API to build mixers, so this uses the Editor's own AudioMixerController by reflection.
// - Particle sprites and material, AudioTuning and FeelTuning (Resources), Sfx bank volumes with headroom.
// Menu: Sa Kabilang Pinto > Setup > Run M8 Audio Setup. Batch: -executeMethod SkpAudioSetup.RunAll
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Skp.Audio;
using Skp.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

public static class SkpAudioSetup
{
    public const string MixerPath = "Assets/Audio/SkpMixer.mixer";
    public const string LoopDir = "Assets/Audio/Music/Loops";
    public const string GenSfxDir = "Assets/Audio/Sfx/Generated";
    public const string TuningPath = "Assets/Resources/Audio/AudioTuning.asset";
    public const string FeelPath = "Assets/Resources/Feel/FeelTuning.asset";
    public const string FxDir = "Assets/Art/Fx";
    public const string ParticleMaterialPath = FxDir + "/fx_particles.mat";
    public const float LoopCrossfadeSeconds = 2f;
    /// <summary>Peak target for effect clips after the bank volume (headroom for overlapping voices).</summary>
    public const float SfxPeak = 0.7f;
    /// <summary>Peak target for the music loops.</summary>
    public const float MusicPeak = 0.85f;

    public static readonly string[] Tracks = { "menu", "a1", "a2", "a3", "boss" };
    public static string LoopPath(string track) => $"{LoopDir}/mus_{track}_loop.wav";

    [MenuItem("Sa Kabilang Pinto/Setup/Run M8 Audio Setup")]
    public static void RunAll() => SkpProjectSetup.RunAll();

    /// <summary>Called by SkpProjectSetup.RunAll after the M6 skill assets (the bank exists).</summary>
    public static void CreateAssets()
    {
        SkpProjectSetup.EnsureFolder(LoopDir);
        SkpProjectSetup.EnsureFolder(GenSfxDir);
        SkpProjectSetup.EnsureFolder("Assets/Resources/Audio");
        SkpProjectSetup.EnsureFolder("Assets/Resources/Feel");
        SkpProjectSetup.EnsureFolder(FxDir);

        WriteSounds();
        MakeLoops();
        AssetDatabase.Refresh();
        var groups = BuildMixer();
        BuildBank();
        BuildTuning(groups);
        BuildFeel();
        AssetDatabase.SaveAssets();
        Debug.Log("[SkpAudioSetup] M8 audio and feel assets ready.");
    }

    // ------------------------------------------------------------------ sounds

    const int Rate = 22050;

    static float Square(float ph) => (ph % 1f) < 0.5f ? 1f : -1f;
    static float Tri(float ph) { float p = ph % 1f; return p < 0.5f ? 4f * p - 1f : 3f - 4f * p; }
    static float Sin(float t, float f) => Mathf.Sin(t * f * 6.2831853f);

    static void WriteSounds()
    {
        var rng = new System.Random(11);
        float Noise() => (float)(rng.NextDouble() * 2 - 1);

        // land: soft low thump with a scuff of noise
        float lp = 0;
        Synth("sfx_land", 0.1f, (t, d) => { lp += (Noise() - lp) * 0.25f; return (Sin(t, Mathf.Lerp(150, 70, t / d)) * 0.5f + lp * 0.35f) * Env(t, d, 0.002f, 0.08f); });
        // letter pickup: paper flutter then two warm notes (G5, D6)
        float lp2 = 0;
        Synth("sfx_letter", 0.55f, (t, d) =>
        {
            lp2 += (Noise() - lp2) * 0.3f;
            float flutter = t < 0.14f ? lp2 * 0.35f * (0.6f + 0.4f * Square(t * 38f)) * Env(t, 0.14f, 0.005f, 0.05f) : 0f;
            float n1 = t >= 0.1f && t < 0.3f ? Tri((t - 0.1f) * 783.99f) * 0.28f * Env(t - 0.1f, 0.2f, 0.004f, 0.08f) : 0f;
            float n2 = t >= 0.24f ? (Tri((t - 0.24f) * 1174.66f) * 0.24f + Sin(t, 587.33f) * 0.1f) * Env(t - 0.24f, d - 0.24f, 0.004f, 0.25f) : 0f;
            return flutter + n1 + n2;
        });
        // the toll hit: a deep church bell (inharmonic partials, long decay) with a low hum
        Synth("sfx_toll", 1.6f, (t, d) =>
        {
            float e = Mathf.Exp(-t * 2.2f) * Env(t, d, 0.003f, 0.3f);
            float hum = Sin(t, 98f) * 0.35f;
            float body = Sin(t, 196f) * 0.3f + Sin(t, 233f * 2.01f) * 0.18f + Sin(t, 196f * 2.76f) * 0.14f + Sin(t, 196f * 5.4f) * 0.08f * Mathf.Exp(-t * 6f);
            return (hum + body) * e * 0.9f;
        });
        // the parry: a bright clang and a rising shimmer
        Synth("sfx_parry", 0.7f, (t, d) =>
        {
            float clang = (Sin(t, 880f) * 0.3f + Sin(t, 880f * 2.4f) * 0.2f + Sin(t, 880f * 3.9f) * 0.12f) * Mathf.Exp(-t * 6f);
            float hit = Noise() * 0.3f * Mathf.Exp(-t * 60f);
            float shimmer = Square(Phase(t, f => Mathf.Lerp(520, 1046, Mathf.Clamp01(f / 0.4f)))) * 0.08f * Env(t, d, 0.05f, 0.25f);
            return (clang + hit + shimmer) * Env(t, d, 0.001f, 0.2f);
        });
    }

    static float Env(float t, float d, float attack, float release)
    {
        if (t < 0 || t > d) return 0;
        float a = attack > 0 ? Mathf.Clamp01(t / attack) : 1;
        float r = release > 0 ? Mathf.Clamp01((d - t) / release) : 1;
        return a * r;
    }

    static float Phase(float t, Func<float, float> freq)
    {
        const int steps = 32;
        float dt = t / steps, ph = 0;
        for (int i = 0; i < steps; i++) ph += freq((i + 0.5f) * dt) * dt;
        return ph;
    }

    static void Synth(string name, float seconds, Func<float, float, float> sample)
    {
        int n = Mathf.CeilToInt(seconds * Rate);
        var data = new float[n];
        for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(sample(i / (float)Rate, seconds), -1f, 1f);
        WriteWav($"{GenSfxDir}/{name}.wav", data, Rate);
    }

    static void WriteWav(string path, float[] data, int rate)
    {
        using var ms = new MemoryStream();
        using (var w = new BinaryWriter(ms))
        {
            int bytes = data.Length * 2;
            w.Write(new[] { (byte)'R', (byte)'I', (byte)'F', (byte)'F' });
            w.Write(36 + bytes);
            w.Write(new[] { (byte)'W', (byte)'A', (byte)'V', (byte)'E', (byte)'f', (byte)'m', (byte)'t', (byte)' ' });
            w.Write(16); w.Write((short)1); w.Write((short)1); w.Write(rate); w.Write(rate * 2); w.Write((short)2); w.Write((short)16);
            w.Write(new[] { (byte)'d', (byte)'a', (byte)'t', (byte)'a' });
            w.Write(bytes);
            foreach (var s in data) w.Write((short)Mathf.RoundToInt(Mathf.Clamp(s, -1f, 1f) * 32000f));
            w.Flush();
            var b = ms.ToArray();
            if (File.Exists(path) && File.ReadAllBytes(path).SequenceEqual(b)) return;
            File.WriteAllBytes(path, b);
        }
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
    }

    // ------------------------------------------------------------------ music loops

    /// <summary>
    /// Crossfade loop: with length L and crossfade C, out[i] = head[i] * sin + tail[L - C + i] * cos for i &lt; C,
    /// then the rest of the track up to L - C. The last output sample is followed (on loop) by out[0], which
    /// continues the tail exactly, so there is no click and no gap.
    /// </summary>
    public static float[] CrossfadeLoop(float[] src, int fade)
    {
        int L = src.Length;
        fade = Mathf.Clamp(fade, 1, L / 3);
        int n = L - fade;
        var o = new float[n];
        for (int i = 0; i < n; i++) o[i] = src[i];
        for (int i = 0; i < fade; i++)
        {
            float x = (i + 0.5f) / fade;
            o[i] = src[i] * Mathf.Sin(x * Mathf.PI * 0.5f) + src[n + i] * Mathf.Cos(x * Mathf.PI * 0.5f);
        }
        return o;
    }

    static void MakeLoops()
    {
        foreach (var tr in Tracks)
        {
            string src = $"Assets/Audio/Music/mus_{tr}.ogg";
            if (!File.Exists(src)) { Debug.LogError("[SkpAudioSetup] missing " + src); continue; }
            string loop = LoopPath(tr);
            // a loop is rebuilt only when missing (decoding is slow); delete it to rebuild
            if (File.Exists(loop)) { ConfigureLoop(loop); continue; }

            // decode a temporary copy (the provided file stays streamed and untouched)
            string tmp = $"{LoopDir}/decode_{tr}.ogg";
            File.Copy(src, tmp, true);
            AssetDatabase.ImportAsset(tmp, ImportAssetOptions.ForceUpdate);
            var ai = (AudioImporter)AssetImporter.GetAtPath(tmp);
            var s = ai.defaultSampleSettings;
            s.loadType = AudioClipLoadType.DecompressOnLoad;
            s.compressionFormat = AudioCompressionFormat.PCM;
            ai.defaultSampleSettings = s;
            ai.forceToMono = true;
            ai.SaveAndReimport();
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(tmp);
            if (clip == null) { Debug.LogError("[SkpAudioSetup] could not decode " + src); continue; }
            clip.LoadAudioData();
            var data = new float[clip.samples * clip.channels];
            if (!clip.GetData(data, 0)) { Debug.LogError("[SkpAudioSetup] GetData failed for " + src); AssetDatabase.DeleteAsset(tmp); continue; }
            int rate = clip.frequency;
            AssetDatabase.DeleteAsset(tmp);

            // trim leading and trailing near-silence so the loop does not breathe
            int a = 0, b = data.Length - 1;
            while (a < b && Mathf.Abs(data[a]) < 0.003f) a++;
            while (b > a && Mathf.Abs(data[b]) < 0.003f) b--;
            var trimmed = new float[b - a + 1];
            Array.Copy(data, a, trimmed, 0, trimmed.Length);
            var outData = CrossfadeLoop(trimmed, Mathf.RoundToInt(LoopCrossfadeSeconds * rate));
            float peak = 0;
            foreach (var v in outData) peak = Mathf.Max(peak, Mathf.Abs(v));
            if (peak > 0) for (int i = 0; i < outData.Length; i++) outData[i] *= MusicPeak / peak;
            WriteWav(loop, outData, rate);
            ConfigureLoop(loop);
        }
    }

    static void ConfigureLoop(string path)
    {
        var ai = AssetImporter.GetAtPath(path) as AudioImporter;
        if (ai == null) return;
        var s = ai.defaultSampleSettings;
        bool change = s.loadType != AudioClipLoadType.Streaming || s.compressionFormat != AudioCompressionFormat.Vorbis || Math.Abs(s.quality - 0.5f) > 0.001f || !ai.forceToMono;
        if (!change) return;
        s.loadType = AudioClipLoadType.Streaming;
        s.compressionFormat = AudioCompressionFormat.Vorbis;
        s.quality = 0.5f; // same as SkpAssetImportSettings, so imports agree
        ai.defaultSampleSettings = s;
        ai.forceToMono = true;
        ai.SaveAndReimport();
    }

    // ------------------------------------------------------------------ mixer

    public class MixerGroups { public AudioMixer mixer; public AudioMixerGroup master, music, sfx, game, ui; }

    static readonly BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    static MixerGroups BuildMixer()
    {
        var existing = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
        if (existing != null && MixerComplete(existing)) return Groups(existing);
        if (existing != null) AssetDatabase.DeleteAsset(MixerPath);

        var asm = typeof(Editor).Assembly;
        Type tCtrl = asm.GetType("UnityEditor.Audio.AudioMixerController");
        Type tGroup = asm.GetType("UnityEditor.Audio.AudioMixerGroupController");
        Type tEffect = asm.GetType("UnityEditor.Audio.AudioMixerEffectController");
        Type tGroupPath = asm.GetType("UnityEditor.Audio.AudioGroupParameterPath");
        Type tEffectPath = asm.GetType("UnityEditor.Audio.AudioEffectParameterPath");
        if (tCtrl == null || tGroup == null || tEffect == null || tGroupPath == null || tEffectPath == null)
        {
            Debug.LogError("[SkpAudioSetup] the Editor's mixer types were not found; mixer not built");
            return null;
        }

        var ctrl = tCtrl.GetMethod("CreateMixerControllerAtPath", Any).Invoke(null, new object[] { MixerPath });
        object master = tCtrl.GetProperty("masterGroup", Any).GetValue(ctrl);
        object NewGroup(string name, object parent)
        {
            var g = tCtrl.GetMethod("CreateNewGroup", Any).Invoke(ctrl, new object[] { name, false });
            tCtrl.GetMethod("AddChildToParent", Any).Invoke(ctrl, new[] { g, parent });
            // views are only for the mixer window; a new mixer has none, so the call is skipped
            return g;
        }
        var music = NewGroup("Music", master);
        var sfx = NewGroup("SFX", master);
        var game = NewGroup("Game", sfx);
        var ui = NewGroup("UI", sfx);

        object NewEffect(object group, string effectName, int index)
        {
            var e = Activator.CreateInstance(tEffect, Any, null, new object[] { effectName }, null);
            var addSub = tCtrl.GetMethod("AddNewSubAsset", Any);
            if (addSub != null) addSub.Invoke(ctrl, new[] { e, (object)false });
            else AssetDatabase.AddObjectToAsset((UnityEngine.Object)e, (UnityEngine.Object)ctrl);
            tEffect.GetMethod("PreallocateGUIDs", Any).Invoke(e, null);
            tGroup.GetMethod("InsertEffect", Any).Invoke(group, new[] { e, (object)index });
            return e;
        }
        var lowpass = NewEffect(music, "Lowpass Simple", 0);
        var comp = NewEffect(master, "Compressor", 1);

        GUID G(object o, string method) => (GUID)tGroup.GetMethod(method, Any).Invoke(o, null);
        GUID EG(object e, string param) => (GUID)tEffect.GetMethod("GetGUIDForParameter", Any).Invoke(e, new object[] { param });

        void ExposeGroup(object group, GUID guid, string name) => Expose(ctrl, tCtrl, Activator.CreateInstance(tGroupPath, Any, null, new[] { group, (object)guid }, null), guid, name);
        void ExposeEffect(object group, object effect, GUID guid, string name) => Expose(ctrl, tCtrl, Activator.CreateInstance(tEffectPath, Any, null, new[] { group, effect, (object)guid }, null), guid, name);

        ExposeGroup(music, G(music, "GetGUIDForVolume"), AudioTuning.MusicVolume);
        ExposeGroup(music, G(music, "GetGUIDForPitch"), AudioTuning.MusicPitch);
        ExposeGroup(sfx, G(sfx, "GetGUIDForVolume"), AudioTuning.SfxVolume);
        ExposeGroup(game, G(game, "GetGUIDForVolume"), AudioTuning.GameVolume);
        var cutoffGuid = EG(lowpass, "Cutoff freq");
        ExposeEffect(music, lowpass, cutoffGuid, AudioTuning.MusicCutoff);

        // start values: open filter, music under the effects, a gentle master compressor against overlaps
        var snap = tCtrl.GetProperty("TargetSnapshot", Any)?.GetValue(ctrl) ?? tCtrl.GetProperty("startSnapshot", Any)?.GetValue(ctrl);
        if (snap != null)
        {
            var setValue = snap.GetType().GetMethod("SetValue", Any, null, new[] { typeof(GUID), typeof(float) }, null);
            void Set(GUID g, float v) { if (setValue != null) setValue.Invoke(snap, new object[] { g, v }); }
            Set(cutoffGuid, 22000f);
            Set(G(music, "GetGUIDForVolume"), -9f);
            Set(G(sfx, "GetGUIDForVolume"), -3f);
            Set(EG(comp, "Threshold"), -10f);
            Set(EG(comp, "Attack"), 5f);
            Set(EG(comp, "Release"), 80f);
        }
        EditorUtility.SetDirty((UnityEngine.Object)ctrl);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(MixerPath, ImportAssetOptions.ForceUpdate);
        var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
        if (mixer == null || !MixerComplete(mixer)) Debug.LogError("[SkpAudioSetup] mixer built but incomplete: " + MixerPath);
        return Groups(mixer);
    }

    static void Expose(object ctrl, Type tCtrl, object path, GUID guid, string name)
    {
        tCtrl.GetMethod("AddExposedParameter", Any).Invoke(ctrl, new[] { path });
        var prop = tCtrl.GetProperty("exposedParameters", Any);
        var arr = (Array)prop.GetValue(ctrl);
        for (int i = 0; i < arr.Length; i++)
        {
            var el = arr.GetValue(i);
            var gField = el.GetType().GetField("guid", Any);
            if (gField == null || !gField.GetValue(el).Equals(guid)) continue;
            el.GetType().GetField("name", Any).SetValue(el, name);
            arr.SetValue(el, i);
        }
        prop.SetValue(ctrl, arr);
        tCtrl.GetMethod("OnChangedExposedParameter", Any)?.Invoke(ctrl, null);
    }

    public static readonly string[] Exposed = { AudioTuning.MusicVolume, AudioTuning.SfxVolume, AudioTuning.GameVolume, AudioTuning.MusicPitch, AudioTuning.MusicCutoff };

    static bool MixerComplete(AudioMixer m)
    {
        if (m == null) return false;
        foreach (var n in new[] { "Music", "SFX", "Game", "UI" }) if (m.FindMatchingGroups("Master/" + (n == "Game" || n == "UI" ? "SFX/" : "") + n).Length == 0) return false;
        var prop = m.GetType().GetProperty("exposedParameters", Any);
        if (prop == null) return false;
        var names = new HashSet<string>();
        foreach (var el in (Array)prop.GetValue(m)) names.Add((string)el.GetType().GetField("name", Any).GetValue(el));
        return Exposed.All(names.Contains);
    }

    static MixerGroups Groups(AudioMixer m)
    {
        if (m == null) return null;
        AudioMixerGroup One(string p) { var g = m.FindMatchingGroups(p); return g.Length > 0 ? g[0] : null; }
        return new MixerGroups { mixer = m, master = One("Master"), music = One("Master/Music"), sfx = One("Master/SFX"), game = One("Master/SFX/Game"), ui = One("Master/SFX/UI") };
    }

    // ------------------------------------------------------------------ bank, tuning, feel

    /// <summary>Re-reads every sfx_* clip (the M6 bank plus the generated ones) and sets each volume for headroom.</summary>
    static void BuildBank()
    {
        var bank = AssetDatabase.LoadAssetAtPath<SfxBank>(SkpSkillSetup.BankPath);
        if (bank == null) { bank = ScriptableObject.CreateInstance<SfxBank>(); AssetDatabase.CreateAsset(bank, SkpSkillSetup.BankPath); }
        var entries = new List<SfxBank.Entry>();
        foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio/Sfx" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            string n = Path.GetFileNameWithoutExtension(path);
            if (!n.StartsWith("sfx_")) continue;
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            float peak = Peak(clip);
            entries.Add(new SfxBank.Entry { cue = n.Substring(4), clip = clip, volume = peak > 0 ? Mathf.Min(0.8f, SfxPeak / peak) : 0.7f });
        }
        entries.Sort((a, b) => string.CompareOrdinal(a.cue, b.cue));
        bank.entries = entries.ToArray();
        EditorUtility.SetDirty(bank);
    }

    public static float Peak(AudioClip clip)
    {
        if (clip == null) return 0;
        clip.LoadAudioData();
        var d = new float[clip.samples * clip.channels];
        if (!clip.GetData(d, 0)) return 1f;
        float p = 0;
        foreach (var v in d) p = Mathf.Max(p, Mathf.Abs(v));
        return p;
    }

    static void BuildTuning(MixerGroups g)
    {
        var t = AssetDatabase.LoadAssetAtPath<AudioTuning>(TuningPath);
        if (t == null) { t = ScriptableObject.CreateInstance<AudioTuning>(); AssetDatabase.CreateAsset(t, TuningPath); }
        if (g != null)
        {
            t.mixer = g.mixer; t.musicGroup = g.music; t.sfxGroup = g.sfx; t.gameGroup = g.game; t.uiGroup = g.ui;
        }
        AudioClip L(string tr) => AssetDatabase.LoadAssetAtPath<AudioClip>(LoopPath(tr));
        t.menu = L("menu"); t.act1 = L("a1"); t.act2 = L("a2"); t.act3 = L("a3"); t.boss = L("boss");
        EditorUtility.SetDirty(t);
    }

    static void BuildFeel()
    {
        var t = AssetDatabase.LoadAssetAtPath<FeelTuning>(FeelPath);
        if (t == null) { t = ScriptableObject.CreateInstance<FeelTuning>(); AssetDatabase.CreateAsset(t, FeelPath); }

        var spark = Sprite(FxDir + "/fx_spark_3x3.png", new[] { ".w.", "www", ".w." });
        var flame = Sprite(FxDir + "/fx_flame_3x4.png", new[] { ".w.", "www", "www", ".w." });
        var ash = Sprite(FxDir + "/fx_ash_2x2.png", new[] { "ww", "w." });
        var mote = Sprite(FxDir + "/fx_mote_2x2.png", new[] { "ww", "ww" });

        var mat = AssetDatabase.LoadAssetAtPath<Material>(ParticleMaterialPath);
        if (mat == null)
        {
            var sh = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default");
            mat = new Material(sh) { name = "fx_particles" };
            AssetDatabase.CreateAsset(mat, ParticleMaterialPath);
        }
        t.particleMaterial = mat;
        FxBurst S(FxBurst b, Sprite s) { b.sprite = s; return b; }
        t.flame = S(t.flame, flame);
        t.fragment = S(t.fragment, spark);
        t.veil = S(t.veil, mote);
        t.bolt = S(t.bolt, spark);
        t.burnt = S(t.burnt, ash);
        t.ember = S(t.ember, mote);
        t.letterGlow = S(t.letterGlow, spark);
        EditorUtility.SetDirty(t);
    }

    /// <summary>A white pixel sprite (tinted by the particle colour), rows top to bottom.</summary>
    static Sprite Sprite(string path, string[] rows)
    {
        int h = rows.Length, w = rows[0].Length;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            tex.SetPixel(x, h - 1 - y, rows[y][x] == 'w' ? Color.white : new Color(0, 0, 0, 0));
        tex.Apply();
        var bytes = tex.EncodeToPNG();
        UnityEngine.Object.DestroyImmediate(tex);
        if (!File.Exists(path) || !File.ReadAllBytes(path).SequenceEqual(bytes))
        {
            File.WriteAllBytes(path, bytes);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }
        var ti = (TextureImporter)AssetImporter.GetAtPath(path);
        if (ti.textureType != TextureImporterType.Sprite || ti.spritePixelsPerUnit != 16 || ti.filterMode != FilterMode.Point)
        {
            ti.textureType = TextureImporterType.Sprite;
            ti.spritePixelsPerUnit = 16;
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.mipmapEnabled = false;
            ti.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
