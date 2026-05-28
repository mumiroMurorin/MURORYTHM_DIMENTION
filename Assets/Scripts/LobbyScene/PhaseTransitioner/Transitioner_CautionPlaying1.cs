using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace TransitionerInLobbyScene
{
    public class Transitioner_CautionPlaying1 : IPhaseTransitionerInLobbyScene
    {
        [SerializeField] SerializeInterface<IPhaseTransitionableInLobbyScene> phaseTransitionable;
        [SerializeField] float delay = 0.5f;
        [SerializeField] TextBoxController previousTopicTextBox;
        [SerializeField] TextBoxController cautionPlaying1TextBox;

        readonly PhaseStatusInLobbyScene status = PhaseStatusInLobbyScene.CautionPlaying1;
        CancellationTokenSource cts;

        bool IPhaseTransitionerInLobbyScene.ConditionChecker(PhaseStatusInLobbyScene status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInLobbyScene.Transition()
        {
            Debug.Log("[Transition] Transition to CautionPlaying1");

            previousTopicTextBox?.Close(OpenCautionPlaying1Window);
        }

        private void OpenCautionPlaying1Window()
        {
            cts?.CancelAndDispose();
            cts = DelayUtility.Run(delay, () =>
            {
                cautionPlaying1TextBox?.Open(TransitionNextPhase);
            });
            phaseTransitionable?.Value?.RegisterCts(cts);
        }

        private void TransitionNextPhase()
        {
            phaseTransitionable.Value.TransitionPhase(PhaseStatusInLobbyScene.CautionPlaying1_Operation);
        }
    }
}
