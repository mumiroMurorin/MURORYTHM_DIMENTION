using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;

namespace TransitionerInLobbyScene
{
    public class Transitioner_ConfirmTutorial_Operation : IPhaseTransitionerInLobbyScene
    {
        [SerializeField] SerializeInterface<IPhaseTransitionableInLobbyScene> phaseTransitionable;

        readonly PhaseStatusInLobbyScene status = PhaseStatusInLobbyScene.ConfirmTutorial_Operation;

        bool IPhaseTransitionerInLobbyScene.ConditionChecker(PhaseStatusInLobbyScene status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInLobbyScene.Transition()
        {
            Debug.Log("【Transition】Transition to \"ConfirmTutorial_Operation\"");

        }

        /// <summary>
        /// 次のフェーズへの移動
        /// </summary>
        private void TransitionNextPhase()
        {
            phaseTransitionable.Value.TransitionPhase(PhaseStatusInLobbyScene.FadeOut);
        }
    }

}
