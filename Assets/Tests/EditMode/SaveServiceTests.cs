using System.IO;
using NUnit.Framework;
using Skp.Save;
using UnityEngine;
using UnityEngine.TestTools;

namespace Skp.Tests
{
    public class SaveServiceTests
    {
        string _dir;
        SaveService _svc;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "skp_save_tests_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
            _svc = new SaveService(_dir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        [Test]
        public void RoundTrip_PreservesEveryField()
        {
            _svc.Load();
            var d = _svc.Data;
            d.unlocked = 5; d.@char = "boy"; d.diff = "nightmare"; d.best = 1234;
            d.cleared = false; d.metLola = true; d.lolaSeen = 4; d.skills = new[] { "dj", "veil" };
            d.cont = new ResumeState { active = true, lv = 4, dfKey = "nightmare", loop = 0, cp = 2, vit = 3, flame = 77, lives = 2, score = 900, clue = true, shards = 2 };
            _svc.Save();

            var other = new SaveService(_dir);
            var r = other.Load();
            Assert.AreEqual(SaveLoadResult.Loaded, other.LastLoadResult);
            Assert.AreEqual(5, r.unlocked);
            Assert.AreEqual("boy", r.@char);
            Assert.AreEqual("nightmare", r.diff);
            Assert.AreEqual(1234, r.best);
            Assert.IsTrue(r.metLola);
            Assert.AreEqual(4, r.lolaSeen);
            CollectionAssert.AreEqual(new[] { "dj", "veil" }, r.skills);
            Assert.IsTrue(r.HasContinue);
            Assert.AreEqual(4, r.cont.lv);
            Assert.AreEqual(2, r.cont.cp);
            Assert.AreEqual(77, r.cont.flame);
            Assert.AreEqual(2, r.cont.lives);
            Assert.IsTrue(r.cont.clue);
            Assert.AreEqual(SaveService.CurrentVersion, r.version);
        }

        [Test]
        public void MissingFile_GivesDefaults()
        {
            var d = _svc.Load();
            Assert.AreEqual(SaveLoadResult.Missing, _svc.LastLoadResult);
            Assert.AreEqual(1, d.unlocked);
            Assert.AreEqual("girl", d.@char);
            Assert.AreEqual("standard", d.diff);
            Assert.IsFalse(d.cleared);
            Assert.IsFalse(d.metLola);
            Assert.IsFalse(d.HasContinue);
            Assert.IsEmpty(d.skills);
        }

        [Test]
        public void CorruptFile_NoBackup_RecoversToDefaults_AndKeepsBrokenFile()
        {
            File.WriteAllText(_svc.MainPath, "{bad json");
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("unreadable"));
            var d = _svc.Load();
            Assert.AreEqual(SaveLoadResult.CorruptReset, _svc.LastLoadResult);
            Assert.AreEqual(1, d.unlocked);
            Assert.IsTrue(File.Exists(_svc.CorruptPath), "broken file kept for inspection");
            Assert.IsFalse(File.Exists(_svc.MainPath), "broken file removed from the main slot");
        }

        [Test]
        public void CorruptFile_WithBackup_RecoversFromBak_AndBakSurvives()
        {
            _svc.Load();
            _svc.Data.unlocked = 3; _svc.Save();
            _svc.Data.unlocked = 4; _svc.Save();       // .bak now holds unlocked=3
            Assert.IsTrue(File.Exists(_svc.BackupPath), ".bak written on the second save");

            File.WriteAllText(_svc.MainPath, "\0\0garbage");
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("unreadable"));
            var fresh = new SaveService(_dir);
            var d = fresh.Load();
            Assert.AreEqual(SaveLoadResult.RecoveredFromBackup, fresh.LastLoadResult);
            Assert.AreEqual(3, d.unlocked);
            Assert.IsTrue(File.Exists(fresh.BackupPath), ".bak is kept");

