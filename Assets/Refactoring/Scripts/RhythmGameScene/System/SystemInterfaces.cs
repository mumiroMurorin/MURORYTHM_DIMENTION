using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Refactoring
{
    /// <summary>
    /// ������L�^����
    /// </summary>
    public interface IJudgementRecorder
    {
        void RecordJudgement(Judgement judgement);
    }

    /// <summary>
    /// �f�[�^����ɕ��ʂ̐������s��
    /// </summary>
    public interface IChartGenerator
    {
        public void Generate(ChartData chartData);
    }
}