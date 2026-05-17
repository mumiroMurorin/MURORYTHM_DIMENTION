using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;

namespace TransitionerInLobbyScene
{
    public class Transitioner_ConfirmTutorial : IPhaseTransitionerInLobbyScene
    {
        [SerializeField] SerializeInterface<IPhaseTransitionableInLobbyScene> phaseTransitionable;
        [SerializeField] float delay = 0.5f;
        [SerializeField] TextBoxController previousTopicTextBox;
        [SerializeField] TextBoxController confirmTutorialTextBox;

        readonly PhaseStatusInLobbyScene status = PhaseStatusInLobbyScene.ConfirmTutorial;
        CancellationTokenSource cts;

        bool IPhaseTransitionerInLobbyScene.ConditionChecker(PhaseStatusInLobbyScene status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInLobbyScene.Transition()
        {
            Debug.Log("【Transition】Transition to \"ConfirmTutorial\"");

            previousTopicTextBox?.Close(OpenConfirmTutorialWindow);
        }

        private void OpenConfirmTutorialWindow()
        {
            cts?.CancelAndDispose();
            cts = DelayUtility.Run(delay, () =>
            {
                confirmTutorialTextBox?.Open(TransitionNextPhase);
            });
            phaseTransitionable?.Value?.RegisterCts(cts);
        }

        /// <summary>
        /// 次のフェーズへの移動
        /// </summary>
        private void TransitionNextPhase()
        {
            phaseTransitionable.Value.TransitionPhase(PhaseStatusInLobbyScene.ConfirmTutorial_Operation);
        }
    }

}
