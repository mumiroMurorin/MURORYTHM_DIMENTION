using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static JsonUtil.JsonLoader;
using static JsonUtil.JsonWriter;

public static class MusicRecordPersistence
{
    const string FILE_NAME = "musicRecordBest.json";

    public static string MakeChartKey(string musicName, Difficulty difficulty)
    {
        return $"{musicName}_{difficulty}";
    }

    public static void LoadAndApply(MusicData musicData)
    {
        if (musicData == null) { return; }
        if (string.IsNullOrWhiteSpace(musicData.MusicName)) { return; }

        if (!TryLoadDatabase(out MusicRecordSaveDatabase database))
        {
            return;
        }

        foreach (Difficulty difficulty in System.Enum.GetValues(typeof(Difficulty)))
        {
            string chartKey = MakeChartKey(musicData.MusicName, difficulty);
            var record = FindRecord(database, chartKey);
            if (record == null) { continue; }

            var scoreRank = ScoreRankUtility.GetRankFromScore(record.score);

            musicData.SetMusicRecord(
                difficulty,
                new MusicRecord(record.score, scoreRank, record.comboRank, JudgementToCount.zero)
            );
        }
    }

    public static void SaveIfBetter(MusicData musicData, Difficulty difficulty, MusicRecord record)
    {
        if (musicData == null) { return; }
        if (record == null) { return; }
        if (string.IsNullOrWhiteSpace(musicData.MusicName)) { return; }

        string chartKey = MakeChartKey(musicData.MusicName, difficulty);
        var database = LoadDatabaseOrCreate();

        var current = FindRecord(database, chartKey);
        if (current != null && !IsBetterRecord(current, record))
        {
            return;
        }

        var newRecord = new MusicRecordSaveData
        {
            chartKey = chartKey,
            score = record.Score,
            comboRank = record.ComboRank,
        };

        ReplaceRecord(database, newRecord);

        if (!SaveDatabase(database))
        {
            Debug.LogWarning($"[MusicRecordPersistence] Save failed: {chartKey}");
            return;
        }
    }

    public static bool TryGetSavedRecord(string chartKey, out MusicRecordSaveData savedRecord)
    {
        savedRecord = null;
        if (!TryLoadDatabase(out MusicRecordSaveDatabase database))
        {
            return false;
        }

        savedRecord = FindRecord(database, chartKey);
        return savedRecord != null;
    }

    static bool IsBetterRecord(MusicRecordSaveData current, MusicRecord candidate)
    {
        if (candidate.Score > current.score) { return true; }
        if (candidate.Score < current.score) { return false; }

        return candidate.ComboRank > current.comboRank;
    }

    static MusicRecordSaveData FindRecord(MusicRecordSaveDatabase database, string chartKey)
    {
        if (database == null || database.records == null) { return null; }

        return database.records.Find(x => x != null && x.chartKey == chartKey);
    }

    static void ReplaceRecord(MusicRecordSaveDatabase database, MusicRecordSaveData newRecord)
    {
        if (database.records == null)
        {
            database.records = new List<MusicRecordSaveData>();
        }

        int index = database.records.FindIndex(x => x != null && x.chartKey == newRecord.chartKey);
        if (index >= 0)
        {
            database.records[index] = newRecord;
            return;
        }

        database.records.Add(newRecord);
    }

    static MusicRecordSaveDatabase LoadDatabaseOrCreate()
    {
        if (TryLoadDatabase(out MusicRecordSaveDatabase database))
        {
            return database;
        }

        return new MusicRecordSaveDatabase();
    }

    static bool TryLoadDatabase(out MusicRecordSaveDatabase database)
    {
        string filePath = GetFilePath();
        if (!File.Exists(filePath))
        {
            database = new MusicRecordSaveDatabase();
            return false;
        }

        if (!TryLoadFromJsonFile(filePath, out database))
        {
            database = new MusicRecordSaveDatabase();
            return false;
        }

        if (database.records == null)
        {
            database.records = new List<MusicRecordSaveData>();
        }

        return true;
    }

    static bool SaveDatabase(MusicRecordSaveDatabase database)
    {
        string filePath = GetFilePath();
        return TrySaveToJsonFile(database, filePath);
    }

    static string GetFilePath()
    {
        return Path.Combine(Application.persistentDataPath, FILE_NAME);
    }
}
