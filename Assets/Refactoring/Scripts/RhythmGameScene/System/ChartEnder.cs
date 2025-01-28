using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using VContainer;
using System;

namespace Refactoring
{
    public class ChartEnder : MonoBehaviour, IChartEnder
    {
        [Header("フェーズ遷移管理")]
        [SerializeField] SerializeInterface<IPhaseTransitionable> phaseTransitionable; 

        IScoreGetter scoreGetter;
        IChartDataGetter chartDataGetter;

        [Inject]
        public void Constructor(IScoreGetter scoreGetter, IChartDataGetter chartDataGetter)
        {
            this.scoreGetter = scoreGetter;
            this.chartDataGetter = chartDataGetter;
        }

        void IChartEnder.BindOnEndChart(Action callback)
        {
            // 譜面終了処理を購読
            scoreGetter.JudgedNum
                .Where(num => num == chartDataGetter.Chart.MaxCombo)
                .Subscribe(_ => {
                    callback?.Invoke();
                    OnEndChart();
                })
                .AddTo(this.gameObject);
        }

        private void OnEndChart()
        {
            phaseTransitionable.Value.TransitionPhase(PhaseStatusInRhythmGame.EndAnimation);
        }
    }

}
