using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[System.Serializable]
public class MusicData
{
    [Header("Music Name")]
    [SerializeField] private string music_name;
    public string MusicName { get { return music_name; } set { music_name = value; } }

    [Header("Composer")]
    [SerializeField] private string composer_name;
    public string ComposerName { get { return composer_name; } set { composer_name = value; } }
    [Header("Other Creators")]
    [SerializeField] private string[] other_creator = new string[0];
    public string[] OtherCreator { get { return other_creator ?? new string[0]; } set { other_creator = value ?? new string[0]; } }

    [Header("Chart Designers")]
    [SerializeField] private string[] chart_designers = new string[4];
    public string[] ChartDesigners { get { return chart_designers ?? new string[4]; } set { chart_designers = NormalizeDifficultyTexts(value); } }
    public string GetChartDesigner(Difficulty difficulty)
    {
        int index = (int)difficulty;
        if (chart_designers == null || index < 0 || index >= chart_designers.Length) { return string.Empty; }

        return chart_designers[index];
    }

    [Header("Jacket")]
    [SerializeField] private Sprite music_spr;
    public Sprite MusicSprite { get { return music_spr; } set { music_spr = value; } }

    [Header("Theme Image")]
    [SerializeField] private Sprite theme_spr;
    public Sprite ThemeSprite { get { return theme_spr; } set { theme_spr = value; } }

    [Header("Music Clip")]
    [SerializeField] private AudioClip clip;
    public AudioClip MusicClip { get { return clip; } set { clip = value; } }

    [Header("Sample Clip")]
    [SerializeField] private AudioClip sample_clip;
    public AudioClip SampleClip { get { return sample_clip; } set { sample_clip = value; } }

    [Header("Symphony Type")]
    [SerializeField] SymphonyType symphonyType = SymphonyType.None;
    public SymphonyType SymphonyType { get { return symphonyType; } set { symphonyType = value; } }

    [Header("Stage")]
    [SerializeField] StageType stageType = StageType.CreationNoon;
    public StageType StageType { get { return stageType; } set { stageType = value; } }

    [Header("Difficulty")]
    [SerializeField] private int[] difficulties = new int[4];
    public int GetDifficulty(Difficulty name) { return difficulties[(int)name]; }
    public void SetDifficulty(int[] difficulties)
    {
        for (int i = 0; i < this.difficulties.Length && i < difficulties.Length; i++)
        {
            this.difficulties[i] = difficulties[i];
        }
    }
    public void SetDifficulty(Difficulty dif, int level)
    { 
        this.difficulties[(int)dif] = level;
    }

    private string[] NormalizeDifficultyTexts(string[] values)
    {
        string[] result = new string[4];
        if (values == null) { return result; }

        for (int i = 0; i < result.Length && i < values.Length; i++)
        {
            result[i] = values[i];
        }

        return result;
    }

    [Header("Chart")]
    [SerializeField] private string[] chartPaths = new string[4];
    public string GetChartPath(Difficulty name) { return chartPaths[(int)name]; }
    public void SetChartPath(Difficulty diff, string chartPath)
    {
        if((int)diff >= chartPaths.Length || (int)diff < 0)
        {
            Debug.LogError($"[System] Invalid difficulty index: {diff},{(int)diff}");
            return;
        }
        chartPaths[(int)diff] = chartPath;
    }

    // スコア記録
    DifficultyToRecord records;
    public MusicRecord GetMusicRecord(Difficulty dif) 
    {
        if (records == null) { return MusicRecord.zero; }

        return records.GetRecord(dif);
    }
    public void SetMusicRecord(Difficulty dif, MusicRecord newRecord) 
    {
        // 初プレイなら結果をインスタンス化
        if (records == null)
        {
            records = new DifficultyToRecord();
        }

        // 初プレイなら結果をインスタンス化
        if (records.GetRecord(dif) == MusicRecord.zero)
        {
            records.SetRecord(dif, newRecord);
        }
        // スコア更新
        else
        {
            records.GetRecord(dif).UpdateHighScore(newRecord);
        }
    }
}
