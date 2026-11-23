using System;
using System.Collections.Generic;
using System.IO;
using Skp.Audio;
using Skp.Levels;
using Skp.Player;
using Skp.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Repeatable presentation integration; never rewrites level tuning, progression or save data.</summary>
public static class SkpPresentationUpgrade
{
    const string Recovered = "Assets/Art/Recovered";
    public const string Foley = "Assets/Audio/Licensed/Foley/";
    public const string MusicDir = "Assets/Audio/Licensed/Music/";

    [MenuItem("Sa Kabilang Pinto/Art/Apply Presentation Upgrade")]
    public static void Run()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        SkpRecoveredArtSetup.Run();
        var art = AssetDatabase.LoadAssetAtPath<LevelArt>(SkpLevelSetup.LevelArtPath);
        var catalog = AssetDatabase.LoadAssetAtPath<LevelPrefabCatalog>(SkpPrefabSetup.CatalogPath);
        var theme = AssetDatabase.LoadAssetAtPath<UiTheme>(SkpUiSetup.ThemePath);
        if (art == null || catalog == null || theme == null) throw new InvalidOperationException("Existing game assets required.");
        ApplyTerrain(art, catalog);
        ApplyHazards(art, catalog, theme);
        var effects = ApplyEffects(art, catalog, theme);
        ApplySkillIcons();
        var spirit = Fit(Recovered + "/Environments/DoorSpirit.png", 1.75f);
        var inverted = Fit(Recovered + "/Environments/DoorInverted.png", 1.75f);
        var home = Fit(Recovered + "/Environments/DoorOrdinary.png", 1.75f);
        art.doorSpirit = spirit; art.doorInvert = inverted; art.doorHome = home;
        Patch(catalog.doorSpirit, p => p.main.sprite = spirit);
        Patch(catalog.doorInverted, p => p.main.sprite = inverted);
        Patch(catalog.doorHome, p => p.main.sprite = home);
        // Exit uses the same detailed architecture; the open state glows while the locked state stays dim.
        var exit = Fit(Recovered + "/Environments/DoorOrdinary.png", 1.75f);
        Patch(catalog.exit, p => { p.main.sprite = exit; p.states = new[] { exit, exit }; });
        art.exitLocked = art.exitOpen = exit;
        ApplyBoss(art, catalog);

        var parol = Fit(Recovered + "/Props/Parol.png", 1.4f);
        Patch(catalog.lantern, p =>
        {
            p.main.sprite = parol;
            p.main.transform.localScale = Vector3.one;
            p.main.transform.localPosition = new Vector3(0, 1.8f, 0);
        });
        var fragment = Fit(Recovered + "/Props/BellFragment.png", 0.55f);
        art.shard = fragment;
        Patch(catalog.fragment, p => p.main.sprite = fragment);

