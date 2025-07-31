using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

public class ScoreHolder : IJudgementRecorder, IScoreGetter, IScoreSetter
{
    // 判定関係
    // Perfect
    ReactiveProperty<int> perfectNum = new ReactiveProperty<int>(0);
    public IReadOnlyReactiveProperty<int> PerfectNum { get { return perfectNum; } }

    // Great
    ReactiveProperty<int> greatNum = new ReactiveProperty<int>(0);
    public IReadOnlyReactiveProperty<int> GreatNum { get { return greatNum; } }

    // Good
    ReactiveProperty<int> goodNum = new ReactiveProperty<int>(0);
    public IReadOnlyReactiveProperty<int> GoodNum { get { return goodNum; } }

    // Miss
    ReactiveProperty<int> missNum = new ReactiveProperty<int>(0);
    public IReadOnlyReactiveProperty<int> MissNum { get { return missNum; } }

    ReactiveCollection<NoteJudgementData> noteJudgementDatas = new ReactiveCollection<NoteJudgementData>();
    public IReadOnlyReactiveCollection<NoteJudgementData> NoteJudgementDatas { get { return noteJudgementDatas; } }

    // Combo
    ReactiveProperty<int> combo = new ReactiveProperty<int>(0);
    public IReadOnlyReactiveProperty<int> Combo { get { return combo; } }

    // Score
    ScoreCalculater scoreCalculater = new ScoreCalculater(1);
    public IReadOnlyReactiveProperty<float> Score { get { return scoreCalculater.Score; } }
    public void SetScoreCalculater(ScoreCalculater scoreCalculater)
    {
        this.scoreCalculater = scoreCalculater;
    }

    // ComboRank
    ReactiveProperty<ComboRank> comboRank = new ReactiveProperty<ComboRank>(ComboRank.AllPerfect);
    public IReadOnlyReactiveProperty<ComboRank> CurrentComboRank { get { return comboRank; } }

    // ScoreRank
    ReactiveProperty<ScoreRank> scoreRank = new ReactiveProperty<ScoreRank>(ScoreRank.E);
    public IReadOnlyReactiveProperty<ScoreRank> CurrentScoreRank { get { return scoreRank; } }

    /// <summary>
    /// 判定のリセット
    /// </summary>
    public void ResetScore()
    {
        perfectNum.Value = 0;
        greatNum.Value = 0;
        goodNum.Value = 0;
        missNum.Value = 0;
        noteJudgementDatas.Clear();
        combo.Value = 0;
        scoreCalculater = null;
        comboRank.Value = ComboRank.AllPerfect;
        scoreRank.Value = ScoreRank.E;
    }

    /// <summary>
    /// 判定の記録
    /// </summary>
    /// <param name="judgement"></param>
    void IJudgementRecorder.RecordJudgement(NoteJudgementData judgementData)
    {
        // デバッグ用
        //Debug.Log($"【Judgement】判定 {judgementData.NoteData.NoteType}: {judgementData.Judgement}");

        SetComboRank(judgementData.Judgement);
        noteJudgementDatas.Add(judgementData);
        scoreCalculater?.AddJudgement(judgementData.Judgement);

        switch (judgementData.Judgement)
        {
            case Judgement.Perfect:
                perfectNum.Value++;
                combo.Value++;
                break;
            case Judgement.Great:
                greatNum.Value++;
                combo.Value++;
                break;
            case Judgement.Good:
                goodNum.Value++;
                combo.Value++;
                break;
            case Judgement.Miss:
                missNum.Value++;
                combo.Value = 0;
                break;
        }
    }

    /// <summary>
    /// コンボランクのセット
    /// </summary>
    /// <param name="judge"></param>
    private void SetComboRank(Judgement judgement)
    {
        switch (judgement)
        {
            // Great判定のとき、AllPerfectでなくす
            case Judgement.Great:
                comboRank.Value = (ComboRank)Mathf.Min((int)comboRank.Value, (int)ComboRank.GreatCombo);
                break;
            case Judgement.Good:
                comboRank.Value = (ComboRank)Mathf.Min((int)comboRank.Value, (int)ComboRank.FullCombo);
                break;
            case Judgement.Miss:
                comboRank.Value = (ComboRank)Mathf.Min((int)comboRank.Value, (int)ComboRank.TrackComplete);
                break;
        }
    }
}

public class ScoreCalculater
{
    const int MAX_SCORE = 1000000;
    const float GREAT_RATIO = 0.9f;
    const float GOOD_RATIO = 0.5f;
    const float MISS_RATIO = 0f;

    const int MAX_THRESHOLD = 1000000;
    const int SSPlus_THRESHOLD = 995000;
    const int SS_THRESHOLD = 990000;
    const int SPlus_THRESHOLD = 985000;
    const int S_THRESHOLD = 980000;
    const int APlus_THRESHOLD = 970000;
    const int A_THRESHOLD = 950000;
    const int B_THRESHOLD = 900000;
    const int C_THRESHOLD = 750000;
    const int D_THRESHOLD = 500000;

    float addScoreOnPerfect;

    public ScoreCalculater(int maxCombo)
    {
        addScoreOnPerfect = (float)MAX_SCORE / maxCombo;
    }

    ReactiveProperty<float> score = new ReactiveProperty<float>(0);
    public IReadOnlyReactiveProperty<float> Score => score;

    public ReactiveProperty<ScoreRank> Rank { get; } = new ReactiveProperty<ScoreRank>(ScoreRank.E);

    public void AddJudgement(Judgement judgement)
    {
        switch (judgement)
        {
            case Judgement.Perfect:
                score.Value += addScoreOnPerfect;
                break;
            case Judgement.Great:
                score.Value += addScoreOnPerfect * GREAT_RATIO;
                break;
            case Judgement.Good:
                score.Value += addScoreOnPerfect * GOOD_RATIO;
                break;
            case Judgement.Miss:
                score.Value += addScoreOnPerfect * MISS_RATIO;
                break;
        }

        UpdateScoreRank((int)score.Value);
    }

    private void UpdateScoreRank(int score)
    {
        //if(score )
    }

}
