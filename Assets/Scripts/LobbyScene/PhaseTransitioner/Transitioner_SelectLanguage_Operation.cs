using UnityEngine;

namespace TransitionerInLobbyScene
{
    public class Transitioner_SelectLanguage_Operation : IPhaseTransitionerInLobbyScene
    {
        readonly PhaseStatusInLobbyScene status = PhaseStatusInLobbyScene.SelectLanguage_Operation;

        bool IPhaseTransitionerInLobbyScene.ConditionChecker(PhaseStatusInLobbyScene status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInLobbyScene.Transition()
        {
            Debug.Log("ÅyTransitionÅzTransition to \"SelectLanguage_Operation\"");

        }
    }
}