        var girl = PlayerSet("girl", "Girl");
        var boy = PlayerSet("boy", "Boy");
        var shadowMaterial = ShadowMaterial();
        art.anino = new[] { girl.Get(TalaAnim.Run).frames[0], girl.Get(TalaAnim.Run).frames[1], girl.Get(TalaAnim.Run).frames[2], girl.Get(TalaAnim.Run).frames[3] };
        Patch(catalog.anino, p =>
        {
            p.states = new[] { girl.Get(TalaAnim.Run).frames[0], girl.Get(TalaAnim.Run).frames[1], girl.Get(TalaAnim.Run).frames[2], girl.Get(TalaAnim.Run).frames[3] };
            p.main.sprite = p.states[0]; p.main.sharedMaterial = shadowMaterial;
            p.main.color = new Color(0.04f, 0.025f, 0.07f, 1); p.artFacesLeft = false;
            var white = AssetDatabase.LoadAssetAtPath<Sprite>(SkpUiSetup.WhitePath);
            var renderers = new List<SpriteRenderer> { p.main };
            for (int i = 0; i < 2; i++)
            {
                string name = "PaleEye" + i;
                var child = p.transform.Find(name);
                if (child == null) { child = new GameObject(name).transform; child.SetParent(p.transform, false); }
                var eye = child.GetComponent<SpriteRenderer>();
                if (eye == null) eye = child.gameObject.AddComponent<SpriteRenderer>();
                eye.sprite = white; eye.sortingOrder = p.main.sortingOrder + 1;
                eye.color = new Color(0.85f, 0.95f, 1, 1);
                child.localPosition = new Vector3(i * 0.06f, 0.7f, 0); child.localScale = new Vector3(0.72f, 0.56f, 1);
                renderers.Add(eye);
            }
            p.extra = new[] { renderers[1], renderers[2] };
            if (p.dimension != null) p.dimension.renderers = renderers.ToArray();
        });
        var playerRoot = PrefabUtility.LoadPrefabContents(SkpTestRoomBuilder.PlayerPrefabPath);
        try
        {
            var view = playerRoot.GetComponentInChildren<PlayerView>(true);
            view.girlAnims = girl; view.boyAnims = boy;
            if (view.wardRing == null)
            {
                view.wardRing = new GameObject("WardRing").AddComponent<SpriteRenderer>();
                view.wardRing.transform.SetParent(playerRoot.transform, false);
                view.wardRing.sharedMaterial = view.body.sharedMaterial;
                view.wardRing.sortingOrder = 11; view.wardRing.enabled = false;
            }
            view.wardRing.sprite = effects[1]; view.wardRing.color = Color.white;
            view.veilRing.sprite = effects[7]; view.veilRing.color = Color.white;
            view.glideWings.sprite = effects[8]; view.glideWings.color = Color.white;
            PrefabUtility.SaveAsPrefabAsset(playerRoot, SkpTestRoomBuilder.PlayerPrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(playerRoot); }
        theme.titleArt = home;
        theme.cardGirl = girl.Get(TalaAnim.Idle).frames[0];
        theme.cardBoy = boy.Get(TalaAnim.Idle).frames[0];
        theme.menuBackdrop = art.backdrops[0];
        theme.portraitGirl = Portrait(theme.cardGirl.texture, Recovered + "/Girl/Portrait.asset", new Rect(10, 27, 24, 24));
        theme.portraitBoy = Portrait(theme.cardBoy.texture, Recovered + "/Boy/Portrait.asset", new Rect(10, 27, 24, 24));
        theme.portraitAnino = Fit(Recovered + "/Anino/Portrait.png", 1);
        var bossTexture = art.boss.texture;
        float bossScale = bossTexture.width / 1280f;
        theme.portraitBoss = Portrait(bossTexture, Recovered + "/Bellkeeper/Portrait.asset", new Rect(195 * bossScale, bossTexture.height - 340 * bossScale, 255 * bossScale, 255 * bossScale));
        Patch(catalog.mapNodeIcon, p => { p.states = new[] { effects[0], catalog.fallingBell.main.sprite }; p.main.sprite = effects[0]; });
        var lola = AtlasSprites(Recovered + "/Lola/LolaAtlas.png", "Lola", 1.25f, 4);
        if (lola != null)
        {
            var lolaTexture = lola[0].texture;
            float sx = lolaTexture.width / 1280f, sy = lolaTexture.height / 1280f;
            theme.portraitLola = Portrait(lolaTexture, Recovered + "/Lola/Portrait.asset", new Rect(180 * sx, lolaTexture.height - 365 * sy, 310 * sx, 330 * sy));
            Patch(catalog.mapLola, p => { p.main.sprite = lola[0]; p.states = lola; });
        }
        EditorUtility.SetDirty(theme); EditorUtility.SetDirty(art);
        var strings = AssetDatabase.LoadAssetAtPath<StringTable>(SkpUiSetup.StringsPath);
        strings.Set("credits.body", StringDefaults.En["credits.body"]);
        for (int i = 0; i < strings.entries.Count; i++)
        {
            var entry = strings.entries[i];
            if (entry.text.Contains("golden ring")) strings.Set(entry.key, entry.text.Replace("golden ring", "closing ring"));
        }
        EditorUtility.SetDirty(strings);
        ApplyRecordedAudio();
        SkpHandoffBuild.ApplyBranding();
        AssetDatabase.SaveAssets();
        UiTheme.ClearCache();
        SkpUiSetup.RebuildSharedScreens();
        SkpUiSetup.BuildTitleScene();
        var mapScene = EditorSceneManager.OpenScene("Assets/Scenes/Map.unity", OpenSceneMode.Single);
        var map = UnityEngine.Object.FindAnyObjectByType<MapController>();
        if (map != null)
        {
            if (lola != null) { map.lolaFrames = lola; map.lola = lola[0]; }
            map.girlFrames = new[] { girl.Get(TalaAnim.Idle).frames[0], girl.Get(TalaAnim.Run).frames[0], girl.Get(TalaAnim.Run).frames[3], girl.Get(TalaAnim.JumpRise).frames[0] };
            map.boyFrames = new[] { boy.Get(TalaAnim.Idle).frames[0], boy.Get(TalaAnim.Run).frames[0], boy.Get(TalaAnim.Run).frames[3], boy.Get(TalaAnim.JumpRise).frames[0] };
            const string mapPath = Recovered + "/Map/ThreeActs.png";
            if (File.Exists(mapPath))
            {
                map.background.sprite = Fit(mapPath, 16.875f);
                map.background.drawMode = SpriteDrawMode.Sliced;
                map.background.size = new Vector2(90, 16.875f);
                map.background.transform.localScale = Vector3.one;
                map.background.color = new Color(0.7f, 0.7f, 0.78f, 1);
            }
            EditorSceneManager.SaveScene(mapScene);
        }
        var levelScene = EditorSceneManager.OpenScene("Assets/Scenes/Level.unity", OpenSceneMode.Single);
        var runner = UnityEngine.Object.FindAnyObjectByType<LevelRunner>();
        if (runner != null && runner.backdrop != null)
        {
            runner.backdrop.color = new Color(0.48f, 0.46f, 0.55f, 1);
            EditorSceneManager.SaveScene(levelScene);
        }
        var shadow = UnityEngine.Object.FindAnyObjectByType<Skp.Dimensions.ShadowView>();
        if (shadow != null)
        {
            shadow.silhouetteMaterial = shadowMaterial;
            shadow.eyeSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SkpUiSetup.WhitePath);
            EditorSceneManager.SaveScene(levelScene);
        }
        AssetDatabase.SaveAssets();
        WriteInventory();
        Debug.Log("[PresentationUpgrade] detailed Tala, doors, checkpoints, fragments, title/selection/map token and recorded foley applied.");
    }

    public static Sprite Fit(string path, float height, bool feet = false)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("Presentation asset missing", path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.GetSourceTextureWidthAndHeight(out int w, out int h);
        float footInset = 0;
        if (feet)
        {
            var analysis = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            analysis.LoadImage(File.ReadAllBytes(path));
            var pixels = analysis.GetPixels32();
            int row = 0;
            bool found = false;
            for (; row < analysis.height && !found; row++)
                for (int x = 0; x < analysis.width; x++) if (pixels[row * analysis.width + x].a >= 192) { found = true; break; }
            footInset = found ? row - 1 : 0;
            UnityEngine.Object.DestroyImmediate(analysis);
        }
        var before = new TextureImporterSettings(); importer.ReadTextureSettings(before);
        var alignment = feet ? SpriteAlignment.Custom : SpriteAlignment.Center;
        var pivot = new Vector2(0.5f, feet ? footInset / h : 0.5f);
        if (importer.textureType == TextureImporterType.Sprite && importer.spriteImportMode == SpriteImportMode.Single &&
            Mathf.Abs(importer.spritePixelsPerUnit - h / height) < 0.001f && importer.filterMode == FilterMode.Point &&
            importer.textureCompression == TextureImporterCompression.Uncompressed && !importer.mipmapEnabled &&
            importer.maxTextureSize == 4096 && before.spriteAlignment == (int)alignment &&
            (!feet || Vector2.Distance(before.spritePivot, pivot) < 0.0001f))
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = h / height;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = 4096;
        importer.alphaIsTransparency = true;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)alignment;
        settings.spritePivot = pivot;
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null || Mathf.Abs(sprite.bounds.size.y - height) > 0.001f) throw new InvalidOperationException(path + " world height changed");
        return sprite;
    }

    static PlayerAnimSet PlayerSet(string id, string folder)
    {
        var source = new Sprite[16];
        for (int i = 0; i < source.Length; i++) source[i] = Fit($"{Recovered}/{folder}/{i:00}.png", 18f / 16, true);
        var actions = ActionKeys(folder);
        int[][] indices = {
            new[] {0,1,2,3}, new[] {4,5,6,7,8,9}, new[] {10,10}, new[] {10}, new[] {11,11},
            new[] {15,0}, new[] {7,8,9}, new[] {12,13,12}, new[] {14,15},
            new[] {14,14,15,15,15}, new[] {0,1,2,3}, new[] {10,11}, new[] {12,13}
        };
        string path = $"Assets/Data/Player/TalaRecovered_{id}.asset";
        var set = AssetDatabase.LoadAssetAtPath<PlayerAnimSet>(path);
        if (set == null) { set = ScriptableObject.CreateInstance<PlayerAnimSet>(); AssetDatabase.CreateAsset(set, path); }
        set.clips = new PlayerAnimSet.Clip[SkpTalaAnimSetup.Table.Length];
        for (int i = 0; i < set.clips.Length; i++)
        {
            var spec = SkpTalaAnimSetup.Table[i];
            var frames = new Sprite[indices[i].Length];
            for (int f = 0; f < frames.Length; f++) frames[f] = source[indices[i][f]];
            int key = spec.anim switch {
                TalaAnim.Dash => 0, TalaAnim.Ward => 1, TalaAnim.Glide => 2, TalaAnim.Bolt => 3,
                TalaAnim.Land => 4, TalaAnim.Hurt => 5, TalaAnim.Death => 6, TalaAnim.Door => 7, _ => -1
            };
            if (key >= 0) for (int f = 0; f < frames.Length; f++) frames[f] = actions[key];
            if (spec.anim == TalaAnim.Land) frames[frames.Length - 1] = source[0];
            if (spec.anim == TalaAnim.Death) frames[0] = actions[5];
            set.clips[i] = new PlayerAnimSet.Clip { anim = spec.anim, frames = frames, ticksPerFrame = spec.ticks, loop = spec.loop };
        }
        set.InvalidateCache();
        EditorUtility.SetDirty(set);
        return set;
    }

    static Sprite[] ActionKeys(string folder)
    {
        string path = $"{Recovered}/{folder}/ActionKeysAtlas.png";
        Fit(path, 2.25f);
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        var analysis = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        analysis.LoadImage(File.ReadAllBytes(path));
        var pixels = analysis.GetPixels32();
        int w = texture.width / 4, h = texture.height / 2;
        var result = new Sprite[8];
        for (int i = 0; i < result.Length; i++)
        {
            string assetPath = $"{Recovered}/{folder}/Action{i}.asset";
            result[i] = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (result[i] != null) continue;
            int x0 = i % 4 * w, y0 = (1 - i / 4) * h, foot = 0;
            for (; foot < h; foot++)
            {
                bool found = false;
                for (int x = 0; x < w; x++) if (pixels[(y0 + foot) * texture.width + x0 + x].a >= 192) { found = true; break; }
                if (found) break;
            }
            result[i] = Sprite.Create(texture, new Rect(x0, y0, w, h), new Vector2(0.5f, foot / (float)h), h / 1.125f, 0, SpriteMeshType.FullRect);
            result[i].name = folder + "Action" + i;
            AssetDatabase.CreateAsset(result[i], assetPath);
        }
        UnityEngine.Object.DestroyImmediate(analysis);
        return result;
    }

    static void Patch(LevelPiece prefab, Action<LevelPiece> change)
    {
        string path = AssetDatabase.GetAssetPath(prefab);
        var root = PrefabUtility.LoadPrefabContents(path);
        try { change(root.GetComponent<LevelPiece>()); PrefabUtility.SaveAsPrefabAsset(root, path); }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    static Material ShadowMaterial()
    {
        const string path = Recovered + "/ShadowSilhouette.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            var shader = Shader.Find("Skp/ShadowSilhouette");
            if (shader == null) throw new InvalidOperationException("Shadow silhouette shader not imported");
            material = new Material(shader); AssetDatabase.CreateAsset(material, path);
        }
        return material;
    }

    static Sprite Portrait(Texture2D texture, string path, Rect rect)
    {
        var portrait = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (portrait == null)
        {
            portrait = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), rect.height / 1.5f, 0, SpriteMeshType.FullRect);
            portrait.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(portrait, path);
        }
        return portrait;
    }

    public static void ApplyRecordedAudio()
    {
        var bank = AssetDatabase.LoadAssetAtPath<SfxBank>(SkpSkillSetup.BankPath);
        var replacements = new Dictionary<string, string> {
            ["jump"]="cloth1", ["land"]="footstep04", ["dash"]="cloth4", ["ward"]="metalPot1",
            ["hurt"]="dropLeather", ["die"]="dropLeather", ["door"]="doorOpen_1", ["locked"]="metalLatch",
            ["letter"]="bookFlip2", ["clue"]="bookOpen", ["flame"]="handleCoins", ["shard"]="handleCoins2",
            ["lantern"]="metalClick", ["select"]="metalClick", ["crumble"]="creak3",
            ["skill_dj"]="cloth2", ["skill_glide"]="cloth3", ["skill_bolt"]="knifeSlice",
            ["skill_bolt3"]="knifeSlice2", ["skill_veil"]="clothBelt", ["skill_veil_end"]="clothBelt2",
            ["bolt_hit"]="metalClick", ["parry"]="metalPot3", ["kill"]="chop", ["stun"]="metalPot2",
            ["dimension"]="creak1", ["step_left"]="footstep00", ["step_right"]="footstep01",
            ["skill_learned"]="handleCoins2", ["skill_known"]="bookPlace1",
            ["bell"]="church_bell", ["toll"]="church_bell", ["burn"]="fire_crackle", ["splash"]="water_splash", ["win"]="handleCoins2"
        };
        var entries = new List<SfxBank.Entry>(bank.entries);
        foreach (var pair in replacements)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Foley + pair.Value + ".ogg") ?? AssetDatabase.LoadAssetAtPath<AudioClip>(Foley + pair.Value + ".wav");
            if (clip == null) throw new FileNotFoundException("Recorded cue missing: " + pair.Value);
            float volume = pair.Key.StartsWith("step_") ? 0.26f : pair.Key == "select" ? 0.35f : 0.65f;
            var entry = new SfxBank.Entry { cue = pair.Key, clip = clip, volume = volume };
            int index = entries.FindIndex(x => x.cue == pair.Key);
            if (index >= 0) entries[index] = entry; else entries.Add(entry);
        }
        bank.entries = entries.ToArray(); EditorUtility.SetDirty(bank);
        var tuning = AssetDatabase.LoadAssetAtPath<AudioTuning>(SkpAudioSetup.TuningPath);
        AudioClip Track(string name) => AssetDatabase.LoadAssetAtPath<AudioClip>(MusicDir + "Loops/" + name + "_loop.wav");
        if (Track("Evening Fall - Harp") != null) tuning.menu = Track("Evening Fall - Harp");
        if (Track("Ghost Story") != null) tuning.act1 = Track("Ghost Story");
        if (Track("Darkest Child") != null) tuning.act2 = Track("Darkest Child");
        if (Track("Ossuary 6 - Air") != null) tuning.act3 = Track("Ossuary 6 - Air");
        if (Track("The House of Leaves") != null) tuning.boss = Track("The House of Leaves");
        tuning.nightAmbience = AssetDatabase.LoadAssetAtPath<AudioClip>(Foley + "night_crickets.mp3");
        EditorUtility.SetDirty(tuning);
    }

    public static void RefreshAudioBatch()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        ApplyRecordedAudio();
        AssetDatabase.SaveAssets();
    }

    public static void WriteInventory()
    {
        var roots = new List<string> { "Assets/Scenes/Boot.unity", "Assets/Scenes/Title.unity", "Assets/Scenes/Map.unity", "Assets/Scenes/Level.unity", SkpPrefabSetup.CatalogPath };
        foreach (string guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets/Resources" })) roots.Add(AssetDatabase.GUIDToAssetPath(guid));
        var paths = AssetDatabase.GetDependencies(roots.ToArray(), true);
        Array.Sort(paths, StringComparer.Ordinal);
        var report = new System.Text.StringBuilder("# Active presentation inventory\n\nGenerated from scene, prefab-catalog and Resources dependencies. Integration is not visual/listening acceptance.\n\n| Asset | Integration status |\n| --- | --- |\n");
        foreach (string path in paths)
        {
            string extension = Path.GetExtension(path).ToLowerInvariant();
            if (extension != ".png" && extension != ".ogg" && extension != ".mp3" && extension != ".wav" && extension != ".prefab") continue;
            string status = path.StartsWith(Recovered) ? "Recovered/generated art integrated; visual review required" : path.StartsWith("Assets/Audio/Licensed/") ? "Licensed source integrated; listening review required" : "Existing presentation: inspect and replace/improve as required";
            report.AppendLine($"| {path} | {status} |");
        }
        Directory.CreateDirectory("docs");
        File.WriteAllText("docs/PRESENTATION_INVENTORY.md", report.ToString());
    }

    static void ApplyBoss(LevelArt art, LevelPrefabCatalog catalog)
    {
        const string path = Recovered + "/Bellkeeper/BellkeeperAtlas.png";
        var states = AtlasSprites(path, "Bellkeeper", 3.5f, 4);
        if (states == null) return;
        art.boss = states[0]; art.bossStun = states[1]; art.bossToll = states[2];
        Patch(catalog.bossBody, p => { p.main.sprite = states[0]; p.states = states; });
    }

    static void ApplyTerrain(LevelArt art, LevelPrefabCatalog catalog)
    {
        const string path = Recovered + "/Terrain/TerrainAtlas.png";
        if (!File.Exists(path)) return;
        Fit(path, 3f);
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        var analysis = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        analysis.LoadImage(File.ReadAllBytes(path));
        var pixels = analysis.GetPixels32();
        int cw = analysis.width / 3, ch = analysis.height / 3;
        var sprites = new Sprite[9];
        for (int i = 0; i < sprites.Length; i++)
        {
            int ox = (i % 3) * cw, oy = (2 - i / 3) * ch;
            int minX = cw, minY = ch, maxX = 0, maxY = 0;
            for (int y = 0; y < ch; y++)
            {
                int count = 0;
                for (int x = 0; x < cw; x++)
                {
                    if (pixels[(oy + y) * analysis.width + ox + x].a < 192) continue;
                    minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x); minY = Mathf.Min(minY, y); count++;
                }
                // Ignore isolated fringe pixels above the flat load-bearing top edge.
                if (count >= cw / 4) maxY = y;
            }
            if (minX >= maxX || minY >= maxY) throw new InvalidOperationException("Terrain atlas cell empty: " + i);
            var rect = new Rect(ox + minX, oy + minY, maxX - minX + 1, maxY - minY + 1);
            string spritePath = Recovered + "/Terrain/Tile" + i + ".asset";
            sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprites[i] == null)
            {
                float cap = Mathf.Min(rect.height, rect.width / 3);
                sprites[i] = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), rect.height, 0, SpriteMeshType.FullRect, new Vector4(cap, 0, cap, 0));
                sprites[i].name = "Terrain_" + i;
                AssetDatabase.CreateAsset(sprites[i], spritePath);
            }
        }
        UnityEngine.Object.DestroyImmediate(analysis);
        art.wood = sprites[0]; art.rootLedge = sprites[1]; art.stoneLedge = sprites[2];
        art.spirit = sprites[3]; art.inverted = sprites[4]; art.crumble = sprites[5]; art.moving = sprites[6]; art.floorBlock = sprites[7];
        for (int i = 0; i < 3; i++) { int index = i; Patch(catalog.ledgeByAct[i], p => p.main.sprite = sprites[index]); }
        Patch(catalog.ledgeSpirit, p => p.main.sprite = sprites[3]);
        Patch(catalog.ledgeInverted, p => p.main.sprite = sprites[4]);
        Patch(catalog.ledgeCrumble, p => p.main.sprite = sprites[5]);
        Patch(catalog.ledgeMoving, p => p.main.sprite = sprites[6]);
        Patch(catalog.groundBlock, p => p.main.sprite = sprites[7]);
    }

    static void ApplyHazards(LevelArt art, LevelPrefabCatalog catalog, UiTheme theme)
    {
        const string path = Recovered + "/Hazards/HazardsAtlas.png";
        if (!File.Exists(path)) return;
        Fit(path, 3f);
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        var analysis = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        analysis.LoadImage(File.ReadAllBytes(path));
        var pixels = analysis.GetPixels32();
        // Reviewed source regions (top-left coordinates on the 1280-square master), not blind grid cuts:
        // the tall striking root extends above its nominal middle cell.
        var regions = new[] {
            new Rect(0,170,410,160), new Rect(420,70,420,270), new Rect(840,145,430,175),
            new Rect(0,480,420,280), new Rect(420,350,420,410), new Rect(880,330,330,445),
            new Rect(60,780,300,450), new Rect(420,780,420,450), new Rect(840,850,410,340)
        };
        float[] heights = { 7f/16, 7f/16, 0.35f, 0.65f, 1, 130f/16, 130f/16, 20f/16, 9f/16 };
        var sprites = new Sprite[regions.Length];
        for (int i = 0; i < regions.Length; i++)
        {
            var roi = regions[i]; float sx = analysis.width / 1280f, sy = analysis.height / 1280f;
            int x0 = Mathf.RoundToInt(roi.xMin * sx), x1 = Mathf.Min(analysis.width, Mathf.RoundToInt(roi.xMax * sx));
            int y0 = Mathf.Max(0, analysis.height - Mathf.RoundToInt(roi.yMax * sy)), y1 = analysis.height - Mathf.RoundToInt(roi.yMin * sy);
            int minX = x1, minY = y1, maxX = x0, maxY = y0;
            for (int y = y0; y < y1; y++) for (int x = x0; x < x1; x++)
            {
                if (pixels[y * analysis.width + x].a < 192) continue;
                minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x); minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y);
            }
            var rect = new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);
            string spritePath = Recovered + "/Hazards/Hazard" + i + ".asset";
            sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprites[i] == null)
            {
                bool root = i >= 2 && i <= 4;
                sprites[i] = Sprite.Create(texture, rect, root ? new Vector2(0.5f, 0) : new Vector2(0.5f, 0.5f), rect.height / heights[i], 0, SpriteMeshType.FullRect);
                sprites[i].name = "Hazard_" + i;
                AssetDatabase.CreateAsset(sprites[i], spritePath);
            }
        }
        UnityEngine.Object.DestroyImmediate(analysis);
        art.spike = sprites[0]; art.spikeGauntlet = sprites[1];
        art.rootDormant = sprites[2]; art.rootWarning = sprites[3]; art.rootStrike = sprites[4];
        art.thorns = sprites[5]; art.thornsBurnt = sprites[6]; art.bell = sprites[7]; art.clue = sprites[8];
        theme.portraitClue = sprites[8];
        Patch(catalog.spike, p => p.main.sprite = sprites[0]);
        Patch(catalog.spikeGauntlet, p => p.main.sprite = sprites[1]);
        Patch(catalog.root, p =>
        {
            if (p.main.transform == p.transform)
            {
                var body = new GameObject("PresentationBody"); body.transform.SetParent(p.transform, false);
                var renderer = body.AddComponent<SpriteRenderer>();
                renderer.sharedMaterial = p.main.sharedMaterial; renderer.sortingOrder = p.main.sortingOrder;
                p.main.enabled = false; p.main = renderer;
                if (p.dimension != null) p.dimension.renderers = new[] { renderer };
            }
            p.main.transform.localPosition = new Vector3(0, -0.5f, 0);
            p.states = new[] { sprites[2], sprites[3], sprites[4] }; p.presentationWidth = 0.75f; p.SetState(0);
        });
        Patch(catalog.thorns, p => { p.states = new[] { sprites[5], sprites[6] }; p.presentationWidth = 1f; p.SetState(0); });
        Patch(catalog.fallingBell, p => p.main.sprite = sprites[7]);
        Patch(catalog.bossBell, p => p.main.sprite = sprites[7]);
        Patch(catalog.letter, p => p.main.sprite = sprites[8]);
    }

    static Sprite[] ApplyEffects(LevelArt art, LevelPrefabCatalog catalog, UiTheme theme)
    {
        const string path = Recovered + "/Effects/EffectsAtlas.png";
        Fit(path, 3);
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        var analysis = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        analysis.LoadImage(File.ReadAllBytes(path));
        var pixels = analysis.GetPixels32();
        int w = texture.width / 4, h = texture.height / 3;
        float[] heights = { 0.55f, 1, 0.28f, 1, 0.5f, 2.5f, 2, 1, 0.75f, 1, 1, 0.125f };
        var sprites = new Sprite[12];
        for (int i = 0; i < sprites.Length; i++)
        {
            string assetPath = Recovered + "/Effects/Effect" + i + ".asset";
            sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprites[i] != null) continue;
            int ox = i % 4 * w, oy = (2 - i / 4) * h;
            int x0 = w, x1 = -1, y0 = h, y1 = -1;
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
                if (pixels[(oy + y) * texture.width + ox + x].a >= 192)
                { x0 = Mathf.Min(x0, x); x1 = Mathf.Max(x1, x); y0 = Mathf.Min(y0, y); y1 = Mathf.Max(y1, y); }
            if (x1 < x0) throw new InvalidOperationException("Empty effect cell " + i);
            var rect = new Rect(ox + x0, oy + y0, x1 - x0 + 1, y1 - y0 + 1);
            sprites[i] = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), rect.height / heights[i], 0, SpriteMeshType.FullRect);
            sprites[i].name = "Effect_" + i; AssetDatabase.CreateAsset(sprites[i], assetPath);
        }
        UnityEngine.Object.DestroyImmediate(analysis);
        art.flame = sprites[0]; art.curtain = sprites[3]; art.waterSurface = sprites[4];
        art.propRoots = sprites[5]; art.propCandle = sprites[6];
        Patch(catalog.flame, p => p.main.sprite = sprites[0]);
        Patch(catalog.bolt, p => p.main.sprite = sprites[2]);
        art.shockwave = sprites[2];
        Patch(catalog.shockwave, p =>
        {
            var original = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Generated/fx_shockwave_12x12.png");
            var canvas = original.bounds.size;
            p.main.sprite = sprites[2]; p.main.color = theme.danger;
            p.main.transform.localScale = new Vector3(canvas.x / sprites[2].bounds.size.x, canvas.y / sprites[2].bounds.size.y, 1);
        });
        Patch(catalog.curtain, p => { p.main.sprite = sprites[3]; p.main.drawMode = SpriteDrawMode.Sliced; });
        Patch(catalog.waterSurface, p => p.main.sprite = sprites[4]);
        Patch(catalog.decorByAct[1], p => p.main.sprite = sprites[5]);
        Patch(catalog.decorByAct[2], p => p.main.sprite = sprites[6]);
        theme.heartFull = sprites[9]; theme.heartEmpty = sprites[10];
        var feel = Resources.Load<Skp.Core.FeelTuning>(Skp.Core.FeelTuning.ResourcePath);
        const string corePath = Recovered + "/Effects/MoteCore.asset";
        var core = AssetDatabase.LoadAssetAtPath<Sprite>(corePath);
        if (core == null)
        {
            var center = sprites[11].rect.center;
            core = Sprite.Create(texture, new Rect(Mathf.Floor(center.x) - 4, Mathf.Floor(center.y) - 4, 8, 8), new Vector2(0.5f, 0.5f), 64, 0, SpriteMeshType.FullRect);
            core.name = "NeutralMoteCore"; AssetDatabase.CreateAsset(core, corePath);
        }
        feel.flame.sprite = feel.ember.sprite = sprites[11];
        feel.fragment.sprite = feel.veil.sprite = feel.bolt.sprite = feel.burnt.sprite = feel.letterGlow.sprite = core;
        EditorUtility.SetDirty(feel);
        return sprites;
    }

    static void ApplySkillIcons()
    {
        const string path = Recovered + "/Skills/SkillsAtlas.png";
        Fit(path, 2);
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        string[] ids = { "dj", "bolt", "veil", "swift", "spread", "glide", "veil2", "heart" };
        int w = texture.width / 4, h = texture.height / 2;
        for (int i = 0; i < ids.Length; i++)
        {
            string assetPath = Recovered + "/Skills/" + ids[i] + ".asset";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null)
            {
                sprite = Sprite.Create(texture, new Rect(i % 4 * w, (1 - i / 4) * h, w, h), new Vector2(0.5f, 0.5f), h, 0, SpriteMeshType.FullRect);
                sprite.name = "Skill_" + ids[i]; AssetDatabase.CreateAsset(sprite, assetPath);
            }
            var skill = AssetDatabase.LoadAssetAtPath<Skp.Skills.SkillDefinition>("Assets/Data/Skills/Skill_" + ids[i] + ".asset");
            skill.icon = sprite; EditorUtility.SetDirty(skill);
        }
    }

    static Sprite[] AtlasSprites(string path, string prefix, float height, int count)
    {
        if (!File.Exists(path)) return null;
        Fit(path, height * 2);
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        int cellW = texture.width / 2, cellH = texture.height / 2;
        var states = new Sprite[count];
        for (int i = 0; i < states.Length; i++)
        {
            string spritePath = Path.GetDirectoryName(path).Replace('\\', '/') + "/" + prefix + i + ".asset";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite == null)
            {
                var rect = new Rect((i % 2) * cellW, (1 - i / 2) * cellH, cellW, cellH);
                sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0), cellH / height, 0, SpriteMeshType.FullRect);
                sprite.name = prefix + "_" + i;
                AssetDatabase.CreateAsset(sprite, spritePath);
            }
            states[i] = sprite;
        }
        return states;
    }

    public static void Capture()
    {
        Directory.CreateDirectory("tests/Screenshots/presentation");
        EditorSceneManager.OpenScene("Assets/Scenes/Title.unity", OpenSceneMode.Single);
        var flow = UnityEngine.Object.FindAnyObjectByType<TitleFlow>();
        foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Camera.main;
            canvas.planeDistance = 1f;
        }
        flow.title.Bind(false); flow.title.Open(); flow.title.newJourney.SetHighlighted(true);
        Canvas.ForceUpdateCanvases();
        SkpScreenshots.Capture(Camera.main, 1280, 720, "presentation/title.png");
        flow.title.Close(); flow.characters.Open();
        Canvas.ForceUpdateCanvases();
        SkpScreenshots.Capture(Camera.main, 1280, 720, "presentation/character.png");
        EditorSceneManager.OpenScene("Assets/Scenes/Map.unity", OpenSceneMode.Single);
        Camera.main.aspect = 1280f / 720f;
        var mapPixels = Camera.main.GetComponent<UnityEngine.Rendering.Universal.PixelPerfectCamera>();
        if (mapPixels != null) mapPixels.enabled = false;
        var map = UnityEngine.Object.FindAnyObjectByType<MapController>();
        map.Present(new Skp.Save.SaveData { metLola = true }); map.screen.Open();
        foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
        { canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = Camera.main; canvas.planeDistance = 1; }
        Canvas.ForceUpdateCanvases();
        SkpScreenshots.Capture(Camera.main, 1280, 720, "presentation/map.png");
        CaptureActionAlignment();
        SkpLevelShots.Presentation();
    }

    static void CaptureActionAlignment()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camera = new GameObject("Action alignment camera").AddComponent<Camera>();
        camera.orthographic = true; camera.orthographicSize = 3.5f;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.04f, 0.025f, 0.065f);
        camera.transform.position = new Vector3(13, 0.5f, -10);
        var theme = UiTheme.Load();
        for (int row = 0; row < 2; row++)
        {
            string id = row == 0 ? "girl" : "boy";
            var set = AssetDatabase.LoadAssetAtPath<PlayerAnimSet>($"Assets/Data/Player/TalaRecovered_{id}.asset");
            int column = 0;
            foreach (TalaAnim state in Enum.GetValues(typeof(TalaAnim)))
            {
                float x = 1 + column++ * 2, y = 1.5f - row * 2.7f;
                var body = new GameObject(id + " " + state).AddComponent<SpriteRenderer>();
                body.sprite = set.Get(state).frames[state == TalaAnim.Death ? 1 : 0];
                body.transform.position = new Vector3(x, y, 0);
                var floor = new GameObject("Foot baseline").AddComponent<SpriteRenderer>();
                floor.sprite = theme.white; floor.color = theme.gold;
                floor.transform.position = new Vector3(x, y - 0.015f, 0);
                floor.transform.localScale = new Vector3(1.7f / theme.white.bounds.size.x, 0.02f / theme.white.bounds.size.y, 1);
                var label = new GameObject("Label").AddComponent<TMPro.TextMeshPro>();
                label.font = theme.font; label.fontSize = 1.5f; label.text = state.ToString();
                label.alignment = TMPro.TextAlignmentOptions.Center; label.color = theme.text;
                label.rectTransform.sizeDelta = new Vector2(2, 0.5f);
                label.transform.position = new Vector3(x, y - 0.4f, 0);
            }
        }
        SkpScreenshots.Capture(camera, 2400, 640, "presentation/action_alignment.png");
    }

    public static void RunBatch() { Run(); Capture(); }
}
