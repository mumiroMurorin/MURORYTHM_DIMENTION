using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System.Linq;

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
    public IReadOnlyReactiveProperty<ScoreRank> CurrentScoreRank { get { return scoreCalculater.Rank; } }

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

    readonly List<RankToThreshold> rankThreshold = new List<RankToThreshold>
    {
        new RankToThreshold(ScoreRank.MAX,1000000),
        new RankToThreshold(ScoreRank.SSPlus,995000),
        new RankToThreshold(ScoreRank.SS,990000),
        new RankToThreshold(ScoreRank.SPlus,985000),
        new RankToThreshold(ScoreRank.S,980000),
        new RankToThreshold(ScoreRank.APlus,970000),
        new RankToThreshold(ScoreRank.A,950000),
        new RankToThreshold(ScoreRank.B,900000),
        new RankToThreshold(ScoreRank.C,750000),
        new RankToThreshold(ScoreRank.D,500000),
    };

    float addScoreOnPerfect;

    public ScoreCalculater(int maxCombo)
    {
        addScoreOnPerfect = (float)MAX_SCORE / maxCombo;
        rankThreshold = rankThreshold.OrderByDescending(x => x.Threshold).ToList();
    }

    ReactiveProperty<float> score = new ReactiveProperty<float>(0);
    public IReadOnlyReactiveProperty<float> Score => score;

    ReactiveProperty<ScoreRank> rank = new ReactiveProperty<ScoreRank>(ScoreRank.E);
    public IReadOnlyReactiveProperty<ScoreRank> Rank => rank;

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
        for(int i = 0; i < rankThreshold.Count; i++)
        {
            if (rankThreshold[i].Threshold > score) { continue; }
            if (rank.Value != rankThreshold[i].Rank) { rank.Value = rankThreshold[i].Rank; Debug.Log($"RankUp => {rank.Value}"); }
            break;
        }
    }

    class RankToThreshold
    {
        public RankToThreshold(ScoreRank rank, int threshold)
        {
            Rank = rank;
            Threshold = threshold;
        }

        public ScoreRank Rank { get; set; }
        public int Threshold { get; set; }
    }
}
