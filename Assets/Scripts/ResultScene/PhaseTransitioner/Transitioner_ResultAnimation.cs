using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace TransitionerInResultScene
{
    public class Transitioner_ResultAnimation : IPhaseTransitionerInResultScene
    {
        [SerializeField] SerializeInterface<IPhaseTransitionableInResultScene> phaseTransitionable;
        [SerializeField] SerializeInterface<ITimelinePlayer> resultAnimation;


        readonly PhaseStatusInResultScene status = PhaseStatusInResultScene.ResultAnimation;

        bool IPhaseTransitionerInResultScene.ConditionChecker(PhaseStatusInResultScene status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInResultScene.Transition()
        {
            Debug.Log("【Transition】Transition to \"Result\"");

            if (resultAnimation == null || resultAnimation.Value == null) { TransitionNextPhase(); }
            else { resultAnimation?.Value?.PlayAnimation(TransitionNextPhase); }
        }

        /// <summary>
        /// 次のフェーズへの移動
        /// </summary>
        private void TransitionNextPhase()
        {
            phaseTransitionable.Value.TransitionPhase(PhaseStatusInResultScene.Result);
        }

    }
}
