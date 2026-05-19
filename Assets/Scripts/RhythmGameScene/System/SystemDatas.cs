using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using TMPro;
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

[System.Serializable]
public class ScoreRankToSprite
{
    [SerializeField] ScoreRank rank;
    [SerializeField] Sprite sprite;

    public bool CheckCondition(ScoreRank rank) { return this.rank == rank; }

    public Sprite Sprite { get { return sprite; } }
}

[System.Serializable]
public class ComboRankToSprite
{
    [SerializeField] ComboRank rank;
    [SerializeField] Sprite sprite;

    public bool CheckCondition(ComboRank rank) { return this.rank == rank; }

    public Sprite Sprite { get { return sprite; } }
}

[System.Serializable]
public class ScoreRankTextMaterialPreset
{
    [SerializeField] string text;
    [SerializeField] ScoreRank scoreRank;
    [SerializeField] Material fontMaterial;
    [SerializeField] TMP_ColorGradient colorGradient;

    public ScoreRank ScoreRank { get { return scoreRank; } }

    public void ApplyPreset(TextMeshPro tmp, bool isApplyText = false)
    {
        if (tmp == null) { return; }

        tmp.fontMaterial = fontMaterial;
        tmp.colorGradientPreset = colorGradient;
        if (isApplyText) { tmp.text = this.text; }
    }

    public void ApplyPreset(TextMeshProUGUI tmp, bool isApplyText = false)
    {
        if (tmp == null) { return; }

        tmp.fontMaterial = fontMaterial;
        tmp.colorGradientPreset = colorGradient;
        if (isApplyText) { tmp.text = this.text; }
    }
}

[System.Serializable]
public class DifficultyToColor
{
    [SerializeField] Difficulty difficulty;
    [SerializeField] Color color;

    public bool CheckCondition(Difficulty difficulty) { return this.difficulty == difficulty; }

    public Color Color { get { return color; } }
}

[System.Serializable]
public class DifficultyToSprite
{
    [SerializeField] Difficulty difficulty;
    [SerializeField] Sprite sprite;

    public bool CheckCondition(Difficulty difficulty) { return this.difficulty == difficulty; }

    public Sprite Sprite { get { return sprite; } }
}

[System.Serializable]
public class DifficultyToTMPColorGradient
{
    [SerializeField] Difficulty difficulty;
    [SerializeField] TMPro.TMP_ColorGradient gradientAsset;

    public bool CheckCondition(Difficulty difficulty) { return this.difficulty == difficulty; }

    public TMPro.TMP_ColorGradient GradientAsset { get { return gradientAsset; } }

    public void SetGradient(TMPro.TMP_Text tmp)
    {
        tmp.colorGradientPreset = gradientAsset;
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

