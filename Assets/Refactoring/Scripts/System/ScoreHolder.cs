using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace Refactoring
{
    public class ScoreHolder : IJudgementRecorder, IScoreGetter, IScoreSetter
    {
        // ����֌W
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

        // Miss
        ReactiveProperty<int> judgedNum = new ReactiveProperty<int>(0);
        public IReadOnlyReactiveProperty<int> JudgedNum { get { return judgedNum; } }

        // Combo
        ReactiveProperty<int> combo = new ReactiveProperty<int>(0);
        public IReadOnlyReactiveProperty<int> Combo { get { return combo; } }

        // Score
        ReactiveProperty<float> score = new ReactiveProperty<float>(0);
        public IReadOnlyReactiveProperty<float> Score { get { return score; } }

        // ComboRank
        ReactiveProperty<ComboRank> comboRank = new ReactiveProperty<ComboRank>(ComboRank.AllPerfect);
        public IReadOnlyReactiveProperty<ComboRank> CurrentComboRank { get { return comboRank; } }

        // ScoreRank
        ReactiveProperty<ScoreRank> scoreRank = new ReactiveProperty<ScoreRank>(ScoreRank.E);
        public IReadOnlyReactiveProperty<ScoreRank> CurrentScoreRank { get { return scoreRank; } }

        /// <summary>
        /// ����̃��Z�b�g
        /// </summary>
        public void ResetScore()
        {
            perfectNum.Value = 0;
            greatNum.Value = 0;
            goodNum.Value = 0;
            missNum.Value = 0;
            judgedNum.Value = 0;
            combo.Value = 0;
            score.Value = 0;
            comboRank.Value = ComboRank.AllPerfect;
            scoreRank.Value = ScoreRank.E;
        }

        /// <summary>
        /// ����̋L�^
        /// </summary>
        /// <param name="judgement"></param>
        void IJudgementRecorder.RecordJudgement(Judgement judgement)
        {
            // �f�o�b�O�p
            Debug.Log($"�yJudgement�z���� {judgement}");

            SetComboRank(judgement);

            switch (judgement)
            {
                case Judgement.Perfect:
                    perfectNum.Value++;
                    combo.Value++;
                    judgedNum.Value++;
                    break;
                case Judgement.Great:
                    greatNum.Value++;
                    combo.Value++;
                    judgedNum.Value++;
                    break;
                case Judgement.Good:
                    goodNum.Value++;
                    judgedNum.Value++;
                    combo.Value++;
                    break;
                case Judgement.Miss:
                    missNum.Value++;
                    combo.Value = 0;
                    judgedNum.Value++;
                    break;
            }
        }

        /// <summary>
        /// �R���{�����N�̃Z�b�g
        /// </summary>
        /// <param name="judge"></param>
        private void SetComboRank(Judgement judgement)
        {
            switch (judgement)
            {
                // Great����̂Ƃ��AAllPerfect�łȂ���
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

}
