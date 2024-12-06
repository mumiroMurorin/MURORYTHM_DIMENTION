using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[CreateAssetMenu(menuName = "ScriptableObject/MusicData", fileName = "Music")]
public class MusicData : ScriptableObject
{
    [Header("曲名")]
    [SerializeField] private string music_name;
    public string MusicName { get { return music_name; } }

    [Header("コンポーザー")]
    [SerializeField] private string composer_name;
    public string ComposerName { get { return music_name; } }

    [Header("サムネイル")]
    [SerializeField] private Sprite music_spr;
    public Sprite MusicSprite { get { return music_spr; } }

    [Header("テーマ画像")]
    [SerializeField] private Sprite theme_spr;
    public Sprite ThemeSprite { get { return theme_spr; } }

    [Header("音楽ファイル")]
    [SerializeField] private AudioClip clip;
    public AudioClip MusicClip { get { return clip; } }

    [Header("視聴ファイル")]
    [SerializeField] private AudioClip sample_clip;
    public AudioClip SampleClip { get { return sample_clip; } }

    [Header("難易度")]
    [SerializeField] private int[] difficulities = new int[Enum.GetNames(typeof(DifficulityName)).Length];
    public int GetDifficulity(DifficulityName name) { return difficulities[(int)name]; }

    [Header("譜面")]
    [SerializeField] private TextAsset[] charts = new TextAsset[Enum.GetNames(typeof(DifficulityName)).Length];
    public TextAsset GetChart(DifficulityName name) { return charts[(int)name]; }

    [Header("記録")]
    [SerializeField] private MusicRecord[] records = new MusicRecord[Enum.GetNames(typeof(DifficulityName)).Length];
    public MusicRecord GetMusicRecord(DifficulityName name) { return records[(int)name]; }
    public void SetMusicRecord(DifficulityName name, MusicRecord new_record) { records[(int)name] = new_record; }
}

[Serializable]
public class MusicRecord
{
    public int score = 0;
    public ScoreRank rank = ScoreRank.None;
    public ComboRank combo_rank = ComboRank.None;
    public int[] judge_num = new int[Enum.GetNames(typeof(TimingJudge)).Length];
}

//難易度
public enum DifficulityName
{
    Initiate = 0,
    Fanatic = 1,
    Skyclad = 2,
    Dream = 3
}

//タイミング評価
public enum TimingJudge
{
    perfect = 0,
    great = 1,
    good = 2,
    miss = 3
}

//ランク評価
public enum ScoreRank
{
    None,
    E,
    D,
    C,
    B,
    A,
    A_plus,
    S,
    S_plus,
    MAX
}

//コンボ評価
public enum ComboRank
{
    None,
    TrackFailed,
    TrackComplete,
    FullCombo,
    AllPerfect
}