// M7 screenshots: every skill obstacle type with its tell, the flood, Act III bells, a manananggal, and the Bellkeeper
// (toll ring, shockwave, boss bar). Renders the real Level scene in edit mode at 960x540 and writes
// tests/Screenshots/m7/*.png plus a contact sheet. Menu: Sa Kabilang Pinto > Screenshots > M7 Levels.
// Batch: -executeMethod SkpLevelShots.RunBatch
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Skp.Core;
using Skp.Enemies;
using Skp.Levels;
using Skp.Player;
using Skp.Save;
using Skp.Skills;
using Skp.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SkpLevelShots
{
    public const string OutDir = "tests/Screenshots/m7";
    const int W = 960, H = 540;

    [MenuItem("Sa Kabilang Pinto/Screenshots/M7 Levels")]
    public static void RunMenu() => Debug.Log($"[SkpLevelShots] {Run().Count} screenshots in {OutDir}");

    public static void RunBatch()
    {
        var shots = Run();
        Debug.Log($"[SkpLevelShots] {shots.Count} screenshots in {OutDir}");
        EditorApplication.Exit(shots.Count >= 9 ? 0 : 3);
    }

    static string _out = OutDir;
    static bool _withLevel1;
    public const string R1Dir = "tests/Screenshots/r1/after";

    /// <summary>
    /// R1: renders the same shots with the prefab-built views into tests/Screenshots/r1/after (plus level 1 at its first lantern),
    /// compares each with the M7 render made before R1 (the world drawing code did not change between M7 and R1), and writes
    /// tests/Screenshots/r1/screenshot_compare.md. Exit 0 when every pair matches within the tolerance.
    /// </summary>
    public static void RunR1Batch()
    {
        _out = R1Dir; _withLevel1 = true;
        List<string> files;
        try { files = Run(); }
        finally { _out = OutDir; _withLevel1 = false; }
        var md = new System.Text.StringBuilder("# R1 screenshots: before (M7 render) and after (prefabs)\n\n| Shot | Pixels differing | Largest channel difference | Result |\n| --- | --- | --- | --- |\n");
        bool ok = true;
        foreach (var after in files)
        {
            var name = Path.GetFileName(after);
            if (name.StartsWith("contact_sheet")) continue;
            var before = Path.Combine(OutDir, name);
            if (!File.Exists(before)) { md.Append($"| {name} | no pre-R1 render | | rendered for the record |\n"); continue; }
            var (n, max) = Diff(before, after);
            bool pass = n == 0; // tolerance: identical pixels
            ok &= pass;
            md.Append($"| {name} | {n} | {max} | {(pass ? "identical" : "DIFFERENT")} |\n");
        }
        Directory.CreateDirectory("tests/Screenshots/r1");
        File.WriteAllText("tests/Screenshots/r1/screenshot_compare.md", md.ToString());
        Debug.Log("[SkpLevelShots] R1 compare:\n" + md);
        EditorApplication.Exit(ok ? 0 : 4);
    }

    static (int count, int max) Diff(string a, string b)
    {
        var ta = new Texture2D(2, 2); ta.LoadImage(File.ReadAllBytes(a));
        var tb = new Texture2D(2, 2); tb.LoadImage(File.ReadAllBytes(b));
        if (ta.width != tb.width || ta.height != tb.height) return (-1, 255);
        var pa = ta.GetPixels32(); var pb = tb.GetPixels32();
        int n = 0, max = 0;
        for (int i = 0; i < pa.Length; i++)
        {
            int d = Mathf.Max(Mathf.Abs(pa[i].r - pb[i].r), Mathf.Max(Mathf.Abs(pa[i].g - pb[i].g), Mathf.Abs(pa[i].b - pb[i].b)));
            if (d > 0) { n++; if (d > max) max = d; }
        }
        Object.DestroyImmediate(ta); Object.DestroyImmediate(tb);
        return (n, max);
    }

    class Shot { public bool Search; public string Name; public int Level; public string[] Skills; public System.Action<LevelSession, LevelRunner> Setup; public System.Func<LevelSession, Vector2?> Focus; }

    public static List<string> Run()
    {
        Directory.CreateDirectory(_out);
        var list = new List<Shot>
        {
            Obstacle("01_double_jump_climb", 3, "dj"),
            Obstacle("02_veil_gauntlet", 5, "veil"),
            Obstacle("03_bolt_thorn_wall", 4, "bolt"),
            Obstacle("04_dash_gap", 5, "dash"),
            Obstacle("05_triple_bolt_pack", 7, "pack"),
            Obstacle("06_glide_gap", 8, "glide"),
            new Shot { Name = "07_flood_level5", Level = 4, Skills = SkillIds.All.Take(4).ToArray(), Focus = s => s.World.Lanterns[1],
                       Setup = (s, r) => { s.World.Flood.Y = s.World.Lanterns[1].y - 2f; } },
            new Shot { Name = "08_act3_falling_bell", Level = 6, Skills = SkillIds.All.Take(6).ToArray(),
                       Focus = s => s.World.Bells.Count > 0 ? new Vector2(s.World.Bells[0].X, s.World.Bells[0].GroundY + 3) : (Vector2?)null,
                       Setup = (s, r) => { foreach (var b in s.World.Bells) { b.Warning = true; } if (s.World.Bells.Count > 1) { var b = s.World.Bells[1]; b.Falling = true; b.Y = b.GroundY + 3; } } },
            new Shot { Name = "09_manananggal", Level = 7, Skills = SkillIds.All.Take(7).ToArray(),
                       Focus = s => s.World.Enemies.Where(e => e.Kind == EnemyKind.Manananggal).Select(e => (Vector2?)e.Center).FirstOrDefault() },
            new Shot { Name = "10_bellkeeper_toll", Level = 8, Skills = SkillIds.All, Focus = s => new Vector2(16, s.World.Arena.FloorY + 6.5f),
                       Setup = (s, r) => Boss(s, BossState.Toll) },
            new Shot { Name = "11_bellkeeper_shockwave_bells", Level = 8, Skills = SkillIds.All, Focus = s => new Vector2(16, s.World.Arena.FloorY + 6.5f),
                       Setup = (s, r) => Boss(s, BossState.Bells) },
        };
        if (_withLevel1)
            list.Insert(0, new Shot { Name = "00_level1_first_lantern", Level = 0, Skills = new string[0], Focus = s => s.World.Lanterns[1] });
        var files = new List<string>();
        var tex = new List<(string, Texture2D)>();
        var tmp = Path.Combine(Path.GetTempPath(), "skp_shots_save");
        foreach (var shot in list)
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Level.unity", OpenSceneMode.Single);
            var runner = Object.FindAnyObjectByType<LevelRunner>();
            var save = new SaveService(tmp);
            save.Load();
            save.Data.skills = shot.Skills;
            save.Data.unlocked = 9;
            var session = new LevelSession(runner.levels[shot.Level].spec, new LevelRequest { Level = shot.Level, Difficulty = "standard" },
                                           runner.tuning, runner.difficulties, runner.curve, save);
            var focus = shot.Focus(session);
            if (focus == null && shot.Search)
                for (int lv = 2; lv < 9 && focus == null; lv++)
                {
                    shot.Level = lv;
                    session = new LevelSession(runner.levels[lv].spec, new LevelRequest { Level = lv, Difficulty = "standard" }, runner.tuning, runner.difficulties, runner.curve, save);
                    focus = shot.Focus(session);
                }
            if (focus == null) { Debug.LogWarning("[SkpLevelShots] nothing to show for " + shot.Name); continue; }
            runner.view.act = runner.levels[shot.Level].spec.act;
            runner.view.Build(session.World, session.Sim);
            runner.backdrop.sprite = runner.view.art.backdrops[runner.view.act - 1];
            runner.director.Refresh();
            if (runner.director.screenTint != null && runner.director.screenTint.sprite == null)
                runner.director.screenTint.sprite = Skp.Dimensions.DimensionDirector.WhiteSprite();
            // Tala stands near the subject
            var sim = session.Sim;
            sim.Position = focus.Value + new Vector2(-3f, -1.5f);
            shot.Setup?.Invoke(session, runner);
            runner.player.view.Render(sim, sim.Position, 1f);
            BossView bv = null;
            if (session.Boss != null && session.Boss.Active)
            {
                bv = Object.Instantiate(runner.view.Catalog.bossView);
                bv.Bind(session.Boss, runner.view.Catalog);
            }
            // world objects follow their state (LateUpdate does not run in edit mode)
            typeof(LevelView).GetMethod("LateUpdate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(runner.view, null);
            bv?.Render();
            var cam = Camera.main;
            var ppc = cam.GetComponent<UnityEngine.Rendering.Universal.PixelPerfectCamera>();
            if (ppc != null) ppc.enabled = false;
            cam.transform.position = new Vector3(focus.Value.x, focus.Value.y, -10);
            foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include)) c.gameObject.SetActive(false); // the world only
            var rt = new RenderTexture(W, H, 24);
            cam.targetTexture = rt;
            cam.Render(); cam.Render();
            RenderTexture.active = rt;
            var t = new Texture2D(W, H, TextureFormat.RGB24, false) { hideFlags = HideFlags.HideAndDontSave };
            t.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            t.Apply();
            RenderTexture.active = null;
            cam.targetTexture = null;
            rt.Release();
            var file = Path.Combine(_out, shot.Name + ".png");
            File.WriteAllBytes(file, t.EncodeToPNG());
            files.Add(file);
            tex.Add((shot.Name, t));
        }
        if (Directory.Exists(tmp)) Directory.Delete(tmp, true);
        Sheet(tex, files);
        foreach (var (_, t) in tex) Object.DestroyImmediate(t);
        return files;
    }

    static Shot Obstacle(string name, int level, string type) => new Shot
    {
        Name = name, Level = level, Search = true,
        Skills = type == "dash" ? SkillIds.All.Where(x => x != "glide").ToArray() : SkillIds.All, // a glide makes dash gaps unnecessary: none are built with it
        Focus = s =>
        {
            var o = s.Layout.Obstacles.FirstOrDefault(x => x.type == type);
            if (o == null) return null;
            var l = s.World.Ledges[s.Layout.Plats.IndexOf(o.Over ?? o.To)];
            return l.Rect.center + new Vector2(0, 2.5f);
        },
    };

    static void Boss(LevelSession s, BossState want)
    {
        var lantern = s.World.Lanterns[s.Layout.Rooms];
        s.Sim.Position = new Vector2(lantern.x - 0.3f, lantern.y);
        s.Sim.Invulnerable = 1000000;
        s.Sim.Step(default);
        for (int i = 0; i < 4000 && s.Boss.State != want; i++) { s.Sim.Position = new Vector2(lantern.x - 3, lantern.y); s.Sim.Step(default); }
        for (int i = 0; i < (want == BossState.Toll ? 40 : 90); i++) { s.Sim.Position = new Vector2(lantern.x - 3, lantern.y); s.Sim.Step(default); }
    }

    static void Sheet(List<(string name, Texture2D tex)> shots, List<string> files)
    {
        if (shots.Count == 0) return;
        const int cols = 3, tw = 480, pad = 8;
        int th = tw * H / W, rows = (shots.Count + cols - 1) / cols;
        int SW = cols * tw + (cols + 1) * pad, SH = rows * th + (rows + 1) * pad;
        var sheet = new Texture2D(SW, SH, TextureFormat.RGB24, false);
        var fill = Enumerable.Repeat(new Color32(0x14, 0x10, 0x1c, 0xff), SW * SH).ToArray();
        sheet.SetPixels32(fill);
        for (int i = 0; i < shots.Count; i++)
        {
            int cx = pad + (i % cols) * (tw + pad), cy = SH - (pad + (i / cols) * (th + pad)) - th;
            for (int y = 0; y < th; y++) for (int x = 0; x < tw; x++)
                sheet.SetPixel(cx + x, cy + y, shots[i].tex.GetPixelBilinear((x + 0.5f) / tw, (y + 0.5f) / th));
        }
        sheet.Apply();
        var f = Path.Combine(_out, "contact_sheet_m7_levels.png");
        File.WriteAllBytes(f, sheet.EncodeToPNG());
        Object.DestroyImmediate(sheet);
        files.Add(f);
    }
}
