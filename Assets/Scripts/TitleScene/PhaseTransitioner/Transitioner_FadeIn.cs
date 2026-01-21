using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;

namespace TransitionerInTitleScene
{
    public class Transitioner_FadeIn : IPhaseTransitionerInTitleScene
    {
        [SerializeField] SerializeInterface<IPhaseTransitionableInTitleScene> phaseTransitionable;
        [SerializeField] FadeController fadeController;

        readonly PhaseStatusInTitleScene status = PhaseStatusInTitleScene.FadeIn;

        bool IPhaseTransitionerInTitleScene.ConditionChecker(PhaseStatusInTitleScene status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInTitleScene.Transition()
        {
            Debug.Log("【Transition】Transition to \"FadeIn\"");

            // アニメーションの再生
            fadeController?.FadeIn(TransitionNextPhase);
        }

        /// <summary>
        /// 次のフェーズへの移動
        /// </summary>
        private void TransitionNextPhase()
        {
            phaseTransitionable.Value.TransitionPhase(PhaseStatusInTitleScene.WaitingForPlayer);
        }
    }

}
