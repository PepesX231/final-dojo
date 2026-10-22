using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamRush.Core
{
    /// <summary>Persistent player data (versioned JSON, GDD §25.6). Only plain serialisable fields.</summary>
    [Serializable]
    public class SaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public SettingsData settings = new SettingsData();
        public ProgressionData progression = new ProgressionData();
        public DiaryData diary = new DiaryData();

        public SaveData Clone() => JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(this));
    }

    [Serializable]
    public class SettingsData
    {
        [Range(0.02f, 0.5f)] public float mouseSensitivity = 0.12f;
        [Range(30f, 400f)] public float stickSensitivity = 160f;
        public bool invertY;
        [Range(0f, 1f)] public float masterVolume = 0.8f;
        [Range(0f, 1f)] public float musicVolume = 0.7f;
        [Range(0f, 1f)] public float sfxVolume = 0.9f;
        public bool reduceScreenShake;
        public bool reduceFlashing;
        [Range(0.8f, 1.6f)] public float textScale = 1f;
        public bool subtitles = true;

        public void Clamp()
        {
            mouseSensitivity = Mathf.Clamp(mouseSensitivity, 0.02f, 0.5f);
            stickSensitivity = Mathf.Clamp(stickSensitivity, 30f, 400f);
            masterVolume = Mathf.Clamp01(masterVolume);
            musicVolume = Mathf.Clamp01(musicVolume);
            sfxVolume = Mathf.Clamp01(sfxVolume);
            textScale = Mathf.Clamp(textScale, 0.8f, 1.6f);
        }
    }

    [Serializable]
    public class ProgressionData
    {
        public int dreamMemory;
        public int mattressLevel;
        // Legacy save field only; the removed reroll upgrade has no runtime effect.
        public int pillowLevel;
        public int blanketLevel;
        public List<LevelRecord> levelRecords = new List<LevelRecord>();

        public LevelRecord FindLevelRecord(string scene)
        {
            if (levelRecords == null) return null;
            return levelRecords.Find(record => record != null && record.scene == scene);
        }

        // Optional mastery goal: no currency bonus and no change to unlocks or difficulty.
        public bool RecordClear(string scene, float seconds, int stars)
        {
            if (string.IsNullOrEmpty(scene) || seconds <= 0f || float.IsNaN(seconds) || float.IsInfinity(seconds)) return false;
            levelRecords ??= new List<LevelRecord>();
            var record = FindLevelRecord(scene);
            if (record == null) { record = new LevelRecord { scene = scene }; levelRecords.Add(record); }
            bool improved = record.bestSeconds <= 0f || seconds < record.bestSeconds;
            if (improved) record.bestSeconds = seconds;
            record.clears++;
            record.bestStars = Mathf.Max(record.bestStars, Mathf.Max(0, stars));
            return improved;
        }
    }

    [Serializable]
    public class LevelRecord
    {
        public string scene;
        public int clears;
        public float bestSeconds;
        public int bestStars;
    }

    [Serializable]
    public class DiaryData
    {
        public int runsStarted;
        public int runsWon;
        public int runsLost;
        /// <summary>Victory method ids discovered (e.g. "Exhaustion", "Rooted").</summary>
        public List<string> discoveredVictories = new List<string>();
        public List<string> bossesMet = new List<string>();

        public bool Discover(string victoryId)
        {
            if (string.IsNullOrEmpty(victoryId) || discoveredVictories.Contains(victoryId)) return false;
            discoveredVictories.Add(victoryId);
            return true;
        }
    }
}
