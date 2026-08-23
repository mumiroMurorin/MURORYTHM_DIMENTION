using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static JsonUtil.JsonLoader;
using static JsonUtil.JsonWriter;

public static class MusicRecordPersistence
{
    const string FILE_NAME = "musicRecordBest.json";
    const int MAX_RECORD_COUNT = 5;

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
            var record = FindBestRecord(database, chartKey);
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

        var newRecord = new MusicRecordSaveData
        {
            score = record.Score,
            comboRank = record.ComboRank,
        };

        AddRecord(database, chartKey, newRecord);

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

        savedRecord = FindBestRecord(database, chartKey);
        return savedRecord != null;
    }

    public static bool TryGetSavedRecords(string chartKey, out IReadOnlyList<MusicRecordSaveData> savedRecords)
    {
        savedRecords = null;
        if (!TryLoadDatabase(out MusicRecordSaveDatabase database))
        {
            return false;
        }

        var ranking = FindRanking(database, chartKey);
        if (ranking == null || ranking.records == null || ranking.records.Count <= 0)
        {
            return false;
        }

        savedRecords = ranking.records;
        return true;
    }

    static int CompareRecord(MusicRecordSaveData x, MusicRecordSaveData y)
    {
        if (x == null && y == null) { return 0; }
        if (x == null) { return 1; }
        if (y == null) { return -1; }
        int scoreComparison = y.score.CompareTo(x.score);
        if (scoreComparison != 0) { return scoreComparison; }

        return y.comboRank.CompareTo(x.comboRank);
    }

    static MusicRecordSaveData FindBestRecord(MusicRecordSaveDatabase database, string chartKey)
    {
        var ranking = FindRanking(database, chartKey);
        if (ranking == null || ranking.records == null || ranking.records.Count <= 0) { return null; }

        return ranking.records[0];
    }

    static MusicRecordRankingSaveData FindRanking(MusicRecordSaveDatabase database, string chartKey)
    {
        if (database == null || database.records == null) { return null; }

        return database.records.Find(x => x != null && x.chartKey == chartKey);
    }

    static void AddRecord(MusicRecordSaveDatabase database, string chartKey, MusicRecordSaveData newRecord)
    {
        if (database.records == null)
        {
            database.records = new List<MusicRecordRankingSaveData>();
        }

        var ranking = FindRanking(database, chartKey);
        if (ranking == null)
        {
            ranking = new MusicRecordRankingSaveData
            {
                chartKey = chartKey,
                records = new List<MusicRecordSaveData>(),
            };
            database.records.Add(ranking);
        }

        if (ranking.records == null)
        {
            ranking.records = new List<MusicRecordSaveData>();
        }

        ranking.records.Add(newRecord);
        ranking.records.Sort(CompareRecord);

        if (ranking.records.Count > MAX_RECORD_COUNT)
        {
            ranking.records.RemoveRange(MAX_RECORD_COUNT, ranking.records.Count - MAX_RECORD_COUNT);
        }
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

        if (NeedsLegacyMigration(database) && TryLoadLegacyDatabase(filePath, out var migratedDatabase))
        {
            database = migratedDatabase;
            return true;
        }

        if (database.records == null)
        {
            database.records = new List<MusicRecordRankingSaveData>();
        }

        NormalizeDatabase(database);
        return true;
    }

    static bool NeedsLegacyMigration(MusicRecordSaveDatabase database)
    {
        if (database == null || database.records == null) { return false; }
        if (database.records.Count <= 0) { return false; }

        return database.records.Exists(x => x != null && (x.records == null || x.records.Count <= 0));
    }

    static bool TryLoadLegacyDatabase(string filePath, out MusicRecordSaveDatabase database)
    {
        database = new MusicRecordSaveDatabase();
        if (!TryLoadFromJsonFile(filePath, out LegacyMusicRecordSaveDatabase legacyDatabase))
        {
            return false;
        }

        if (legacyDatabase.records == null)
        {
            return true;
        }

        foreach (var legacyRecord in legacyDatabase.records)
        {
            if (legacyRecord == null || string.IsNullOrWhiteSpace(legacyRecord.chartKey)) { continue; }

            AddRecord(
                database,
                legacyRecord.chartKey,
                new MusicRecordSaveData
                {
                    score = legacyRecord.score,
                    comboRank = legacyRecord.comboRank,
                }
            );
        }

        return true;
    }

    static void NormalizeDatabase(MusicRecordSaveDatabase database)
    {
        if (database == null) { return; }
        if (database.records == null)
        {
            database.records = new List<MusicRecordRankingSaveData>();
            return;
        }

        foreach (var ranking in database.records)
        {
            if (ranking == null) { continue; }
            if (ranking.records == null)
            {
                ranking.records = new List<MusicRecordSaveData>();
            }

            ranking.records.Sort(CompareRecord);
            if (ranking.records.Count > MAX_RECORD_COUNT)
            {
                ranking.records.RemoveRange(MAX_RECORD_COUNT, ranking.records.Count - MAX_RECORD_COUNT);
            }
        }
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

    class LegacyMusicRecordSaveData
    {
        public string chartKey;
        public int score;
        public ComboRank comboRank;
    }

    class LegacyMusicRecordSaveDatabase
    {
        public List<LegacyMusicRecordSaveData> records;
    }
}
