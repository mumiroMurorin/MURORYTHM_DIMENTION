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

    // Score計算機
    ScoreCalculater scoreCalculater = new ScoreCalculater(1);
    public void SetScoreCalculater(ScoreCalculater scoreCalculater)
    {
        this.scoreCalculater = scoreCalculater;

        score.Value = scoreCalculater.Score;
        scoreRank.Value = scoreCalculater.Rank;
    }

    // Score
    ReactiveProperty<float> score = new ReactiveProperty<float>();
    public IReadOnlyReactiveProperty<float> Score { get { return score; } }

    // ComboRank
    ReactiveProperty<ComboRank> comboRank = new ReactiveProperty<ComboRank>(ComboRank.AllPerfect);
    public IReadOnlyReactiveProperty<ComboRank> CurrentComboRank { get { return comboRank; } }

    // ScoreRank
    ReactiveProperty<ScoreRank> scoreRank = new ReactiveProperty<ScoreRank>();
    public IReadOnlyReactiveProperty<ScoreRank> CurrentScoreRank { get { return scoreRank; } }

    public string GetCurrentScoreRankString()
    {
        switch (CurrentScoreRank.Value)
        {
            case ScoreRank.APlus:
                return "A+";
            case ScoreRank.SPlus:
                return "S+";
            case ScoreRank.SSPlus:
                return "SS+";
            default:
                return CurrentScoreRank.Value.ToString();
        }
    }

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

        // スコアの更新
        scoreCalculater?.AddJudgement(judgementData.Judgement);
        score.Value = scoreCalculater.Score;
        scoreRank.Value = scoreCalculater.Rank;

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
            //case Judgement.Great:
            //    comboRank.Value = (ComboRank)Mathf.Min((int)comboRank.Value, (int)ComboRank.GreatCombo);
            //    break;
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
    const int BASE_SCORE = 1000000;

    readonly List<RankToThreshold> rankThreshold = new List<RankToThreshold>
    {
        new RankToThreshold(ScoreRank.SSS,1000000),
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

    int maxScore;
    double addScoreOnPerfect;
    double addScoreOnGreat;
    double addScoreOnGood;
    double addScoreOnMiss;

    public ScoreCalculater(int maxCombo)
    {
        // 理論値の設定
        maxScore = BASE_SCORE + maxCombo;
        
        // 追加スコアの設定
        addScoreOnPerfect = (double)maxScore / maxCombo;
        addScoreOnGreat = addScoreOnPerfect - 10f;
        addScoreOnGood = addScoreOnPerfect * 0.5f;
        addScoreOnMiss = 0;

        // スコアランクの閾値を更新
        rankThreshold.Add(new RankToThreshold(ScoreRank.MAX, maxScore));
        rankThreshold = rankThreshold.OrderByDescending(x => x.Threshold).ToList();
    }

    double scoreOrigin = 0;

    float score = 0;
    public float Score { get { return score; } }

    ScoreRank rank = ScoreRank.E;
    public ScoreRank Rank { get { return rank; } } 

    public void AddJudgement(Judgement judgement)
    {
        switch (judgement)
        {
            case Judgement.Perfect:
                scoreOrigin += addScoreOnPerfect;
                break;
            case Judgement.Great:
                scoreOrigin += addScoreOnGreat;
                break;
            case Judgement.Good:
                scoreOrigin += addScoreOnGood;
                break;
            case Judgement.Miss:
                scoreOrigin += addScoreOnMiss;
                break;
        }

        score = (float)scoreOrigin;

        UpdateScoreRank((int)score);
    }

    private void UpdateScoreRank(int score)
    {
        for(int i = 0; i < rankThreshold.Count; i++)
        {
            if (rankThreshold[i].Threshold > score) { continue; }
            if (rank != rankThreshold[i].Rank) { rank = rankThreshold[i].Rank; Debug.Log($"RankUp => {rank}"); }
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
