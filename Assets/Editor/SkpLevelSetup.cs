// M4: level data assets (9 LevelConfigs, 8 SkillDefinitions, LevelArt) and the Level scene.
using Skp.Core;
using Skp.Dimensions;
using Skp.Levels;
using Skp.Player;
using Skp.Skills;
using Skp.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

public static class SkpLevelSetup
{
    public const string LevelsDir = "Assets/Data/Levels";
    public const string SkillsDir = "Assets/Data/Skills";
    public const string LevelArtPath = "Assets/Data/LevelArt.asset";

    public static string LevelPath(int i) => $"{LevelsDir}/LevelConfig_{i + 1:00}.asset";
    public static string SkillPath(string id) => $"{SkillsDir}/Skill_{id}.asset";

    [MenuItem("Sa Kabilang Pinto/Setup/Create Level Data")]
    public static void CreateDataAssets()
    {
        SkpProjectSetup.EnsureFolder(LevelsDir);
        SkpProjectSetup.EnsureFolder(SkillsDir);

        foreach (var e in SkillCatalog.Defaults)
        {
            var path = SkillPath(e.id);
            var s = AssetDatabase.LoadAssetAtPath<SkillDefinition>(path);
            if (s != null) continue; // keep Inspector edits
            s = ScriptableObject.CreateInstance<SkillDefinition>();
            s.id = e.id; s.displayName = e.name; s.filipinoName = e.filipino; s.description = e.description;
            s.tag = e.tag; s.color = e.color; s.type = e.type;
            AssetDatabase.CreateAsset(s, path);
        }

        var specs = LevelCatalog.Defaults();
        for (int i = 0; i < specs.Length; i++)
        {
            var path = LevelPath(i);
            var c = AssetDatabase.LoadAssetAtPath<LevelConfig>(path);
            if (c != null) continue; // keep Inspector edits
            c = ScriptableObject.CreateInstance<LevelConfig>();
            c.index = i;
            c.spec = specs[i];
            if (!string.IsNullOrEmpty(specs[i].skill)) c.skill = AssetDatabase.LoadAssetAtPath<SkillDefinition>(SkillPath(specs[i].skill));
            AssetDatabase.CreateAsset(c, path);
        }

        var art = AssetDatabase.LoadAssetAtPath<LevelArt>(LevelArtPath);
        bool isNew = art == null;
        if (isNew) art = ScriptableObject.CreateInstance<LevelArt>();
        art.wood = S("Tiles/tile_wood_ledge48x16"); art.spirit = S("Tiles/tile_spirit_ledge48x16"); art.inverted = S("Tiles/tile_inv_ledge48x16");
        art.crumble = S("Tiles/tile_crumble_ledge48x16"); art.moving = S("Tiles/tile_moving_ledge48x16"); art.floorBlock = S("Tiles/tile_floor_block_64x48");
        art.doorSpirit = S("Props/door_spirit_16x28"); art.doorInvert = S("Props/door_invert_16x28"); art.doorHome = S("Props/door_home_16x28");
        art.exitLocked = S("Props/door_exit_locked_20x32"); art.exitOpen = S("Props/door_exit_open_20x32");
        art.spike = S("Props/hazard_spike_24x7"); art.spikeGauntlet = S("Props/hazard_spike_gauntlet_72x7");
        art.thorns = S("Props/hazard_thorns_16x130"); art.thornsBurnt = S("Props/hazard_thorns_burnt_16x130");
        art.rootDormant = S("Props/hazard_root_dormant_12x16"); art.rootWarning = S("Props/hazard_root_warning_12x16"); art.rootStrike = S("Props/hazard_root_strike_12x16");
        art.flame = S("Items/spr_star"); art.shard = S("Items/spr_shard"); art.clue = S("Items/spr_clue");
        art.anino = new[] { S("Enemies/spr_anino_0"), S("Enemies/spr_anino_1") };
        art.tikbalang = new[] { S("Enemies/spr_tik_0"), S("Enemies/spr_tik_1") };
        art.manananggal = new[] { S("Enemies/spr_manan_0"), S("Enemies/spr_manan_1") };
        art.backdrops = new[] { S("Backgrounds/bg_act1"), S("Backgrounds/bg_act2"), S("Backgrounds/bg_act3") };
        if (isNew) AssetDatabase.CreateAsset(art, LevelArtPath); else EditorUtility.SetDirty(art);
        AssetDatabase.SaveAssets();
    }

    static Sprite S(string rel) => AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/{rel}.png");

    public static void BuildLevelScene(TMP_FontAsset font, GameObject touchPrefab)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cam = SkpProjectSetup.CreatePixelCamera();
        var follow = cam.gameObject.AddComponent<CameraFollow>();
        follow.cam = cam;

        var bg = new GameObject("Backdrop").AddComponent<SpriteRenderer>();
        bg.sprite = S("Backgrounds/bg_act1");
        bg.sortingOrder = -100;
        bg.transform.SetParent(cam.transform, false);
        bg.transform.localPosition = new Vector3(0, 0, 10);
        var tint = new GameObject("DimensionTint").AddComponent<SpriteRenderer>();
        tint.sortingOrder = -99;
        tint.transform.SetParent(cam.transform, false);
        tint.transform.localPosition = new Vector3(0, 0, 9);
        tint.transform.localScale = new Vector3(80, 50, 1);

        var playerGo = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(SkpTestRoomBuilder.PlayerPrefabPath));
        var pc = playerGo.GetComponent<PlayerController>();
        pc.cameraFollow = follow;

        var shadow = new GameObject("Anino Shadow");
        var ssr = shadow.AddComponent<SpriteRenderer>();
        ssr.sortingOrder = 9;
        var sv = shadow.AddComponent<ShadowView>();
        sv.player = pc; sv.body = ssr;
        sv.frames = new[] { S("Enemies/spr_anino_0"), S("Enemies/spr_anino_1") };

        var director = new GameObject("Dimension Director").AddComponent<DimensionDirector>();
        director.player = pc;
        director.screenTint = tint;

        var runnerGo = new GameObject("Level");
        var view = runnerGo.AddComponent<LevelView>();
        view.art = AssetDatabase.LoadAssetAtPath<LevelArt>(LevelArtPath);
        var runner = runnerGo.AddComponent<LevelRunner>();
        runner.levels = new LevelConfig[9];
        for (int i = 0; i < 9; i++) runner.levels[i] = AssetDatabase.LoadAssetAtPath<LevelConfig>(LevelPath(i));
        runner.tuning = AssetDatabase.LoadAssetAtPath<PlayerTuning>(SkpTestRoomBuilder.TuningPath);
        runner.difficulties = AssetDatabase.LoadAssetAtPath<DifficultyTable>(SkpTestRoomBuilder.DifficultyPath);
        runner.curve = AssetDatabase.LoadAssetAtPath<DifficultyCurve>("Assets/Data/DifficultyCurve.asset");
        runner.player = pc;
        runner.view = view;
        runner.director = director;
        runner.backdrop = bg;

        // M5 UI: HUD, dialogs, pause, Level Clear, Game Over, Ending, touch controls.
        SkpUiSetup.BuildLevelUi(runner, touchPrefab);

        EditorSceneManager.SaveScene(scene, $"{SkpProjectSetup.ScenesDir}/{SceneNames.Level}.unity");
    }
}
