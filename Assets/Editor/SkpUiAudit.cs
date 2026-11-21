// M5 and M6 layout audit: opens every screen at 1280x720, 2400x1080, 2340x1080 with a simulated notch, 1024x768 and
// 800x480, checks it in code (overlapping targets, 48 dp minimum, safe area, clipped text, contrast, dialog vs
// pause button) and saves a screenshot of each plus a contact sheet per resolution in tests/Screenshots/m6 (M5: m5).
// Menu: Sa Kabilang Pinto > Screenshots > M5 UI Audit. Batch: -executeMethod SkpUiAudit.RunBatch
// The EditMode test UiLayoutAuditTests runs the same audit and fails on any problem.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Skp.Core;
using Skp.Levels;
using Skp.Save;
using Skp.Skills;
using Skp.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;

public static class SkpUiAudit
{
    public const string OutDir = "tests/Screenshots/m6"; // M5 evidence stays in tests/Screenshots/m5

    public struct Res
    {
        public string Name;
        public int W, H;
        public float Dpi;
        public Rect Safe;
        public Res(string n, int w, int h, float dpi, Rect safe) { Name = n; W = w; H = h; Dpi = dpi; Safe = safe; }
    }

    /// <summary>
    /// The five required targets. Phones use a realistic density (a 6.4" 20:9 phone is about 400 dpi); the others
    /// use the 160 dpi fallback. The notch case reserves a 100 px cutout on the left and the gesture bar.
    /// </summary>
    public static readonly Res[] Resolutions =
    {
        new Res("1280x720", 1280, 720, 160, new Rect(0, 0, 1, 1)),
        new Res("2400x1080", 2400, 1080, 400, new Rect(0, 0, 1, 1)),
        new Res("2340x1080_notch", 2340, 1080, 400, new Rect(100f / 2340, 40f / 1080, 1 - 200f / 2340, 1 - 40f / 1080)),
        new Res("1024x768", 1024, 768, 160, new Rect(0, 0, 1, 1)),
        new Res("800x480", 800, 480, 160, new Rect(0, 0, 1, 1)),
    };

    public class Report
    {
        public readonly List<string> Problems = new List<string>();
        public readonly List<string> Shots = new List<string>();
        public int Checks;
        public void Fail(string res, string state, string msg) => Problems.Add($"[{res}] {state}: {msg}");
    }

    struct State { public string Scene, Name; public Action<Ctx> Setup; }

    static readonly string[] M6States = { "04b", "08b", "08c", "10c", "10d", "10e", "10f", "10g", "10h", "12s", "12t", "12u", "13b" };

    class Ctx
    {
        public Res Res;
        public Camera Cam;
        public SaveService Save;
        public LevelUi Ui;
        public LevelSession Session;
        public MapController Map;
        public TitleFlow Title;
    }

    [MenuItem("Sa Kabilang Pinto/Screenshots/M5 UI Audit")]
    public static void RunMenu()
    {
        var r = Run(true);
        Debug.Log($"[SkpUiAudit] {r.Checks} checks, {r.Problems.Count} problems, {r.Shots.Count} screenshots");
    }

    public static void RunBatch()
    {
        var r = Run(true);
        Debug.Log($"[SkpUiAudit] {r.Checks} checks, {r.Problems.Count} problems, {r.Shots.Count} screenshots");
        foreach (var p in r.Problems) Debug.Log("[SkpUiAudit] PROBLEM " + p);
        EditorApplication.Exit(r.Problems.Count == 0 ? 0 : 3);
    }

