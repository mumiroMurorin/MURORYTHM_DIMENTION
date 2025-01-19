using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Refactoring
{
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