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
    None,
    NoteSpeed,
    Offset,
    MasterVolume,
    JudgementSEVolume,
    MusicVolume,
}

/// <summary>
/// 難易度
/// </summary>
public enum Difficulty
{
    Initiate = 0,
    Fanatic = 1,
    Skyclad = 2,
    Dream = 3
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
    A_plus,
    S,
    S_plus,
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
    GreatCombo = 4,
    AllPerfect = 5
}

public enum StageType
{
    CreationNoon = 100,
    CreationNight = 101,
    CreationEvening = 102,
    Destruction = 200,
}

[System.Serializable]
public class MusicRecord
{
    public int Score { get; set; } = 0;
    public ScoreRank ScoreRank { get; set; } = ScoreRank.None;
    public ComboRank ComboRank { get; set; } = ComboRank.None;
    public int[] JudgementCount { get; set; } = new int[Enum.GetNames(typeof(Judgement)).Length];
}
