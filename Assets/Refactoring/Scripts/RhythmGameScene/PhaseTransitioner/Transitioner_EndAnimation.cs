using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;

namespace Refactoring
{
    public class Transitioner_EndAnimation : IPhaseTransitioner
    {
        [SerializeField] SerializeInterface<IPhaseTransitionable> phaseTransitionable;
        [SerializeField] SerializeInterface<IAssessmentController> controller;

        readonly PhaseStatusInRhythmGame status = PhaseStatusInRhythmGame.EndAnimation;

        bool IPhaseTransitioner.ConditionChecker(PhaseStatusInRhythmGame status)
        {
            return this.status == status;
        }

        void IPhaseTransitioner.Transition()
        {
            Debug.Log("【Transition】Transition to \"EndAnimation\"");

            // 評価アニメーションの再生
            controller.Value.PlayAnimation(TransitionNextPhase);
        }

        /// <summary>
        /// 次のフェーズへの移動
        /// </summary>
        private void TransitionNextPhase()
        {
            phaseTransitionable.Value.TransitionPhase(PhaseStatusInRhythmGame.TransitionResultScene);
        }
    }

}
