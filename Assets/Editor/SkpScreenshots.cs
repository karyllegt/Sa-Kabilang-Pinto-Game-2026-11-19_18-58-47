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

    public static void Capture(Camera cam, int w, int h, string file)
    {
        Directory.CreateDirectory(Dir);
        var rt = new RenderTexture(w, h, 24);
        var prev = cam.targetTexture;
        cam.targetTexture = rt;
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
