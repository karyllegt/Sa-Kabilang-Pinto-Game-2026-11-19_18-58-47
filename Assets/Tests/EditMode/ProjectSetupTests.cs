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
            // M5 thumb layout: left, right, jump, dash, ward, door (pause moved to the HUD; veil and bolt arrive in M6)
            Assert.GreaterOrEqual(buttons.Length, 6);
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
            // per second and in units: 7.3 px/frame -> 27.375 u/s, 0.42 px/frame^2 -> 94.5 u/s^2
            Assert.AreEqual(7.3f * 60f / 16f, t.jumpSpeed, 1e-4f);
            Assert.AreEqual(0.42f * 3600f / 16f, t.gravity, 1e-4f);
            Assert.AreEqual(1.95f * 60f / 16f, t.runSpeed, 1e-4f);
            Assert.AreEqual(6, Skp.Player.PlayerTuning.Ticks(t.coyoteTime));
            Assert.AreEqual(7, Skp.Player.PlayerTuning.Ticks(t.jumpBufferTime));
            Assert.AreEqual(11, Skp.Player.PlayerTuning.Ticks(t.dashDuration));
            Assert.AreEqual(34, Skp.Player.PlayerTuning.Ticks(t.dashCooldown));
            Assert.AreEqual(85, Skp.Player.PlayerTuning.Ticks(t.invulnerableTime));
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<DifficultyTable>("Assets/Data/DifficultyTable.asset"));
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Skp.Levels.DifficultyCurve>("Assets/Data/DifficultyCurve.asset"));
        }

        [Test]
        public void PlayerPrefab_HasBothCharactersFourFrames()
        {
            var p = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player.prefab");
            Assert.IsNotNull(p);
            var pc = p.GetComponent<Skp.Player.PlayerController>();
            Assert.IsNotNull(pc.tuning);
            Assert.IsNotNull(pc.difficulties);
            var v = p.GetComponentInChildren<Skp.Player.PlayerView>();
            Assert.AreEqual(4, v.girl.Length);
            Assert.AreEqual(4, v.boy.Length);
            Assert.IsTrue(v.girl.All(s => s != null) && v.boy.All(s => s != null));
        }

        [Test]
        public void TestRoom_HasLedgesAt1To4Tiles_SpikesAndLanterns()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Dev/TestRoom.unity", OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects();
                var world = roots.SelectMany(g => g.GetComponentsInChildren<Skp.Levels.LevelWorldAuthoring>()).Single().Build();
                foreach (var h in new[] { 1f, 2f, 3f, 4f })
                    Assert.IsTrue(world.Ledges.Any(l => Mathf.Approximately(l.Rect.yMax, h)), "ledge top at " + h + " tiles");
                Assert.GreaterOrEqual(world.Hazards.Count, 1);
                Assert.GreaterOrEqual(world.Lanterns.Count, 2);
                var pc = roots.SelectMany(g => g.GetComponentsInChildren<Skp.Player.PlayerController>()).Single();
                Assert.IsNotNull(pc.worldSource);
                Assert.IsNotNull(pc.cameraFollow);
                CollectionAssert.DoesNotContain(EditorBuildSettings.scenes.Select(s => s.path).ToArray(), "Assets/Scenes/Dev/TestRoom.unity", "dev scene stays out of the build");
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        [Test]
        public void PixelFont_AssetExists_InResources()
        {
            Assert.IsNotNull(Resources.Load<TMPro.TMP_FontAsset>(DebugOverlay.FontResource));
        }
    }
}
