using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;

namespace TransitionerInGameOverScene
{
    public class Transitioner_ConfirmContinue : IPhaseTransitionerInGameOverScene
    {
        [SerializeField] SerializeInterface<IPhaseTransitionableInGameOverScene> phaseTransitionable;
        [SerializeField] TextBoxController textBoxController;

        readonly PhaseStatusInGameOverScene status = PhaseStatusInGameOverScene.ConfirmContinue;

        bool IPhaseTransitionerInGameOverScene.ConditionChecker(PhaseStatusInGameOverScene status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInGameOverScene.Transition()
        {
            Debug.Log("【Transition】Transition to \"ConfirmContinue\"");

            textBoxController?.Open(TransitionNextPhase);
        }

        /// <summary>
        /// 次のフェーズへの移動
        /// </summary>
        private void TransitionNextPhase()
        {
            phaseTransitionable.Value.TransitionPhase(PhaseStatusInGameOverScene.ConfirmContinue_Operation);
        }
    }

}
