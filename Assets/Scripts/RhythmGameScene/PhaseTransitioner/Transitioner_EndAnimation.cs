using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;

namespace TransitionerInRhythmGameScene
{
    public class Transitioner_EndAnimation : IPhaseTransitionerInRhythmGameScene
    {
        [SerializeField] SerializeInterface<IPhaseTransitionableInRhythmGameScene> phaseTransitionable;
        [SerializeField] SerializeInterface<IAssessmentController> controller;

        readonly PhaseStatusInRhythmGame status = PhaseStatusInRhythmGame.EndAnimation;

        bool IPhaseTransitionerInRhythmGameScene.ConditionChecker(PhaseStatusInRhythmGame status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInRhythmGameScene.Transition()
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
            phaseTransitionable.Value.TransitionPhase(PhaseStatusInRhythmGame.FadeOut);
        }
    }

}
