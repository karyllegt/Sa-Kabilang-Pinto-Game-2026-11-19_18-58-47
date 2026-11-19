using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SaKabilangPinto.UI.EditorTools
{
    /// <summary>
    /// Builds the before/after-stage screens with placeholder boxes and default text:
    /// LevelSelect scene, IntroBox / CompleteBox / ScreenFader prefabs, LevelList asset,
    /// and a stand-in TestLevel scene. Run once from Tools > Sa Kabilang Pinto > Build UI Screens.
    /// Every Image sprite and Text font can be replaced in the Inspector afterwards.
    /// </summary>
    public static class UIScreenBuilder
    {
        const string ScenesUI = "Assets/Scenes/UI";
        const string ScenesTest = "Assets/Scenes/Test";
        const string PrefabsUI = "Assets/Prefabs/UI";

        const string LevelSelectPath = ScenesUI + "/LevelSelect.unity";
        const string TestLevelPath = ScenesTest + "/TestLevel.unity";
        const string FaderPath = PrefabsUI + "/ScreenFader.prefab";
        const string IntroBoxPath = PrefabsUI + "/IntroBox.prefab";
        const string CompleteBoxPath = PrefabsUI + "/CompleteBox.prefab";
        const string LevelListPath = PrefabsUI + "/LevelList.asset";
        const string StoryPath = "Assets/Story/levels.json";

        // Layout in reference pixels (Canvas Scaler reference resolution 480x270).
        static readonly Vector2 ReferenceResolution = new Vector2(480, 270);
        const float ButtonHeight = 48f; // minimum touch height

        // Placeholder colours.
        static readonly Color Dim = new Color(0f, 0f, 0f, 0.6f);
        static readonly Color BoxColor = new Color(0.16f, 0.16f, 0.22f, 1f);
        static readonly Color ButtonColor = new Color(0.35f, 0.35f, 0.5f, 1f);
        static readonly Color BackgroundColor = new Color(0.08f, 0.08f, 0.12f, 1f);

        static readonly (string id, string label)[] CoreLevels =
        {
            ("1", "1"), ("2", "2"), ("4", "4"), ("5", "5"), ("boss", "Boss")
        };

        [MenuItem("Tools/Sa Kabilang Pinto/Build UI Screens")]
        public static void Build()
        {
            var existing = new List<string>();
            foreach (var path in new[] { LevelSelectPath, TestLevelPath, FaderPath, IntroBoxPath, CompleteBoxPath })
            {
                if (AssetDatabase.LoadAssetAtPath<Object>(path) != null) existing.Add(path);
            }
            if (existing.Count > 0 && !EditorUtility.DisplayDialog("Build UI Screens",
                    "These files already exist and will be replaced (any art assigned to them will be lost):\n\n" +
                    string.Join("\n", existing), "Replace", "Cancel"))
            {
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            EnsureFolder("Assets/Scenes", "UI");
            EnsureFolder("Assets/Scenes", "Test");
            EnsureFolder("Assets/Prefabs", "UI");

            var story = AssetDatabase.LoadAssetAtPath<TextAsset>(StoryPath);
            if (story == null)
            {
                Debug.LogWarning("Build UI Screens: " + StoryPath + " was not found. Assign the story file on IntroBox and CompleteBox later.");
            }

            var levelList = LoadOrCreateLevelList();

            // Prefabs are built in a scratch scene, then saved.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var fader = BuildFaderPrefab();
            var introBox = BuildIntroBoxPrefab(story, levelList);
            var completeBox = BuildCompleteBoxPrefab(story, levelList, fader);

            BuildLevelSelectScene(levelList, fader);
            BuildTestLevelScene(introBox, completeBox);
            UpdateBuildSettings();

            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(LevelSelectPath, OpenSceneMode.Single);
            Debug.Log("Build UI Screens: done. Press Play in LevelSelect to test.");
        }

        // ---------- Assets ----------

        static LevelList LoadOrCreateLevelList()
        {
            var list = AssetDatabase.LoadAssetAtPath<LevelList>(LevelListPath);
            if (list != null) return list; // keep any scene names already set in the Inspector

            list = ScriptableObject.CreateInstance<LevelList>();
            foreach (var level in CoreLevels)
            {
                // Every level uses the stand-in test scene until the real level scenes exist.
                list.levels.Add(new LevelList.Entry { id = level.id, sceneName = "TestLevel" });
            }
            AssetDatabase.CreateAsset(list, LevelListPath);
            return list;
        }

        static ScreenFader BuildFaderPrefab()
        {
            var root = CreateCanvas("ScreenFader", 1000);
            var group = root.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;

            var cover = CreateRect("Cover", root.transform);
            Stretch(cover);
            AddImage(cover.gameObject, Color.black);

            var fader = root.AddComponent<ScreenFader>();
            SetRef(fader, "canvasGroup", group);

            return SavePrefab(root, FaderPath).GetComponent<ScreenFader>();
        }

        static IntroBox BuildIntroBoxPrefab(TextAsset story, LevelList levelList)
        {
            var root = CreateCanvas("IntroBox", 10);

            // Full-screen dim layer: tapping anywhere closes the box.
            var panel = CreateRect("Panel", root.transform);
            Stretch(panel);
            var dim = AddImage(panel.gameObject, Dim);
            var tap = panel.gameObject.AddComponent<Button>();
            tap.targetGraphic = dim;
            tap.transition = Selectable.Transition.None;

            var box = CreateRect("Box", panel);
            Place(box, Vector2.zero, new Vector2(360, 190));
            AddImage(box.gameObject, BoxColor);

            var title = CreateText("LevelName", box, "Level Name", 16, TextAnchor.MiddleCenter, FontStyle.Bold);
            Place(title.rectTransform, new Vector2(0, 70), new Vector2(330, 28));

            var intro = CreateText("IntroText", box, "Intro text goes here.", 12, TextAnchor.UpperLeft, FontStyle.Normal);
            Place(intro.rectTransform, new Vector2(0, -5), new Vector2(330, 110));

            var hint = CreateText("TapHint", box, "Tap to begin", 10, TextAnchor.MiddleCenter, FontStyle.Italic);
            Place(hint.rectTransform, new Vector2(0, -78), new Vector2(330, 20));

            var introBox = root.AddComponent<IntroBox>();
            SetRef(introBox, "storyFile", story);
            SetRef(introBox, "levelList", levelList);
            SetRef(introBox, "panel", panel.gameObject);
            SetRef(introBox, "levelNameText", title);
            SetRef(introBox, "introText", intro);
            SetRef(introBox, "tapToClose", tap);

            return SavePrefab(root, IntroBoxPath).GetComponent<IntroBox>();
        }

        static CompleteBox BuildCompleteBoxPrefab(TextAsset story, LevelList levelList, ScreenFader fader)
        {
            var root = CreateCanvas("CompleteBox", 20);

            var panel = CreateRect("Panel", root.transform);
            Stretch(panel);
            AddImage(panel.gameObject, Dim); // blocks taps on the level behind

            var box = CreateRect("Box", panel);
            Place(box, Vector2.zero, new Vector2(420, 250));
            AddImage(box.gameObject, BoxColor);

            var title = CreateText("Title", box, "Level Complete", 16, TextAnchor.MiddleCenter, FontStyle.Bold);
            Place(title.rectTransform, new Vector2(0, 104), new Vector2(390, 26));

            var clue = CreateText("ClueText", box, "Clue text goes here.", 12, TextAnchor.UpperLeft, FontStyle.Normal);
            Place(clue.rectTransform, new Vector2(0, 58), new Vector2(390, 60));

            var collected = CreateText("CollectedText", box, "Flames: 0    Fragments: 0", 12, TextAnchor.MiddleCenter, FontStyle.Normal);
            Place(collected.rectTransform, new Vector2(0, 16), new Vector2(390, 20));

            var ending = CreateText("EndingText", box, "Ending text goes here.", 12, TextAnchor.UpperLeft, FontStyle.Normal);
            Place(ending.rectTransform, new Vector2(0, -32), new Vector2(390, 64));
            ending.gameObject.SetActive(false);

            // Button row: a layout group keeps the buttons centred when Next level is hidden.
            var row = CreateRect("Buttons", box);
            Place(row, new Vector2(0, -94), new Vector2(390, ButtonHeight));
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 8;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var next = CreateButton("NextButton", row, "Next level", new Vector2(120, ButtonHeight));
            var replay = CreateButton("ReplayButton", row, "Replay", new Vector2(120, ButtonHeight));
            var select = CreateButton("LevelSelectButton", row, "Level select", new Vector2(120, ButtonHeight));

            var complete = root.AddComponent<CompleteBox>();
            SetRef(complete, "storyFile", story);
            SetRef(complete, "levelList", levelList);
            SetRef(complete, "faderPrefab", fader);
            SetRef(complete, "panel", panel.gameObject);
            SetRef(complete, "clueText", clue);
            SetRef(complete, "collectedText", collected);
            SetRef(complete, "endingText", ending);
            SetRef(complete, "nextButton", next);
            SetRef(complete, "replayButton", replay);
            SetRef(complete, "levelSelectButton", select);

            panel.gameObject.SetActive(false);
            return SavePrefab(root, CompleteBoxPath).GetComponent<CompleteBox>();
        }

        // ---------- Scenes ----------

        static void BuildLevelSelectScene(LevelList levelList, ScreenFader fader)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera();
            CreateEventSystem();

            var canvas = CreateCanvas("LevelSelectCanvas", 0);

            var background = CreateRect("Background", canvas.transform);
            Stretch(background);
            AddImage(background.gameObject, BackgroundColor);

            var title = CreateText("Title", canvas.transform, "Level Select", 20, TextAnchor.MiddleCenter, FontStyle.Bold);
            Place(title.rectTransform, new Vector2(0, 95), new Vector2(400, 32));

            var nodesRoot = CreateRect("Nodes", canvas.transform);
            Place(nodesRoot, Vector2.zero, new Vector2(400, 64));

            var nodes = new List<LevelNode>();
            const float nodeSize = 56f; // at least 48 px tall
            const float spacing = 72f;
            for (int i = 0; i < CoreLevels.Length; i++)
            {
                float x = (i - (CoreLevels.Length - 1) / 2f) * spacing;
                var button = CreateButton("Node_" + CoreLevels[i].label, nodesRoot, CoreLevels[i].label, new Vector2(nodeSize, nodeSize));
                ((RectTransform)button.transform).anchoredPosition = new Vector2(x, 0);

                var colors = button.colors;
                colors.disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.6f);
                button.colors = colors;

                // Lock icon placeholder (top-right corner), shown while locked.
                var lockMark = CreateRect("LockIcon", button.transform);
                lockMark.anchorMin = lockMark.anchorMax = new Vector2(1, 1);
                lockMark.pivot = new Vector2(1, 1);
                lockMark.anchoredPosition = new Vector2(-3, -3);
                lockMark.sizeDelta = new Vector2(14, 14);
                AddImage(lockMark.gameObject, new Color(0.85f, 0.7f, 0.2f, 1f)).raycastTarget = false;

                var node = button.gameObject.AddComponent<LevelNode>();
                SetInt(node, "levelIndex", i);
                SetRef(node, "button", button);
                SetRef(node, "lockedMark", lockMark.gameObject);
                nodes.Add(node);
            }

            var screen = canvas.AddComponent<LevelSelectScreen>();
            SetRef(screen, "levelList", levelList);
            SetRef(screen, "faderPrefab", fader);
            SetRefArray(screen, "nodes", nodes.ToArray());

            EditorSceneManager.SaveScene(scene, LevelSelectPath);
        }

        static void BuildTestLevelScene(IntroBox introBoxPrefab, CompleteBox completeBoxPrefab)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera();
            CreateEventSystem();

            var hud = CreateCanvas("TestHUD", 0);
            var label = CreateText("Label", hud.transform, "TEST LEVEL (stand-in)", 16, TextAnchor.MiddleCenter, FontStyle.Bold);
            Place(label.rectTransform, new Vector2(0, 20), new Vector2(400, 30));

            var finish = CreateButton("FinishLevelButton", hud.transform, "Finish level", new Vector2(140, ButtonHeight));
            ((RectTransform)finish.transform).anchoredPosition = new Vector2(0, -40);

            PrefabUtility.InstantiatePrefab(introBoxPrefab.gameObject, scene);
            var complete = (GameObject)PrefabUtility.InstantiatePrefab(completeBoxPrefab.gameObject, scene);

            var debug = hud.AddComponent<DebugFinishLevel>();
            SetRef(debug, "button", finish);
            SetRef(debug, "completeBox", complete.GetComponent<CompleteBox>());

            EditorSceneManager.SaveScene(scene, TestLevelPath);
        }

        static void UpdateBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == LevelSelectPath || s.path == TestLevelPath);
            scenes.Insert(0, new EditorBuildSettingsScene(LevelSelectPath, true));
            scenes.Add(new EditorBuildSettingsScene(TestLevelPath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // ---------- Helpers ----------

        static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name);
        }

        static GameObject SavePrefab(GameObject root, string path)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        static void CreateCamera()
        {
            var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            go.tag = "MainCamera";
            go.transform.position = new Vector3(0, 0, -10);
            var cam = go.GetComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
        }

        static void CreateEventSystem()
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        static GameObject CreateCanvas(string name, int sortingOrder)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.layer = LayerMask.NameToLayer("UI");

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f; // keep 270 px height on wider phones
            scaler.referencePixelsPerUnit = 16f; // matches 16 px sprites at 16 Pixels Per Unit
            return go;
        }

        static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static void Place(RectTransform rt, Vector2 position, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
        }

        static Image AddImage(GameObject go, Color color)
        {
            var image = go.AddComponent<Image>();
            image.color = color; // placeholder: assign a Sprite in the Inspector to replace
            return image;
        }

        static Text CreateText(string name, Transform parent, string content, int size, TextAnchor alignment, FontStyle style)
        {
            var rt = CreateRect(name, parent);
            var text = rt.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // replace in the Inspector
            text.text = content;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        static Button CreateButton(string name, Transform parent, string label, Vector2 size)
        {
            var rt = CreateRect(name, parent);
            rt.sizeDelta = size;
            var image = AddImage(rt.gameObject, ButtonColor);
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            var text = CreateText("Label", rt, label, 12, TextAnchor.MiddleCenter, FontStyle.Normal);
            Stretch(text.rectTransform);
            return button;
        }

        static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(field);
            if (property == null)
            {
                Debug.LogError("Build UI Screens: field '" + field + "' not found on " + target.GetType().Name);
                return;
            }
            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetInt(Object target, string field, int value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetRefArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(field);
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
