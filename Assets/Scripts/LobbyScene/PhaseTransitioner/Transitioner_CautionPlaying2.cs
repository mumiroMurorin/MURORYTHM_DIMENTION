using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace TransitionerInLobbyScene
{
    public class Transitioner_CautionPlaying2 : IPhaseTransitionerInLobbyScene
    {
        [SerializeField] SerializeInterface<IPhaseTransitionableInLobbyScene> phaseTransitionable;
        [SerializeField] float delay = 0.5f;
        [SerializeField] TextBoxController previousTopicTextBox;
        [SerializeField] TextBoxController cautionPlaying2TextBox;

        readonly PhaseStatusInLobbyScene status = PhaseStatusInLobbyScene.CautionPlaying2;
        CancellationTokenSource cts;

        bool IPhaseTransitionerInLobbyScene.ConditionChecker(PhaseStatusInLobbyScene status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInLobbyScene.Transition()
        {
            Debug.Log("[Transition] Transition to CautionPlaying2");

            previousTopicTextBox?.Close(OpenCautionPlaying2Window);
        }

        private void OpenCautionPlaying2Window()
        {
            cts?.CancelAndDispose();
            cts = DelayUtility.Run(delay, () =>
            {
                cautionPlaying2TextBox?.Open(TransitionNextPhase);
            });
            phaseTransitionable?.Value?.RegisterCts(cts);
        }

        private void TransitionNextPhase()
        {
            phaseTransitionable.Value.TransitionPhase(PhaseStatusInLobbyScene.CautionPlaying2_Operation);
        }
    }
}
