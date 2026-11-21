using System;
using System.IO;
using UnityEngine;

namespace Skp.Save
{
    public enum TouchMode { Auto, On, Off }

    /// <summary>Player options (the prototype's settings object plus high contrast). Kept apart from progress:
    /// deleting the save resets the journey, not the options.</summary>
    [Serializable]
    public class GameSettings
    {
        public bool music = true;
        public bool sfx = true;
        public bool vibration = true;
        public bool shake = true;
        public bool reducedFlash;
        public bool highContrast;
        public bool leftHanded;
        public TouchMode touch = TouchMode.Auto;

        public GameSettings Clone() => (GameSettings)MemberwiseClone();
    }

    /// <summary>settings.json next to the save, written atomically; unreadable files fall back to defaults.</summary>
    public class SettingsStore
    {
        public const string FileName = "skp_settings.json";
        public string Directory { get; }
        public string PathOnDisk => Path.Combine(Directory, FileName);
        public GameSettings Data { get; private set; } = new GameSettings();

        /// <summary>Raised after any change is applied (touch layout, high contrast and audio listen to it).</summary>
        public event Action<GameSettings> Changed;

        public SettingsStore(string directory) { Directory = directory; }

        public GameSettings Load()
        {
            try
            {
                Data = File.Exists(PathOnDisk) ? JsonUtility.FromJson<GameSettings>(File.ReadAllText(PathOnDisk)) ?? new GameSettings() : new GameSettings();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Settings] unreadable settings file, using defaults: " + e.Message);
                Data = new GameSettings();
            }
            return Data;
        }

        public void Save()
        {
            System.IO.Directory.CreateDirectory(Directory);
            var tmp = PathOnDisk + ".tmp";
            File.WriteAllText(tmp, JsonUtility.ToJson(Data, true));
            if (File.Exists(PathOnDisk)) File.Delete(PathOnDisk);
            File.Move(tmp, PathOnDisk);
            Changed?.Invoke(Data);
        }
    }
}
