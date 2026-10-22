using System;
using System.IO;
using UnityEngine;

namespace DreamRush.Core
{
    /// <summary>
    /// Loads/saves <see cref="SaveData"/> as versioned JSON with atomic writes (temp file + replace) and a
    /// migration step. Path is injectable so tests never touch the real save.
    /// </summary>
    public class SaveManager
    {
        public static SaveManager Instance { get; private set; }
        public static SaveManager GetOrCreate()
        {
            if (Instance == null)
            {
                Instance = new SaveManager(Path.Combine(Application.persistentDataPath, "dreamrush_save.json"));
                Instance.Load();
            }
            return Instance;
        }

        public string FilePath { get; }
        public SaveData Data { get; private set; } = new SaveData();
        public event Action<SaveData> Changed;

        public SaveManager(string filePath) { FilePath = filePath; }

        public SaveData Load()
        {
            try
            {
                if (!File.Exists(FilePath)) { Data = new SaveData(); return Data; }
                Data = Deserialize(File.ReadAllText(FilePath));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[DreamRush] Save unreadable ({e.Message}); keeping a backup and starting fresh.");
                TryBackupCorrupt();
                Data = new SaveData();
            }
            return Data;
        }

        public void Save()
        {
            Data.version = SaveData.CurrentVersion;
            Data.settings.Clamp();
            string json = JsonUtility.ToJson(Data, true);
            string dir = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            string tmp = FilePath + ".tmp";
            File.WriteAllText(tmp, json);
            if (File.Exists(FilePath)) File.Replace(tmp, FilePath, null);
            else File.Move(tmp, FilePath);
            Changed?.Invoke(Data);
        }

        public static SaveData Deserialize(string json)
        {
            var data = JsonUtility.FromJson<SaveData>(json) ?? new SaveData();
            Migrate(data, json);
            data.settings ??= new SettingsData();
            data.progression ??= new ProgressionData();
            data.progression.levelRecords ??= new System.Collections.Generic.List<LevelRecord>();
            data.diary ??= new DiaryData();
            data.diary.discoveredVictories ??= new System.Collections.Generic.List<string>();
            data.diary.bossesMet ??= new System.Collections.Generic.List<string>();
            data.settings.Clamp();
            return data;
        }

        /// <summary>Version upgrades. v0 = files written before versioning existed.</summary>
        static void Migrate(SaveData data, string rawJson)
        {
            bool hadVersion = rawJson.Contains("\"version\"");
            if (!hadVersion) data.version = 0;
            if (data.version < 1)
            {
                // v0 → v1: no structural change yet; defaults fill missing fields.
                data.version = 1;
            }
        }

        void TryBackupCorrupt()
        {
            try { if (File.Exists(FilePath)) File.Copy(FilePath, FilePath + ".corrupt", true); } catch { }
        }

        /// <summary>For tests / dev: replace the singleton.</summary>
        public static void SetInstance(SaveManager m) => Instance = m;
    }
}
