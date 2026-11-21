// M1 project setup. Idempotent: safe to run again, it rebuilds the generated assets.
// Menu: Sa Kabilang Pinto > Setup > Run M1 Setup. Batch: -executeMethod SkpProjectSetup.RunAll
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Skp.Core;
using Skp.Levels;
using Skp.Player;
using Skp.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

public static class SkpProjectSetup
{
    public const string ActionsPath = "Assets/Settings/Input/SkpControls.inputactions";
    public const string FontPath = "Assets/Art/Fonts/PressStart2P-Regular.ttf";
    public const string FontAssetPath = "Assets/Resources/Fonts/PressStart2P SDF.asset";
    public const string PixelBoxSprite = "Assets/Art/UI/ui_pixelbox_9slice_border6.png";
    public const string PixelBoxPrefab = "Assets/Prefabs/UI/PixelBox.prefab";
    public const string TouchPrefab = "Assets/Prefabs/UI/TouchControls.prefab";
    public const string ScenesDir = "Assets/Scenes";
    const string ProjectWideActionsKey = "com.unity.input.settings.actions";

    public static readonly Color32 Panel = new Color32(0x14, 0x10, 0x1c, 0xff);
    public static readonly Color32 Gold = new Color32(0xe8, 0xb8, 0x5a, 0xff);
    public static readonly Color32 Text = new Color32(0xf6, 0xe7, 0xc1, 0xff);
    public static readonly Color32 Shadow = new Color32(0x0a, 0x07, 0x10, 0xff);

    [MenuItem("Sa Kabilang Pinto/Setup/Run M1 Setup")]
    public static void RunAll()
    {
        ImportTmpEssentials();
        SetProjectWideActions();
        SetLandscapeOnly();
        KeepGitFriendlyVersionControl();
        CreateDataAssets();
        var font = CreateFontAsset();
        CreatePixelBoxPrefab();
        SkpUiSetup.CreateUiAssets();
        SkpSkillSetup.CreateAssets();    // skill data, icons, fx sprites and sounds (the HUD and the player use them)
        var touch = CreateTouchControlsPrefab(font);
        SkpTestRoomBuilder.Build();      // Player prefab first: the Level scene uses it
        SkpLevelSetup.CreateDataAssets();
        BuildScenes(font, touch);
        AssetDatabase.SaveAssets();
        foreach (var o in AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset"))
            AssetDatabase.SaveAssetIfDirty(o);
        Debug.Log("[SkpProjectSetup] M1 setup complete.");
    }

    // ---------------------------------------------------------------- settings

    static void ImportTmpEssentials()
    {
        if (AssetDatabase.FindAssets("t:TMP_Settings").Length > 0) return;
        var info = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.ugui");
        var pkg = info == null ? null : Directory.GetFiles(info.resolvedPath, "TMP Essential Resources.unitypackage", SearchOption.AllDirectories).FirstOrDefault();
        if (pkg == null) { Debug.LogError("[SkpProjectSetup] TMP Essential Resources package not found in com.unity.ugui"); return; }
        UnityEditor.AssetPackage.Package.Import(pkg, false);
        AssetDatabase.Refresh();
    }

    static void SetProjectWideActions()
    {
        var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsPath);
        if (asset == null) { Debug.LogError("[SkpProjectSetup] Missing " + ActionsPath); return; }
        EditorBuildSettings.AddConfigObject(ProjectWideActionsKey, asset, true);
        InputSystem.actions = asset;
    }

    static void SetLandscapeOnly()
    {
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        // Player Settings are only written to disk when the settings object is dirty.
        foreach (var o in AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset"))
            EditorUtility.SetDirty(o);
    }

    // A package (Collab/Unity Version Control) switches this on open; the repo uses git.
    static void KeepGitFriendlyVersionControl()
    {
        if (VersionControlSettings.mode != "Visible Meta Files")
            VersionControlSettings.mode = "Visible Meta Files";
    }

    static void CreateDataAssets()
    {
        EnsureFolder("Assets/Data");
        CreateIfMissing<PlayerTuning>("Assets/Data/PlayerTuning.asset");
        CreateIfMissing<DifficultyCurve>("Assets/Data/DifficultyCurve.asset");
        CreateIfMissing<DifficultyTable>("Assets/Data/DifficultyTable.asset");
    }

    // ---------------------------------------------------------------- UI assets

    static TMP_FontAsset CreateFontAsset()
    {
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (existing != null) return existing;
        var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
        if (font == null) { Debug.LogError("[SkpProjectSetup] Missing " + FontPath); return null; }
        EnsureFolder(Path.GetDirectoryName(FontAssetPath).Replace('\\', '/'));
        var fa = TMP_FontAsset.CreateFontAsset(font, 32, 4, GlyphRenderMode.SDFAA, 512, 512, AtlasPopulationMode.Dynamic, true);
        fa.name = "PressStart2P SDF";
        AssetDatabase.CreateAsset(fa, FontAssetPath);
        fa.material.name = fa.name + " Material";
        AssetDatabase.AddObjectToAsset(fa.material, fa);
        fa.atlasTextures[0].name = fa.name + " Atlas";
        AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);
        // Pre-bake the characters the game uses (ASCII plus Filipino and typographic marks).
        var chars = new string(Enumerable.Range(32, 95).Select(i => (char)i).ToArray()) + "ñÑ’‘“”…—";
        fa.TryAddCharacters(chars, out _);
        EditorUtility.SetDirty(fa);
        AssetDatabase.SaveAssets();
        return fa;
    }

