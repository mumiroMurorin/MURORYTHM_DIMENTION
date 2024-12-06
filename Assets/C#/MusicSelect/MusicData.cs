using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[CreateAssetMenu(menuName = "ScriptableObject/MusicData", fileName = "Music")]
public class MusicData : ScriptableObject
{
    [Header("�Ȗ�")]
    [SerializeField] private string music_name;
    public string MusicName { get { return music_name; } }

    [Header("�R���|�[�U�[")]
    [SerializeField] private string composer_name;
    public string ComposerName { get { return music_name; } }

    [Header("�T���l�C��")]
    [SerializeField] private Sprite music_spr;
    public Sprite MusicSprite { get { return music_spr; } }

    [Header("�e�[�}�摜")]
    [SerializeField] private Sprite theme_spr;
    public Sprite ThemeSprite { get { return theme_spr; } }

    [Header("���y�t�@�C��")]
    [SerializeField] private AudioClip clip;
    public AudioClip MusicClip { get { return clip; } }

    [Header("�����t�@�C��")]
    [SerializeField] private AudioClip sample_clip;
    public AudioClip SampleClip { get { return sample_clip; } }

    [Header("��Փx")]
    [SerializeField] private int[] difficulities = new int[Enum.GetNames(typeof(DifficulityName)).Length];
    public int GetDifficulity(DifficulityName name) { return difficulities[(int)name]; }

    [Header("����")]
    [SerializeField] private TextAsset[] charts = new TextAsset[Enum.GetNames(typeof(DifficulityName)).Length];
    public TextAsset GetChart(DifficulityName name) { return charts[(int)name]; }

    [Header("�L�^")]
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

//��Փx
public enum DifficulityName
{
    Initiate = 0,
    Fanatic = 1,
    Skyclad = 2,
    Dream = 3
}

//�^�C�~���O�]��
public enum TimingJudge
{
    perfect = 0,
    great = 1,
    good = 2,
    miss = 3
}

//�����N�]��
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

//�R���{�]��
public enum ComboRank
{
    None,
    TrackFailed,
    TrackComplete,
    FullCombo,
    AllPerfect
}