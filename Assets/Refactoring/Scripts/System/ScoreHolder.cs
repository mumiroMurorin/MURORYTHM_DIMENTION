using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace Refactoring
{
    public class ScoreHolder : IJudgementRecorder
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

        // Combo
        ReactiveProperty<int> combo = new ReactiveProperty<int>(0);
        public IReadOnlyReactiveProperty<int> Combo { get { return combo; } }

        /// <summary>
        /// 判定のリセット
        /// </summary>
        public void ResetJudgement()
        {
            perfectNum.Value = 0;
            greatNum.Value = 0;
            goodNum.Value = 0;
            missNum.Value = 0;
            combo.Value = 0;
        }

        /// <summary>
        /// 判定の記録
        /// </summary>
        /// <param name="judgement"></param>
        void IJudgementRecorder.RecordJudgement(Judgement judgement)
        {
            // デバッグ用
            Debug.Log($"【Judgement】判定 {judgement}");

            switch (judgement)
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
    }

}
