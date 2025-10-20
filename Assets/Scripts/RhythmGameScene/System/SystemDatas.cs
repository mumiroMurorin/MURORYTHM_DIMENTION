using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// インゲーム部の内部ステータスの列挙型
/// </summary>
public enum PhaseStatusInRhythmGame
{
    LoadData,
    LoadChart,
    FadeIn,
    LoadBody,
    StartAnimation,
    Play,
    EndAnimation,
    FadeOut,
    TransitionResultScene,
    TransitionSelectScene,
    Retry,
}

/// <summary>
/// リザルトシーンの内部ステータス
/// </summary>
public enum PhaseStatusInResultScene
{
    LoadData,
    FadeIn,
    Result,
    FadeOut,
    TransitionSelectScene,
    Retry,
}

/// <summary>
/// セレクトシーンの内部ステータス
/// </summary>
public enum PhaseStatusInSelectScene
{
    LoadData,
    FadeIn,
    MusicSelect,
    DetailSelect,
    MusicOption,
    FadeOut,
    TransitionRhythmGameScene,
    TransitionRootScene,
}

/// <summary>
/// 管理者シーンの内部ステータス
/// </summary>
public enum PhaseStatusInRootScene
{
    LoadData,
    SettingOption,
    TransitionSelectScene,
    Reload,
}

public enum OptionType
{
    None = 0,
    NoteSpeed = 10,
    Offset = 20,
    DivisionNum = 25,
    MasterVolume = 30,
    JudgementSEVolume = 40,
    MusicVolume = 100,
    IsEnabledFastLate = 200,
}

public enum SymphonyType
{
    None = 0,
    Creation = 10,    // 創造
    Destruction = 20, // 破壊
}

/// <summary>
/// 難易度
/// </summary>
public enum Difficulty
{
    Easy = 0,
    Normal = 1,
    Hard = 2,
    Master = 3,
}

/// <summary>
/// SSS,AA等のランク評価
/// </summary>
public enum ScoreRank
{
    None,
    E,
    D,
    C,
    B,
    A,
    APlus,
    S,
    SPlus,
    SS,
    SSPlus,
    SSS,
    MAX
}

/// <summary>
/// AllPerfect,FullCombo等コンボ評価
/// </summary>
public enum ComboRank
{
    None = 0,
    TrackFailed = 1,
    TrackComplete = 2,
    FullCombo = 3,
    //GreatCombo = 4,
    AllPerfect = 5
}

public enum StageType
{
    Debug = 1,
    CreationNoon = 100,
    CreationNight = 101,
    CreationEvening = 102,
    Destruction = 200,
}

[System.Serializable]
public class MusicRecord
{
    public MusicRecord(int score, ScoreRank scoreRank, ComboRank comboRank, JudgementToCount judgementCount)
    {
        Score = score;
        ScoreRank = scoreRank;
        ComboRank = comboRank;
        JudgementCount = judgementCount;
    }

    public void UpdateHighScore(MusicRecord newRecord)
    {
        if (Score < newRecord.Score) { Score = newRecord.Score; }
        if (ComboRank < newRecord.ComboRank) { ComboRank = newRecord.ComboRank; }
        if (ScoreRank < newRecord.ScoreRank) { ScoreRank = newRecord.ScoreRank; }
    }

    public int Score { get; set; }
    public ScoreRank ScoreRank { get; set; }
    public ComboRank ComboRank { get; set; }
    public JudgementToCount JudgementCount { get; set; }

    public readonly static MusicRecord zero = new MusicRecord(0, ScoreRank.None, ComboRank.None, JudgementToCount.zero);
}

public class DifficultyToRecord
{
    Dictionary<Difficulty, MusicRecord> dic = new Dictionary<Difficulty, MusicRecord>();

    public MusicRecord GetRecord(Difficulty dif) 
    {
        if (dic.ContainsKey(dif)) { return dic[dif]; }
        else { return MusicRecord.zero; }
    }

    public void SetRecord(Difficulty dif, MusicRecord record)
    {
        if (dic.ContainsKey(dif)) { dic[dif] = record; }
        else { dic.Add(dif, record); }
    }
}

public class JudgementToCount
{
    public JudgementToCount(JudgementToCount judgementToCount)
    {
        SetCount(Judgement.Perfect, judgementToCount.GetCount(Judgement.Perfect));
        SetCount(Judgement.Great, judgementToCount.GetCount(Judgement.Great));
        SetCount(Judgement.Good, judgementToCount.GetCount(Judgement.Good));
        SetCount(Judgement.Miss, judgementToCount.GetCount(Judgement.Miss));
    }

    public JudgementToCount(int perfectNum, int greatNum, int goodNum, int missNum)
    {
        SetCount(Judgement.Perfect, perfectNum);
        SetCount(Judgement.Great, greatNum);
        SetCount(Judgement.Good, goodNum);
        SetCount(Judgement.Miss, missNum);
    }

    Dictionary<Judgement, int> dic = new Dictionary<Judgement, int>();

    public int GetCount(Judgement judgement)
    {
        if (dic.TryGetValue(judgement,out int count)) { return count; }
        else { return 0; }
    }

    public void AddCount(Judgement judgement)
    {
        if (dic.ContainsKey(judgement)) { dic[judgement]++; }
        else { dic.Add(judgement, 1); }
    }

    public void SetCount(Judgement judgement, int count)
    {
        if (dic.ContainsKey(judgement)) { dic[judgement] = count; }
        else { dic.Add(judgement, count); }
    }

    public readonly static JudgementToCount zero = new JudgementToCount(0, 0, 0, 0);
}
