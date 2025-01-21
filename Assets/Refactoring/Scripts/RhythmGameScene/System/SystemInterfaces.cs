using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace Refactoring
{
    /// <summary>
    /// ���ʃf�[�^�̓ǂݍ��݂��s��
    /// </summary>
    public interface IChartLoader
    {
        public UniTask<ChartData> LoadChartData(TextAsset textAsset);
    }

    /// <summary>
    /// ���ʃf�[�^�ɕϊ�
    /// </summary>
    public interface IChartDataConverter
    {
        /// <summary>
        /// string�𕈖ʃf�[�^�ɕϊ�
        /// </summary>
        /// <param name="datas"></param>
        /// <returns></returns>
        public UniTask<ChartData> ParseChartDataAsync(List<string[]> datas);
    }

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

    /// <summary>
    /// ����(�O���E���h)�̃R���g���[�����s��
    /// </summary>
    public interface IGroundController
    {
        /// <summary>
        /// ���ʂ̈ړ��J�n
        /// </summary>
        void StartGroundMove();
    }
}