using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Skp.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace Skp.Tests
{
    /// <summary>Checks the M1 foundation: imports, input, scenes, camera and project settings.</summary>
    public class ProjectSetupTests
    {
        const string ActionsPath = "Assets/Settings/Input/SkpControls.inputactions";

        // ------------------------------------------------------------ imports

        [Test]
        public void EveryArtTexture_IsPointFiltered16PpuUncompressedSprite()
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art" });
            Assert.GreaterOrEqual(guids.Length, 56, "all provided sprites imported");
            foreach (var g in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                if (!path.EndsWith(".png")) continue; // TMP atlas sub-assets live elsewhere
                var ti = (TextureImporter)AssetImporter.GetAtPath(path);
                Assert.AreEqual(TextureImporterType.Sprite, ti.textureType, path);
                Assert.AreEqual(FilterMode.Point, ti.filterMode, path);
                Assert.AreEqual(16f, ti.spritePixelsPerUnit, path);
                Assert.AreEqual(TextureImporterCompression.Uncompressed, ti.textureCompression, path);
                Assert.IsFalse(ti.mipmapEnabled, path);
            }
        }

        [Test]
        public void PixelBox_Has6pxNineSliceBorder_AndLedgesRepeatTheirMiddle()
        {
            var box = (TextureImporter)AssetImporter.GetAtPath("Assets/Art/UI/ui_pixelbox_9slice_border6.png");
            Assert.AreEqual(new Vector4(6, 6, 6, 6), box.spriteBorder);
            var ledge = (TextureImporter)AssetImporter.GetAtPath("Assets/Art/Tiles/tile_wood_ledge48x16.png");
            Assert.AreEqual(new Vector4(16, 0, 16, 0), ledge.spriteBorder);
        }

        [Test]
        public void CharacterPivots_AreBottomCenter_PortraitsCentered()
        {
            Assert.IsTrue(SkpAssetImportSettings.IsCharacter("spr_pl_girl_0"));
            Assert.IsTrue(SkpAssetImportSettings.IsCharacter("spr_lola"));
            Assert.IsFalse(SkpAssetImportSettings.IsCharacter("spr_lolaP"));
            var s = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/spr_pl_boy_0.png");
            Assert.AreEqual(s.rect.width / 2f, s.pivot.x, 0.01f);
            Assert.AreEqual(0f, s.pivot.y, 0.01f);
        }

        [Test]
        public void EveryAudioClip_IsImported()
        {
            Assert.AreEqual(18, AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio/Sfx" }).Length);
            Assert.AreEqual(5, AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio/Music" }).Length);
        }

        // ------------------------------------------------------------ input

        [Test]
        public void ProjectWideActions_AreSkpControls()
        {
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsPath);
            Assert.IsNotNull(asset);
            Assert.AreSame(asset, InputSystem.actions);
        }

        [Test]
        public void EveryGameplayAction_HasKeyboardAndGamepadBindings()
        {
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsPath);
            var map = asset.FindActionMap(InputReader.GameplayMap, true);
            CollectionAssert.AreEquivalent(InputReader.GameplayActions, map.actions.Select(a => a.name).ToArray());
            foreach (var a in map.actions)
            {
                Assert.IsTrue(a.bindings.Any(b => b.path != null && b.path.StartsWith("<Keyboard>")), a.name + " keyboard");
                Assert.IsTrue(a.bindings.Any(b => b.path != null && b.path.StartsWith("<Gamepad>")), a.name + " gamepad");
            }
            Assert.IsNotNull(asset.FindActionMap("UI"), "UI map for the EventSystem");
        }

        [Test]
        public void TouchButtons_DriveBoundGamepadControls()
        {
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsPath);
            var bound = asset.FindActionMap(InputReader.GameplayMap).bindings.Select(b => b.path).ToList();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/TouchControls.prefab");
            Assert.IsNotNull(prefab);
            var buttons = prefab.GetComponentsInChildren<OnScreenButton>(true);
            Assert.GreaterOrEqual(buttons.Length, 7);
            foreach (var b in buttons)
                Assert.Contains(b.controlPath, bound, b.name + " must drive a bound control");
        }

        [Test]
        public void NoLegacyInputApi_InGameCode()
        {
            var legacy = new Regex(@"(?<![\w.])Input\.(GetKey|GetButton|GetAxis|GetMouseButton|touchCount|GetTouch|mousePosition|anyKey)|UnityEngine\.Input\.");
            foreach (var file in Directory.GetFiles("Assets/Scripts", "*.cs", SearchOption.AllDirectories))
                Assert.IsFalse(legacy.IsMatch(File.ReadAllText(file)), "legacy Input API in " + file);
        }

        // ------------------------------------------------------------ scenes and camera

        [Test]
        public void BuildSettings_AreBootTitleMapLevel_InOrder()
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => Path.GetFileNameWithoutExtension(s.path)).ToArray();
            CollectionAssert.AreEqual(SceneNames.All, scenes);
        }

        [Test]
        public void EveryScene_HasPixelPerfectCamera_480x270_16ppu()
        {
            foreach (var name in SceneNames.All)
            {
                var scene = EditorSceneManager.OpenScene($"Assets/Scenes/{name}.unity", OpenSceneMode.Additive);
                try
                {
                    var ppc = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<PixelPerfectCamera>(true)).SingleOrDefault();
                    Assert.IsNotNull(ppc, name);
                    Assert.AreEqual(16, ppc.assetsPPU, name);
                    Assert.AreEqual(480, ppc.refResolutionX, name);
                    Assert.AreEqual(270, ppc.refResolutionY, name);
                    foreach (var scaler in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<CanvasScaler>(true)))
                    {
                        Assert.AreEqual(CanvasScaler.ScaleMode.ScaleWithScreenSize, scaler.uiScaleMode, name);
                        Assert.AreEqual(new Vector2(960, 540), scaler.referenceResolution, name);
                        Assert.AreEqual(0.5f, scaler.matchWidthOrHeight, name);
                    }
                }
                finally
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        [Test]
        public void LandscapeOnly()
        {
            Assert.IsFalse(PlayerSettings.allowedAutorotateToPortrait);
            Assert.IsFalse(PlayerSettings.allowedAutorotateToPortraitUpsideDown);
            Assert.IsTrue(PlayerSettings.allowedAutorotateToLandscapeLeft);
            Assert.IsTrue(PlayerSettings.allowedAutorotateToLandscapeRight);
        }

        [Test]
        public void DataAssets_Exist_WithPrototypeNumbers()
        {
            var t = AssetDatabase.LoadAssetAtPath<Skp.Player.PlayerTuning>("Assets/Data/PlayerTuning.asset");
            Assert.IsNotNull(t);
            Assert.AreEqual(7.3f, t.jumpImpulse, 1e-4f);
            Assert.AreEqual(0.42f, t.gravity, 1e-4f);
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Skp.Levels.DifficultyCurve>("Assets/Data/DifficultyCurve.asset"));
        }

        [Test]
        public void PixelFont_AssetExists_InResources()
        {
            Assert.IsNotNull(Resources.Load<TMPro.TMP_FontAsset>(DebugOverlay.FontResource));
        }
    }
}
