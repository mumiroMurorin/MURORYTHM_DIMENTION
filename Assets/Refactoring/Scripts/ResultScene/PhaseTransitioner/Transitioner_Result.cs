using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace Refactoring.TransitionerInResultScene
{
    public class Transitioner_Result : IPhaseTransitionerInResultScene
    {
        [SerializeField] SerializeInterface<IPhaseTransitionableInResultScene> phaseTransitionable;
        [SerializeField] SerializeInterface<IInputHandler> inputHandler;

        readonly PhaseStatusInResultScene status = PhaseStatusInResultScene.Result;

        bool IPhaseTransitionerInResultScene.ConditionChecker(PhaseStatusInResultScene status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInResultScene.Transition()
        {
            Debug.Log("【Transition】Transition to \"Result\"");

            inputHandler?.Value.OnTouchSlider(new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 }, TransitionNextPhase);
        }

        /// <summary>
        /// 次のフェーズへの移動
        /// </summary>
        private void TransitionNextPhase()
        {
            phaseTransitionable?.Value.TransitionPhase(PhaseStatusInResultScene.FadeOut);
        }
    }
}