    static List<State> States()
    {
        var l = new List<State>();
        void Add(string scene, string name, Action<Ctx> s) => l.Add(new State { Scene = scene, Name = name, Setup = s });
        Add(SceneNames.Title, "01_title", c => { c.Title.title.Bind(false); c.Title.title.Open(); c.Title.title.newJourney.SetHighlighted(true); });
        Add(SceneNames.Title, "02_character", c => { c.Title.characters.Open(); c.Title.characters.girl.SetHighlighted(true); });
        Add(SceneNames.Title, "03_difficulty", c => { c.Title.difficulty.Bind(false, 0); c.Title.difficulty.Open(); c.Title.difficulty.standard.SetHighlighted(true); });
        Add(SceneNames.Title, "04_how_to_play", c => { c.Title.howTo.Open(); });
        Add(SceneNames.Title, "04b_how_to_skills", c => { c.Title.howTo.Open(); c.Title.howTo.ShowPage(HowToScreen.Pages - 1); });
        Add(SceneNames.Title, "05_settings", c => { c.Title.settings.Bind(new SettingsStore(Path.Combine(Path.GetTempPath(), "skp_audit_settings")), null); c.Title.settings.Open(); });
        Add(SceneNames.Title, "06_credits", c => { c.Title.credits.Open(); });
        Add(SceneNames.Map, "07_level_map", c => { MapState(c, 5, true); });
        Add(SceneNames.Map, "07b_level_map_locked", c => { MapState(c, 2, true); c.Map.TapNode(6); });
        Add(SceneNames.Map, "08_lola_dialog", c => { MapState(c, 1, false); c.Map.ShowLolaForAudit(); });
        Add(SceneNames.Map, "08b_map_skill_list", c => { MapState(c, 4, true, "dj", "veil", "bolt"); c.Map.ShowSkills(); });
        Add(SceneNames.Map, "08c_map_skill_list_all", c => { MapState(c, 9, true, SkillIds.All); c.Map.ShowSkills(); });
        Add(SceneNames.Level, "09_before_stage", c => { LevelState(c, 3, false); c.Ui.ShowIntro(); c.Ui.dialog.CompleteLine(); });
        Add(SceneNames.Level, "10_gameplay", c => { LevelState(c, 3, true); });
        Add(SceneNames.Level, "10b_gameplay_lefthanded", c => { LevelState(c, 3, true, leftHanded: true); });
        // M6 HUD: 0 skills (above), 2 and all 8, normal and left-handed touch layouts, plus the keyboard layout
        Add(SceneNames.Level, "10c_hud_2_skills", c => { LevelState(c, 3, true, skills: new[] { "dj", "veil" }); });
        Add(SceneNames.Level, "10d_hud_2_skills_lefthanded", c => { LevelState(c, 3, true, leftHanded: true, skills: new[] { "dj", "veil" }); });
        Add(SceneNames.Level, "10e_hud_8_skills", c => { LevelState(c, 7, true, skills: SkillIds.All); });
        Add(SceneNames.Level, "10f_hud_8_skills_lefthanded", c => { LevelState(c, 7, true, leftHanded: true, skills: SkillIds.All); });
        Add(SceneNames.Level, "10g_hud_8_skills_keyboard", c => { LevelState(c, 7, true, skills: SkillIds.All, touch: false); });
        // worst case for the lanes: the longest objective (Spirit) and a long hint toast beside all eight chips
        Add(SceneNames.Level, "10h_hud_8_skills_spirit_toast", c =>
        {
            LevelState(c, 7, true, skills: SkillIds.All);
            c.Session.Sim.SetDimension(Skp.Dimensions.Dim.Spirit);
            c.Ui.hud.Show(c.Session);
            c.Ui.toast.Show(Loc.Get("hint.thorn"), 99);
            c.Ui.SyncLanes();
        });
        Add(SceneNames.Level, "12_letter", c => { LevelState(c, 3, true); c.Ui.toast.Hide(); c.Ui.ShowLetter(); c.Ui.dialog.CompleteLine(); });
        // M6: the two letter boxes for every skill (letter, then NEW SKILL), and SKILL KNOWN on a replay
        for (int i = 0; i < SkillIds.All.Length; i++)
        {
            int lv = i;
            string id = SkillIds.All[i];
            Add(SceneNames.Level, $"12s_{lv + 1}_{id}_letter", c => { LevelState(c, lv, true); LetterBox(c, lv, id, true, 0); });
            Add(SceneNames.Level, $"12t_{lv + 1}_{id}_new_skill", c => { LevelState(c, lv, true); LetterBox(c, lv, id, true, 1); });
        }
        Add(SceneNames.Level, "12u_skill_known", c => { LevelState(c, 5, true, skills: SkillIds.All); LetterBox(c, 5, "spread", false, 1); });
        Add(SceneNames.Level, "13_level_clear", c => { LevelState(c, 0, true); c.Session.Sim.Score = 750; c.Session.Sim.Shards = 2; c.Ui.ShowCompleted(); });
        Add(SceneNames.Level, "13b_level_clear_new_skill", c =>
        {
            LevelState(c, 2, true, skills: new[] { "dj", "veil" });
            TakeLetter(c);
            c.Ui.dialog.Close();
            c.Session.Sim.Score = 1250; c.Session.Sim.Shards = 3;
            c.Ui.ShowCompleted();
        });
        Add(SceneNames.Level, "14_pause", c => { LevelState(c, 3, true); c.Ui.Pause(); });
        Add(SceneNames.Level, "14b_settings_from_pause", c => { LevelState(c, 3, true); c.Ui.settings.Bind(new SettingsStore(Path.Combine(Path.GetTempPath(), "skp_audit_settings")), null); c.Ui.settings.Open(); });
        Add(SceneNames.Level, "15_game_over", c => { LevelState(c, 3, true); c.Ui.ShowGameOver(); });
        Add(SceneNames.Level, "16_ending", c => { LevelState(c, 8, true); c.Ui.ShowCompleted(); });
        return l;
    }

