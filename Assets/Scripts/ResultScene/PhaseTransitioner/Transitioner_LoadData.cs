using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace Refactoring.TransitionerInResultScene
{
    public class Transitioner_LoadData : IPhaseTransitionerInResultScene
    {
        [SerializeField] SerializeInterface<IPhaseTransitionableInResultScene> phaseTransitionable;
        
        readonly PhaseStatusInResultScene status = PhaseStatusInResultScene.LoadData;

        bool IPhaseTransitionerInResultScene.ConditionChecker(PhaseStatusInResultScene status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInResultScene.Transition()
        {
            Debug.Log("【Transition】Transition to \"LoadData\"");

            TransitionNextPhase();
        }

        /// <summary>
        /// 次のフェーズへの移動
        /// </summary>
        private void TransitionNextPhase()
        {
            phaseTransitionable?.Value.TransitionPhase(PhaseStatusInResultScene.FadeIn);
        }
    }

}
