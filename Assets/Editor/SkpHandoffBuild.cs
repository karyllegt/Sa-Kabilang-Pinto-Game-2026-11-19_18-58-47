using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class SkpHandoffBuild
{
    public static void ApplyBranding()
    {
        var icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Recovered/Brand/AppIcon.png");
        if (icon == null) throw new InvalidOperationException("Launcher icon has not been imported.");
        foreach (var target in new[] { UnityEditor.Build.NamedBuildTarget.Unknown, UnityEditor.Build.NamedBuildTarget.Standalone })
        {
            var sizes = PlayerSettings.GetIconSizes(target, IconKind.Any);
            if (sizes.Length > 0) PlayerSettings.SetIcons(target, Enumerable.Repeat(icon, sizes.Length).ToArray(), IconKind.Any);
        }
        if (BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
        {
            var target = UnityEditor.Build.NamedBuildTarget.Android;
            var sizes = PlayerSettings.GetIconSizes(target, IconKind.Any);
            if (sizes.Length > 0) PlayerSettings.SetIcons(target, Enumerable.Repeat(icon, sizes.Length).ToArray(), IconKind.Any);
            ApplyAndroidIconLayers(target, icon);
        }
    }

    static void ApplyAndroidIconLayers(UnityEditor.Build.NamedBuildTarget target, Texture2D artwork)
    {
        const string clearPath = "Assets/Art/Recovered/Brand/AdaptiveClear.png";
        var clear = AssetDatabase.LoadAssetAtPath<Texture2D>(clearPath);
        if (clear == null)
        {
            // A native transparent utility layer lets the complete doorway artwork fill the background.
            var utility = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            utility.SetPixels(new Color[32 * 32]); utility.Apply();
            File.WriteAllBytes(clearPath, utility.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(utility);
            AssetDatabase.ImportAsset(clearPath, ImportAssetOptions.ForceSynchronousImport);
            clear = AssetDatabase.LoadAssetAtPath<Texture2D>(clearPath);
        }
        foreach (var kind in PlayerSettings.GetSupportedIconKinds(target))
        {
            bool adaptive = kind.ToString().IndexOf("Adaptive", StringComparison.OrdinalIgnoreCase) >= 0;
            bool round = kind.ToString().IndexOf("Round", StringComparison.OrdinalIgnoreCase) >= 0;
            if (!adaptive && !round) continue;
            var slots = PlayerSettings.GetPlatformIcons(target, kind);
            var labels = typeof(PlatformIconKind).GetProperty("customLayerLabels",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(kind) as string[];
            foreach (var slot in slots)
            {
                var textures = Enumerable.Repeat(artwork, slot.minLayerCount).ToArray();
                if (adaptive)
                    for (int layer = 0; layer < textures.Length; layer++)
                    {
                        bool foreground = labels != null && layer < labels.Length
                            ? labels[layer].IndexOf("Foreground", StringComparison.OrdinalIgnoreCase) >= 0 : layer == 0;
                        if (foreground) textures[layer] = clear;
                    }
                slot.SetTextures(textures);
            }
            PlayerSettings.SetPlatformIcons(target, kind, slots);
        }
    }

    // Start Unity with -buildTarget Android. Environment overrides avoid machine paths in the repo.
    public static void Android()
    {
        ApplyBranding();
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            throw new InvalidOperationException("Install this editor's Android module first.");
        var external = Type.GetType("UnityEditor.Android.AndroidExternalToolsSettings, UnityEditor.Android.Extensions", true);
        string[] properties = { "sdkRootPath", "ndkRootPath", "jdkRootPath" };
        string[] variables = { "SKP_ANDROID_SDK", "SKP_ANDROID_NDK", "SKP_ANDROID_JDK" };
        var originalPaths = new object[properties.Length];
        var target = UnityEditor.Build.NamedBuildTarget.Android;
        string originalId = PlayerSettings.GetApplicationIdentifier(target);
        bool originalBundle = EditorUserBuildSettings.buildAppBundle;
        try
        {
            for (int i = 0; i < properties.Length; i++)
            {
                var property = external.GetProperty(properties[i]);
                originalPaths[i] = property.GetValue(null);
                string supplied = Environment.GetEnvironmentVariable(variables[i]);
                if (!string.IsNullOrEmpty(supplied)) property.SetValue(null, supplied);
            }
            Directory.CreateDirectory("Builds/Presentation/Android");
            PlayerSettings.SetApplicationIdentifier(target, "com.pangkatparol.sakabilangpinto.playtest");
            EditorUserBuildSettings.buildAppBundle = false;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                target = BuildTarget.Android, locationPathName = "Builds/Presentation/Android/SaKabilangPinto-playtest.apk",
                options = BuildOptions.None
            });
            string result = $"Android build: {report.summary.result}; errors {report.summary.totalErrors}; warnings {report.summary.totalWarnings}; bytes {report.summary.totalSize}.";
            Directory.CreateDirectory("tests/Results");
            File.WriteAllText("tests/Results/presentation_android_build.txt", result);
            Debug.Log(result);
            if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException(result);
        }
        finally
        {
            PlayerSettings.SetApplicationIdentifier(target, originalId);
            EditorUserBuildSettings.buildAppBundle = originalBundle;
            for (int i = 0; i < properties.Length; i++)
                if (originalPaths[i] != null) external.GetProperty(properties[i]).SetValue(null, originalPaths[i]);
        }
    }

    public static void Windows()
    {
        ApplyBranding();
        Directory.CreateDirectory("Builds/Presentation/Windows");
        var options = new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            target = BuildTarget.StandaloneWindows64,
            locationPathName = "Builds/Presentation/Windows/SaKabilangPinto.exe",
            options = BuildOptions.None
        };
        var report = BuildPipeline.BuildPlayer(options);
        string result = $"Windows build: {report.summary.result}; errors {report.summary.totalErrors}; warnings {report.summary.totalWarnings}; bytes {report.summary.totalSize}.";
        Directory.CreateDirectory("tests/Results");
        File.WriteAllText("tests/Results/presentation_windows_build.txt", result);
        Debug.Log(result);
        if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException(result);
        string licences = "Builds/Presentation/Windows/Licences";
        Directory.CreateDirectory(licences);
        File.Copy("AUDIO_SOURCES.md", licences + "/AUDIO_SOURCES.md", true);
        File.Copy("Assets/Audio/Licensed/Foley/LICENSE.txt", licences + "/Kenney_CC0.txt", true);
        File.Copy("Assets/Audio/Licensed/Foley/RECORDING_CREDITS.txt", licences + "/RECORDING_CREDITS.txt", true);
        File.Copy("Assets/Audio/Licensed/Music/ATTRIBUTION.txt", licences + "/MUSIC_ATTRIBUTION.txt", true);
        File.Copy("Assets/Audio/Licensed/Foley/church_bell.wav", licences + "/church_bell_CC-BY-SA-3.0.wav", true);
        File.Copy("reference/audio-licenses/churchbell.flac", licences + "/churchbell_original_CC-BY-SA-3.0.flac", true);
    }
}
