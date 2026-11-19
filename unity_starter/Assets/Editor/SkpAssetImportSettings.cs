// Applies pixel-art import settings to everything under Assets/Art and Assets/Audio.
// Drop this in Assets/Editor/. Review once in the Unity Editor; it was written without running Unity.
using UnityEditor;
using UnityEngine;

public class SkpAssetImportSettings : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Art/")) return;
        var ti = (TextureImporter)assetImporter;
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = 16;
        ti.filterMode = FilterMode.Point;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.npotScale = TextureImporterNPOTScale.None;
        // Bottom-center pivot for characters and enemies, center for everything else.
        var settings = new TextureImporterSettings();
        ti.ReadTextureSettings(settings);
        string n = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        bool character = n.StartsWith("spr_pl_") || n.StartsWith("spr_anino") || n.StartsWith("spr_tik") ||
                         n.StartsWith("spr_manan") || n.StartsWith("spr_boss") || n.StartsWith("spr_lola");
        settings.spriteAlignment = (int)(character ? SpriteAlignment.BottomCenter : SpriteAlignment.Center);
        ti.SetTextureSettings(settings);
        // 9-slice border for the UI box (6 px on each side).
        if (n == "ui_pixelbox_9slice_border6") ti.spriteBorder = new Vector4(6, 6, 6, 6);
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
