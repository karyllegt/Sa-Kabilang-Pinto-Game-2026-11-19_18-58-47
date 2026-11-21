using System.IO;
using System.Linq;
using NUnit.Framework;
using Skp.Levels;
using Skp.Save;
using Skp.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Skp.Tests
{
    /// <summary>M5 logic without scenes: map model, Lola's lines, strings, settings, dialog line budget, contrast.</summary>
    public class UiModelTests
    {
        static SaveData Save(int unlocked, bool cleared = false) => new SaveData { unlocked = unlocked, cleared = cleared };

        [Test]
        public void NodeStates_FollowTheUnlockRule()
        {
            var s = Save(4);
            var m = new MapModel(s);
            for (int i = 0; i < 9; i++)
            {
                var expect = i < 3 ? NodeState.Cleared : i < 4 ? NodeState.Ready : NodeState.Locked;
                Assert.AreEqual(expect, m.State(i), "node " + i);
            }
            var done = new MapModel(Save(9, cleared: true));
            Assert.AreEqual(NodeState.Cleared, done.State(8), "the last node is cleared only by finishing the story");
            Assert.AreEqual(NodeState.Ready, new MapModel(Save(9)).State(8));
        }

        [Test]
        public void Map_OpensOnNewestNode_AndFromALevel_WalksToTheNext()
        {
            var m = new MapModel(Save(3));
            Assert.AreEqual(2, m.Selected);
            Assert.AreEqual(2, m.At);
            Assert.IsFalse(m.Walking);

            var back = new MapModel(Save(3), from: 1);
            Assert.AreEqual(1, back.At, "token starts on the level just played");
            Assert.AreEqual(2, back.Selected, "and is sent to the next one");
            Assert.IsTrue(back.Walking);
            Assert.IsFalse(back.CanEnter, "cannot enter while walking");
            for (int i = 0; i < 300 && back.Walking; i++) back.Step(1 / 60f);
            Assert.AreEqual(2, back.At);
            Assert.AreEqual(MapModel.Nodes[2], back.Token);
            Assert.IsTrue(back.CanEnter);
        }

        [Test]
        public void Select_WalksNodeByNode_AndClampsToUnlocked()
        {
            var m = new MapModel(Save(6));
            Assert.AreEqual(5, m.Selected);
            Assert.IsTrue(m.Select(1));
            CollectionAssert.AreEqual(new[] { 4, 3, 2, 1 }, m.Queue.ToArray());
            Assert.IsTrue(m.Select(8));
            Assert.AreEqual(5, m.Selected, "cannot walk past the last unlocked node");
            Assert.IsFalse(new MapModel(Save(6)).Select(5), "selecting where you stand changes nothing");
            // speed: 2.3 px per 60 Hz frame
            var w = new MapModel(Save(2), from: 0);
            float d = Vector2.Distance(MapModel.Nodes[0], MapModel.Nodes[1]);
            int frames = 0;
            while (w.Walking) { w.Step(1 / 60f); frames++; }
            Assert.AreEqual(Mathf.CeilToInt(d / 2.3f), frames, 1);
        }

        [Test]
        public void LockedNode_IsPeekedNotEntered()
        {
            var m = new MapModel(Save(2));
            m.PeekLocked(5);
            Assert.AreEqual(5, m.InfoNode);
            Assert.AreEqual(NodeState.Locked, m.State(m.InfoNode));
            Assert.AreEqual(1, m.Selected, "peeking does not move the selection");
            m.Select(0);
            Assert.AreEqual(-1, m.Peek, "walking clears the peek");
        }

        [Test]
        public void NodeAt_FindsTapTargets()
        {
            for (int i = 0; i < 9; i++) Assert.AreEqual(i, MapModel.NodeAt(MapModel.Nodes[i] + new Vector2(3, -8)));
            Assert.AreEqual(-1, MapModel.NodeAt(new Vector2(700, 20)));
            Assert.IsTrue(MapModel.LolaAt(MapModel.Lola + new Vector2(7, 10)));
            var p = new Vector2(123, 45);
            Assert.That(Vector2.Distance(MapModel.ToMap(MapModel.ToWorld(p)), p), Is.LessThan(1e-3f));
        }

        [Test]
        public void Lola_FirstMeetingThenProgressLines_AndMarker()
        {
            var s = Save(1);
            Assert.AreEqual(5, MapModel.LolaKeys(s).Length, "first meeting");
            Assert.IsTrue(new MapModel(s).LolaHasNews);
            s.metLola = true; s.lolaSeen = 1;
            Assert.IsFalse(new MapModel(s).LolaHasNews, "nothing new after she spoke");
            Assert.AreEqual("lola.act1", MapModel.LolaKeys(s)[0]);
            s.unlocked = 4;
            Assert.IsTrue(new MapModel(s).LolaHasNews, "new level unlocked: the ! comes back");
            Assert.AreEqual("lola.act2a", MapModel.LolaKeys(s)[0]);
            s.unlocked = 7; Assert.AreEqual("lola.act2b", MapModel.LolaKeys(s)[0]);
            s.unlocked = 9; Assert.AreEqual("lola.act3", MapModel.LolaKeys(s)[0]);
            s.cleared = true; Assert.AreEqual("lola.cleared", MapModel.LolaKeys(s)[0]);
        }

        [Test]
        public void StringTable_HasEveryKey_AndNoScreenUsesAMissingOne()
        {
            var table = AssetDatabase.LoadAssetAtPath<StringTable>("Assets/Resources/UI/Strings_en.asset");
            Assert.IsNotNull(table, "string table asset");
            foreach (var k in StringDefaults.En.Keys) Assert.IsNotNull(table.Get(k), "missing key " + k);
            // every Loc.Get("...")/Loc.Format("...") literal in the UI code exists
            foreach (var f in Directory.GetFiles("Assets/Scripts", "*.cs", SearchOption.AllDirectories))
                foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(File.ReadAllText(f), "Loc\\.(?:Get|Format)\\(\"([a-z0-9_.]+)\"\\s*[,)]"))
                    Assert.IsTrue(Loc.Has(m.Groups[1].Value), $"{Path.GetFileName(f)} uses unknown key {m.Groups[1].Value}");
        }

        [Test]
        public void Settings_RoundTrip_AndCorruptFileFallsBack()
        {
            var dir = Path.Combine(Path.GetTempPath(), "skp_settings_" + System.Guid.NewGuid().ToString("N"));
            try
            {
                var a = new SettingsStore(dir);
                a.Load();
                Assert.IsTrue(a.Data.music);
                a.Data.leftHanded = true; a.Data.touch = TouchMode.On; a.Data.music = false;
                int changed = 0;
                a.Changed += _ => changed++;
                a.Save();
                Assert.AreEqual(1, changed);
                var b = new SettingsStore(dir);
                b.Load();
                Assert.IsTrue(b.Data.leftHanded);
                Assert.AreEqual(TouchMode.On, b.Data.touch);
                Assert.IsFalse(b.Data.music);
                File.WriteAllText(b.PathOnDisk, "{not json");
                UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("unreadable settings"));
                var c = new SettingsStore(dir);
                c.Load();
                Assert.IsTrue(c.Data.music, "defaults after a corrupt file");
            }
            finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
        }

        [Test]
        public void Palette_MeetsContrast()
        {
            var th = AssetDatabase.LoadAssetAtPath<UiTheme>("Assets/Resources/UI/UiTheme.asset");
            Assert.IsNotNull(th);
            Assert.GreaterOrEqual(UiTheme.Contrast(th.text, th.panel), 7f, "body text");
            Assert.GreaterOrEqual(UiTheme.Contrast(th.textDim, th.panel), 7f, "secondary text");
            Assert.GreaterOrEqual(UiTheme.Contrast(th.text, th.highlight), 7f, "selected row");
            foreach (var c in new[] { th.gold, th.goldBright, th.teal, th.violet, th.danger })
                Assert.GreaterOrEqual(UiTheme.Contrast(c, th.panel), 4.5f, "accent " + c);
        }

        [Test]
        public void DialogTexts_FitTheBox_AtTheNarrowestScreen()
        {
            // The narrowest canvas among the target screens is 4:3 (1024x768 -> 831x623 at match 0.5).
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Screens/DialogBox.prefab");
            Assert.IsNotNull(prefab, "DialogBox prefab");
            var root = new GameObject("root", typeof(RectTransform));
            try
            {
                var rt = (RectTransform)root.transform;
                rt.sizeDelta = new Vector2(831, 483);
                var go = (GameObject)Object.Instantiate(prefab, root.transform);
                go.SetActive(true);
                var d = go.GetComponent<DialogBox>();
                for (int pass = 0; pass < 3; pass++)
                    foreach (var g in go.GetComponentsInChildren<LayoutGroup>(true).Reverse()) LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)g.transform);
                var body = d.body;
                float w = body.rectTransform.rect.width, h = body.rectTransform.rect.height;
                Assert.Greater(w, 300, "dialog body width");

                var texts = new System.Collections.Generic.List<(string, string)>();
                foreach (var g in AssetDatabase.FindAssets("t:LevelConfig"))
                {
                    var lc = AssetDatabase.LoadAssetAtPath<LevelConfig>(AssetDatabase.GUIDToAssetPath(g));
                    Assert.IsFalse(string.IsNullOrWhiteSpace(lc.spec.name), lc.name + " name");
                    Assert.IsFalse(string.IsNullOrWhiteSpace(lc.spec.intro), lc.name + " intro");
                    // the boss level (9) has no letter, as in the prototype: the bell itself ends the story
                    if (!lc.spec.boss) Assert.IsFalse(string.IsNullOrWhiteSpace(lc.spec.clue), lc.name + " clue");
                    texts.Add((lc.name + " intro", lc.spec.intro));
                    if (!lc.spec.boss) texts.Add((lc.name + " clue", lc.spec.clue));
                }
                Assert.AreEqual(17, texts.Count, "nine intros and eight letters");
                foreach (var k in StringDefaults.En.Keys.Where(k => k.StartsWith("lola.") && k.Contains("."))) texts.Add((k, Loc.Get(k)));
                texts.Add(("endless intro", Loc.Get("level.endless.intro")));
                foreach (var s in Skp.Skills.SkillCatalog.Defaults) texts.Add(("skill " + s.id, s.description));
                foreach (var (name, text) in texts)
                {
                    var size = body.GetPreferredValues(text, w, 0);
                    Assert.LessOrEqual(size.y, h + 0.5f, $"{name} needs {size.y:0} px, the box has {h:0}: \"{text}\"");
                }
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void Objective_FollowsThePrototype()
        {
            var cfg = AssetDatabase.LoadAssetAtPath<LevelConfig>("Assets/Data/Levels/LevelConfig_01.asset");
            var dir = Path.Combine(Path.GetTempPath(), "skp_obj_" + System.Guid.NewGuid().ToString("N"));
            try
            {
                var save = new SaveService(dir); save.Load();
                var s = new LevelSession(cfg.spec, new Skp.Core.LevelRequest { Level = 0 }, ScriptableObject.CreateInstance<Skp.Player.PlayerTuning>(),
                                         ScriptableObject.CreateInstance<Skp.Core.DifficultyTable>(), ScriptableObject.CreateInstance<DifficultyCurve>(), save);
                Assert.AreEqual($"CLIMB TO THE LANTERN ABOVE - room 1 of {s.Layout.Rooms}", HudView.Objective(s));
                s.Sim.SetDimension(Skp.Dimensions.Dim.Spirit);
                StringAssert.StartsWith("SPIRIT", HudView.Objective(s));
                s.Sim.SetDimension(Skp.Dimensions.Dim.Ordinary);
                s.Sim.Checkpoint = s.Layout.ClueRoom;
                Assert.AreEqual("FIND THE CLUE IN THIS ROOM", HudView.Objective(s));
                s.Sim.Checkpoint = s.Layout.Rooms;
                Assert.AreEqual("FIND THE CLUE, THEN THE FINAL DOOR", HudView.Objective(s));
                s.Sim.HasClue = true;
                Assert.AreEqual("ENTER THE FINAL DOOR", HudView.Objective(s));
            }
            finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
        }
    }
}
