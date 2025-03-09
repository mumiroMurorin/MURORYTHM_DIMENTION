using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace Refactoring.TransitionerInRhythmGameScene
{
    public class Transitioner_LoadData : IPhaseTransitionerInRhythmGameScene
    {
        [SerializeField] SerializeInterface<IChartLoader> chartLoader;
        [SerializeField] SerializeInterface<IPhaseTransitionableInRhythmGameScene> phaseTransitionable;
        
        readonly PhaseStatusInRhythmGame status = PhaseStatusInRhythmGame.LoadData;

        bool IPhaseTransitionerInRhythmGameScene.ConditionChecker(PhaseStatusInRhythmGame status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInRhythmGameScene.Transition()
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
