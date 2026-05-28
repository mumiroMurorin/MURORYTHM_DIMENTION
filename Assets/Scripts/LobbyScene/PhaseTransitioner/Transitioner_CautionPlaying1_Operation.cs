using UnityEngine;

namespace TransitionerInLobbyScene
{
    public class Transitioner_CautionPlaying1_Operation : IPhaseTransitionerInLobbyScene
    {
        readonly PhaseStatusInLobbyScene status = PhaseStatusInLobbyScene.CautionPlaying1_Operation;

        bool IPhaseTransitionerInLobbyScene.ConditionChecker(PhaseStatusInLobbyScene status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInLobbyScene.Transition()
        {
            Debug.Log("[Transition] Transition to CautionPlaying1_Operation");
        }
    }
}
