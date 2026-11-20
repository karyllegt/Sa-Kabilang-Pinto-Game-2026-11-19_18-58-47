// Renders a scene's main camera to a PNG in tests/Screenshots (evidence for milestone reports).
// Batch: -executeMethod SkpScreenshots.TestRoom
using System.IO;
using Skp.Levels;
using Skp.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SkpScreenshots
{
    public const string Dir = "tests/Screenshots";

    [MenuItem("Sa Kabilang Pinto/Screenshots/All")]
    public static void All() { TestRoom(); DimensionRoom(); Levels(); }

    [MenuItem("Sa Kabilang Pinto/Screenshots/Test Room")]
    public static void TestRoom()
    {
        EditorSceneManager.OpenScene(SkpTestRoomBuilder.TestRoomPath, OpenSceneMode.Single);
        var cam = Camera.main;
        var world = Object.FindAnyObjectByType<LevelWorldAuthoring>();
        // Frame the whole room once, then the spawn view the player sees.
        var ppc = cam.GetComponent<UnityEngine.Rendering.Universal.PixelPerfectCamera>();
        if (ppc != null) ppc.enabled = false;
        cam.transform.position = new Vector3(world.bounds.center.x, world.bounds.center.y, -10);
        float size = cam.orthographicSize;
        cam.orthographicSize = world.bounds.width / (16f / 9f) / 2f;
        Capture(cam, 1920, 1080, "m2_testroom_overview.png");
        cam.orthographicSize = size;
        if (ppc != null) ppc.enabled = true;

        var follow = cam.GetComponent<Skp.Core.CameraFollow>();
        var pc = Object.FindAnyObjectByType<PlayerController>();
        var w = world.Build();
        var spawn = w.Lanterns[0];
        var center = new Vector2(spawn.x, spawn.y + 7f / 16f);
        var t = follow.Target(center, 1);
        cam.transform.position = new Vector3(t.x, t.y, -10);
        pc.transform.position = new Vector3(spawn.x, spawn.y, 0);
        Capture(cam, 960, 540, "m2_testroom_spawn.png");
    }

    [MenuItem("Sa Kabilang Pinto/Screenshots/Dimension Room")]
    public static void DimensionRoom()
    {
        EditorSceneManager.OpenScene(SkpTestRoomBuilder.DimensionRoomPath, OpenSceneMode.Single);
        var cam = Camera.main;
        var world = Object.FindAnyObjectByType<LevelWorldAuthoring>();
        var director = Object.FindAnyObjectByType<Skp.Dimensions.DimensionDirector>();
        var ppc = cam.GetComponent<UnityEngine.Rendering.Universal.PixelPerfectCamera>();
        if (ppc != null) ppc.enabled = false;
        cam.transform.position = new Vector3(world.bounds.center.x, world.bounds.center.y, -10);
        cam.orthographicSize = world.bounds.height / 2f;
        // the tint follows the camera; stretch it over the whole framed room
        if (director.screenTint != null) director.screenTint.transform.localScale = new Vector3(120, 60, 1);
        foreach (var d in new[] { Skp.Dimensions.Dim.Ordinary, Skp.Dimensions.Dim.Spirit, Skp.Dimensions.Dim.Inverted })
        {
            director.Refresh();
            if (director.screenTint != null && director.screenTint.sprite == null)
                director.screenTint.sprite = Skp.Dimensions.DimensionDirector.WhiteSprite();
            director.Apply(d, force: true);
            Capture(cam, 1600, 1040, $"m3_dimension_room_{d.ToString().ToLowerInvariant()}.png");
        }
    }

    [MenuItem("Sa Kabilang Pinto/Screenshots/Levels 1-3")]
    public static void Levels()
    {
        var dir = Path.Combine(Path.GetTempPath(), "skp_shot_save");
        var save = new Skp.Save.SaveService(dir);
        save.Load();
        for (int lv = 0; lv < 3; lv++)
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Level.unity", OpenSceneMode.Single);
            var runner = Object.FindAnyObjectByType<Skp.Levels.LevelRunner>();
            var session = new Skp.Levels.LevelSession(runner.levels[lv].spec, new Skp.Core.LevelRequest { Level = lv, Difficulty = "standard" },
                runner.tuning, runner.difficulties, runner.curve, save);
            runner.view.Build(session.World, session.Sim);
            runner.backdrop.sprite = runner.view.art.backdrops[runner.levels[lv].spec.act - 1];
            runner.director.Refresh();
            if (runner.director.screenTint != null && runner.director.screenTint.sprite == null)
                runner.director.screenTint.sprite = Skp.Dimensions.DimensionDirector.WhiteSprite();
            var pv = runner.player.view;
            pv.Render(session.Sim, session.Sim.Position, 1f);
            var cam = Camera.main;
            var follow = cam.GetComponent<Skp.Core.CameraFollow>();
            follow.bounds = session.World.Bounds;
            var t = follow.Target(session.Sim.Center, 1);
            cam.transform.position = new Vector3(t.x, t.y, -10);
            Capture(cam, 960, 540, $"m4_level{lv + 1}_spawn.png");

            var ppc = cam.GetComponent<UnityEngine.Rendering.Universal.PixelPerfectCamera>();
            if (ppc != null) ppc.enabled = false;
            var b = session.World.Bounds;
            cam.transform.position = new Vector3(b.center.x, b.center.y, -10);
            cam.orthographicSize = b.height / 2f;
            runner.backdrop.enabled = false;
            Capture(cam, Mathf.RoundToInt(1400 * b.width / b.height), 1400, $"m4_level{lv + 1}_tower.png");
        }
        if (Directory.Exists(dir)) Directory.Delete(dir, true);
    }

    public static void Capture(Camera cam, int w, int h, string file)
    {
        Directory.CreateDirectory(Dir);
        var rt = new RenderTexture(w, h, 24);
        var prev = cam.targetTexture;
        cam.targetTexture = rt;
        cam.Render(); // warm-up: the first render after a scene opens in batch mode comes out dark
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        cam.targetTexture = prev;
        RenderTexture.active = null;
        File.WriteAllBytes(Path.Combine(Dir, file), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        rt.Release();
        Debug.Log("[SkpScreenshots] wrote " + file);
    }
}
