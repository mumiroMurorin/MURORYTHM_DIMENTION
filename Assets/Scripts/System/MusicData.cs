using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[System.Serializable]
public class MusicData
{
    [Header("曲名")]
    [SerializeField] private string music_name;
    public string MusicName { get { return music_name; } set { music_name = value; } }

    [Header("コンポーザー")]
    [SerializeField] private string composer_name;
    public string ComposerName { get { return composer_name; } set { composer_name = value; } }

    [Header("サムネイル")]
    [SerializeField] private Sprite music_spr;
    public Sprite MusicSprite { get { return music_spr; } set { music_spr = value; } }

    [Header("テーマ画像")]
    [SerializeField] private Sprite theme_spr;
    public Sprite ThemeSprite { get { return theme_spr; } set { theme_spr = value; } }

    [Header("音楽ファイル")]
    [SerializeField] private AudioClip clip;
    public AudioClip MusicClip { get { return clip; } set { clip = value; } }

    [Header("視聴ファイル")]
    [SerializeField] private AudioClip sample_clip;
    public AudioClip SampleClip { get { return sample_clip; } set { sample_clip = value; } }

    [Header("ステージ")]
    [SerializeField] StageType stageType = StageType.CreationNoon;
    public StageType StageType { get { return stageType; } set { stageType = value; } }

    [Header("難易度")]
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

    [Header("譜面")]
    [SerializeField] private string[] chartPaths = new string[4];
    public string GetChartPath(Difficulty name) { return chartPaths[(int)name]; }
    public void SetChartPath(Difficulty diff, string chartPath)
    {
        if((int)diff >= chartPaths.Length || (int)diff < 0)
        {
            Debug.LogError($"【System】長さが有効ではありません: {diff},{(int)diff}");
            return;
        }
        chartPaths[(int)diff] = chartPath;
    }


    DifficultyToRecord records = new DifficultyToRecord();
    public MusicRecord GetMusicRecord(Difficulty dif) 
    {
        return records.GetRecord(dif);
    }
    public void SetMusicRecord(Difficulty dif, MusicRecord newRecord) 
    {
        records.GetRecord(dif).HighScoreUpdate(newRecord);
    }
}
