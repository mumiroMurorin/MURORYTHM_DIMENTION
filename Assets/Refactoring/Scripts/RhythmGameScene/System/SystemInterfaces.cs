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
        void RecordJudgement(Judgement judgement);
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
}