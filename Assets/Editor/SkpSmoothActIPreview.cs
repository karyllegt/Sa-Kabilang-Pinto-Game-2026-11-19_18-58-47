using System;
using System.IO;
using System.Linq;
using System.Text;
using Skp.Dimensions;
using Skp.Levels;
using Skp.Player;
using Skp.Preview;
using Skp.Save;
using Skp.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class SkpSmoothActIPreview
{
    const string BaseScene = "Assets/Scenes/Level.unity";
    const string Art = "Assets/Art/Smooth/ActI";
    const string Sources = "reference/Smooth/ActI";
    const string TalaArt = "Assets/Art/Smooth/Tala";
    const string Screens = "tests/Screenshots/smooth/act-i";
    enum Fit { Stretch, Contain }

    [InitializeOnLoadMethod]
    static void SetUpSaveIsolation()
    {
        EditorApplication.playModeStateChanged += change =>
        {
            if (change == PlayModeStateChange.ExitingEditMode)
            {
                var start = EditorSceneManager.playModeStartScene;
                string path = start != null ? AssetDatabase.GetAssetPath(start) : SceneManager.GetActiveScene().path;
                bool preview = path == SmoothActIPreview.ScenePath || path == SmoothCreaturePreview.LevelScenePath ||
                    path == SmoothCreaturePreview.MapScenePath || Array.IndexOf(SmoothEnvironmentPreview.ScenePaths, path) >= 0 ||
                    Array.IndexOf(SmoothInteractionPreview.ScenePaths, path) >= 0 || path == SmoothUiPreview.ScenePath;
                SessionState.SetBool(SmoothActIPreview.SessionKey, preview);
                if (preview) Skp.Core.GameRoot.SaveDirectoryOverride = SmoothActIPreview.NewSaveDirectory();
            }
            if (change == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(SmoothActIPreview.SessionKey, false))
            {
                SessionState.EraseBool(SmoothActIPreview.SessionKey);
                Skp.Core.GameRoot.SaveDirectoryOverride = null;
            }
        };
    }

    [MenuItem("Sa Kabilang Pinto/Smooth Art/Open Act I Preview")]
    static void Open()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            EditorSceneManager.OpenScene(SmoothActIPreview.ScenePath);
    }

    [MenuItem("Sa Kabilang Pinto/Smooth Art/Open Creature Encounters")]
    static void OpenCreatureEncounters()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            EditorSceneManager.OpenScene(SmoothCreaturePreview.LevelScenePath);
    }

    [MenuItem("Sa Kabilang Pinto/Smooth Art/Open Lola Map Preview")]
    static void OpenLolaPreview()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            EditorSceneManager.OpenScene(SmoothCreaturePreview.MapScenePath);
    }

    [MenuItem("Sa Kabilang Pinto/Smooth Art/Build and Capture Act I Preview")]
    static void BuildFromEditor()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var setup = EditorSceneManager.GetSceneManagerSetup();
        try { BuildAndCapture(); }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
    }

    // Run in a Unity batch editor with graphics, in a project copy.
    public static void BuildAndCapture()
    {
        Directory.CreateDirectory(Art + "/Textures");
        Directory.CreateDirectory(Screens);
        AssetDatabase.Refresh();

        var before = OpenLevel(BaseScene);
        string gameplay = Capture(before, null, 2400, 1080, "baseline-wide.png", out string baselineAlignment);
        File.Copy(BaseScene, SmoothActIPreview.ScenePath, true);
        AssetDatabase.ImportAsset(SmoothActIPreview.ScenePath, ImportAssetOptions.ForceSynchronousImport);
        var runner = OpenLevel(SmoothActIPreview.ScenePath);
        var report = new StringBuilder("# Act I smooth art alignment\n\n")
            .AppendLine("Sizes and pivots are measured from the Unity Sprite data. Action clips retain their frame counts, ticks per frame and loop flags.")
            .AppendLine().AppendLine("| Visual | World size before | World size after | Normalized pivot before | Normalized pivot after |")
            .AppendLine("| --- | --- | --- | --- | --- |");
        var art = Object.Instantiate(runner.view.art);
        var catalog = Object.Instantiate(runner.view.Catalog);
        var backdrop = LoadSource("capiz-background.png");
        var props = LoadSource("props-sheet.png");
        try
        {
            art.backdrops = (Sprite[])art.backdrops.Clone();
            art.backdrops[0] = Make("CapizBackground", art.backdrops[0], backdrop, new RectInt(0, 0, backdrop.width, backdrop.height), Fit.Stretch, report);
            runner.view.art = SaveAsset(art, "LevelArt");
            catalog.ledgeByAct = (LevelPiece[])catalog.ledgeByAct.Clone();
            catalog.ledgeByAct[0] = Piece(catalog.ledgeByAct[0], props, 0, "WoodPlatform", Fit.Stretch, report);
            catalog.lantern = Piece(catalog.lantern, props, 1, "CheckpointParol", Fit.Contain, report);
            catalog.spike = Piece(catalog.spike, props, 2, "IronSpikes", Fit.Stretch, report);
            runner.view.catalog = SaveAsset(catalog, "LevelPrefabCatalog");

            var playerView = runner.player.view;
            var girl = AssetDatabase.LoadAssetAtPath<PlayerAnimSet>(TalaArt + "/TalaSmoothGirl.asset");
            var boy = AssetDatabase.LoadAssetAtPath<PlayerAnimSet>(TalaArt + "/TalaSmoothBoy.asset");
            if (girl == null || boy == null) throw new InvalidOperationException("Smooth Tala animation sets are missing");
            playerView.girlAnims = girl;
            playerView.boyAnims = boy;
            PrefabUtility.RecordPrefabInstancePropertyModifications(playerView);

            var heart = Make("VitalityHeart", UiTheme.Load().heartFull, props, TrimCell(props, 2, 2, 3), Fit.Contain, report);
            var camera = runner.player.cameraFollow.cam;
            camera.GetComponent<PixelPerfectCamera>().enabled = false;
            var driver = runner.gameObject.AddComponent<SmoothActIPreview>();
            driver.Configure(runner, heart);
            EditorSceneManager.MarkSceneDirty(runner.gameObject.scene);
            if (!EditorSceneManager.SaveScene(runner.gameObject.scene)) throw new IOException("Could not save Act I preview");
            AssetDatabase.SaveAssets();

            string revised = Capture(runner, driver, 2400, 1080, "smooth-wide.png", out string previewAlignment);
            if (gameplay != revised || baselineAlignment != previewAlignment)
                throw new InvalidOperationException("Base and preview generated different gameplay or renderer footprints");
            runner = OpenLevel(SmoothActIPreview.ScenePath);
            Capture(runner, runner.GetComponent<SmoothActIPreview>(), 1280, 720, "smooth-16x9.png", out _);
            report.AppendLine().AppendLine("The paired wide captures use the same Level 1 Standard checkpoint and orthographic framing.")
                .AppendLine("Player hitbox, checkpoint, world bounds, ledge rectangles and hazard rectangles match between scenes.")
                .AppendLine().Append(previewAlignment);
            File.WriteAllText(Screens + "/alignment.md", report.ToString());
            Debug.Log("[Smooth Act I] Scene and captures saved. Gameplay and renderer footprint comparisons passed.");
        }
        finally
        {
            Object.DestroyImmediate(backdrop);
            Object.DestroyImmediate(props);
        }
    }

    public static void CaptureSavedPreview()
    {
        Directory.CreateDirectory(Screens);
        var baseline = Capture(OpenLevel(BaseScene), null, 2400, 1080, "baseline-wide.png", out string before);
        var runner = OpenLevel(SmoothActIPreview.ScenePath);
        var preview = Capture(runner, runner.GetComponent<SmoothActIPreview>(), 2400, 1080, "smooth-wide.png", out string after);
        if (baseline != preview || before != after)
            throw new InvalidOperationException("Base and preview generated different gameplay or renderer footprints");
        runner = OpenLevel(SmoothActIPreview.ScenePath);
        Capture(runner, runner.GetComponent<SmoothActIPreview>(), 1280, 720, "smooth-16x9.png", out _);
        File.WriteAllText(Screens + "/alignment.md", "# Act I smooth art alignment\n\nMatching gameplay and renderer footprints in Unity.\n\n" + after);
        Debug.Log("[Smooth Act I] Saved preview captures and verified matching world geometry.");
    }

    static LevelRunner OpenLevel(string path)
    {
        EditorSceneManager.OpenScene(path);
        return Object.FindAnyObjectByType<LevelRunner>() ?? throw new InvalidOperationException(path + " has no LevelRunner");
    }

    static Texture2D LoadSource(string name)
    {
        var texture = new Texture2D(2, 2);
        if (!texture.LoadImage(File.ReadAllBytes(Path.Combine(Sources, name))))
            throw new IOException("Could not read " + name);
        return texture;
    }

    static RectInt TrimCell(Texture2D src, int columns, int rows, int cell)
    {
        int col = cell % columns, row = rows - 1 - cell / columns;
        int left = src.width * col / columns, right = src.width * (col + 1) / columns;
        int bottom = src.height * row / rows, top = src.height * (row + 1) / rows;
        var pixels = src.GetPixels(left, bottom, right - left, top - bottom);
        int minX = right, minY = top, maxX = -1, maxY = -1, i = 0;
        for (int y = bottom; y < top; ++y)
        for (int x = left; x < right; ++x)
        {
            if (pixels[i++].a < 0.22f) continue;
            minX = Mathf.Min(minX, x); minY = Mathf.Min(minY, y);
            maxX = Mathf.Max(maxX, x); maxY = Mathf.Max(maxY, y);
        }
        if (maxX < 0) throw new InvalidOperationException("Empty illustrated cell " + cell);
        return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    static Sprite Make(string name, Sprite original, Texture2D src, RectInt rect, Fit fit, StringBuilder report)
    {
        int factor = Mathf.Max(1, Mathf.CeilToInt(rect.height / original.rect.height));
        int w = Mathf.RoundToInt(original.rect.width * factor), h = Mathf.RoundToInt(original.rect.height * factor);
        var pivot = original.pivot / original.rect.size;
        float sx = w / (float)rect.width, sy = h / (float)rect.height;
        float x0 = 0f, y0 = 0f;
        if (fit == Fit.Contain)
        {
            sx = sy = Mathf.Min(sx, sy);
            x0 = (w - rect.width * sx) / 2f;
            y0 = (h - rect.height * sy) / 2f;
        }

        var png = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var pixels = new Color32[w * h];
        for (int y = 0; y < h; ++y)
        for (int x = 0; x < w; ++x)
        {
            float u = (x + 0.5f - x0) / sx, v = (y + 0.5f - y0) / sy;
            if (u >= 0 && u < rect.width && v >= 0 && v < rect.height)
                pixels[y * w + x] = src.GetPixelBilinear((rect.x + u) / src.width, (rect.y + v) / src.height);
        }
        png.SetPixels32(pixels);
        png.Apply(false);
        string path = Art + "/Textures/" + name + ".png";
        File.WriteAllBytes(path, png.EncodeToPNG());
        Object.DestroyImmediate(png);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.textureType = TextureImporterType.Sprite;
        settings.spriteMode = (int)SpriteImportMode.Single;
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = pivot;
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteExtrude = 0;
        settings.spritePixelsPerUnit = original.pixelsPerUnit * factor;
        settings.spriteBorder = original.border * factor;
        importer.SetTextureSettings(settings);
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.maxTextureSize = 4096;
        importer.SaveAndReimport();

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null || Vector2.Distance(sprite.bounds.size, original.bounds.size) > 0.0001f ||
            Vector2.Distance(sprite.pivot / sprite.rect.size, pivot) > 0.0001f ||
            Vector4.Distance(sprite.border / factor, original.border) > 0.0001f)
            throw new InvalidOperationException(name + " does not match the baseline sprite geometry");
        report.AppendLine($"| {name} | {Size(original.bounds.size)} | {Size(sprite.bounds.size)} | {Size(pivot)} | {Size(sprite.pivot / sprite.rect.size)} |");
        return sprite;
    }

    static LevelPiece Piece(LevelPiece source, Texture2D sheet, int cell, string name, Fit fit, StringBuilder report)
    {
        var root = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(source));
        try
        {
            var piece = root.GetComponent<LevelPiece>();
            piece.main.sprite = Make(name, piece.main.sprite, sheet, TrimCell(sheet, 2, 2, cell), fit, report);
            return PrefabUtility.SaveAsPrefabAsset(root, Art + "/" + name + ".prefab").GetComponent<LevelPiece>();
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    static T SaveAsset<T>(T data, string name) where T : ScriptableObject
    {
        string path = Art + "/" + name + ".asset";
        var existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing == null) { AssetDatabase.CreateAsset(data, path); return data; }
        EditorUtility.CopySerialized(data, existing);
        EditorUtility.SetDirty(existing);
        Object.DestroyImmediate(data);
        return existing;
    }

    static string Capture(LevelRunner runner, SmoothActIPreview smooth, int width, int height, string file, out string alignment)
    {
        var request = SmoothActIPreview.FirstCheckpoint();
        var session = new LevelSession(runner.levels[0].spec, request, runner.tuning, runner.difficulties, runner.curve,
            new SaveService(SmoothActIPreview.NewSaveDirectory()));
        var sim = session.Sim;
        var cam = runner.player.cameraFollow.cam;
        cam.GetComponent<PixelPerfectCamera>().enabled = false;
        cam.aspect = width / (float)height;
        runner.view.act = 1;
        runner.view.Build(session.World, sim);
        runner.backdrop.sprite = runner.view.art.backdrops[0];
        runner.director.Refresh();
        runner.director.Apply(sim.Dimension, true);
        // Match the actual checkpoint location. Render one grounded view before the next simulation tick.
        sim.Grounded = true;
        runner.player.Attach(sim);
        runner.player.view.Render(sim, sim.Position, 1f);
        runner.view.SendMessage("LateUpdate");
        var hud = runner.ui.hud;
        hud.gameObject.SetActive(true);
        hud.SetLevel(session);
        hud.Show(session);
        if (smooth != null) smooth.SetHearts(hud, sim.Vitality);
        foreach (var canvas in Object.FindObjectsByType<Canvas>())
        {
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 1f;
        }

        var rt = new RenderTexture(width, height, 24);
        var image = new Texture2D(width, height, TextureFormat.RGB24, false);
        var previous = RenderTexture.active;
        try
        {
            cam.targetTexture = rt;
            Canvas.ForceUpdateCanvases();
            cam.Render();
            cam.Render();
            RenderTexture.active = rt;
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply(false);
            File.WriteAllBytes(Path.Combine(Screens, file), image.EncodeToPNG());
        }
        finally
        {
            cam.targetTexture = null;
            RenderTexture.active = previous;
            rt.Release();
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(image);
        }

        var footprint = new StringBuilder()
            .AppendLine($"- Tala hitbox size: {Size(sim.Size)} world units.")
            .AppendLine($"- Idle sprite renderer size: {Size(runner.player.view.body.bounds.size)} world units.")
            .AppendLine($"- First checkpoint position: {Size(session.World.Lanterns[1])}.")
            .AppendLine($"- HUD heart size: {Size(hud.hearts[0].rectTransform.rect.size)} canvas units.")
            .AppendLine($"- Compared {session.World.Ledges.Count} ledges and {session.World.Hazards.Count} hazards.");
        // The first instances of each replacement retain their render positions and displayed dimensions.
        foreach (var (prefix, replacement) in new[] { ("Ledge_Act1_Wood", "WoodPlatform"), ("Lantern", "CheckpointParol"), ("Hazard_Spike", "IronSpikes") })
        {
            var piece = runner.view.Root.GetComponentsInChildren<LevelPiece>()
                .First(p => p.name.StartsWith(prefix + "(", StringComparison.Ordinal) || p.name.StartsWith(replacement + "(", StringComparison.Ordinal));
            footprint.AppendLine($"- {prefix} renderer size: {Size(piece.main.bounds.size)} world units. Center: {Size(piece.main.bounds.center)}.");
        }
        alignment = footprint.ToString();
        return sim.Hitbox.ToString("F4") + session.World.Bounds.ToString("F4") + string.Join("|",
            session.World.Ledges.Select(l => l.Rect.ToString("F4")).Concat(session.World.Hazards.Select(h => h.Rect.ToString("F4"))));
    }

    static string Size(Vector2 v) => $"({v.x:F4}, {v.y:F4})";
}

