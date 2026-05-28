using UnityEngine;

namespace TransitionerInLobbyScene
{
    public class Transitioner_CautionPlaying2_Operation : IPhaseTransitionerInLobbyScene
    {
        readonly PhaseStatusInLobbyScene status = PhaseStatusInLobbyScene.CautionPlaying2_Operation;

        bool IPhaseTransitionerInLobbyScene.ConditionChecker(PhaseStatusInLobbyScene status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInLobbyScene.Transition()
        {
            Debug.Log("[Transition] Transition to CautionPlaying2_Operation");
        }
    }
}
