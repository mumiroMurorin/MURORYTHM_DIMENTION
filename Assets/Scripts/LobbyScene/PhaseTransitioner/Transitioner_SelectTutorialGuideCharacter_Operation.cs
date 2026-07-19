using UnityEngine;

namespace TransitionerInLobbyScene
{
    public class Transitioner_SelectTutorialGuideCharacter_Operation : IPhaseTransitionerInLobbyScene
    {
        readonly PhaseStatusInLobbyScene status = PhaseStatusInLobbyScene.SelectTutorialGuideCharacter_Operation;

        bool IPhaseTransitionerInLobbyScene.ConditionChecker(PhaseStatusInLobbyScene status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInLobbyScene.Transition()
        {
            Debug.Log("【Transition】Transition to \"SelectTutorialGuideCharacter_Operation\"");
        }
    }
}
