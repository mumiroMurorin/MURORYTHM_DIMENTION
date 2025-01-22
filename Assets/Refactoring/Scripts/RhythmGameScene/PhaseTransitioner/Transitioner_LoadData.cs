using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace Refactoring
{
    public class Transitioner_LoadData : IPhaseTransitioner
    {
        [SerializeField] SerializeInterface<IChartLoader> chartLoader;
        [SerializeField] SerializeInterface<IPhaseTransitionable> phaseTransitionable;
        
        readonly PhaseStatusInRhythmGame status = PhaseStatusInRhythmGame.LoadData;

        bool IPhaseTransitioner.ConditionChecker(PhaseStatusInRhythmGame status)
        {
            return this.status == status;
        }

        void IPhaseTransitioner.Transition()
        {
            Debug.Log("【Transition】Transition to \"LoadData\"");

            chartLoader?.Value.LoadChart(TransitionNextPhase);
        }

        /// <summary>
        /// 次のフェーズへの移動
        /// </summary>
        private void TransitionNextPhase()
        {
            phaseTransitionable?.Value.TransitionPhase(PhaseStatusInRhythmGame.LoadChart);
        }
    }

}