    static void CreatePixelBoxPrefab()
    {
        EnsureFolder("Assets/Prefabs/UI");
        var go = new GameObject("PixelBox", typeof(RectTransform), typeof(Image));
        var img = go.GetComponent<Image>();
        img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PixelBoxSprite);
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 0.5f; // 6 px border drawn at 12 px on the 960x540 reference canvas (2x the 480x270 pixel grid)
        ((RectTransform)go.transform).sizeDelta = new Vector2(200, 80);
        PrefabUtility.SaveAsPrefabAsset(go, PixelBoxPrefab);
        Object.DestroyImmediate(go);
    }

    struct TouchSpec { public string name, label, path; public Vector2 anchor, pos, size; }

    static GameObject CreateTouchControlsPrefab(TMP_FontAsset font)
    {
        EnsureFolder("Assets/Prefabs/UI");
        var root = NewCanvas("TouchControls", 100);
        var tc = root.AddComponent<TouchControls>();
        var buttons = new GameObject("Buttons", typeof(RectTransform));
        buttons.transform.SetParent(root.transform, false);
        Stretch((RectTransform)buttons.transform);
        buttons.AddComponent<SafeAreaFitter>(); // thumbs stay clear of notches and rounded corners
        tc.buttons = buttons;

        // Thumb layout (docs/04 known issue 1, M5 and M6): left thumb moves; right thumb has JUMP in the corner with
        // DASH and WARD beside it on the bottom row. The second row holds BOLT above DASH and VEIL above WARD (shown
        // once learned) and DOOR above JUMP only while Tala stands at a door. Two rows at most.
        // Sizes at the 960x540 reference; every button is at least UiKit.MinTarget (48 dp on a 6.4" phone).
        // Pause lives in the HUD (top-right).
        var specs = new[]
        {
            new TouchSpec { name = "Left",     label = "<",    path = "<Gamepad>/dpad/left",    anchor = new Vector2(0, 0), pos = new Vector2(72, 70),    size = new Vector2(104, 96) },
            new TouchSpec { name = "Right",    label = ">",    path = "<Gamepad>/dpad/right",   anchor = new Vector2(0, 0), pos = new Vector2(192, 70),   size = new Vector2(104, 96) },
            new TouchSpec { name = "Jump",     label = "JUMP", path = "<Gamepad>/buttonSouth",  anchor = new Vector2(1, 0), pos = new Vector2(-76, 72),   size = new Vector2(112, 104) },
            new TouchSpec { name = "Dash",     label = "DASH", path = "<Gamepad>/leftShoulder", anchor = new Vector2(1, 0), pos = new Vector2(-200, 64),  size = new Vector2(104, 88) },
            new TouchSpec { name = "Ward",     label = "WARD", path = "<Gamepad>/buttonNorth",  anchor = new Vector2(1, 0), pos = new Vector2(-324, 64),  size = new Vector2(104, 88) },
            new TouchSpec { name = "Bolt",     label = "BOLT", path = "<Gamepad>/buttonWest",   anchor = new Vector2(1, 0), pos = new Vector2(-200, 170), size = new Vector2(104, 80) },
            new TouchSpec { name = "Veil",     label = "VEIL", path = "<Gamepad>/rightShoulder", anchor = new Vector2(1, 0), pos = new Vector2(-324, 170), size = new Vector2(104, 80) },
            new TouchSpec { name = "Door",     label = "DOOR", path = "<Gamepad>/buttonEast",   anchor = new Vector2(1, 0), pos = new Vector2(-76, 178),  size = new Vector2(112, 76) },
        };
        var box = AssetDatabase.LoadAssetAtPath<Sprite>(PixelBoxSprite);
        var slots = new List<TouchControls.Slot>();
        foreach (var s in specs)
        {
            var b = new GameObject(s.name, typeof(RectTransform), typeof(Image));
            b.transform.SetParent(buttons.transform, false);
            var rt = (RectTransform)b.transform;
            rt.anchorMin = rt.anchorMax = s.anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = s.pos;
            rt.sizeDelta = s.size;
            var img = b.GetComponent<Image>();
            img.sprite = box;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 0.5f;
            img.color = new Color(1, 1, 1, 0.55f);
            var osb = b.AddComponent<OnScreenButton>();
            osb.controlPath = s.path;
            AddLabel(b.transform, s.label, font, 14);
            slots.Add(new TouchControls.Slot { rect = rt, anchor = s.anchor, position = s.pos });
            if (s.name == "Door") tc.door = b;
            if (s.name == "Veil") { tc.veil = b; b.SetActive(false); }
            if (s.name == "Bolt") { tc.bolt = b; b.SetActive(false); }
        }
        tc.slots = slots.ToArray();
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, TouchPrefab);
        Object.DestroyImmediate(root);
        return prefab;
    }

    // ---------------------------------------------------------------- scenes

    static void BuildScenes(TMP_FontAsset font, GameObject touchPrefab)
    {
        EnsureFolder(ScenesDir);
        BuildScene(SceneNames.Boot, null, font, "", null, null, null, isBoot: true);
        SkpUiSetup.BuildTitleScene();
        SkpUiSetup.BuildMapScene();
        SkpLevelSetup.BuildLevelScene(font, touchPrefab);

        // Boot first; SampleScene leaves the build list (the file itself is kept).
        EditorBuildSettings.scenes = SceneNames.All
            .Select(n => new EditorBuildSettingsScene($"{ScenesDir}/{n}.unity", true))
            .ToArray();
    }

    static void BuildScene(string name, string backgroundPath, TMP_FontAsset font, string label,
                           string next, string back, GameObject touchPrefab, bool isBoot = false)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreatePixelCamera();

        if (isBoot)
        {
            new GameObject("Boot").AddComponent<BootLoader>();
        }
        else
        {
            if (backgroundPath != null)
            {
                var bg = new GameObject("Background").AddComponent<SpriteRenderer>();
                bg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(backgroundPath);
                bg.sortingOrder = -100;
            }
            var screen = new GameObject("Screen").AddComponent<PlaceholderScreen>();
            screen.nextScene = next;
            screen.backScene = back;

            var canvas = NewCanvas("ScreenCanvas", 0);
            var panel = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PixelBoxPrefab), canvas.transform);
            var prt = (RectTransform)panel.transform;
            prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0f);
            prt.pivot = new Vector2(0.5f, 0f);
            prt.anchoredPosition = new Vector2(0, 24);
            prt.sizeDelta = new Vector2(640, 120);
            AddLabel(panel.transform, label, font, 14);

            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            es.GetComponent<InputSystemUIInputModule>().actionsAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsPath);

            if (touchPrefab != null) PrefabUtility.InstantiatePrefab(touchPrefab);
        }
        EditorSceneManager.SaveScene(scene, $"{ScenesDir}/{name}.unity");
    }

    public static Camera CreatePixelCamera()
    {
        var go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        var cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 270f / 2f / SkpAssetImportSettings.PixelsPerUnit;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Shadow;
        go.transform.position = new Vector3(0, 0, -10);
        go.AddComponent<UniversalAdditionalCameraData>();
        var ppc = go.AddComponent<PixelPerfectCamera>();
        ppc.assetsPPU = SkpAssetImportSettings.PixelsPerUnit;
        ppc.refResolutionX = 480;
        ppc.refResolutionY = 270;
        ppc.gridSnapping = PixelPerfectCamera.GridSnapping.UpscaleRenderTexture;
        ppc.cropFrame = PixelPerfectCamera.CropFrame.None;
        return cam;
    }

    // ---------------------------------------------------------------- helpers

    public static GameObject NewCanvas(string name, int sortOrder)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var c = go.GetComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = sortOrder;
        c.pixelPerfect = false;
        var s = go.GetComponent<CanvasScaler>();
        s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        s.referenceResolution = new Vector2(960, 540);
        s.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        s.matchWidthOrHeight = 0.5f;
        // 16 PPU sprites: one sprite pixel = one reference unit before the per-image multiplier
        s.referencePixelsPerUnit = SkpAssetImportSettings.PixelsPerUnit;
        return go;
    }

    public static void AddLabel(Transform parent, string text, TMP_FontAsset font, float size)
    {
        var go = new GameObject("Label", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        Stretch(rt);
        rt.offsetMin = new Vector2(12, 8);
        rt.offsetMax = new Vector2(-12, -8);
        var t = go.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = Text;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.raycastTarget = false;
    }

    public static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    public static void CreateIfMissing<T>(string path) where T : ScriptableObject
    {
        if (AssetDatabase.LoadAssetAtPath<T>(path) != null) return;
        AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<T>(), path);
    }

    public static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
