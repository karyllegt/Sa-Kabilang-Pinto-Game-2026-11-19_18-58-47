// Applies pixel-art import settings to everything under Assets/Art and Assets/Audio.
// Based on unity_starter/Assets/Editor/SkpAssetImportSettings.cs, reviewed and fixed in M1:
//  - spr_lolaP (a portrait) no longer gets the bottom-center character pivot,
//  - all sprites use a Full Rect mesh (needed for tiled ledges and 9-slicing),
//  - ledge tiles get a 16 px left/right border so their middle 16 px repeats.
using UnityEditor;
using UnityEngine;

public class SkpAssetImportSettings : AssetPostprocessor
{
    public const int PixelsPerUnit = 16;

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Art/")) return;
        var ti = (TextureImporter)assetImporter;
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = PixelsPerUnit;
        ti.filterMode = FilterMode.Point;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.npotScale = TextureImporterNPOTScale.None;
        ti.wrapMode = TextureWrapMode.Clamp;

        var settings = new TextureImporterSettings();
        ti.ReadTextureSettings(settings);
        string n = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        settings.spriteAlignment = (int)(IsCharacter(n) ? SpriteAlignment.BottomCenter : SpriteAlignment.Center);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteExtrude = 0;
        ti.SetTextureSettings(settings);

        ti.spriteBorder = BorderFor(n);
    }

    public static bool IsCharacter(string n)
    {
        if (n.StartsWith("spr_lolaP")) return false; // dialog portrait
        return n.StartsWith("spr_pl_") || n.StartsWith("spr_anino") || n.StartsWith("spr_tik") ||
               n.StartsWith("spr_manan") || n.StartsWith("spr_boss") || n.StartsWith("spr_lola");
    }

    public static Vector4 BorderFor(string n)
    {
        if (n == "ui_pixelbox_9slice_border6") return new Vector4(6, 6, 6, 6);
        if (n.StartsWith("tile_") && n.EndsWith("_ledge48x16")) return new Vector4(16, 0, 16, 0);
        return Vector4.zero;
    }

    void OnPreprocessAudio()
    {
        if (!assetPath.StartsWith("Assets/Audio/")) return;
        var ai = (AudioImporter)assetImporter;
        var s = ai.defaultSampleSettings;
        bool music = System.IO.Path.GetFileName(assetPath).StartsWith("mus_");
        s.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
        s.compressionFormat = AudioCompressionFormat.Vorbis;
        s.quality = music ? 0.5f : 0.7f;
        ai.defaultSampleSettings = s;
        ai.forceToMono = true;
    }
}
