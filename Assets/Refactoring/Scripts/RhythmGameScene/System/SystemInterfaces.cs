using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace Refactoring
{
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
        public void Generate(ChartData chartData);
    }
}