// M5 UI setup: theme, string table, wipe material, the few new pixel sprites, and the Title, Map and
// Level UI. Every screen is built by its own Build() (Assets/Scripts/UI) and saved as a prefab under
// Assets/Prefabs/UI/Screens; the scenes hold connected instances. Idempotent.
// Menu: Sa Kabilang Pinto > Setup > Run M5 UI Setup. Batch: -executeMethod SkpUiSetup.RunAll
using System.IO;
using Skp.Core;
using Skp.Levels;
using Skp.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

public static class SkpUiSetup
{
    public const string ThemePath = "Assets/Resources/UI/UiTheme.asset";
    public const string StringsPath = "Assets/Resources/UI/Strings_en.asset";
    public const string WipeMaterialPath = "Assets/Resources/UI/CircleWipe.mat";
    public const string WhitePath = "Assets/Art/UI/ui_white.png";
    public const string BellPath = "Assets/Art/UI/map_node_bell.png";
    public const string MapBlendedPath = "Assets/Art/Backgrounds/bg_map_overworld_blended.png";
    public const string ScreensDir = "Assets/Prefabs/UI/Screens";

    [MenuItem("Sa Kabilang Pinto/Setup/Run M5 UI Setup")]
    public static void RunAll()
    {
        // The full setup rebuilds the touch prefab and the Level scene (which calls BuildLevelUi).
        SkpProjectSetup.RunAll();
    }

    // ------------------------------------------------------------------ assets

    public static void CreateUiAssets()
    {
        SkpProjectSetup.EnsureFolder("Assets/Resources/UI");
        SkpProjectSetup.EnsureFolder(ScreensDir);
        WritePng(WhitePath, 1, 1, (x, y) => Color.white);
        WriteBell();
        WriteBlendedMap();

        var shader = Shader.Find("Skp/UI/CircleWipe");
        var mat = AssetDatabase.LoadAssetAtPath<Material>(WipeMaterialPath);
        if (mat == null && shader != null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, WipeMaterialPath); }
        if (mat != null) { mat.shader = shader; mat.SetColor("_Color", SkpProjectSetup.Shadow); EditorUtility.SetDirty(mat); }