    static void LetterBox(Ctx c, int level, string id, bool fresh, int line)
    {
        c.Ui.toast.Hide();
        var lines = LevelUi.LetterLines(c.Session.Spec, id, fresh);
        c.Ui.dialog.Show(new[] { lines[line] }, null);
        c.Ui.dialog.CompleteLine();
    }

    static void TakeLetter(Ctx c)
    {
        var sim = c.Session.Sim;
        var clue = c.Session.World.Items.First(i => i.Kind == Skp.Player.ItemKind.Clue && i.Mask == Skp.Dimensions.Dim.Ordinary);
        sim.Invulnerable = 100000;
        sim.Position = new Vector2(clue.Rect.center.x - sim.Size.x / 2, clue.Rect.y);
        for (int i = 0; i < 5 && !sim.HasClue; i++) sim.Step(default);
    }

    static void MapState(Ctx c, int unlocked, bool metLola, params string[] skills)
    {
        var s = new SaveData { unlocked = unlocked, metLola = metLola, lolaSeen = metLola ? unlocked - 1 : 0, diff = "standard", @char = "girl", skills = skills };
        c.Map.Present(s);
        c.Map.screen.Open();
    }

    static void LevelState(Ctx c, int level, bool playing, bool leftHanded = false, string[] skills = null, bool touch = true)
    {
        var runner = UnityEngine.Object.FindAnyObjectByType<LevelRunner>();
        c.Save.Data.unlocked = 9;
        c.Save.Data.skills = skills ?? new string[0];
        c.Session = new LevelSession(runner.levels[level].spec, new LevelRequest { Level = level, Difficulty = "standard" },
                                     runner.tuning, runner.difficulties, runner.curve, c.Save);
        runner.view.Build(c.Session.World, c.Session.Sim);
        runner.backdrop.sprite = runner.view.art.backdrops[runner.levels[level].spec.act - 1];
        runner.director.Refresh();
        if (runner.director.screenTint != null && runner.director.screenTint.sprite == null)
            runner.director.screenTint.sprite = Skp.Dimensions.DimensionDirector.WhiteSprite();
        runner.player.view.Render(c.Session.Sim, c.Session.Sim.Position, 1f);
        var follow = c.Cam.GetComponent<CameraFollow>();
        follow.bounds = c.Session.World.Bounds;
        var t = follow.Target(c.Session.Sim.Center, 1);
        c.Cam.transform.position = new Vector3(t.x, t.y, -10);
        c.Ui = runner.ui;
        c.Ui.Bind(runner, c.Session, false);
        if (!playing) { }
        c.Ui.touch.forceShow = touch;
        c.Ui.ApplySettings(new GameSettings { leftHanded = leftHanded, touch = touch ? TouchMode.On : TouchMode.Off });
        c.Ui.touch.SetSkills(c.Session.Sim.Has(SkillIds.Veil), c.Session.Sim.Has(SkillIds.Bolt));
        c.Ui.touch.SetPlaying(true, true); // worst case: the contextual door button is showing
        c.Ui.hud.Show(c.Session);
        c.Ui.SyncLanes();
    }

    // ------------------------------------------------------------------ run

    public static Report Run(bool saveShots)
    {
        var report = new Report();
        var tmp = Path.Combine(Path.GetTempPath(), "skp_audit_save");
        var save = new SaveService(tmp);
        save.Load();
        var states = States();
        var shotsByRes = new Dictionary<string, List<(string, Texture2D)>>();
        try
        {
            foreach (var group in states.GroupBy(s => s.Scene))
            {
                foreach (var st in group)
                {
                    EditorSceneManager.OpenScene($"Assets/Scenes/{st.Scene}.unity", OpenSceneMode.Single);
                    foreach (var res in Resolutions)
                    {
                        var ctx = new Ctx { Res = res, Save = save, Cam = Camera.main };
                        ctx.Map = UnityEngine.Object.FindAnyObjectByType<MapController>();
                        ctx.Title = UnityEngine.Object.FindAnyObjectByType<TitleFlow>();
                        var tex = Capture(ctx, st, report, saveShots);
                        if (!shotsByRes.TryGetValue(res.Name, out var list)) shotsByRes[res.Name] = list = new List<(string, Texture2D)>();
                        list.Add((st.Name, tex));
                    }
                }
            }
            if (saveShots)
            {
                foreach (var kv in shotsByRes)
                {
                    ContactSheet(kv.Key, kv.Value, report);
                    // the states new in M6 on their own sheet, for review
                    var m6 = kv.Value.Where(v => M6States.Any(p => v.Item1.StartsWith(p))).ToList();
                    if (m6.Count > 0) ContactSheet("m6_" + kv.Key, m6, report);
                }
                WriteReport(report);
            }
        }
        finally
        {
            SafeAreaFitter.Simulated = null;
            foreach (var l in shotsByRes.Values) foreach (var (_, t) in l) UnityEngine.Object.DestroyImmediate(t);
            if (Directory.Exists(tmp)) Directory.Delete(tmp, true);
        }
        return report;
    }

