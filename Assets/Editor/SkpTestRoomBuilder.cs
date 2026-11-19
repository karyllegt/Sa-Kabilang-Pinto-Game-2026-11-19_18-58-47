// M2 test room and Player prefab. Menu: Sa Kabilang Pinto > Setup > Build Test Room.
// The test room is a dev scene (not in the build list): open Assets/Scenes/Dev/TestRoom.unity and press Play.
using Skp.Core;
using Skp.Levels;
using Skp.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SkpTestRoomBuilder
{
    public const string PlayerPrefabPath = "Assets/Prefabs/Player/Player.prefab";
    public const string TestRoomPath = "Assets/Scenes/Dev/TestRoom.unity";
    public const string TuningPath = "Assets/Data/PlayerTuning.asset";
    public const string DifficultyPath = "Assets/Data/DifficultyTable.asset";

    // Layout in tiles (1 unit = 16 px). Floor top is y = 0.
    public static readonly Rect Bounds = new Rect(0, -3, 48, 24);
    public static readonly (float x, float h, float w)[] Steps =
    {
        (6, 1, 4), (12, 2, 4), (18, 3, 4), (24, 4, 4), (30, 7, 4),
    };
    public static readonly Rect Spikes = new Rect(37, 0, 3, 7f / 16f);
    public static readonly Vector2[] Lanterns = { new Vector2(2, 0), new Vector2(32, 7), new Vector2(45, 0) };

    [MenuItem("Sa Kabilang Pinto/Setup/Build Test Room")]
    public static void Build()
    {
        SkpProjectSetup.EnsureFolder("Assets/Data");
        SkpProjectSetup.CreateIfMissing<PlayerTuning>(TuningPath);
        SkpProjectSetup.CreateIfMissing<DifficultyTable>(DifficultyPath);
        var player = BuildPlayerPrefab();
        BuildTestRoom(player);
    }

    static GameObject BuildPlayerPrefab()
    {
        SkpProjectSetup.EnsureFolder("Assets/Prefabs/Player");
        var root = new GameObject("Player");
        var pc = root.AddComponent<PlayerController>();
        pc.tuning = AssetDatabase.LoadAssetAtPath<PlayerTuning>(TuningPath);
        pc.difficulties = AssetDatabase.LoadAssetAtPath<DifficultyTable>(DifficultyPath);

        var sprite = new GameObject("Sprite");
        sprite.transform.SetParent(root.transform, false);
        var sr = sprite.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 10;
        var view = sprite.AddComponent<PlayerView>();
        view.body = sr;
        view.girl = Frames("girl");
        view.boy = Frames("boy");
        sr.sprite = view.girl[0];
        pc.view = view;

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    static Sprite[] Frames(string who)
    {
        var s = new Sprite[4];
        for (int i = 0; i < 4; i++) s[i] = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/Characters/spr_pl_{who}_{i}.png");
        return s;
    }

    static void BuildTestRoom(GameObject playerPrefab)
    {
        SkpProjectSetup.EnsureFolder("Assets/Scenes/Dev");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cam = SkpProjectSetup.CreatePixelCamera();
        var follow = cam.gameObject.AddComponent<CameraFollow>();
        follow.cam = cam;
        follow.bounds = Bounds;

        // Backdrop rides with the camera.
        var bg = new GameObject("Backdrop").AddComponent<SpriteRenderer>();
        bg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Backgrounds/bg_act1.png");
        bg.sortingOrder = -100;
        bg.transform.SetParent(cam.transform, false);
        bg.transform.localPosition = new Vector3(0, 0, 10);

        var world = new GameObject("World").AddComponent<LevelWorldAuthoring>();
        world.bounds = Bounds;

        Marker(world.transform, "Floor", WorldMarker.Kind.Ledge, new Rect(0, -3, Bounds.width, 3), "Assets/Art/Tiles/tile_floor_block_64x48.png");
        foreach (var s in Steps)
            Marker(world.transform, $"Ledge {s.h:0} tiles", WorldMarker.Kind.Ledge, new Rect(s.x, s.h - 1, s.w, 1), "Assets/Art/Tiles/tile_wood_ledge48x16.png");
        Marker(world.transform, "Spikes", WorldMarker.Kind.Hazard, Spikes, "Assets/Art/Props/hazard_spike_24x7.png");

        for (int i = 0; i < Lanterns.Length; i++)
        {
            var l = new GameObject($"Lantern {i}").AddComponent<LanternMarker>();
            l.order = i;
            l.transform.SetParent(world.transform, false);
            l.transform.position = Lanterns[i];
            var flame = new GameObject("Flame").AddComponent<SpriteRenderer>();
            flame.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Items/spr_star.png"); // placeholder until the lantern art (M4)
            flame.sortingOrder = 5;
            flame.transform.SetParent(l.transform, false);
            flame.transform.localPosition = new Vector3(0, 0.75f, 0);
            l.flame = flame;
        }

        var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
        var pc = player.GetComponent<PlayerController>();
        pc.worldSource = world;
        pc.cameraFollow = follow;

        EditorSceneManager.SaveScene(scene, TestRoomPath);
    }

    static void Marker(Transform parent, string name, WorldMarker.Kind kind, Rect r, string spritePath)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = r.position;
        var m = go.AddComponent<WorldMarker>();
        m.kind = kind;
        m.size = r.size;

        var vis = new GameObject("Visual").AddComponent<SpriteRenderer>();
        vis.transform.SetParent(go.transform, false);
        vis.transform.localPosition = r.size * 0.5f;
        vis.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        vis.drawMode = SpriteDrawMode.Tiled;
        vis.tileMode = SpriteTileMode.Continuous;
        vis.size = r.size;
        vis.sortingOrder = kind == WorldMarker.Kind.Hazard ? 2 : 0;
    }
}
