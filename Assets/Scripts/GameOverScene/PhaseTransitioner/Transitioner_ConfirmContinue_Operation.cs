using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;

namespace TransitionerInGameOverScene
{
    public class Transitioner_ConfirmContinue_Operation : IPhaseTransitionerInGameOverScene
    {
        [SerializeField] SerializeInterface<IPhaseTransitionableInGameOverScene> phaseTransitionable;

        readonly PhaseStatusInGameOverScene status = PhaseStatusInGameOverScene.ConfirmContinue_Operation;

        bool IPhaseTransitionerInGameOverScene.ConditionChecker(PhaseStatusInGameOverScene status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInGameOverScene.Transition()
        {
            Debug.Log("【Transition】Transition to \"ConfirmContinue_Operation\"");

        }

        /// <summary>
        /// 次のフェーズへの移動
        /// </summary>
        private void TransitionNextPhase()
        {
            phaseTransitionable.Value.TransitionPhase(PhaseStatusInGameOverScene.FadeOut);
        }
    }

}