            fresh.Save();                               // next save must not put garbage in .bak
            var again = new SaveService(_dir);
            again.Load();
            Assert.AreEqual(SaveLoadResult.Loaded, again.LastLoadResult);
            StringAssert.DoesNotContain("garbage", File.ReadAllText(again.BackupPath));
        }

        [Test]
        public void EmptyFile_IsTreatedAsCorrupt()
        {
            File.WriteAllText(_svc.MainPath, "");
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("unreadable"));
            _svc.Load();
            Assert.AreEqual(SaveLoadResult.CorruptReset, _svc.LastLoadResult);
        }

        [Test]
        public void LeftoverTempFile_IsIgnored_MainFileWins()
        {
            _svc.Load();
            _svc.Data.unlocked = 2; _svc.Save();
            File.WriteAllText(_svc.TempPath, "{half written");
            var d = new SaveService(_dir).Load();
            Assert.AreEqual(2, d.unlocked);
            Assert.IsFalse(File.Exists(_svc.TempPath));
        }

        [Test]
        public void VersionMigration_V0PrototypeSave_LoadsAndUpgrades()
        {
            // A prototype-shaped save: no version, null cont, unlocked 10 after the final level, duplicate skill.
            File.WriteAllText(_svc.MainPath,
                "{\"unlocked\":10,\"char\":\"boy\",\"diff\":\"story\",\"best\":0,\"cont\":null,\"cleared\":true,\"metLola\":true,\"lolaSeen\":9,\"skills\":[\"dj\",\"dj\",\"veil\"]}");
            var d = _svc.Load();
            Assert.AreEqual(SaveLoadResult.Loaded, _svc.LastLoadResult);
            Assert.AreEqual(SaveService.CurrentVersion, d.version);
            Assert.AreEqual(ProgressRules.LevelCount, d.unlocked);
            Assert.IsTrue(d.cleared);
            Assert.IsFalse(d.HasContinue);
            CollectionAssert.AreEqual(new[] { "dj", "veil" }, d.skills);
        }

        [Test]
        public void UnlockCount_NeverDecreases_EvenWhenAStaleCopyIsSaved()
        {
            _svc.Load();
            var stale = _svc.Data.Clone();               // unlocked = 1
            ProgressRules.Unlock(_svc.Data, 6);
            _svc.Save();
            _svc.Save(stale);                            // try to write the old progress back
            Assert.AreEqual(6, new SaveService(_dir).Load().unlocked);
        }

        [Test]
        public void Cleared_NeverRevertsOnSave()
        {
            _svc.Load();
            ProgressRules.CompleteLevel(_svc.Data, ProgressRules.FinalLevelIndex);
            _svc.Save();
            var copy = _svc.Data.Clone();
            copy.cleared = false;
            _svc.Save(copy);
            Assert.IsTrue(new SaveService(_dir).Load().cleared);
        }

        [Test]
        public void Skills_NeverDuplicate_AndAreNeverRemovedBySave()
        {
            _svc.Load();
            ProgressRules.AddSkill(_svc.Data, "dj");
            ProgressRules.AddSkill(_svc.Data, "dj");
            ProgressRules.AddSkill(_svc.Data, "veil");
            _svc.Save();
            var copy = _svc.Data.Clone();
            copy.skills = new[] { "veil", "veil" };
            _svc.Save(copy);
            var d = new SaveService(_dir).Load();
            Assert.AreEqual(2, d.skills.Length);
            CollectionAssert.AreEquivalent(new[] { "dj", "veil" }, d.skills);
        }

        [Test]
        public void Delete_RemovesEverything_AndResetsProgress()
        {
            _svc.Load();
            ProgressRules.Unlock(_svc.Data, 5);
            _svc.Data.metLola = true;
            _svc.Save(); _svc.Save();
            _svc.Delete();
            Assert.IsFalse(File.Exists(_svc.MainPath));
            Assert.IsFalse(File.Exists(_svc.BackupPath));
            var d = new SaveService(_dir).Load();
            Assert.AreEqual(1, d.unlocked);
            Assert.IsFalse(d.metLola, "Lola's first meeting shows again after deleting the save");
        }
    }
}
