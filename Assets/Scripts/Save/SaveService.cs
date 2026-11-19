using System;
using System.IO;
using UnityEngine;

namespace Skp.Save
{
    public enum SaveLoadResult
    {
        /// <summary>The save file was read normally.</summary>
        Loaded,
        /// <summary>No save file: defaults.</summary>
        Missing,
        /// <summary>The main file was missing or corrupt; the last good backup was used.</summary>
        RecoveredFromBackup,
        /// <summary>The main file and the backup were unusable: defaults. The broken file is kept as .corrupt.</summary>
        CorruptReset,
    }

    /// <summary>
    /// JSON save in a directory (persistentDataPath by default).
    /// Writes are atomic (temp file, then replace) and the previous good file is kept as .bak.
    /// A corrupt file never crashes the game: it is moved aside to .corrupt and the backup
    /// (or defaults) is used. Progress is monotonic across saves: unlocked never decreases,
    /// cleared never reverts, skills are never removed. Only <see cref="Delete"/> resets it.
    /// </summary>
    public class SaveService
    {
        public const int CurrentVersion = 1;
        public const string FileName = "skp_save.json";

        public string Directory { get; }
        public string MainPath => Path.Combine(Directory, FileName);
        public string BackupPath => MainPath + ".bak";
        public string TempPath => MainPath + ".tmp";
        public string CorruptPath => MainPath + ".corrupt";

        public SaveData Data { get; private set; } = new SaveData();
        public SaveLoadResult LastLoadResult { get; private set; } = SaveLoadResult.Missing;

        // Highest progress seen this session, so a stale SaveData can never lower it.
        SaveData _floor = new SaveData();

        public SaveService() : this(Application.persistentDataPath) { }

        public SaveService(string directory)
        {
            Directory = directory;
        }

        public SaveData Load()
        {
            System.IO.Directory.CreateDirectory(Directory);
            File.Delete(TempPath); // leftover from an interrupted write; the main file is still intact

            SaveData data;
            if (!File.Exists(MainPath))
            {
                if (TryRead(BackupPath, out data)) LastLoadResult = SaveLoadResult.RecoveredFromBackup;
                else { data = new SaveData(); LastLoadResult = SaveLoadResult.Missing; }
            }
            else if (TryRead(MainPath, out data))
            {
                LastLoadResult = SaveLoadResult.Loaded;
            }
            else
            {
                // Keep the broken file for inspection, never let it overwrite the backup.
                File.Copy(MainPath, CorruptPath, true);
                File.Delete(MainPath);
                if (TryRead(BackupPath, out data)) LastLoadResult = SaveLoadResult.RecoveredFromBackup;
                else { data = new SaveData(); LastLoadResult = SaveLoadResult.CorruptReset; }
                Debug.LogWarning($"[Save] Save file was unreadable; {LastLoadResult}. Kept the broken file at {CorruptPath}");
            }

            Data = data;
            _floor = data.Clone();
            return Data;
        }

        /// <summary>Writes <see cref="Data"/> atomically, keeping the previous file as .bak.</summary>
        public void Save() => Save(Data);

        public void Save(SaveData data)
        {
            ApplyFloor(data);
            Data = data;
            data.version = CurrentVersion;
            System.IO.Directory.CreateDirectory(Directory);
            File.WriteAllText(TempPath, JsonUtility.ToJson(data, true));
            if (File.Exists(MainPath))
            {
                try
                {
                    File.Replace(TempPath, MainPath, BackupPath);
                }
                catch (Exception e) when (e is PlatformNotSupportedException || e is IOException || e is UnauthorizedAccessException)
                {
                    File.Copy(MainPath, BackupPath, true);
                    File.Delete(MainPath);
                    File.Move(TempPath, MainPath);
                }
            }
            else
            {
                File.Move(TempPath, MainPath);
            }
            _floor = data.Clone();
        }

        /// <summary>Removes every save file and resets to defaults (new game from scratch).</summary>
        public void Delete()
        {
            foreach (var p in new[] { MainPath, BackupPath, TempPath, CorruptPath })
                if (File.Exists(p)) File.Delete(p);
            Data = new SaveData();
            _floor = new SaveData();
            LastLoadResult = SaveLoadResult.Missing;
        }

        void ApplyFloor(SaveData d)
        {
            ProgressRules.Sanitize(d);
            ProgressRules.Unlock(d, _floor.unlocked);
            d.cleared |= _floor.cleared;
            if (_floor.best > d.best) d.best = _floor.best;
            if (_floor.metLola) d.metLola = true;
            foreach (var id in _floor.skills ?? new string[0]) ProgressRules.AddSkill(d, id);
        }

        static bool TryRead(string path, out SaveData data)
        {
            data = null;
            if (!File.Exists(path)) return false;
            try
            {
                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json)) return false;
                int fileVersion = JsonUtility.FromJson<VersionProbe>(json).version;
                data = JsonUtility.FromJson<SaveData>(json);
                if (data == null) return false;
                Migrate(data, fileVersion);
                ProgressRules.Sanitize(data);
                return true;
            }
            catch (Exception)
            {
                data = null;
                return false;
            }
        }

        /// <summary>Upgrades data written by an older version. Add a step per version bump.</summary>
        public static void Migrate(SaveData data, int fromVersion)
        {
            if (fromVersion < 1)
            {
                // v0: prototype-shaped save without a version field. Its "cont" could be null and
                // "unlocked" could reach 10 after the final level; Sanitize fixes both, and an
                // old resume state is dropped because v0 had no checkpoint layout to resume into.
                data.cont = new ResumeState();
            }
            data.version = CurrentVersion;
        }

        [Serializable]
        class VersionProbe
        {
            public int version = 0;
        }
    }
}
