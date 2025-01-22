using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using UniRx;

namespace Refactoring
{
    /// <summary>
    /// ���ʃf�[�^�̓ǂݍ��݂��s��
    /// </summary>
    public interface IChartLoader
    {
        public void LoadChart(Action callback = null);
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
        public void Generate(Action callback = null);
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

    /// <summary>
    /// �t�F�[�Y�J�ڂ��s�����Ƃ��o����
    /// </summary>
    public interface IPhaseTransitionable
    {
        public void TransitionPhase(PhaseStatusInRhythmGame phase);
    }

    /// <summary>
    /// �t�F�[�Y�J�ڂ̍ۂ̏������s��
    /// </summary>
    public interface IPhaseTransitioner
    {
        public void Transition();

        /// <summary>
        /// �J�ڏ����̃`�F�b�N
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        public bool ConditionChecker(PhaseStatusInRhythmGame status);
    }

    /// <summary>
    /// �y�ȃf�[�^�̃Z�b�g���s����
    /// </summary>
    public interface IMusicDataSetter
    {
        void SetMusicData(MusicData musicData);

        void SetDifficulty(DifficulityName difficulty);
    }

    /// <summary>
    /// �y�ȃf�[�^�̎擾���o����
    /// </summary>
    public interface IMusicDataGetter
    {
        IReadOnlyReactiveProperty<MusicData> Music{ get; }

        IReadOnlyReactiveProperty<DifficulityName> Difficulty { get; }
    }

    /// <summary>
    /// ���ʃf�[�^�̃Z�b�g���ł���
    /// </summary>
    public interface IChartDataSetter
    {
        void SetChartData(ChartData data);
    }

    /// <summary>
    /// ���ʃf�[�^�̎擾���ł���
    /// </summary>
    public interface IChartDataGetter
    {
        ChartData Chart { get; }
    }
}