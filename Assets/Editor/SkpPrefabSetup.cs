// R1: every level, boss and map object as a prefab, plus the LevelPrefabCatalog that maps each kind (and act) to its
// prefab. Each prefab is built with exactly the sprite, sorting order, colour, draw mode and dimension rule the views used
// to set in code, so nothing looks different. Idempotent: saving over a prefab path keeps its GUID, so the catalog and
// scene references survive a re-run. Called from SkpProjectSetup.RunAll (Run M1 Setup) after the level data exists.
using System.Collections.Generic;
using Skp.Core;
using Skp.Dimensions;
using Skp.Enemies;
using Skp.Levels;
using Skp.Player;
using Skp.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SkpPrefabSetup
{
    public const string LevelDir = "Assets/Prefabs/Level";
    public const string BossDir = "Assets/Prefabs/Boss";
    public const string MapDir = "Assets/Prefabs/Map";
    public const string CatalogPath = "Assets/Resources/Level/LevelPrefabCatalog.asset";

    const string White = SkpUiSetup.WhitePath; // 1x1 px at 16 PPU: scale it in pixels

    [MenuItem("Sa Kabilang Pinto/Setup/Rebuild Level Prefabs (R1)")]
    public static void CreateAll()
    {
        foreach (var d in new[] { LevelDir, BossDir, MapDir, "Assets/Resources/Level" }) SkpProjectSetup.EnsureFolder(d);
        var art = AssetDatabase.LoadAssetAtPath<LevelArt>(SkpLevelSetup.LevelArtPath);
        if (art == null) { Debug.LogError("[SkpPrefabSetup] LevelArt missing: run the level setup first"); return; }

        var cat = AssetDatabase.LoadAssetAtPath<LevelPrefabCatalog>(CatalogPath);
        bool isNew = cat == null;
        if (isNew) cat = ScriptableObject.CreateInstance<LevelPrefabCatalog>();

        BuildLevel(cat, art);
        BuildBoss(cat, art);
        BuildMap(cat);

        if (isNew) AssetDatabase.CreateAsset(cat, CatalogPath);
        EditorUtility.SetDirty(cat);
        AssetDatabase.SaveAssets();
        WirePlayerBolt(cat);
        Debug.Log("[SkpPrefabSetup] level, boss and map prefabs and the catalog are built.");
    }

    // ---------------------------------------------------------------- level

    static void BuildLevel(LevelPrefabCatalog c, LevelArt a)
    {
        Sprite Or(Sprite s, Sprite fallback) => s != null ? s : fallback;
        c.ledgeByAct = new[]
        {
            Ledge("Ledge_Act1_Wood", a.wood, 0),
            Ledge("Ledge_Act2_Root", Or(a.rootLedge, a.wood), 0),
            Ledge("Ledge_Act3_Stone", Or(a.stoneLedge, a.wood), 0),
        };
        c.ledgeSpirit = Ledge("Ledge_Spirit", a.spirit, 0);
        c.ledgeInverted = Ledge("Ledge_Inverted", a.inverted, 0);
        c.ledgeCrumble = Ledge("Ledge_Crumble", a.crumble, 0);
        c.ledgeMoving = Ledge("Ledge_Moving", a.moving, 0);
        c.groundBlock = Ledge("Ground_Block", a.floorBlock, -1);

        c.doorSpirit = Piece(LevelDir, "Door_Spirit", a.doorSpirit, 1, DimensionMember.Ghost.Door);
        c.doorInverted = Piece(LevelDir, "Door_Inverted", a.doorInvert, 1, DimensionMember.Ghost.Door);
        c.doorHome = Piece(LevelDir, "Door_Home", a.doorHome, 1, DimensionMember.Ghost.Door);
        c.exit = Piece(LevelDir, "Door_Exit", a.exitLocked, 1, DimensionMember.Ghost.AlwaysVisible, states: new[] { a.exitLocked, a.exitOpen });

        c.spike = Piece(LevelDir, "Hazard_Spike", a.spike, 3, DimensionMember.Ghost.Hazard, tiled: true);
        c.spikeGauntlet = Piece(LevelDir, "Hazard_SpikeGauntlet", a.spikeGauntlet, 3, DimensionMember.Ghost.Hazard, tiled: true);
        c.root = Piece(LevelDir, "Hazard_Root", a.rootDormant, 3, DimensionMember.Ghost.Hazard, states: new[] { a.rootDormant, a.rootWarning, a.rootStrike });
        c.thorns = Piece(LevelDir, "Hazard_Thorns", a.thorns, 3, DimensionMember.Ghost.Hazard, states: new[] { a.thorns, a.thornsBurnt });
        c.curtain = Piece(LevelDir, "Hazard_VeilCurtain", Or(a.curtain, a.thorns), 3, DimensionMember.Ghost.Hazard, tiled: true);

        c.flame = Piece(LevelDir, "Item_Flame", a.flame, 4, DimensionMember.Ghost.Hidden);
        c.fragment = Piece(LevelDir, "Item_Fragment", a.shard, 4, DimensionMember.Ghost.Hidden);
        c.letter = Piece(LevelDir, "Item_Letter", a.clue, 4, DimensionMember.Ghost.Hidden);
        c.lantern = Lantern(a);

        c.anino = Creature("Creature_Anino", a.anino);
        c.tikbalang = Creature("Creature_Tikbalang", a.tikbalang);
        c.manananggal = Creature("Creature_Manananggal", a.manananggal);

        c.fallingBell = Piece(LevelDir, "Bell_Falling", a.bell, 7, DimensionMember.Ghost.Hidden);
        c.bellMark = Piece(LevelDir, "Bell_WarningMark", W(), 2, DimensionMember.Ghost.Hidden, color: new Color32(0xff, 0x5a, 0x4a, 0xa0));
        c.waterBody = Piece(LevelDir, "Water_Body", W(), 20, DimensionMember.Ghost.AlwaysVisible, color: new Color(6 / 255f, 12 / 255f, 20 / 255f, 0.82f));
        c.waterSurface = Piece(LevelDir, "Water_Surface", a.waterSurface, 21, DimensionMember.Ghost.AlwaysVisible, tiled: true);
        c.decorByAct = new[]
        {
            null,
            Piece(LevelDir, "Decor_Act2_Roots", a.propRoots, -2, DimensionMember.Ghost.Ledge, color: new Color(1, 1, 1, 0.85f)),
            Piece(LevelDir, "Decor_Act3_Candle", a.propCandle, -2, DimensionMember.Ghost.Ledge, color: new Color(1, 1, 1, 0.85f)),
        };
        c.bolt = Piece(LevelDir, "Bolt", AssetDatabase.LoadAssetAtPath<Sprite>(SkpSkillSetup.BoltSprite), 11, null);
        c.fx = Fx();
    }

    static Sprite W() => AssetDatabase.LoadAssetAtPath<Sprite>(White);

    static LevelPiece Ledge(string name, Sprite s, int order) => Piece(LevelDir, name, s, order, DimensionMember.Ghost.Ledge, tiled: true);

    static LevelPiece Creature(string name, Sprite[] frames) =>
        Piece(LevelDir, name, frames != null && frames.Length > 0 ? frames[0] : null, 6, DimensionMember.Ghost.Hidden, states: frames);

    /// <summary>One sprite on the prefab root. ghost null: no dimension rule (boss and map pieces, bolts).</summary>
    static LevelPiece Piece(string dir, string name, Sprite s, int order, DimensionMember.Ghost? ghost,
                            bool tiled = false, Sprite[] states = null, Color? color = null, Vector2? scale = null)
    {
        var go = new GameObject(name);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = s;
        sr.sortingOrder = order;
        if (color.HasValue) sr.color = color.Value;
        if (tiled)
        {
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;
            if (s != null) sr.size = s.bounds.size;
        }
        if (scale.HasValue) go.transform.localScale = new Vector3(scale.Value.x, scale.Value.y, 1);
        var p = go.AddComponent<LevelPiece>();
        p.main = sr;
        p.states = states != null ? (Sprite[])states.Clone() : new Sprite[0];
        p.artFacesLeft = s != null && AssetDatabase.GetAssetPath(s).StartsWith("Assets/Art/Recovered/") && name.StartsWith("Creature_");
        if (ghost.HasValue) p.dimension = Member(go, ghost.Value, sr);
        return Save(go, dir, name);
    }

    static DimensionMember Member(GameObject go, DimensionMember.Ghost ghost, params SpriteRenderer[] rs)
    {
        var dm = go.AddComponent<DimensionMember>();
        dm.mask = Dim.All;
        dm.ghost = ghost;
        dm.renderers = rs;
        return dm;
    }

    /// <summary>Lantern: a 2 x 34 px dark post and the flame star at 2x on top (the prototype's drawing).</summary>
    static LevelPiece Lantern(LevelArt a)
    {
        var go = new GameObject("Lantern");
        var post = Child(go, "Post", W(), 2, new Vector3(0, 17f / 16f, 0), new Vector3(2, 34, 1));
        post.color = new Color32(0x3a, 0x24, 0x10, 0xff);
        var flame = Child(go, "Flame", a.flame, 5, new Vector3(0, 2.1f, 0), Vector3.one * 2f);
        var p = go.AddComponent<LevelPiece>();
        p.main = flame;
        p.extra = new[] { post };
        p.dimension = Member(go, DimensionMember.Ghost.AlwaysVisible, post, flame);
        return Save(go, LevelDir, "Lantern");
    }

    static SpriteRenderer Child(GameObject parent, string name, Sprite s, int order, Vector3 localPos, Vector3 localScale)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = localScale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = s;
        sr.sortingOrder = order;
        return sr;
    }

    /// <summary>One child ParticleSystem per LevelFx kind; LevelFx applies the FeelTuning numbers to them at runtime.</summary>
    static LevelFx Fx()
    {
        var feel = Resources.Load<FeelTuning>(FeelTuning.ResourcePath);
        var go = new GameObject("Level Fx");
        var fx = go.AddComponent<LevelFx>();
        foreach (LevelFx.Kind k in System.Enum.GetValues(typeof(LevelFx.Kind)))
        {
            var c = new GameObject(LevelFx.ChildName(k));
            c.transform.SetParent(go.transform, false);
            var ps = c.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.playOnAwake = false;
            var em = ps.emission; em.enabled = false;
            var shape = ps.shape; shape.enabled = false;
            var r = c.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.sortingOrder = 30;
            if (feel != null && feel.particleMaterial != null) r.sharedMaterial = feel.particleMaterial;
        }
        var path = LevelDir + "/Level Fx.prefab";
        var saved = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return saved.GetComponent<LevelFx>();
    }

    static LevelPiece Save(GameObject go, string dir, string name)
    {
        var saved = PrefabUtility.SaveAsPrefabAsset(go, $"{dir}/{name}.prefab");
        Object.DestroyImmediate(go);
        return saved.GetComponent<LevelPiece>();
    }

    // ---------------------------------------------------------------- boss

    static void BuildBoss(LevelPrefabCatalog c, LevelArt a)
    {
        var view = new GameObject("Bellkeeper View");
        view.AddComponent<BossView>();
        c.bossView = PrefabUtility.SaveAsPrefabAsset(view, BossDir + "/Bellkeeper View.prefab").GetComponent<BossView>();
        Object.DestroyImmediate(view);

        c.bossBody = Piece(BossDir, "Bellkeeper", a.boss, 8, null, states: new[] { a.boss, a.bossStun, a.bossToll });
        c.shockwave = Piece(BossDir, "Shockwave", a.shockwave, 9, null);
        c.bossMark = Piece(BossDir, "Bell_WarningLine", W(), 2, null, color: new Color(1f, 0.35f, 0.29f, 0.4f), scale: new Vector2(2, 230));
        c.bossBell = Piece(BossDir, "Bell_Boss", a.bell, 9, null);

        // the toll ring: a closed line in the sprites' own material, so it is drawn like the rest of the level
        var go = new GameObject("Toll Ring");
        var probe = new GameObject("probe").AddComponent<SpriteRenderer>();
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop = true;
        lr.positionCount = 48;
        lr.widthMultiplier = 3f / 16f;
        lr.sharedMaterial = probe.sharedMaterial;
        lr.sortingOrder = 12;
        lr.startColor = lr.endColor = new Color32(0xff, 0xd3, 0x6a, 0xff);
        Object.DestroyImmediate(probe.gameObject);
        var p = go.AddComponent<LevelPiece>();
        p.line = lr;
        c.tollRing = Save(go, BossDir, "Toll Ring");
    }

    // ---------------------------------------------------------------- map

    static void BuildMap(LevelPrefabCatalog c)
    {
        var th = AssetDatabase.LoadAssetAtPath<UiTheme>(SkpUiSetup.ThemePath);
        var star = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Items/spr_star.png");
        var bell = AssetDatabase.LoadAssetAtPath<Sprite>(SkpUiSetup.BellPath);
        c.mapDot = Piece(MapDir, "Path_Dot", W(), 2, null, scale: new Vector2(3, 2));
        c.mapNodeShadow = Piece(MapDir, "Node_Shadow", W(), 3, null, color: new Color(0, 0, 0, 0.45f), scale: new Vector2(16, 3));
        c.mapNodePlate = Piece(MapDir, "Node_Plate", W(), 4, null, scale: new Vector2(14, 3));
        c.mapNodeIcon = Piece(MapDir, "Node_Icon", star, 5, null, states: new[] { star, bell });
        c.mapLola = Piece(MapDir, "Lola", AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Npc/spr_lola.png"), 6, null);
        c.mapToken = Piece(MapDir, "Tala_Token", AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/spr_pl_girl_0.png"), 9, null);
        c.mapNumber = Number(th, "Node_Number", th != null ? th.text : Color.white);

        // selection corner: a 4 x 1 and a 1 x 4 gold bar (the controller places both)
        var corner = new GameObject("Selection_Corner");
        var h = Child(corner, "H", W(), 6, Vector3.zero, new Vector3(4, 1, 1));
        var v = Child(corner, "V", W(), 6, Vector3.zero, new Vector3(1, 4, 1));
        if (th != null) h.color = v.color = th.goldBright;
        var cp = corner.AddComponent<LevelPiece>();
        cp.main = h;
        cp.extra = new[] { v };
        c.mapCorner = Save(corner, MapDir, "Selection_Corner");

        // Lola's "!": a 9 x 9 box in the text colour with the mark in the panel colour
        var mark = new GameObject("Lola_Mark");
        var box = Child(mark, "Box", W(), 7, Vector3.zero, new Vector3(9, 9, 1));
        if (th != null) box.color = th.text;
        var label = NumberLabel(mark, th, "!", th != null ? th.panel : Color.black);
        var mp = mark.AddComponent<LevelPiece>();
        mp.main = box;
        mp.label = label;
        c.mapLolaMark = Save(mark, MapDir, "Lola_Mark");
    }

    static LevelPiece Number(UiTheme th, string name, Color color)
    {
        var go = new GameObject(name);
        var t = Label(go, th, "1", color);
        var p = go.AddComponent<LevelPiece>();
        p.label = t;
        return Save(go, MapDir, name);
    }

    static TextMeshPro NumberLabel(GameObject parent, UiTheme th, string text, Color color)
    {
        var go = new GameObject("Num " + text);
        go.transform.SetParent(parent.transform, false);
        return Label(go, th, text, color);
    }

    /// <summary>8 px Press Start 2P glyphs at 16 PPU with the shared outline material (as the map drew its numbers).</summary>
    static TextMeshPro Label(GameObject go, UiTheme th, string text, Color color)
    {
        var t = go.AddComponent<TextMeshPro>();
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SkpProjectSetup.FontAssetPath);
        if (font != null) t.font = font;
        t.text = text;
        t.fontSize = 3.2f;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.rectTransform.sizeDelta = new Vector2(2, 1);
        t.sortingOrder = 8;
        t.color = color;
        if (th != null && th.fontOutline != null) t.fontSharedMaterial = th.fontOutline;
        return t;
    }

    // ---------------------------------------------------------------- player bolts

    /// <summary>The player prefab draws its bolts from the Bolt prefab.</summary>
    static void WirePlayerBolt(LevelPrefabCatalog c)
    {
        var root = PrefabUtility.LoadPrefabContents(SkpTestRoomBuilder.PlayerPrefabPath);
        var view = root.GetComponentInChildren<PlayerView>(true);
        if (view != null)
        {
            view.boltPrefab = c.bolt;
            PrefabUtility.SaveAsPrefabAsset(root, SkpTestRoomBuilder.PlayerPrefabPath);
        }
        PrefabUtility.UnloadPrefabContents(root);
    }
}