    static Texture2D Capture(Ctx c, State st, Report report, bool save)
    {
        var res = c.Res;
        SafeAreaFitter.Simulated = res.Safe;
        foreach (var s in UnityEngine.Object.FindObjectsByType<UiScreen>(FindObjectsInactive.Include)) s.gameObject.SetActive(false);
        foreach (var f in UnityEngine.Object.FindObjectsByType<SafeAreaFitter>(FindObjectsInactive.Include)) f.Apply();

        var rt = new RenderTexture(res.W, res.H, 24);
        var cam = c.Cam;
        var ppc = cam.GetComponent<UnityEngine.Rendering.Universal.PixelPerfectCamera>();
        if (ppc != null) ppc.enabled = false;
        cam.targetTexture = rt;
        var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include).Where(k => k.isRootCanvas && k.GetComponent<CircleWipe>() == null).ToArray();
        foreach (var k in canvases)
        {
            k.renderMode = RenderMode.ScreenSpaceCamera;
            k.worldCamera = cam;
            k.planeDistance = 1 + (100 - k.sortingOrder) * 0.01f;
        }

        st.Setup(c);
        // as with an overlay canvas in play, the world draws under the UI: move every world renderer below the canvases
        foreach (var wr in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include))
            if (wr.GetComponentInParent<Canvas>() == null && wr.sortingOrder > -20000) wr.sortingOrder -= 20000;
        // as in play: the touch buttons hide while any box or dialog is open
        var lu = UnityEngine.Object.FindAnyObjectByType<LevelUi>();
        if (lu != null && lu.touch != null && UnityEngine.Object.FindObjectsByType<UiScreen>(FindObjectsInactive.Exclude).Length > 0) lu.touch.SetPlaying(false, false);
        foreach (var f in UnityEngine.Object.FindObjectsByType<SafeAreaFitter>(FindObjectsInactive.Include)) f.Apply();
        Relayout(canvases);
        if (lu != null && lu.Session != null) { lu.hud.FitLanes(true); lu.SyncLanes(); Relayout(canvases); }

        // render once without any text: the contrast check measures each text against the real pixels behind it
        var texts = UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude).Where(t => t.enabled).ToArray();
        var alphas = texts.Select(t => t.alpha).ToArray();
        foreach (var t in texts) t.alpha = 0f; // invisible but still laid out
        cam.Render();
        cam.Render();
        RenderTexture.active = rt;
        _behind = new Texture2D(res.W, res.H, TextureFormat.RGB24, false);
        _behind.ReadPixels(new Rect(0, 0, res.W, res.H), 0, 0);
        _behind.Apply();
        RenderTexture.active = null;
        for (int i = 0; i < texts.Length; i++) texts[i].alpha = alphas[i];
        Relayout(canvases);

        Check(c, st.Name, canvases, report);
        UnityEngine.Object.DestroyImmediate(_behind);

        cam.Render();
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(res.W, res.H, TextureFormat.RGB24, false) { hideFlags = HideFlags.HideAndDontSave }; // survives scene changes until the sheet is made
        tex.ReadPixels(new Rect(0, 0, res.W, res.H), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        cam.targetTexture = null;
        rt.Release();
        if (save)
        {
            var dir = Path.Combine(OutDir, res.Name);
            Directory.CreateDirectory(dir);
            var file = Path.Combine(dir, st.Name + ".png");
            File.WriteAllBytes(file, tex.EncodeToPNG());
            report.Shots.Add(file);
        }
        return tex;
    }

    static void Relayout(Canvas[] canvases)
    {
        var handle = typeof(CanvasScaler).GetMethod("Handle", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        for (int pass = 0; pass < 3; pass++)
        {
            foreach (var k in canvases)
            {
                var sc = k.GetComponent<CanvasScaler>();
                if (sc != null) handle?.Invoke(sc, null);
                Canvas.ForceUpdateCanvases();
                foreach (var rt in k.GetComponentsInChildren<RectTransform>()) if (rt.GetComponent<LayoutGroup>() != null) LayoutRebuilder.MarkLayoutForRebuild(rt);
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)k.transform);
            }
            Canvas.ForceUpdateCanvases();
        }
        foreach (var t in UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude)) t.ForceMeshUpdate(true);
    }

    // ------------------------------------------------------------------ checks

    static Rect ScreenRect(RectTransform rt, Camera cam)
    {
        var w = new Vector3[4];
        rt.GetWorldCorners(w);
        Vector2 a = RectTransformUtility.WorldToScreenPoint(cam, w[0]);
        Vector2 b = RectTransformUtility.WorldToScreenPoint(cam, w[2]);
        return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
    }

    static bool Overlaps(Rect a, Rect b, float tol = 1f) =>
        a.xMin < b.xMax - tol && b.xMin < a.xMax - tol && a.yMin < b.yMax - tol && b.yMin < a.yMax - tol;

    static bool Inside(Rect inner, Rect outer, float tol = 1.5f) =>
        inner.xMin >= outer.xMin - tol && inner.yMin >= outer.yMin - tol && inner.xMax <= outer.xMax + tol && inner.yMax <= outer.yMax + tol;

    static string PathOf(Transform t)
    {
        var sb = new StringBuilder(t.name);
        for (var p = t.parent; p != null && p.GetComponent<Canvas>() == null; p = p.parent) sb.Insert(0, p.name + "/");
        return sb.ToString();
    }

    static void Check(Ctx c, string state, Canvas[] canvases, Report r)
    {
        var res = c.Res;
        var cam = c.Cam;
        var th = UiTheme.Load();
        var safe = new Rect(res.Safe.x * res.W, res.Safe.y * res.H, res.Safe.width * res.W, res.Safe.height * res.H);
        float minPx = 48f * res.Dpi / 160f;
        string rn = res.Name;

        // interactive targets: visible buttons and on-screen touch buttons, grouped by the layer that receives taps
        var targets = new List<(RectTransform rt, string layer)>();
        foreach (var b in UnityEngine.Object.FindObjectsByType<PixelButton>(FindObjectsInactive.Exclude))
        {
            var scr = b.GetComponentInParent<UiScreen>();
            targets.Add(((RectTransform)b.transform, scr != null ? scr.name : "hud"));
        }
        foreach (var b in UnityEngine.Object.FindObjectsByType<OnScreenButton>(FindObjectsInactive.Exclude))
            targets.Add(((RectTransform)b.transform, "hud"));

        // the top modal (one with a scrim) hides everything below it
        UiScreen top = null;
        foreach (var s in UnityEngine.Object.FindObjectsByType<UiScreen>(FindObjectsInactive.Exclude))
            if (top == null || s.transform.GetSiblingIndex() > top.transform.GetSiblingIndex()) top = s;
        bool modal = top != null && top.transform.Find("Scrim") != null;

        var live = targets.Where(t => !modal || t.layer == top.name).ToList();
        foreach (var (rt, layer) in live)
        {
            var sr = ScreenRect(rt, cam);
            r.Checks++;
            if (sr.width < minPx - 0.5f || sr.height < minPx - 0.5f)
                r.Fail(rn, state, $"target {PathOf(rt)} is {sr.width:0}x{sr.height:0} px, below 48 dp ({minPx:0} px)");
            r.Checks++;
            if (!Inside(sr, safe)) r.Fail(rn, state, $"target {PathOf(rt)} leaves the safe area ({sr})");
        }
        for (int i = 0; i < live.Count; i++)
            for (int j = i + 1; j < live.Count; j++)
            {
                if (live[i].layer != live[j].layer && live[i].layer != "hud" && live[j].layer != "hud") continue;
                r.Checks++;
                var a = ScreenRect(live[i].rt, cam);
                var b = ScreenRect(live[j].rt, cam);
                if (Overlaps(a, b)) r.Fail(rn, state, $"targets overlap: {PathOf(live[i].rt)} and {PathOf(live[j].rt)}");
            }

        // HUD blocks must not touch each other or the touch controls
        var hud = UnityEngine.Object.FindAnyObjectByType<HudView>();
        if (hud != null && !modal)
        {
            var blocks = new List<RectTransform>();
            foreach (var n in new[] { "Left", "Skills", "Center", "Right", "Objective" })
            {
                var t = hud.transform.Find(n);
                if (t != null && t.gameObject.activeInHierarchy) blocks.Add((RectTransform)t);
            }
            blocks.Add((RectTransform)hud.pause.transform);
            foreach (var b in UnityEngine.Object.FindObjectsByType<OnScreenButton>(FindObjectsInactive.Exclude)) blocks.Add((RectTransform)b.transform);
            var toast = UnityEngine.Object.FindAnyObjectByType<Toast>();
            if (toast != null && toast.Visible) blocks.Add(toast.box.rectTransform);
            for (int i = 0; i < blocks.Count; i++)
                for (int j = i + 1; j < blocks.Count; j++)
                {
                    r.Checks++;
                    if (Overlaps(ScreenRect(blocks[i], cam), ScreenRect(blocks[j], cam)))
                        r.Fail(rn, state, $"HUD overlap: {PathOf(blocks[i])} and {PathOf(blocks[j])}");
                }
            // skill chips: inside the safe area, 48 dp tall in the touch layout, never on top of each other
            var chipRects = new List<RectTransform>();
            foreach (var chip in hud.chips)
            {
                if (chip == null || !chip.gameObject.activeInHierarchy) continue;
                var cr = ScreenRect((RectTransform)chip.transform, cam);
                chipRects.Add((RectTransform)chip.transform);
                r.Checks++;
                if (hud.TallChips && chip.active && cr.height < minPx - 0.5f) r.Fail(rn, state, $"skill chip {chip.skillId} is {cr.height:0} px tall, below 48 dp ({minPx:0} px)");
                r.Checks++;
                if (!Inside(cr, safe)) r.Fail(rn, state, $"skill chip {chip.skillId} leaves the safe area");
            }
            for (int i = 0; i < chipRects.Count; i++)
                for (int j = i + 1; j < chipRects.Count; j++)
                {
                    r.Checks++;
                    if (Overlaps(ScreenRect(chipRects[i], cam), ScreenRect(chipRects[j], cam))) r.Fail(rn, state, $"skill chips overlap: {chipRects[i].name} and {chipRects[j].name}");
                }
            // the dialog never covers the pause button
            var dlg = UnityEngine.Object.FindAnyObjectByType<DialogBox>();
            if (dlg != null && dlg.IsOpen)
            {
                r.Checks++;
                if (Overlaps(ScreenRect(dlg.box, cam), ScreenRect((RectTransform)hud.pause.transform, cam)))
                    r.Fail(rn, state, "dialog box covers the pause button");
            }
        }

        // no two texts may overlap (in a modal, only the modal's own texts count; the scrim hides the rest)
        var inks = new List<(Rect ink, TMP_Text t)>();
        foreach (var t in UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Exclude))
        {
            if (!t.enabled || string.IsNullOrEmpty(t.text) || t.textBounds.size.x <= 0) continue;
            if (t.GetComponentInParent<CircleWipe>() != null) continue;
            if (modal && !t.transform.IsChildOf(top.transform)) continue;
            Vector2 q0 = RectTransformUtility.WorldToScreenPoint(cam, t.transform.TransformPoint(t.textBounds.min));
            Vector2 q1 = RectTransformUtility.WorldToScreenPoint(cam, t.transform.TransformPoint(t.textBounds.max));
            inks.Add((Rect.MinMaxRect(Mathf.Min(q0.x, q1.x), Mathf.Min(q0.y, q1.y), Mathf.Max(q0.x, q1.x), Mathf.Max(q0.y, q1.y)), t));
        }
        // texts of the open dialog sit over the HUD band only if the dialog is tall; check them against everything
        for (int i = 0; i < inks.Count; i++)
            for (int j = i + 1; j < inks.Count; j++)
            {
                var si = inks[i].t.GetComponentInParent<UiScreen>();
                var sj = inks[j].t.GetComponentInParent<UiScreen>();
                // a lower screen's text under an open dialog box is covered by the box, not overlapping it
                if (si != sj && (si is DialogBox || sj is DialogBox)) continue;
                r.Checks++;
                if (Overlaps(inks[i].ink, inks[j].ink, 0.5f))
                    r.Fail(rn, state, $"texts overlap: {PathOf(inks[i].t.transform)} \"{Short(inks[i].t.text)}\" and {PathOf(inks[j].t.transform)} \"{Short(inks[j].t.text)}\"");
            }

        // text: inside the safe area, not overflowing its rect, inside its panel, readable size, contrast
        foreach (var t in UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Exclude))
        {
            if (!t.enabled || string.IsNullOrEmpty(t.text) || t.GetComponentInParent<Canvas>() == null) continue;
            if (t.GetComponentInParent<CircleWipe>() != null) continue;
            string p = PathOf(t.transform);
            var tr = ScreenRect(t.rectTransform, cam);
            r.Checks++;
            if (t.fontSize < th.tinySize - 0.01f) r.Fail(rn, state, $"text {p} is {t.fontSize}px, below 10 px");
            var b = t.textBounds;
            if (b.size.x > 0)
            {
                var wmin = t.transform.TransformPoint(b.min);
                var wmax = t.transform.TransformPoint(b.max);
                Vector2 a0 = RectTransformUtility.WorldToScreenPoint(cam, wmin), a1 = RectTransformUtility.WorldToScreenPoint(cam, wmax);
                var ink = Rect.MinMaxRect(Mathf.Min(a0.x, a1.x), Mathf.Min(a0.y, a1.y), Mathf.Max(a0.x, a1.x), Mathf.Max(a0.y, a1.y));
                r.Checks++;
                if (!Inside(ink, tr, 3f)) r.Fail(rn, state, $"text overflows its box: {p} \"{Short(t.text)}\" ink {ink.width:0}x{ink.height:0} in {tr.width:0}x{tr.height:0}");
                bool inTouch = t.GetComponentInParent<TouchControls>() != null;
                r.Checks++;
                if (!Inside(ink, safe, 1.5f)) r.Fail(rn, state, $"text outside the safe area: {p} \"{Short(t.text)}\"");
                var panel = PanelOf(t);
                if (panel != null)
                {
                    r.Checks++;
                    if (!Inside(ink, ScreenRect(panel.rectTransform, cam), 2f)) r.Fail(rn, state, $"text clipped by its panel: {p} \"{Short(t.text)}\"");
                }
                // text under a modal scrim is not meant to be read; touch labels sit on their own buttons
                if (!inTouch && (!modal || t.transform.IsChildOf(top.transform))) ContrastCheck(t, ink, th, r, rn, state, p);
            }
        }
    }

    static Image PanelOf(TMP_Text t)
    {
        var th = UiTheme.Load();
        for (var p = t.transform.parent; p != null; p = p.parent)
        {
            var img = p.GetComponent<Image>();
            if (img != null && img.enabled && img.sprite == th.pixelBox && img.color.a > 0.5f) return img;
        }
        return null;
    }

    static Texture2D _behind;

    /// <summary>The background behind a text: the 90th-percentile luminance pixel of the text-free render inside its ink box.</summary>
    static Color Behind(Rect ink)
    {
        var lum = new List<(float l, Color c)>();
        int x0 = Mathf.Clamp((int)ink.xMin, 0, _behind.width - 1), x1 = Mathf.Clamp((int)ink.xMax, 0, _behind.width - 1);
        int y0 = Mathf.Clamp((int)ink.yMin, 0, _behind.height - 1), y1 = Mathf.Clamp((int)ink.yMax, 0, _behind.height - 1);
        int step = Mathf.Max(1, (x1 - x0) / 60);
        for (int y = y0; y <= y1; y += Mathf.Max(1, (y1 - y0) / 12))
            for (int x = x0; x <= x1; x += step)
            {
                var c = _behind.GetPixel(x, y);
                lum.Add((UiTheme.Luminance(c), c));
            }
        if (lum.Count == 0) return Color.black;
        lum.Sort((a, b) => a.l.CompareTo(b.l));
        return lum[Mathf.Min(lum.Count - 1, (int)(lum.Count * 0.9f))].c;
    }

    static void ContrastCheck(TMP_Text t, Rect ink, UiTheme th, Report r, string rn, string state, string p)
    {
        // disabled labels are exempt (WCAG 1.4.3), outlined text sits on its own black outline
        if (ApproximatelyEqual(t.color, th.textDisabled)) return;
        // outlined text carries its own black edge; everything else is measured against what is really behind it
        Color bg = t.outlineWidth >= 0.2f ? Color.black : Behind(ink);
        bool gold = ApproximatelyEqual(t.color, th.gold) || ApproximatelyEqual(t.color, th.goldBright);
        bool heading = t.fontSize >= th.headingSize - 4.01f;
        float need = gold || heading ? 4.5f : 7f;
        float got = UiTheme.Contrast(Blend(t.color, bg), bg);
        r.Checks++;
        if (got < need) r.Fail(rn, state, $"contrast {got:0.0}:1 below {need}:1 for {p} \"{Short(t.text)}\"");
    }

    static Color Blend(Color c, Color under) => new Color(Mathf.Lerp(under.r, c.r, c.a), Mathf.Lerp(under.g, c.g, c.a), Mathf.Lerp(under.b, c.b, c.a), 1);
    static bool ApproximatelyEqual(Color a, Color b) => Mathf.Abs(a.r - b.r) < 0.01f && Mathf.Abs(a.g - b.g) < 0.01f && Mathf.Abs(a.b - b.b) < 0.01f;
    static string Short(string s) { s = s.Replace("\n", " "); return s.Length > 40 ? s.Substring(0, 40) + "..." : s; }

    // ------------------------------------------------------------------ outputs

    static void WriteReport(Report r)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# UI layout audit (M5 screens and M6 skills)");
        sb.AppendLine();
        sb.AppendLine($"Generated by `SkpUiAudit` ({DateTime.Now:yyyy-MM-dd HH:mm}). {r.Checks} checks, {r.Problems.Count} problems, {r.Shots.Count} screenshots.");
        sb.AppendLine();
        sb.AppendLine("Resolutions: " + string.Join(", ", Resolutions.Select(x => $"{x.Name} ({x.Dpi:0} dpi)")) + ".");
        sb.AppendLine("Checks per screen: 48 dp targets, no overlapping targets, HUD/touch/toast separation, safe area, text overflow and panel clipping, text >= 10 px, contrast (7:1 body, 4.5:1 gold and headings), dialog vs pause button.");
        sb.AppendLine();
        if (r.Problems.Count == 0) sb.AppendLine("**No problems.**");
        foreach (var p in r.Problems) sb.AppendLine("- " + p);
        File.WriteAllText(Path.Combine(OutDir, "layout_report.md"), sb.ToString());
    }

    /// <summary>All screens of one resolution on one sheet, 4 per row, each labelled with its state name.</summary>
    static void ContactSheet(string res, List<(string name, Texture2D tex)> shots, Report r)
    {
        const int cols = 4, tw = 480, pad = 8, label = 18;
        int th = Mathf.RoundToInt(tw * shots[0].tex.height / (float)shots[0].tex.width);
        int rows = (shots.Count + cols - 1) / cols;
        int W = cols * tw + (cols + 1) * pad, H = rows * (th + label) + (rows + 1) * pad;
        var sheet = new Texture2D(W, H, TextureFormat.RGB24, false);
        var bgc = new Color32(0x14, 0x10, 0x1c, 0xff);
        var fill = new Color32[W * H];
        for (int i = 0; i < fill.Length; i++) fill[i] = bgc;
        sheet.SetPixels32(fill);
        for (int i = 0; i < shots.Count; i++)
        {
            int cx = pad + (i % cols) * (tw + pad);
            int cyTop = pad + (i / cols) * (th + label + pad);
            var src = shots[i].tex;
            for (int y = 0; y < th; y++)
                for (int x = 0; x < tw; x++)
                    sheet.SetPixel(cx + x, H - 1 - (cyTop + label + y), src.GetPixelBilinear((x + 0.5f) / tw, 1 - (y + 0.5f) / th));
            MiniFont.Draw(sheet, cx + 2, H - 1 - (cyTop + 3), shots[i].name.ToUpperInvariant(), new Color32(0xe8, 0xb8, 0x5a, 0xff), 2);
        }
        sheet.Apply();
        var file = Path.Combine(OutDir, $"contact_sheet_{res}.png");
        File.WriteAllBytes(file, sheet.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(sheet);
        r.Shots.Add(file);
    }

    /// <summary>A 3x5 pixel font for contact-sheet labels (letters, digits, _ and x).</summary>
    static class MiniFont
    {
        static readonly Dictionary<char, string> G = new Dictionary<char, string>
        {
            ['A'] = "010101111101101", ['B'] = "110101110101110", ['C'] = "011100100100011", ['D'] = "110101101101110",
            ['E'] = "111100110100111", ['F'] = "111100110100100", ['G'] = "011100101101011", ['H'] = "101101111101101",
            ['I'] = "111010010010111", ['J'] = "001001001101010", ['K'] = "101101110101101", ['L'] = "100100100100111",
            ['M'] = "101111111101101", ['N'] = "110101101101101", ['O'] = "010101101101010", ['P'] = "110101110100100",
            ['Q'] = "010101101110011", ['R'] = "110101110101101", ['S'] = "011100010001110", ['T'] = "111010010010010",
            ['U'] = "101101101101111", ['V'] = "101101101101010", ['W'] = "101101111111101", ['X'] = "101101010101101",
            ['Y'] = "101101010010010", ['Z'] = "111001010100111", ['0'] = "111101101101111", ['1'] = "010110010010111",
            ['2'] = "110001010100111", ['3'] = "110001010001110", ['4'] = "101101111001001", ['5'] = "111100110001110",
            ['6'] = "011100111101111", ['7'] = "111001010010010", ['8'] = "111101111101111", ['9'] = "111101111001110",
            ['_'] = "000000000000111", [' '] = "000000000000000",
        };

        public static void Draw(Texture2D tex, int x, int yTop, string s, Color32 c, int scale)
        {
            foreach (var ch in s)
            {
                if (G.TryGetValue(ch, out var g))
                    for (int gy = 0; gy < 5; gy++)
                        for (int gx = 0; gx < 3; gx++)
                            if (g[gy * 3 + gx] == '1')
                                for (int sy = 0; sy < scale; sy++)
                                    for (int sx = 0; sx < scale; sx++)
                                        tex.SetPixel(x + gx * scale + sx, yTop - gy * scale - sy, c);
                x += 4 * scale;
            }
        }
    }
}
