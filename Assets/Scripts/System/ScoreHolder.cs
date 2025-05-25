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
    ScoreCalculater scoreCalculater;
    public IReadOnlyReactiveProperty<float> Score { get { return scoreCalculater?.Score; } }
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
        // Debug.Log($"【Judgement】判定 {judgementData.Judgement}");

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

    float addScoreOnPerfect;

    public ScoreCalculater(int maxCombo)
    {
        addScoreOnPerfect = MAX_SCORE / maxCombo;
    }

    public ReactiveProperty<float> Score { get; } = new ReactiveProperty<float>(0);

    public void AddJudgement(Judgement judgement)
    {
        switch (judgement)
        {
            case Judgement.Perfect:
                Score.Value += addScoreOnPerfect;
                break;
            case Judgement.Great:
                Score.Value += addScoreOnPerfect * GREAT_RATIO;
                break;
            case Judgement.Good:
                Score.Value += addScoreOnPerfect * GOOD_RATIO;
                break;
            case Judgement.Miss:
                Score.Value += addScoreOnPerfect * MISS_RATIO;
                break;
        }
    }

}
