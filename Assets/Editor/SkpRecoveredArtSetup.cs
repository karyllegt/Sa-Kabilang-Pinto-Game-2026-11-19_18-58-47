using System;
using System.IO;
using Skp.Levels;
using UnityEditor;
using UnityEngine;

/// <summary>Integrates recovered project art without touching simulation geometry or original assets.</summary>
public static class SkpRecoveredArtSetup
{
    const string Dir = "Assets/Art/Recovered";

    [MenuItem("Sa Kabilang Pinto/Art/Integrate Recovered Creatures")]
    public static void Run()
    {
        var art = AssetDatabase.LoadAssetAtPath<LevelArt>(SkpLevelSetup.LevelArtPath);
        var catalog = AssetDatabase.LoadAssetAtPath<LevelPrefabCatalog>(SkpPrefabSetup.CatalogPath);
        if (art == null || catalog == null) throw new InvalidOperationException("Level art and catalog are required.");
        art.tikbalang = Creature(catalog.tikbalang, "Tikbalang");
        art.manananggal = Creature(catalog.manananggal, "Manananggal");
        art.backdrops = new[] { Background("HouseWall"), Background("ForestWall"), Background("BelfryWall") };
        EditorUtility.SetDirty(art);
        AssetDatabase.SaveAssets();
        Debug.Log("Recovered creatures integrated: four frames each, existing world height and bottom pivot preserved.");
    }

    static Sprite Background(string name)
    {
        string path = $"{Dir}/Environments/{name}.png";
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 32;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.wrapMode = TextureWrapMode.Repeat;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Center;
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static Sprite[] Creature(LevelPiece prefab, string name)
    {
        // Read the original reference canvas on every run: repeated setup cannot compound scale changes.
        string originalPath = name == "Tikbalang" ? "Assets/Art/Enemies/spr_tik_0.png" : "Assets/Art/Enemies/spr_manan_0.png";
        var original = AssetDatabase.LoadAssetAtPath<Sprite>(originalPath);
        float height = original != null ? original.bounds.size.y : (name == "Tikbalang" ? 26f / 16 : 20f / 16);
        var frames = new Sprite[4];
        for (int i = 0; i < frames.Length; i++)
        {
            string path = $"{Dir}/{name}/{i:00}.png";
            if (!File.Exists(path)) throw new FileNotFoundException("Recovered frame missing", path);
            frames[i] = SkpPresentationUpgrade.Fit(path, height, true);
            if (frames[i] == null || Mathf.Abs(frames[i].bounds.size.y - height) > 0.001f)
                throw new InvalidOperationException("Recovered creature import failed: " + path);
        }
        string prefabPath = AssetDatabase.GetAssetPath(prefab);
        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            var piece = root.GetComponent<LevelPiece>();
            piece.states = frames;
            piece.main.sprite = frames[0];
            piece.artFacesLeft = true;
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        return frames;
    }
}