        var th = AssetDatabase.LoadAssetAtPath<UiTheme>(ThemePath);
        if (th == null) { th = ScriptableObject.CreateInstance<UiTheme>(); AssetDatabase.CreateAsset(th, ThemePath); }
        th.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SkpProjectSetup.FontAssetPath);
        th.fontOutline = OutlineMaterial(th.font);
        th.pixelBox = S(SkpProjectSetup.PixelBoxSprite);
        th.heartFull = S("Assets/Art/UI/ui_heart_full.png");
        th.heartEmpty = S("Assets/Art/UI/ui_heart_empty.png");
        th.white = S(WhitePath);
        th.portraitLola = S("Assets/Art/UI/Portraits/portrait_lola.png");
        th.portraitBoy = S("Assets/Art/UI/Portraits/portrait_player_boy.png");
        th.portraitGirl = S("Assets/Art/UI/Portraits/portrait_player_girl.png");
        th.portraitClue = S("Assets/Art/UI/Portraits/portrait_clue.png");
        th.portraitAnino = S("Assets/Art/UI/Portraits/portrait_anino.png");
        th.portraitBoss = S("Assets/Art/UI/Portraits/portrait_boss.png");
        th.titleArt = S("Assets/Art/UI/title_art.png");
        th.cardBoy = S("Assets/Art/UI/char_card_boy.png");
        th.cardGirl = S("Assets/Art/UI/char_card_girl.png");
        th.menuBackdrop = S("Assets/Art/Backgrounds/spr_menubg.png");
        EditorUtility.SetDirty(th);

        // Strings: add any missing keys; never overwrite text someone edited in the asset.
        var st = AssetDatabase.LoadAssetAtPath<StringTable>(StringsPath);
        if (st == null) { st = ScriptableObject.CreateInstance<StringTable>(); st.ResetToDefaults(); AssetDatabase.CreateAsset(st, StringsPath); }
        else
        {
            foreach (var kv in StringDefaults.En)
                if (st.Get(kv.Key) == null) st.entries.Add(new StringTable.Entry { key = kv.Key, text = kv.Value });
            EditorUtility.SetDirty(st);
        }
        AssetDatabase.SaveAssets();
        UiTheme.ClearCache();
        Loc.Table = st;
    }

    const string OutlineMaterialPath = "Assets/Resources/Fonts/PressStart2P SDF Outline.mat";

    static Material OutlineMaterial(TMP_FontAsset font)
    {
        if (font == null) return null;
        var m = AssetDatabase.LoadAssetAtPath<Material>(OutlineMaterialPath);
        if (m == null) { m = new Material(font.material); AssetDatabase.CreateAsset(m, OutlineMaterialPath); }
        m.shader = font.material.shader;
        m.SetTexture("_MainTex", font.atlasTexture);
        m.EnableKeyword("OUTLINE_ON");
        m.SetFloat("_OutlineWidth", 0.25f);
        m.SetColor("_OutlineColor", Color.black);
        EditorUtility.SetDirty(m);
        return m;
    }

    static bool Same(byte[] a, byte[] b)
    {
        if (a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
        return true;
    }

    static Sprite S(string path)
    {
        var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (s == null) Debug.LogError("[SkpUiSetup] missing sprite " + path);
        return s;
    }

    static void WritePng(string path, int w, int h, System.Func<int, int, Color32> px)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) tex.SetPixel(x, y, px(x, y));
        tex.Apply();
        var bytes = tex.EncodeToPNG();
        Object.DestroyImmediate(tex);
        if (File.Exists(path) && Same(File.ReadAllBytes(path), bytes)) return;
        File.WriteAllBytes(path, bytes);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
    }

    /// <summary>The bell for the last map node (a known art gap), 16x20, the prototype's bell drawn as pixels.</summary>
    static void WriteBell()
    {
        // rows top to bottom; o outline, g gold, h highlight, d dark gold, . clear
        string[] rows =
        {
            ".......oo.......",
            "......oddo......",
            ".....oggggo.....",
            "....oghgggdo....",
            "...oghggggddo...",
            "...oghggggddo...",
            "..oghgggggggdo..",
            "..oghgggggggdo..",
            "..oghgggggggdo..",
            ".oghggggggggddo.",
            ".oghggggggggddo.",
            ".oggggggggggggo.",
            "oggggggggggggggo",
            "oddddddddddddddo",
            ".oooooooooooooo.",
            "......oddo......",
            "......ohho......",
            ".......oo.......",
            "................",
            "................",
        };
        var o = new Color32(0x2a, 0x1c, 0x10, 0xff);
        var g = new Color32(0xe8, 0xb8, 0x5a, 0xff);
        var hl = new Color32(0xff, 0xf3, 0xb0, 0xff);
        var d = new Color32(0xa8, 0x72, 0x2e, 0xff);
        WritePng(BellPath, 16, 20, (x, y) =>
        {
            char c = rows[19 - y][x];
            return c == 'o' ? o : c == 'g' ? g : c == 'h' ? hl : c == 'd' ? d : new Color32(0, 0, 0, 0);
        });
    }

    /// <summary>
    /// The provided 1440x270 map has hard color steps where the acts meet (docs/04 known issue 4). This writes
    /// a blended copy: within 64 px of each seam every row is shifted toward the midpoint of the two acts'
    /// colors at the seam (details are kept, only the base color slides), with a 2x2 ordered dither so the
    /// gradient stays pixel art.
    /// </summary>
    static void WriteBlendedMap()
    {
        const string src = "Assets/Art/Backgrounds/bg_map_overworld.png";
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(File.ReadAllBytes(src));
        int w = tex.width, h = tex.height;
        var px = tex.GetPixels32();
        const int R = 64;
        float[] dither = { 0f, 0.5f, 0.75f, 0.25f };
        foreach (int seam in new[] { w / 3, 2 * w / 3 })
        {
            for (int y = 0; y < h; y++)
            {
                // average a few columns on each side for a stable base color
                Color left = Color.clear, right = Color.clear;
                for (int k = 1; k <= 4; k++) { left += (Color)px[y * w + seam - k]; right += (Color)px[y * w + seam + k - 1]; }
                left /= 4; right /= 4;
                var mid = (left + right) / 2;
                for (int dx = -R; dx < R; dx++)
                {
                    int x = seam + dx;
                    float t = 1f - Mathf.Abs(dx + 0.5f) / R; // 1 at the seam, 0 at R
                    t = t * t * (3 - 2 * t);
                    var baseC = dx < 0 ? left : right;
                    Color c = px[y * w + x];
                    var shifted = c + (mid - baseC) * t;
                    // quantize the shift to the dither pattern
                    float dz = dither[(x & 1) + 2 * (y & 1)] / 255f;
                    shifted.r = Mathf.Floor(Mathf.Clamp01(shifted.r) * 255f + dz * 255f) / 255f;
                    shifted.g = Mathf.Floor(Mathf.Clamp01(shifted.g) * 255f + dz * 255f) / 255f;
                    shifted.b = Mathf.Floor(Mathf.Clamp01(shifted.b) * 255f + dz * 255f) / 255f;
                    shifted.a = c.a;
                    px[y * w + x] = shifted;
                }
            }
        }
        var outTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        outTex.SetPixels32(px);
        outTex.Apply();
        var bytes = outTex.EncodeToPNG();
        Object.DestroyImmediate(tex);
        Object.DestroyImmediate(outTex);
        if (File.Exists(MapBlendedPath) && Same(File.ReadAllBytes(MapBlendedPath), bytes)) return;
        File.WriteAllBytes(MapBlendedPath, bytes);
        AssetDatabase.ImportAsset(MapBlendedPath, ImportAssetOptions.ForceUpdate);
    }

    // ------------------------------------------------------------------ helpers

    /// <summary>A screen canvas (960x540 reference, match 0.5) with a safe-area child for content.</summary>
    public static (GameObject canvas, RectTransform safe) UiCanvas(string name, int order)
    {
        var canvas = SkpProjectSetup.NewCanvas(name, order);
        var safe = UiKit.Rect(canvas.transform, "SafeArea");
        UiKit.Stretch(safe);
        safe.gameObject.AddComponent<SafeAreaFitter>();
        return (canvas, safe);
    }

    public static void EventSystemObject()
    {
        var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        es.GetComponent<InputSystemUIInputModule>().actionsAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(SkpProjectSetup.ActionsPath);
    }

    /// <summary>Saves a built screen as a prefab and keeps the scene object connected to it.</summary>
    static T Prefab<T>(T screen) where T : Component
    {
        var go = screen.gameObject;
        bool active = go.activeSelf;
        go.SetActive(true);
        var path = $"{ScreensDir}/{go.name}.prefab";
        PrefabUtility.SaveAsPrefabAssetAndConnect(go, path, InteractionMode.AutomatedAction);
        go.SetActive(active);
        return screen;
    }

    static SpriteRenderer Backdrop(string spritePath, Vector3 pos)
    {
        var bg = new GameObject("Background").AddComponent<SpriteRenderer>();
        bg.sprite = S(spritePath);
        bg.sortingOrder = -100;
        bg.transform.position = pos;
        return bg;
    }

    // ------------------------------------------------------------------ scenes

    public static void BuildTitleScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cam = SkpProjectSetup.CreatePixelCamera();
        // dimmed so text placed straight on the backdrop (headings, descriptions, footer) keeps its contrast
        Backdrop("Assets/Art/Backgrounds/spr_menubg.png", Vector3.zero).color = new Color(0.42f, 0.42f, 0.42f, 1);
        var (canvas, safe) = UiCanvas("MenuCanvas", 0);
        var flow = canvas.AddComponent<TitleFlow>();
        flow.title = Prefab(TitleScreen.Build(safe));
        flow.characters = Prefab(CharacterScreen.Build(safe));
        flow.difficulty = Prefab(DifficultyScreen.Build(safe));
        flow.settings = Prefab(SettingsScreen.Build(safe));
        flow.howTo = Prefab(HowToScreen.Build(safe));
        flow.credits = Prefab(CreditsScreen.Build(safe));
        EventSystemObject();
        EditorSceneManager.SaveScene(scene, $"{SkpProjectSetup.ScenesDir}/{SceneNames.Title}.unity");
    }

    public static void BuildMapScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cam = SkpProjectSetup.CreatePixelCamera();
        // map pixels (0..1440, 0..270) map to world (0..90, 0..16.875); the sprite pivot is its center
        var bg = Backdrop(MapBlendedPath, new Vector3(1440f / 32f, 270f / 32f, 0));
        var (canvas, safe) = UiCanvas("MapCanvas", 0);
        var mc = new GameObject("Map").AddComponent<MapController>();
        mc.cam = cam;
        mc.background = bg;
        mc.screen = Prefab(MapScreen.Build(safe));
        mc.dialog = Prefab(DialogBox.Build(safe));
        mc.skillsScreen = Prefab(SkillsScreen.Build(safe));
        mc.white = S(WhitePath);
        mc.star = S("Assets/Art/Items/spr_star.png");
        mc.bell = S(BellPath);
        mc.lola = S("Assets/Art/Npc/spr_lola.png");
        for (int i = 0; i < 4; i++)
        {
            mc.boyFrames[i] = S($"Assets/Art/Characters/spr_pl_boy_{i}.png");
            mc.girlFrames[i] = S($"Assets/Art/Characters/spr_pl_girl_{i}.png");
        }
        mc.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SkpProjectSetup.FontAssetPath);
        for (int i = 0; i < 9; i++) mc.levels[i] = AssetDatabase.LoadAssetAtPath<LevelConfig>(SkpLevelSetup.LevelPath(i));
        EventSystemObject();
        EditorSceneManager.SaveScene(scene, $"{SkpProjectSetup.ScenesDir}/{SceneNames.Map}.unity");
    }

    /// <summary>Called by SkpLevelSetup.BuildLevelScene with the scene open.</summary>
    public static void BuildLevelUi(LevelRunner runner, GameObject touchPrefab)
    {
        var (canvas, safe) = UiCanvas("LevelCanvas", 10);
        var ui = canvas.AddComponent<LevelUi>();
        ui.hud = Prefab(HudView.Build(safe));
        ui.toast = Prefab(Toast.Build(safe));
        ui.dialog = Prefab(DialogBox.Build(safe));
        ui.pause = Prefab(PauseScreen.Build(safe));
        ui.settings = Prefab(SettingsScreen.Build(safe));
        ui.clear = Prefab(LevelClearScreen.Build(safe));
        ui.gameOver = Prefab(GameOverScreen.Build(safe));
        ui.ending = Prefab(EndingScreen.Build(safe));
        runner.ui = ui;
        EventSystemObject();
        if (touchPrefab != null)
        {
            var t = (GameObject)PrefabUtility.InstantiatePrefab(touchPrefab);
            ui.touch = t.GetComponent<TouchControls>();
        }
    }
}
