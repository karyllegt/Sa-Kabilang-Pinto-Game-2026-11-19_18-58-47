using System;
using System.IO;
using System.Linq;
using Skp.Dimensions;
using Skp.Levels;
using Skp.Player;
using Skp.Preview;
using Skp.Save;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class SkpSmoothEnvironmentPreview
{
    const string Output = "tests/Screenshots/smooth/environments";
    const string InteractionOutput = "tests/Screenshots/smooth/interactions";

    [MenuItem("Sa Kabilang Pinto/Smooth Art/Environments/Open Act I")]
    static void OpenActI() => Open(SmoothEnvironmentPreview.ScenePaths[0]);
    [MenuItem("Sa Kabilang Pinto/Smooth Art/Environments/Open Act II")]
    static void OpenActII() => Open(SmoothEnvironmentPreview.ScenePaths[1]);
    [MenuItem("Sa Kabilang Pinto/Smooth Art/Environments/Open Act III")]
    static void OpenActIII() => Open(SmoothEnvironmentPreview.ScenePaths[2]);

    [MenuItem("Sa Kabilang Pinto/Smooth Art/Interactions/Open Act I")]
    static void OpenInteractionActI() => Open(SmoothInteractionPreview.ScenePaths[0]);
    [MenuItem("Sa Kabilang Pinto/Smooth Art/Interactions/Open Act II")]
    static void OpenInteractionActII() => Open(SmoothInteractionPreview.ScenePaths[1]);
    [MenuItem("Sa Kabilang Pinto/Smooth Art/Interactions/Open Act III")]
    static void OpenInteractionActIII() => Open(SmoothInteractionPreview.ScenePaths[2]);

    static void Open(string scenePath)
    {
        if (!EditorApplication.isPlayingOrWillChangePlaymode && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            EditorSceneManager.OpenScene(scenePath);
    }

    [MenuItem("Sa Kabilang Pinto/Smooth Art/Environments/Capture All Three Acts")]
    static void CaptureMenu() => RunCapture(CaptureBatch);

    [MenuItem("Sa Kabilang Pinto/Smooth Art/Interactions/Capture Gameplay Views")]
    static void CaptureInteractionMenu() => RunCapture(CaptureInteractions);

    static void RunCapture(Action capture)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var previous = EditorSceneManager.GetSceneManagerSetup();
        try { capture(); }
        finally { EditorSceneManager.RestoreSceneManagerSetup(previous); }
    }

    public static void CaptureBatch()
    {
        Directory.CreateDirectory(Output);
        Capture(0, Dim.Ordinary, "act-i-house-towers");
        Capture(1, Dim.Ordinary, "act-ii-blackwater-well");
        Capture(2, Dim.Ordinary, "act-iii-bell-tower");
        Capture(0, Dim.Spirit, "spirit-scenery");
        Capture(0, Dim.Inverted, "inverted-scenery");
        Debug.Log("[Smooth environments] Saved three act views and both dimension views to " + Output);
    }

    public static void CaptureInteractions()
    {
        Directory.CreateDirectory(InteractionOutput);
        Capture(0, Dim.Ordinary, "doors-and-ward", true);
        Capture(1, Dim.Ordinary, "water-veil-and-bolt", true);
        Capture(2, Dim.Ordinary, "falling-bell-and-dash", true);
        Capture(0, Dim.Spirit, "spirit-transition", true);
        Capture(0, Dim.Inverted, "inverted-transition", true);
        Debug.Log("[Smooth interactions] Saved five views to " + InteractionOutput);
    }

    static void Capture(int act, Dim dimension, string name, bool interactions = false)
    {
        var paths = interactions ? SmoothInteractionPreview.ScenePaths : SmoothEnvironmentPreview.ScenePaths;
        EditorSceneManager.OpenScene(paths[act]);
        var runner = Object.FindAnyObjectByType<LevelRunner>();
        var environment = Object.FindAnyObjectByType<SmoothEnvironmentPreview>();
        var smooth = Object.FindAnyObjectByType<SmoothActIPreview>();
        var save = new SaveService(SmoothActIPreview.NewSaveDirectory());
        var request = environment.MakeRequest(save);
        var spec = runner.levels[request.Level].spec;
        var session = new LevelSession(spec, request, runner.tuning, runner.difficulties, runner.curve, save);
        var sim = session.Sim;
        var interaction = Object.FindAnyObjectByType<SmoothInteractionPreview>();
        if (interaction != null) interaction.ApplyVisuals();

        if (dimension != Dim.Ordinary)
        {
            sim.SetDimension(dimension);
            var ledge = session.World.Ledges.Where(l => l.Mask == dimension && l.Rect.width >= 2f)
                .Where(l => !session.World.Hazards.Any(h => (h.Mask & dimension) != 0 &&
                    h.Rect.Overlaps(PlayerOnLedge(l, dimension, sim.Size))))
                .OrderBy(l => (l.Rect.center - sim.Center).sqrMagnitude).FirstOrDefault();
            if (ledge == null) throw new InvalidOperationException("No ledge in preview dimension " + dimension);
            sim.Position = PlayerOnLedge(ledge, dimension, sim.Size).position;
        }
        else if (interactions)
        {
            // Pose an ordinary campaign checkpoint with real input and a sample of the bell's drop for review.
            sim.Position += Vector2.right * 1.1f;
            sim.Step(new PlayerInputFrame
            {
                WardPressed = act == 0, VeilPressed = act == 1, BoltPressed = act == 1,
                DashPressed = act == 2, MoveX = act == 2 ? 1 : 0,
            });
            if (act == 1)
                for (int i = 0; i < 5; i++) sim.Step(default);
            if (act == 2)
            {
                var bell = session.World.Bells.OrderBy(b => Mathf.Abs(b.TopY - sim.Center.y)).FirstOrDefault();
                if (bell != null) { bell.Falling = true; bell.Y = (bell.TopY + bell.GroundY) * 0.5f; }
            }
        }

        const int width = 1280, height = 720;
        var camera = runner.player.cameraFollow.cam;
        camera.aspect = width / (float)height;
        runner.view.act = spec.act;
        runner.view.Build(session.World, sim);
        runner.backdrop.sprite = runner.view.art.backdrops[spec.act - 1];
        if (runner.director.screenTint.sprite == null)
            runner.director.screenTint.sprite = DimensionDirector.WhiteSprite();
        runner.director.Refresh();
        runner.director.Apply(dimension, true);
        runner.player.Attach(sim);
        if (interaction != null && dimension != Dim.Ordinary) interaction.ShowTransition(sim, dimension);
        if (interaction != null)
        {
            runner.player.view.Tick(sim);
            interaction.Draw(sim);
        }
        runner.player.view.Render(sim, sim.Position, 1f);
        runner.view.SendMessage("LateUpdate");
        environment.DrawScenery(dimension);
        if (session.World.Flood != null)
            camera.transform.position = new Vector3(camera.transform.position.x, sim.Center.y - 3.15f, camera.transform.position.z);

        var hud = runner.ui.hud;
        hud.gameObject.SetActive(true);
        hud.SetLevel(session);
        hud.Show(session);
        if (smooth != null) smooth.SetHearts(hud, sim.Vitality);
        foreach (var canvas in Object.FindObjectsByType<Canvas>())
        {
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
        }

        var target = new RenderTexture(width, height, 24);
        var image = new Texture2D(width, height, TextureFormat.RGB24, false);
        var previous = RenderTexture.active;
        try
        {
            camera.targetTexture = target;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            camera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            image.Apply(false);
            File.WriteAllBytes(Path.Combine(interactions ? InteractionOutput : Output, name + ".png"), image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = null;
            RenderTexture.active = previous;
            target.Release();
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(image);
        }
    }

    static Rect PlayerOnLedge(Ledge ledge, Dim dimension, Vector2 size)
    {
        var surface = dimension == Dim.Inverted ? ledge.Rect.yMin - size.y : ledge.Rect.yMax;
        return new Rect(ledge.Rect.center.x - size.x * 0.5f, surface, size.x, size.y);
    }
}

