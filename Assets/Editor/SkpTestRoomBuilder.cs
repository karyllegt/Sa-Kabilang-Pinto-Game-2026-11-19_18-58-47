// M2 test room and Player prefab. Menu: Sa Kabilang Pinto > Setup > Build Test Room.
// The test room is a dev scene (not in the build list): open Assets/Scenes/Dev/TestRoom.unity and press Play.
using Skp.Core;
using Skp.Dimensions;
using Skp.Levels;
using Skp.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SkpTestRoomBuilder
{
    public const string PlayerPrefabPath = "Assets/Prefabs/Player/Player.prefab";
    public const string TestRoomPath = "Assets/Scenes/Dev/TestRoom.unity";
    public const string DimensionRoomPath = "Assets/Scenes/Dev/DimensionRoom.unity";
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
        BuildDimensionRoom(player);
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

        // M6 skill effects: veil ring, glide wings (siblings of the body so they are placed in world space) and the bolt sprite
        var veil = new GameObject("VeilRing").AddComponent<SpriteRenderer>();
        veil.transform.SetParent(root.transform, false);
        veil.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SkpSkillSetup.VeilRingSprite);
        veil.sortingOrder = 11;
        veil.enabled = false;
        view.veilRing = veil;
        var wings = new GameObject("GlideWings").AddComponent<SpriteRenderer>();
        wings.transform.SetParent(root.transform, false);
        wings.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SkpSkillSetup.GlideWingsSprite);
        wings.sortingOrder = 9;
        wings.enabled = false;
        view.glideWings = wings;
        view.boltSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SkpSkillSetup.BoltSprite);

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

    static void Marker(Transform parent, string name, WorldMarker.Kind kind, Rect r, string spritePath, Dim mask = Dim.All)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = r.position;
        var m = go.AddComponent<WorldMarker>();
        m.kind = kind;
        m.size = r.size;
        m.mask = mask;

        var vis = new GameObject("Visual").AddComponent<SpriteRenderer>();
        vis.transform.SetParent(go.transform, false);
        vis.transform.localPosition = r.size * 0.5f;
        vis.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        vis.drawMode = SpriteDrawMode.Tiled;
        vis.tileMode = SpriteTileMode.Continuous;
        vis.size = r.size;
        vis.sortingOrder = kind == WorldMarker.Kind.Hazard ? 2 : 0;
        var dm = go.AddComponent<DimensionMember>();
        dm.mask = mask;
        dm.ghost = kind == WorldMarker.Kind.Hazard ? DimensionMember.Ghost.Hazard : DimensionMember.Ghost.Ledge;
        dm.renderers = new[] { vis };
    }

    static void DoorAt(Transform parent, string name, DoorKind kind, Dim mask, Dim target, Vector2 bottomLeft, string spritePath,
                       bool hanging = false, Vector2? arrival = null)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = bottomLeft;
        var d = go.AddComponent<DoorMarker>();
        d.kind = kind; d.mask = mask; d.target = target; d.hanging = hanging;
        d.size = kind == DoorKind.Exit ? new Vector2(20f / 16f, 2f) : new Vector2(1f, 28f / 16f);
        d.teleport = arrival.HasValue;
        d.arrival = arrival ?? Vector2.zero;
        var vis = new GameObject("Visual").AddComponent<SpriteRenderer>();
        vis.transform.SetParent(go.transform, false);
        vis.transform.localPosition = d.size * 0.5f;
        vis.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        vis.flipY = hanging;
        vis.sortingOrder = 1;
        var dm = go.AddComponent<DimensionMember>();
        dm.mask = mask;
        dm.ghost = kind == DoorKind.Exit ? DimensionMember.Ghost.AlwaysVisible : DimensionMember.Ghost.Door;
        dm.ghostTint = kind == DoorKind.Enter ? target : Dim.Ordinary;
        dm.renderers = new[] { vis };
    }

    // ------------------------------------------------------------------ M3 dimension room
    // Floor top y = 0. Spirit stairs lead up to an all-dimension ledge with lantern 1.
    // The Inverted door drops Tala UP through three ceiling ledges to a hanging home door
    // that brings her back to lantern 1 in the Waking World.
    public static readonly Rect DimBounds = new Rect(0, -3, 40, 26);
    public const float SpiritDoorX = 6f, InvertDoorX = 30f;
    public static readonly Vector2[] DimLanterns = { new Vector2(2.5f, 0), new Vector2(22.5f, 10) };

    static void BuildDimensionRoom(GameObject playerPrefab)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cam = SkpProjectSetup.CreatePixelCamera();
        var follow = cam.gameObject.AddComponent<CameraFollow>();
        follow.cam = cam;
        follow.bounds = DimBounds;

        var bg = new GameObject("Backdrop").AddComponent<SpriteRenderer>();
        bg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Backgrounds/bg_act1.png");
        bg.sortingOrder = -100;
        bg.transform.SetParent(cam.transform, false);
        bg.transform.localPosition = new Vector3(0, 0, 10);
        var tint = new GameObject("DimensionTint").AddComponent<SpriteRenderer>();
        tint.sortingOrder = -99;
        tint.transform.SetParent(cam.transform, false);
        tint.transform.localPosition = new Vector3(0, 0, 9);
        tint.transform.localScale = new Vector3(80, 50, 1);

        var world = new GameObject("World").AddComponent<LevelWorldAuthoring>();
        world.bounds = DimBounds;
        var w = world.transform;
        const string wood = "Assets/Art/Tiles/tile_wood_ledge48x16.png";
        const string spirit = "Assets/Art/Tiles/tile_spirit_ledge48x16.png";
        const string inv = "Assets/Art/Tiles/tile_inv_ledge48x16.png";
        const string spikes = "Assets/Art/Props/hazard_spike_24x7.png";

        Marker(w, "Floor (all)", WorldMarker.Kind.Ledge, new Rect(0, -3, DimBounds.width, 3), "Assets/Art/Tiles/tile_floor_block_64x48.png");
        // Ceiling (all): the next room's floor in a real level; catches Tala rising in Inverted gravity.
        Marker(w, "Ceiling (all)", WorldMarker.Kind.Ledge, new Rect(0, DimBounds.yMax - 1, DimBounds.width, 1), wood);
        // Ordinary only: one ledge and floor spikes (harmless and see-through in Spirit)
        Marker(w, "Ordinary ledge", WorldMarker.Kind.Ledge, new Rect(11, 2, 4, 1), wood, Dim.Ordinary);
        Marker(w, "Ordinary spikes", WorldMarker.Kind.Hazard, new Rect(16, 0, 2, 7f / 16f), spikes, Dim.Ordinary);
        // Spirit only: stairs up to the lantern ledge
        Marker(w, "Spirit 1", WorldMarker.Kind.Ledge, new Rect(9, 1, 3, 1), spirit, Dim.Spirit);
        Marker(w, "Spirit 2", WorldMarker.Kind.Ledge, new Rect(12.5f, 3, 3, 1), spirit, Dim.Spirit);
        Marker(w, "Spirit 3", WorldMarker.Kind.Ledge, new Rect(16, 5, 3, 1), spirit, Dim.Spirit);
        Marker(w, "Spirit 4", WorldMarker.Kind.Ledge, new Rect(19, 7, 3, 1), spirit, Dim.Spirit);
        Marker(w, "Lantern ledge (all)", WorldMarker.Kind.Ledge, new Rect(20, 9, 5, 1), wood);
        // Inverted only: ceiling ledges, landed on from below
        Marker(w, "Inverted 1", WorldMarker.Kind.Ledge, new Rect(27, 6, 8, 1), inv, Dim.Inverted);
        Marker(w, "Inverted 2", WorldMarker.Kind.Ledge, new Rect(34, 12, 6, 1), inv, Dim.Inverted);
        Marker(w, "Inverted 3", WorldMarker.Kind.Ledge, new Rect(29, 18, 6, 1), inv, Dim.Inverted);
        Marker(w, "Inverted spikes", WorldMarker.Kind.Hazard, new Rect(36.5f, 11, 1.5f, 7f / 16f), spikes, Dim.Inverted);

        DoorAt(w, "Door to Spirit", DoorKind.Enter, Dim.Ordinary, Dim.Spirit, new Vector2(SpiritDoorX, 0), "Assets/Art/Props/door_spirit_16x28.png");
        DoorAt(w, "Door home (Spirit)", DoorKind.Home, Dim.Spirit, Dim.Ordinary, new Vector2(SpiritDoorX, 0), "Assets/Art/Props/door_home_16x28.png");
        DoorAt(w, "Door to Inverted", DoorKind.Enter, Dim.Ordinary, Dim.Inverted, new Vector2(InvertDoorX, 0), "Assets/Art/Props/door_invert_16x28.png");
        DoorAt(w, "Door home (hanging)", DoorKind.Home, Dim.Inverted, Dim.Ordinary, new Vector2(30.5f, 18f - 28f / 16f), "Assets/Art/Props/door_home_16x28.png",
               hanging: true, arrival: DimLanterns[1]);

        for (int i = 0; i < DimLanterns.Length; i++)
        {
            var l = new GameObject($"Lantern {i}").AddComponent<LanternMarker>();
            l.order = i;
            l.transform.SetParent(w, false);
            l.transform.position = DimLanterns[i];
            var flame = new GameObject("Flame").AddComponent<SpriteRenderer>();
            flame.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Items/spr_star.png");
            flame.sortingOrder = 5;
            flame.transform.SetParent(l.transform, false);
            flame.transform.localPosition = new Vector3(0, 0.75f, 0);
            l.flame = flame;
        }

        var playerGo = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
        var pc = playerGo.GetComponent<PlayerController>();
        pc.worldSource = world;
        pc.cameraFollow = follow;

        var shadow = new GameObject("Anino Shadow");
        var ssr = shadow.AddComponent<SpriteRenderer>();
        ssr.sortingOrder = 9;
        var sv = shadow.AddComponent<ShadowView>();
        sv.player = pc;
        sv.body = ssr;
        sv.frames = new[]
        {
            AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Enemies/spr_anino_0.png"),
            AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Enemies/spr_anino_1.png"),
        };

        var director = new GameObject("Dimension Director").AddComponent<DimensionDirector>();
        director.player = pc;
        director.screenTint = tint;

        EditorSceneManager.SaveScene(scene, DimensionRoomPath);
    }
}
