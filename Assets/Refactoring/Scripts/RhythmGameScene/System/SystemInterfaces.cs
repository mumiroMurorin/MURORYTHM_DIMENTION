using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using UniRx;

namespace Refactoring
{
    /// <summary>
    /// 譜面データの読み込みを行う
    /// </summary>
    public interface IChartLoader
    {
        public void LoadChart(Action callback = null);
    }

    /// <summary>
    /// 譜面データに変換
    /// </summary>
    public interface IChartDataConverter
    {
        /// <summary>
        /// stringを譜面データに変換
        /// </summary>
        /// <param name="datas"></param>
        /// <returns></returns>
        public UniTask<ChartData> ParseChartDataAsync(List<string[]> datas);
    }

    /// <summary>
    /// 判定を記録する
    /// </summary>
    public interface IJudgementRecorder
    {
        void RecordJudgement(NoteJudgementData judgementData);
    }

    /// <summary>
    /// スコアのセット
    /// </summary>
    public interface IScoreSetter
    {
        /// <summary>
        /// スコアのリセット
        /// </summary>
        void ResetScore();
    }

    /// <summary>
    /// スコアの取得
    /// </summary>
    public interface IScoreGetter
    {
        public IReadOnlyReactiveProperty<int> PerfectNum { get;  }

        public IReadOnlyReactiveProperty<int> GreatNum { get; }

        public IReadOnlyReactiveProperty<int> GoodNum { get; }

        public IReadOnlyReactiveProperty<int> MissNum { get; }

        //public IReadOnlyReactiveProperty<int> JudgedNum { get; }

        public IReadOnlyReactiveCollection<NoteJudgementData> NoteJudgementDatas { get; }

        public IReadOnlyReactiveProperty<int> Combo { get; }

        public IReadOnlyReactiveProperty<float> Score { get; }

        public IReadOnlyReactiveProperty<ComboRank> CurrentComboRank { get; }

        public IReadOnlyReactiveProperty<ScoreRank> CurrentScoreRank { get; }
    }

    /// <summary>
    /// データを基に譜面の生成を行う
    /// </summary>
    public interface IChartGenerator
    {
        public void Generate(Action callback = null);
    }

    /// <summary>
    /// 譜面(グラウンド)のコントロールを行う
    /// </summary>
    public interface IGroundController
    {
        /// <summary>
        /// 譜面の移動開始
        /// </summary>
        void StartGroundMove();
    }

    /// <summary>
    /// フェーズ遷移を行うことが出来る
    /// </summary>
    public interface IPhaseTransitionable
    {
        public void TransitionPhase(PhaseStatusInRhythmGame phase);
    }

    /// <summary>
    /// フェーズ遷移の際の処理を行う
    /// </summary>
    public interface IPhaseTransitioner
    {
        public void Transition();

        /// <summary>
        /// 遷移条件のチェック
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        public bool ConditionChecker(PhaseStatusInRhythmGame status);
    }

    /// <summary>
    /// 楽曲データのセットが行える
    /// </summary>
    public interface IMusicDataSetter
    {
        void SetMusicData(MusicData musicData);

        void SetDifficulty(DifficulityName difficulty);
    }

    /// <summary>
    /// 楽曲データの取得が出来る
    /// </summary>
    public interface IMusicDataGetter
    {
        IReadOnlyReactiveProperty<MusicData> Music{ get; }

        IReadOnlyReactiveProperty<DifficulityName> Difficulty { get; }
    }

    /// <summary>
    /// 譜面データのセットができる
    /// </summary>
    public interface IChartDataSetter
    {
        void SetChartData(ChartData data);
    }

    /// <summary>
    /// 譜面データの取得ができる
    /// </summary>
    public interface IChartDataGetter
    {
        ChartData Chart { get; }
    }

    /// <summary>
    /// 評価アニメの再生を行える
    /// </summary>
    public interface IAssessmentPlayer
    {
        public void PlayAnimation(Action callback = null);

        /// <summary>
        /// アニメーション条件のチェック
        /// </summary>
        /// <param name="rank"></param>
        /// <returns></returns>
        public bool ConditionChecker(ComboRank rank);
    }

    /// <summary>
    /// 評価アニメのコントロールを行う
    /// </summary>
    public interface IAssessmentController
    {
        void PlayAnimation(Action callback = null);
    }

    /// <summary>
    /// タイムラインの再生を行う
    /// </summary>
    public interface ITimelinePlayer
    {
        public void PlayAnimation(Action callback = null);
    }

    /// <summary>
    /// 譜面の終了処理を購読する
    /// </summary>
    public interface IChartEnder
    {
        void BindOnEndChart(Action callback = null);
    }

    /// <summary>
    /// 体のデータ取得を行う
    /// </summary>
    public interface IBodyLoader
    {
        void WaitForLoadBody(Action callback);
    }
}